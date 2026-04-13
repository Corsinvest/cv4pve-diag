/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Net.Http.Headers;
using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    // PVE major version → Debian release name
    private static readonly Dictionary<int, string> _pveToDebian = new()
    {
        { 9, "trixie"   },
        { 8, "bookworm" },
        { 7, "bullseye" },
        { 6, "buster"   },
    };

    private static readonly string[] _cvssMetricKeys = ["cvssMetricV31", "cvssMetricV30", "cvssMetricV2"];

    // Debian Tracker: package → CVE → (status, urgency) — filtered to the detected release at parse time
    private record DebianCveEntry(string Status, string Urgency);

    // NVD CVE entry — only what we need
    private record NvdCveEntry(string Id,
                               string Description,
                               double? CvssScore,
                               string? Severity,
                               string? VersionStart,
                               string? VersionEnd);

    private Dictionary<string, Dictionary<string, DebianCveEntry>>? _debianCveData;
    private List<NvdCveEntry>? _nvdCveData;

    private async Task FetchCveDataAsync(int pveMajorVersion)
    {
        var debianRelease = _pveToDebian.GetValueOrDefault(pveMajorVersion, "bookworm");

        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("cv4pve-diag/1.0");

        var tasks = new List<Task>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

        if (settings.Cve.DebianTrackerEnabled)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await using var stream = await httpClient.GetStreamAsync("https://security-tracker.debian.org/tracker/data/json", cts.Token);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
                    var result = new Dictionary<string, Dictionary<string, DebianCveEntry>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var pkg in doc.RootElement.EnumerateObject())
                    {
                        var cveMap = new Dictionary<string, DebianCveEntry>(StringComparer.OrdinalIgnoreCase);
                        foreach (var cve in pkg.Value.EnumerateObject())
                        {
                            if (!cve.Value.TryGetProperty("releases", out var releases)) { continue; }

                            // Filter at parse time — keep only the detected Debian release
                            if (!releases.TryGetProperty(debianRelease, out var rel)) { continue; }

                            var status = rel.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                            var urgency = rel.TryGetProperty("urgency", out var u) ? u.GetString() ?? "" : "";

                            if (status != "open") { continue; }
                            if (urgency is "unimportant" or "end-of-life") { continue; }

                            cveMap[cve.Name] = new DebianCveEntry(status, urgency);
                        }
                        if (cveMap.Count > 0) { result[pkg.Name] = cveMap; }
                    }
                    _debianCveData = result;
                }
                catch { _debianCveData = []; }
            }));
        }

        if (settings.Cve.NvdEnabled)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await using var stream = await httpClient.GetStreamAsync("https://services.nvd.nist.gov/rest/json/cves/2.0?keywordSearch=proxmox&resultsPerPage=100", cts.Token);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
                    var result = new List<NvdCveEntry>();
                    if (!doc.RootElement.TryGetProperty("vulnerabilities", out var vulns)) { return; }

                    foreach (var vuln in vulns.EnumerateArray())
                    {
                        if (!vuln.TryGetProperty("cve", out var cve)) { continue; }
                        var id = cve.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

                        // Description (English only)
                        var desc = "";
                        if (cve.TryGetProperty("descriptions", out var descs))
                        {
                            foreach (var d in descs.EnumerateArray())
                            {
                                if (d.TryGetProperty("lang", out var lang) && lang.GetString() == "en")
                                {
                                    desc = d.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
                                    break;
                                }
                            }
                        }

                        // CVSS score — prefer v31, fallback v30, fallback v2
                        double? score = null;
                        string? severity = null;
                        if (cve.TryGetProperty("metrics", out var metrics))
                        {
                            foreach (var metricKey in _cvssMetricKeys)
                            {
                                if (!metrics.TryGetProperty(metricKey, out var metricArr)) { continue; }
                                foreach (var m in metricArr.EnumerateArray())
                                {
                                    if (!m.TryGetProperty("cvssData", out var cvssData)) { continue; }
                                    if (cvssData.TryGetProperty("baseScore", out var bs)) { score = bs.GetDouble(); }
                                    if (m.TryGetProperty("baseSeverity", out var sev)) { severity = sev.GetString(); }
                                    else if (cvssData.TryGetProperty("baseSeverity", out var sev2)) { severity = sev2.GetString(); }
                                    break;
                                }
                                if (score.HasValue) { break; }
                            }
                        }

                        // Filter by MinCvssScore at parse time
                        if (score < settings.Cve.MinCvssScore) { continue; }

                        // Version range from CPE — first proxmox:virtual_environment entry
                        string? versionStart = null, versionEnd = null;
                        if (cve.TryGetProperty("configurations", out var configs))
                        {
                            foreach (var config in configs.EnumerateArray())
                            {
                                if (!config.TryGetProperty("nodes", out var nodes)) { continue; }
                                foreach (var node in nodes.EnumerateArray())
                                {
                                    if (!node.TryGetProperty("cpeMatch", out var cpeMatches)) { continue; }
                                    foreach (var cpe in cpeMatches.EnumerateArray())
                                    {
                                        if (!cpe.TryGetProperty("criteria", out var criteria)) { continue; }
                                        if (!criteria.GetString()!.Contains("proxmox:virtual_environment", StringComparison.OrdinalIgnoreCase)) { continue; }
                                        if (cpe.TryGetProperty("versionStartIncluding", out var vs)) { versionStart = vs.GetString(); }
                                        if (cpe.TryGetProperty("versionEndExcluding", out var ve)) { versionEnd = ve.GetString(); }
                                        else if (cpe.TryGetProperty("versionEndIncluding", out var vi)) { versionEnd = vi.GetString(); }
                                        break;
                                    }
                                    if (versionStart != null || versionEnd != null) { break; }
                                }
                                if (versionStart != null || versionEnd != null) { break; }
                            }
                        }

                        // Skip CVE with no CPE for proxmox:virtual_environment (Salt, Jenkins, Foreman, Terraform, etc.)
                        if (versionStart == null && versionEnd == null) { continue; }

                        // Skip CVE with no upper version bound — cannot determine if current version is affected
                        if (string.IsNullOrWhiteSpace(versionEnd)) { continue; }

                        result.Add(new NvdCveEntry(id, desc, score, severity, versionStart, versionEnd));
                    }
                    _nvdCveData = result;
                }
                catch { _nvdCveData = []; }
            }));
        }

        await Task.WhenAll(tasks);
    }

    // Compare two version strings numerically segment by segment (e.g. "8.1.4" vs "7.2-3").
    // Non-numeric segments are compared as strings. Returns negative if a < b, 0 if equal, positive if a > b.
    private static int CompareVersions(string a, string b)
    {
        var aParts = a.Split('.', '-');
        var bParts = b.Split('.', '-');
        var len = Math.Max(aParts.Length, bParts.Length);
        for (var i = 0; i < len; i++)
        {
            var aSeg = i < aParts.Length ? aParts[i] : "0";
            var bSeg = i < bParts.Length ? bParts[i] : "0";
            var cmp = int.TryParse(aSeg, out var aNum) && int.TryParse(bSeg, out var bNum)
                        ? aNum.CompareTo(bNum)
                        : string.Compare(aSeg, bSeg, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) { return cmp; }
        }
        return 0;
    }

    private void CheckNodeCve(string id, IEnumerable<NodeAptVersion> aptVersions)
    {
        // Debian Security Tracker — data already filtered to the correct release at fetch time
        if (settings.Cve.DebianTrackerEnabled && _debianCveData != null)
        {
            foreach (var pkg in aptVersions)
            {
                if (string.IsNullOrWhiteSpace(pkg.Package)) { continue; }
                if (!_debianCveData.TryGetValue(pkg.Package, out var cveMap)) { continue; }

                foreach (var (cveId, entry) in cveMap)
                {
                    var gravity = entry.Urgency switch
                    {
                        "high" => DiagnosticResultGravity.Critical,
                        "medium" => DiagnosticResultGravity.Warning,
                        "not yet assigned" => DiagnosticResultGravity.Warning, // open CVE, fix not yet available
                        _ => DiagnosticResultGravity.Info,
                    };

                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = gravity == DiagnosticResultGravity.Critical ? "CN0014" : "WN0041",
                        Description = $"Package '{pkg.Package}' ({pkg.Version}) has open CVE {cveId} (urgency: {entry.Urgency})",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "CVE",
                        Gravity = gravity,
                    });
                }
            }
        }

        // NVD — Proxmox VE specific CVE, already filtered by MinCvssScore at fetch time
        if (settings.Cve.NvdEnabled && _nvdCveData != null)
        {
            var pvePkg = aptVersions.FirstOrDefault(p => p.Package?.Equals("pve-manager", StringComparison.OrdinalIgnoreCase) is true);
            var pveVerStr = pvePkg?.Version ?? "";

            foreach (var cve in _nvdCveData)
            {
                // Skip if installed pve-manager version is beyond the vulnerable range
                if (!string.IsNullOrWhiteSpace(pveVerStr) && !string.IsNullOrWhiteSpace(cve.VersionEnd))
                {
                    if (CompareVersions(pveVerStr, cve.VersionEnd) > 0) { continue; }
                }

                var gravity = cve.CvssScore switch
                {
                    >= 9.0 => DiagnosticResultGravity.Critical,
                    >= 7.0 => DiagnosticResultGravity.Warning,
                    _ => DiagnosticResultGravity.Info,
                };

                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = gravity == DiagnosticResultGravity.Critical ? "CN0015" : "WN0042",
                    Description = $"Proxmox CVE {cve.Id} (CVSS: {cve.CvssScore:F1}, {cve.Severity}): {cve.Description}",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "CVE",
                    Gravity = gravity,
                });
            }
        }
    }
}
