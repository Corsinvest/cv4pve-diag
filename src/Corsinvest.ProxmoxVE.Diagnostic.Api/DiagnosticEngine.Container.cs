/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private record ContainerFetchData(ClusterResource Item,
                                      VmConfigLxc Config,
                                      VmFirewallOptions? Firewall,
                                      IReadOnlyList<KeyValue> Pending,
                                      IReadOnlyList<VmSnapshot>? Snapshots);

    private async Task<ContainerFetchData> FetchContainerDataAsync(ClusterResource item)
    {
        var vmApi = client.Nodes[item.Node].Lxc[item.VmId];
        var id = item.GetWebUrl();
        var firewallTask = vmApi.Firewall.Options.GetAsync().ToSafeSingle(_result, id, DiagnosticResultContext.Lxc, $"firewall options of CT {item.VmId}");
        var pendingTask = vmApi.Pending.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Lxc, $"pending changes of CT {item.VmId}");
        var snapshotTask = settings.Snapshot.Enabled
                            ? vmApi.Snapshot.GetAsync().ToSafeEnumOrNull(_result, id, DiagnosticResultContext.Lxc, $"snapshots of CT {item.VmId}")
                            : Task.FromResult<IReadOnlyList<VmSnapshot>?>(null);
        await Task.WhenAll(firewallTask, pendingTask, snapshotTask);
        return new ContainerFetchData(item, (VmConfigLxc)_vmConfigs[item.VmId],
                                      firewallTask.Result, pendingTask.Result, snapshotTask.Result);
    }

    private async Task CheckContainerAsync()
    {
        var ctItems = _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                           && a.VmType == VmType.Lxc
                                           && !a.IsTemplate).ToList();
        var ctFetchResults = await RunParallelAsync(ctItems, FetchContainerDataAsync);

        foreach (var fetch in ctFetchResults)
        {
            var item = fetch.Item;
            var lxcConfig = fetch.Config;
            var id = item.GetWebUrl();

            #region Firewall and IP filter
            // Firewall is null when its fetch failed — the failure was already recorded, so skip.
            if (fetch.Firewall != null) { CheckVmFirewall(fetch.Firewall, id, DiagnosticResultContext.Lxc); }
            #endregion

            if (lxcConfig is VmConfigLxc lxc)
            {
                ComplianceMapping[] containerIsolationControls =
                [
                    ComplianceControls.Iso27001.A_5_15,
                    ComplianceControls.Iso27001.A_8_2,
                    ComplianceControls.Nis2.Art_21_i,
                    ComplianceControls.PciDss.R_7_2,
                    ComplianceControls.Gdpr.Art_5_1_f,
                    ComplianceControls.AgId.ABSC_5_1,
                    ComplianceControls.Ens.OP_ACC_2,
                    ComplianceControls.C5.IDM_06,
                    ComplianceControls.C5.OPS_23,
                    ComplianceControls.Soc2.CC6_3,
                    ComplianceControls.Soc2.A1_1,
                    ComplianceControls.Nist80053.AC_6,
                    ComplianceControls.Nist80053.CP_10,
                    ComplianceControls.Cis.C_6,
                    ComplianceControls.NistCsf.PR_AA_05,
                    ComplianceControls.NistCsf.ID_AM_02,
                    ComplianceControls.Acn.PR_AA_05,
                    ComplianceControls.Acn.PR_PS_01,
                    ComplianceControls.BsiGrundschutz.ORP_4_A10,
                    ComplianceControls.BsiGrundschutz.SYS_1_6_A17,
                    ComplianceControls.Nis2Ir.C_11_2,
                    ComplianceControls.Nis2Ir.C_11_3,
                    ComplianceControls.Nis2Ir.C_6_3,
                ];

                #region Nesting without keyctl
                // nesting=1 is what Docker and nested containers need. In an unprivileged container
                // they usually also need keyctl=1, which allows the keyctl() system call (PVE docs:
                // "for unprivileged containers only"); without it Docker or systemd services may fail.
                // It is not an isolation measure. Replaces WG0038, which reported it as a security gap.
                if (lxc.HasNesting && lxc.Unprivileged)
                {
                    CreateResult(
                        isOk: lxc.HasKeyctl,
                        id: id,
                        errorCode: "IG0017",
                        subContext: "Features",
                        context: DiagnosticResultContext.Lxc,
                        gravityKo: DiagnosticResultGravity.Info,
                        descriptionKo: "Container has nesting=1 without keyctl=1 — Docker or systemd inside the container may not work",
                        descriptionOk: "Container has nesting=1 with keyctl=1",
                        compliance: []);
                }
                #endregion

                #region Privileged container
                // Privileged containers share the host user namespace — root inside = root on host
                CreateResult(
                    isOk: lxc.Unprivileged,
                    id: id,
                    errorCode: "WG0039",
                    subContext: "Security",
                    context: DiagnosticResultContext.Lxc,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Container is privileged (Unprivileged=false) — root inside the container has host-level access",
                    descriptionOk: "Container is unprivileged",
                    compliance: containerIsolationControls);

                if (!lxc.Unprivileged)
                {
                    // Privileged container with AppArmor disabled via raw lxc.apparmor.profile=unconfined
                    // — no kernel confinement at all. pve-container has no feature flag for AppArmor.
                    var appArmorDisabled = RawLxcEntries(lxcConfig).Any(kv =>
                        kv.Key.Equals("lxc.apparmor.profile", StringComparison.OrdinalIgnoreCase)
                        && kv.Value.Equals("unconfined", StringComparison.OrdinalIgnoreCase));

                    CreateResult(
                        isOk: !appArmorDisabled,
                        id: id,
                        errorCode: "CG0006",
                        subContext: "Security",
                        context: DiagnosticResultContext.Lxc,
                        gravityKo: DiagnosticResultGravity.Critical,
                        descriptionKo: "Privileged container has AppArmor disabled — no kernel confinement, root inside has unrestricted host access",
                        descriptionOk: "Privileged container retains AppArmor confinement",
                        compliance: containerIsolationControls);
                }
                #endregion

                #region No memory limit
                // Memory=0 means unbounded RAM — the container can consume all host memory and starve other VMs/CTs
                CreateResult(
                    isOk: lxc.Memory != 0,
                    id: id,
                    errorCode: "WG0040",
                    subContext: "Memory",
                    context: DiagnosticResultContext.Lxc,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Container has no memory limit (Memory=0) — can consume all host RAM and starve other guests",
                    descriptionOk: $"Container has a memory limit configured ({lxc.Memory} MB)",
                    compliance: []);
                #endregion

                #region Swap disabled
                // Swap=0 means no swap — under memory pressure the OOM killer will terminate processes
                CreateResult(
                    isOk: lxc.Swap != 0,
                    id: id,
                    errorCode: "IG0013",
                    subContext: "Memory",
                    context: DiagnosticResultContext.Lxc,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "Container has swap=0 — OOM killer may terminate processes under memory pressure",
                    descriptionOk: $"Container has swap configured ({lxc.Swap} MB)",
                    compliance: []);
                #endregion

                #region No hostname
                CreateResult(
                    isOk: !string.IsNullOrWhiteSpace(lxc.Hostname),
                    id: id,
                    errorCode: "IG0014",
                    subContext: "Config",
                    context: DiagnosticResultContext.Lxc,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "Container has no hostname configured — difficult to identify in logs",
                    descriptionOk: $"Container hostname is configured ('{lxc.Hostname}')",
                    compliance: []);
                #endregion

                #region Raw LXC config entries
                // lxc.X entries bypass PVE abstractions and may introduce unsafe configurations
                var rawLxcKeys = RawLxcEntries(lxcConfig).Select(kv => kv.Key).Distinct().ToList();
                CreateResult(
                    isOk: rawLxcKeys.Count == 0,
                    id: id,
                    errorCode: "WG0041",
                    subContext: "Config",
                    context: DiagnosticResultContext.Lxc,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"Container has raw LXC config entries ({string.Join(", ", rawLxcKeys)}) — bypasses PVE abstractions",
                    descriptionOk: "Container has no raw lxc.* config entries",
                    compliance: containerIsolationControls);
                #endregion
            }

            await CheckCommonVmAsync(settings,
                                     settings.Lxc,
                                     lxcConfig,
                                     fetch.Pending,
                                     fetch.Snapshots,
                                     await client.Nodes[item.Node].Lxc[item.VmId].Rrddata.GetAsync(settings.Lxc.Rrd.TimeFrame, settings.Lxc.Rrd.Consolidation)
                                                 .ToSafeEnum(_result, id, DiagnosticResultContext.Lxc, $"RRD data for CT {item.VmId}"),
                                     DiagnosticResultContext.Lxc,
                                     item.Node,
                                     item.VmId,
                                     id,
                                     _backupStoragesByNode.GetValueOrDefault(item.Node, []));
        }
    }

    /// <summary>
    /// Raw <c>lxc.*</c> entries of a container config. The API returns them as a list of
    /// [key, value] pairs under <c>lxc</c>, which the SDK exposes as <see cref="VmConfigLxc.Lxc"/>;
    /// top-level <c>lxc.*</c> keys are read too, should any end up in the extension data.
    /// </summary>
    internal static IReadOnlyList<(string Key, string Value)> RawLxcEntries(VmConfigLxc config)
        => [.. (config.Lxc ?? []).Where(p => p is { Length: > 0 } && !string.IsNullOrWhiteSpace(p[0]))
                                 .Select(p => (Key: p[0].Trim(), Value: p.Length > 1 ? (p[1] ?? "").Trim() : ""))
                                 .Concat(config.ExtensionData?.Where(kv => kv.Key.StartsWith("lxc.", StringComparison.OrdinalIgnoreCase))
                                                              .Select(kv => (kv.Key, Value: kv.Value?.ToString()?.Trim() ?? ""))
                                         ?? [])];
}
