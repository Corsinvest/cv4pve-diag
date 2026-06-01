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
                                      IReadOnlyList<VmSnapshot> Snapshots);

    private async Task<ContainerFetchData> FetchContainerDataAsync(ClusterResource item)
    {
        var vmApi = client.Nodes[item.Node].Lxc[item.VmId];
        var id = item.GetWebUrl();
        var firewallTask = vmApi.Firewall.Options.GetAsync().ToSafeSingle(_result, id, DiagnosticResultContext.Lxc, $"firewall options of CT {item.VmId}");
        var pendingTask = vmApi.Pending.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Lxc, $"pending changes of CT {item.VmId}");
        var snapshotTask = settings.Snapshot.Enabled
                            ? vmApi.Snapshot.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Lxc, $"snapshots of CT {item.VmId}")
                            : Task.FromResult<IReadOnlyList<VmSnapshot>>([]);
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
                    ComplianceControls.Ens.OP_ACC_2,
                    ComplianceControls.Nist80053.AC_6,
                    ComplianceControls.Soc2.CC6_3,
                    ComplianceControls.C5.IDM_09,
                    ComplianceControls.Ens.MP_S_1,
                    ComplianceControls.Nist80053.CP_10,
                    ComplianceControls.Soc2.A1_1,
                    ComplianceControls.C5.PI_02,
                ];

                #region Nesting without keyctl
                // nesting=1 allows Docker/nested containers inside LXC.
                // keyctl=1 is required alongside nesting for proper isolation of kernel keyrings
                // between nested containers. Without keyctl the inner containers share the host
                // keyring and may leak secrets or fail cryptographic operations.
                if (lxc.HasNesting)
                {
                    CreateResult(
                        isOk: lxc.HasKeyctl,
                        id: id,
                        errorCode: "WG0038",
                        subContext: "Features",
                        context: DiagnosticResultContext.Lxc,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: "Container has nesting=1 but keyctl=1 is not enabled — kernel keyring isolation may be incomplete",
                        descriptionOk: "Container has nesting=1 with keyctl=1 — kernel keyring isolation is in place",
                        compliance: containerIsolationControls);
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
                    // Privileged container with AppArmor explicitly disabled via features=apparmor=0
                    // or via raw lxc.apparmor.profile=unconfined — no kernel confinement at all
                    var appArmorDisabledViaFeatures = (lxc.Features ?? "")
                        .Split(',')
                        .Any(p => p.Trim().Equals("apparmor=0", StringComparison.OrdinalIgnoreCase));

                    var appArmorDisabledViaRaw = lxcConfig.ExtensionData?.Any(kv =>
                        kv.Key.Equals("lxc.apparmor.profile", StringComparison.OrdinalIgnoreCase)
                        && kv.Value?.ToString()?.Equals("unconfined", StringComparison.OrdinalIgnoreCase) is true) is true;

                    CreateResult(
                        isOk: !(appArmorDisabledViaFeatures || appArmorDisabledViaRaw),
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
                var rawLxcKeys = lxcConfig.ExtensionData?.Keys
                    .Where(k => k.StartsWith("lxc.", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? [];
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
}
