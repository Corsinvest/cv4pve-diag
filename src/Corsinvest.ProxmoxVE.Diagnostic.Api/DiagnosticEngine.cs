/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
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
    private List<ClusterResource> _resources = [];
    private IEnumerable<ClusterBackup> _clusterBackups = [];
    private Dictionary<long, VmConfig> _vmConfigs = [];
    private Dictionary<string, IEnumerable<NodeStorage>> _backupStoragesByNode = [];

    // One entry per unique storage: shared storages appear once (deduped by name),
    // non-shared appear once per node. Used everywhere instead of filtering _resources.
    private List<ClusterResource> _storageResources = [];

    // Backup content keyed by storage name — loaded once in CheckStorageAsync, reused in CheckCommonAsync.
    // Shared storages are fetched only once regardless of how many nodes mount them.
    private readonly Dictionary<string, List<NodeStorageContent>> _backupContentByStorage = [];

    // Storage names that are shared — used in CheckCommonAsync to build the correct lookup key.
    private readonly HashSet<string> _sharedStorageNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Analyze cluster by querying PVE API directly
    /// </summary>
    public async Task<ICollection<DiagnosticResult>> AnalyzeAsync(List<DiagnosticResult> ignoredIssues)
    {
        var allResources = await client.Cluster.Resources.GetAsync();
        _resources = [.. allResources.Where(a => !a.IsUnknown)];

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

        // Deduplicated storage list: shared → one record per storage name, non-shared → one per node
        _storageResources = [.. _resources.Where(a => a.ResourceType == ClusterResourceType.Storage)
                                      .GroupBy(a => a.Shared ? a.Storage : $"{a.Node}/{a.Storage}")
                                      .Select(g => g.First())];

        var hasCluster = await CheckClusterAsync();

        // Pre-fetch backup storages once per node — shared by CheckQemuAsync and CheckLxcAsync
        _backupStoragesByNode = [];
        foreach (var node in _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                       .Select(a => a.Node))
        {
            _backupStoragesByNode[node] = settings.Backup.Enabled
                                            ? await client.Nodes[node].Storage.GetAsync(content: "backup", enabled: true)
                                            : [];
        }

        // Pre-fetch VM configs once — shared by CheckQemuAsync, CheckLxcAsync, CheckStorageAsync
        _vmConfigs = [];
        foreach (var vm in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
        {
            var nodeApi = client.Nodes[vm.Node];
            _vmConfigs[vm.VmId] = vm.VmType == VmType.Qemu
                                    ? await nodeApi.Qemu[vm.VmId].Config.GetAsync()
                                    : await nodeApi.Lxc[vm.VmId].Config.GetAsync();
        }

        await CheckStorageAsync();
        await CheckNodesAsync(hasCluster);
        await CheckQemuAsync(hasCluster);
        await CheckLxcAsync();

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
