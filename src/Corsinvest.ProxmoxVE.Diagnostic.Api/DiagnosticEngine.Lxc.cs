/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task CheckLxcAsync(List<ClusterResource> resources,
                                     IEnumerable<ClusterBackup> clusterBackups)
    {
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                  && a.VmType == VmType.Lxc
                                                  && !a.IsTemplate))
        {
            var vmApi = client.Nodes[item.Node].Lxc[item.VmId];
            var id = item.GetWebUrl();
            var lxcConfig = await vmApi.Config.GetAsync();

            var rrdData = settings.Lxc.TimeSeries switch
            {
                SettingsTimeSeriesType.Day => await vmApi.Rrddata.GetAsync(RrdDataTimeFrame.Day, RrdDataConsolidation.Average),
                SettingsTimeSeriesType.Week => await vmApi.Rrddata.GetAsync(RrdDataTimeFrame.Week, RrdDataConsolidation.Average),
                _ => throw new NotImplementedException("settings.Lxc.TimeSeries"),
            };

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
                        ErrorCode = "WL0001",
                        Description = "Container has nesting=1 but keyctl=1 is not enabled — kernel keyring isolation may be incomplete",
                        Context = DiagnosticResultContext.Lxc,
                        SubContext = "Features",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
                #endregion

            }

            await CheckCommonVmAsync(settings,
                                     settings.Lxc,
                                     lxcConfig,
                                     await vmApi.Pending.GetAsync(),
                                     await vmApi.Snapshot.GetAsync(),
                                     rrdData,
                                     DiagnosticResultContext.Lxc,
                                     item.Node,
                                     item.VmId,
                                     id,
                                     clusterBackups);
        }
    }
}
