/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Diagnostic helper
/// </summary>
public partial class DiagnosticEngine(PveClient client, Settings settings)
{
    private readonly List<DiagnosticResult> _result = [];
    private readonly DateTime _now = DateTime.Now;


    /// <summary>
    /// Analyze cluster by querying PVE API directly
    /// </summary>
    public async Task<ICollection<DiagnosticResult>> AnalyzeAsync(List<DiagnosticResult> ignoredIssues)
    {
        var allResources = await client.Cluster.Resources.GetAsync();
        var resources = allResources.Where(a => !a.IsUnknown).ToList();

        // Resources with unknown type are always a problem — report them all as Critical
        _result.AddRange(allResources.Where(a => a.IsUnknown)
                                     .Select(a => new DiagnosticResult
                                     {
                                         Id = a.GetWebUrl(),
                                         ErrorCode = "CU0001",
                                         Description = $"Unknown resource {a.Type}",
                                         Context = DiagnosticResult.DecodeContext(a.Type),
                                         SubContext = "Status",
                                         Gravity = DiagnosticResultGravity.Critical,
                                     }));

        var (hasCluster, clusterBackups) = await CheckClusterAsync(resources);

        // Pre-fetch backup storages once per node — shared by CheckQemuAsync and CheckLxcAsync
        var backupStoragesByNode = new Dictionary<string, IEnumerable<NodeStorage>>();
        foreach (var node in resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                      .Select(a => a.Node))
        {
            backupStoragesByNode[node] = settings.Backup.Enabled
                                            ? await client.Nodes[node].Storage.GetAsync(enabled: true, content: "backup")
                                            : [];
        }

        // Pre-fetch VM configs once — shared by CheckQemuAsync, CheckLxcAsync, CheckStorageAsync
        var vmConfigs = new Dictionary<long, VmConfig>();
        foreach (var vm in resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
        {
            var nodeApi = client.Nodes[vm.Node];
            vmConfigs[vm.VmId] = vm.VmType == VmType.Qemu
                                    ? await nodeApi.Qemu[vm.VmId].Config.GetAsync()
                                    : await nodeApi.Lxc[vm.VmId].Config.GetAsync();
        }

        await CheckStorageAsync(resources, vmConfigs);
        await CheckNodesAsync(resources, hasCluster);
        await CheckQemuAsync(resources, clusterBackups, hasCluster, backupStoragesByNode, vmConfigs);
        await CheckLxcAsync(resources, clusterBackups, backupStoragesByNode, vmConfigs);

        foreach (var ignoredIssue in ignoredIssues)
        {
            foreach (var item in _result.Where(a => ignoredIssue.CheckIgnoreIssue(a)))
            {
                item.IsIgnoredIssue = true;
            }
        }

        return _result;
    }
}
