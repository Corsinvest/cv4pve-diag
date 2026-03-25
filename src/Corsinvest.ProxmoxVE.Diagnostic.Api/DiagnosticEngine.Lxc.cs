/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task CheckLxcAsync(List<ClusterResource> resources,
                                     IEnumerable<ClusterBackup> clusterBackups,
                                     Dictionary<string, IEnumerable<NodeStorage>> backupStoragesByNode,
                                     Dictionary<long, VmConfig> vmConfigs)
    {
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                  && a.VmType == VmType.Lxc
                                                  && !a.IsTemplate))
        {
            var vmApi = client.Nodes[item.Node].Lxc[item.VmId];
            var id = item.GetWebUrl();
            var lxcConfig = (VmConfigLxc)vmConfigs[item.VmId];

            #region Firewall and IP filter
            CheckVmFirewall(_result, await vmApi.Firewall.Options.GetAsync(), id, DiagnosticResultContext.Lxc);
            #endregion

            if (lxcConfig is VmConfigLxc lxc)
            {
                #region Nesting without keyctl
                // nesting=1 allows Docker/nested containers inside LXC.
                // keyctl=1 is required alongside nesting for proper isolation of kernel keyrings
                // between nested containers. Without keyctl the inner containers share the host
                // keyring and may leak secrets or fail cryptographic operations.
                if (lxc.HasNesting && !lxc.HasKeyctl)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WL0019",
                        Description = "Container has nesting=1 but keyctl=1 is not enabled — kernel keyring isolation may be incomplete",
                        Context = DiagnosticResultContext.Lxc,
                        SubContext = "Features",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
                #endregion

                #region Privileged container
                // Privileged containers share the host user namespace — root inside = root on host
                if (!lxc.Unprivileged)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WL0020",
                        Description = "Container is privileged (Unprivileged=false) — root inside the container has host-level access",
                        Context = DiagnosticResultContext.Lxc,
                        SubContext = "Security",
                        Gravity = DiagnosticResultGravity.Warning,
                    });

                    // Privileged container with AppArmor explicitly disabled via features=apparmor=0
                    // or via raw lxc.apparmor.profile=unconfined — no kernel confinement at all
                    var appArmorDisabledViaFeatures = (lxc.Features ?? string.Empty)
                        .Split(',')
                        .Any(p => p.Trim().Equals("apparmor=0", StringComparison.OrdinalIgnoreCase));

                    var appArmorDisabledViaRaw = lxcConfig.ExtensionData?.Any(kv =>
                        kv.Key.Equals("lxc.apparmor.profile", StringComparison.OrdinalIgnoreCase)
                        && kv.Value?.ToString()?.Equals("unconfined", StringComparison.OrdinalIgnoreCase) == true) == true;

                    if (appArmorDisabledViaFeatures || appArmorDisabledViaRaw)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "CL0004",
                            Description = "Privileged container has AppArmor disabled — no kernel confinement, root inside has unrestricted host access",
                            Context = DiagnosticResultContext.Lxc,
                            SubContext = "Security",
                            Gravity = DiagnosticResultGravity.Critical,
                        });
                    }
                }
                #endregion

                #region Swap disabled
                // Swap=0 means no swap — under memory pressure the OOM killer will terminate processes
                if (lxc.Swap == 0)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "IL0003",
                        Description = "Container has swap=0 — OOM killer may terminate processes under memory pressure",
                        Context = DiagnosticResultContext.Lxc,
                        SubContext = "Memory",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }
                #endregion

                #region No hostname
                if (string.IsNullOrWhiteSpace(lxc.Hostname))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "IL0004",
                        Description = "Container has no hostname configured — difficult to identify in logs",
                        Context = DiagnosticResultContext.Lxc,
                        SubContext = "Config",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }
                #endregion

                #region Raw LXC config entries
                // lxc.X entries bypass PVE abstractions and may introduce unsafe configurations
                var rawLxcKeys = lxcConfig.ExtensionData?.Keys
                    .Where(k => k.StartsWith("lxc.", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? [];
                if (rawLxcKeys.Count > 0)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WL0021",
                        Description = $"Container has raw LXC config entries ({string.Join(", ", rawLxcKeys)}) — bypasses PVE abstractions",
                        Context = DiagnosticResultContext.Lxc,
                        SubContext = "Config",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
                #endregion
            }

            await CheckCommonVmAsync(settings,
                                     settings.Lxc,
                                     lxcConfig,
                                     await vmApi.Pending.GetAsync(),
                                     settings.Snapshot.Enabled
                                        ? await vmApi.Snapshot.GetAsync()
                                        : [],
                                     await vmApi.Rrddata.GetAsync(settings.Lxc.Rrd.TimeFrame, settings.Lxc.Rrd.Consolidation),
                                     DiagnosticResultContext.Lxc,
                                     item.Node,
                                     item.VmId,
                                     id,
                                     clusterBackups,
                                     backupStoragesByNode.GetValueOrDefault(item.Node, []));
        }
    }
}
