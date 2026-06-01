/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private static readonly string[] _cvssMetricKeys = ["cvssMetricV31", "cvssMetricV30", "cvssMetricV2"];

    // NVD CVE entry — only what we need
    private record NvdCveEntry(string Id,
                               string Description,
                               double? CvssScore,
                               string? Severity,
                               string? VersionStart,
                               string? VersionEnd);

    private List<NvdCveEntry>? _nvdCveData;

    private async Task FetchCveDataAsync(int pveMajorVersion)
    {
        // pveMajorVersion is currently unused (the NVD query targets the product, not a release).
        // Kept in the signature in case future CVE sources need it.
        _ = pveMajorVersion;

        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("cv4pve-diag/1.0");

        if (settings.Cve.NvdEnabled) { await FetchNvdCveAsync(); }
    }

    private async Task FetchNvdCveAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        try
        {
            // Query by CPE prefix so the server returns only Proxmox VE entries (no Salt /
            // Jenkins / Foreman / Terraform noise to filter out client-side).
            // Uses virtualMatchString (not cpeName): cpeName requires a concrete version, while
            // virtualMatchString allows matching all versions of the product.
            // resultsPerPage=2000 is the NVD maximum and comfortably covers all historical PVE CVEs.
            await using var stream = await httpClient.GetStreamAsync("https://services.nvd.nist.gov/rest/json/cves/2.0?virtualMatchString=cpe:2.3:a:proxmox:virtual_environment&resultsPerPage=2000", cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
            var result = new List<NvdCveEntry>();
            if (!doc.RootElement.TryGetProperty("vulnerabilities", out var vulns)) { _nvdCveData = result; return; }

            foreach (var vuln in vulns.EnumerateArray())
            {
                if (!vuln.TryGetProperty("cve", out var cve)) { continue; }
                var id = cve.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

                var (score, severity) = ExtractCvss(cve);

                // A CVE with no CVSS score cannot be ranked for severity — skip rather than
                // emit an Info finding with no actionable information.
                if (score is null || score < settings.Cve.MinCvssScore) { continue; }

                var (versionStart, versionEnd) = ExtractProxmoxVersionRange(cve);

                // Without an upper version bound we cannot tell whether the installed version is affected.
                // This also drops CVEs where the CPE only references other products.
                if (string.IsNullOrWhiteSpace(versionEnd)) { continue; }

                var desc = ExtractEnglishDescription(cve);
                // A finding without a description is noise — skip it.
                if (string.IsNullOrWhiteSpace(desc)) { continue; }

                result.Add(new NvdCveEntry(id, desc, score, severity, versionStart, versionEnd));
            }
            _nvdCveData = result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _nvdCveData = [];
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = DiagnosticSafeExtensions.ApiErrorCode,
                Description = $"Unable to fetch NVD CVE data: {ex.Message}",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "ApiError",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
    }

    // English description, or empty if not present.
    private static string ExtractEnglishDescription(JsonElement cve)
    {
        if (!cve.TryGetProperty("descriptions", out var descs)) { return ""; }
        foreach (var d in descs.EnumerateArray())
        {
            if (d.TryGetProperty("lang", out var lang) && lang.GetString() == "en")
            {
                return d.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
            }
        }
        return "";
    }

    // CVSS base score + severity. Preference order v3.1 → v3.0 → v2, mirroring NVD's own ordering.
    private static (double? Score, string? Severity) ExtractCvss(JsonElement cve)
    {
        if (!cve.TryGetProperty("metrics", out var metrics)) { return (null, null); }

        foreach (var metricKey in _cvssMetricKeys)
        {
            if (!metrics.TryGetProperty(metricKey, out var metricArr)) { continue; }
            foreach (var m in metricArr.EnumerateArray())
            {
                if (!m.TryGetProperty("cvssData", out var cvssData)) { continue; }
                double? score = cvssData.TryGetProperty("baseScore", out var bs) ? bs.GetDouble() : null;
                var severity = cvssData.TryGetProperty("baseSeverity", out var sev) ? sev.GetString() : null;
                if (score.HasValue) { return (score, severity); }
            }
        }
        return (null, null);
    }

    // Pulls (versionStartIncluding, versionEndExcluding|versionEndIncluding) from the first
    // CPE entry that references proxmox:virtual_environment. The CPE filter is still needed
    // even with virtualMatchString= on the request: a CVE may include CPEs for other products too,
    // and we must take the range from the right one.
    private static (string? Start, string? End) ExtractProxmoxVersionRange(JsonElement cve)
    {
        if (!cve.TryGetProperty("configurations", out var configs)) { return (null, null); }
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

                    var start = cpe.TryGetProperty("versionStartIncluding", out var vs) ? vs.GetString() : null;
                    string? end = null;
                    if (cpe.TryGetProperty("versionEndExcluding", out var ve)) { end = ve.GetString(); }
                    else if (cpe.TryGetProperty("versionEndIncluding", out var vi)) { end = vi.GetString(); }
                    return (start, end);
                }
            }
        }
        return (null, null);
    }

    private void CheckNodeCve(string id, IEnumerable<NodeAptVersion> aptVersions)
    {
        // NVD — Proxmox VE specific CVEs, already filtered by MinCvssScore at fetch time.
        // Matched against the installed pve-manager version (the canonical PVE version marker).
        if (!settings.Cve.NvdEnabled || _nvdCveData == null) { return; }

        var pvePkg = aptVersions.FirstOrDefault(p => p.Package?.Equals("pve-manager", StringComparison.OrdinalIgnoreCase) is true);
        var pveVerStr = pvePkg?.Version ?? "";

        ComplianceMapping[] cveControls =
        [
            ComplianceControls.Iso27001.A_8_8,
            ComplianceControls.Nis2.Art_21_e,
            ComplianceControls.PciDss.R_6_3,
            ComplianceControls.Gdpr.Art_32_1_b,
            ComplianceControls.AgId.ABSC_2_3,
            ComplianceControls.AgId.ABSC_4_1,
            ComplianceControls.AgId.ABSC_4_4,
            ComplianceControls.Ens.OP_EXP_4,
            ComplianceControls.C5.OPS_18,
            ComplianceControls.Soc2.CC7_1,
            ComplianceControls.Nist80053.SI_2,
            ComplianceControls.Nist80053.SI_5,
            ComplianceControls.Iso27017.CLD_9_5_2,
            ComplianceControls.Cis.C_7,
            ComplianceControls.NistCsf.PR_PS_02,
            ComplianceControls.NistCsf.ID_RA_01,
        ];

        // Applicable CVEs: those whose vulnerable range covers the installed pve-manager version.
        var applicable = _nvdCveData
            .Where(cve => string.IsNullOrWhiteSpace(pveVerStr)
                          || string.IsNullOrWhiteSpace(cve.VersionEnd)
                          || DebianVersion.Compare(pveVerStr, cve.VersionEnd) <= 0)
            .Select(cve => new
            {
                Cve = cve,
                Gravity = cve.CvssScore switch
                {
                    >= 9.0 => DiagnosticResultGravity.Critical,
                    >= 7.0 => DiagnosticResultGravity.Warning,
                    _ => DiagnosticResultGravity.Info,
                },
            })
            .ToList();

        // Critical CVEs → CN0015
        CreateResultPerItem(
            items: applicable.Where(a => a.Gravity == DiagnosticResultGravity.Critical).ToList(),
            isItemOk: _ => false,
            itemId: _ => id,
            itemDescriptionKo: a => $"Proxmox CVE {a.Cve.Id} (CVSS: {a.Cve.CvssScore:F1}, {a.Cve.Severity}): {a.Cve.Description}",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "No critical (CVSS ≥ 9.0) Proxmox VE CVE applies to the installed version",
            errorCode: "CN0015",
            subContext: "CVE",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance: cveControls);

        // Warning / Info CVEs → WN0042 (rendered at Warning gravity; mixed Info ones still surface here)
        CreateResultPerItem(
            items: applicable.Where(a => a.Gravity != DiagnosticResultGravity.Critical).ToList(),
            isItemOk: _ => false,
            itemId: _ => id,
            itemDescriptionKo: a => $"Proxmox CVE {a.Cve.Id} (CVSS: {a.Cve.CvssScore:F1}, {a.Cve.Severity}): {a.Cve.Description}",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "No non-critical Proxmox VE CVE applies to the installed version",
            errorCode: "WN0042",
            subContext: "CVE",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: cveControls);
    }
}
