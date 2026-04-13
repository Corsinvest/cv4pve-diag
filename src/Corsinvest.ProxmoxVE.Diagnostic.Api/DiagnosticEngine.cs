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
public partial class DiagnosticEngine(PveClient client, Settings settings, HttpClient httpClient)
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
        var originalTimeout = client.Timeout;
        if (settings.ApiTimeout > 0) { client.Timeout = TimeSpan.FromSeconds(settings.ApiTimeout); }

        try
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

            // Detect PVE major version from first online node — used to pick the correct Debian release for CVE filtering
            var firstOnlineNode = _resources.FirstOrDefault(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
            var pveMajorVersion  = 8; // default to bookworm
            if (firstOnlineNode != null)
            {
                var ver = await client.Nodes[firstOnlineNode.Node].Version.GetAsync();
                _ = int.TryParse(ver.Version?.Split('.')[0], out pveMajorVersion);
            }

            await FetchCveDataAsync(pveMajorVersion);

            var hasCluster = await CheckClusterAsync();

            // Pre-fetch backup storages once per node — shared by CheckQemuAsync and CheckLxcAsync
            var backupStorageResults = await RunParallelAsync(_resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                                                        .Select(a => a.Node),
                                                              async node => new
                                                              {
                                                                  node,
                                                                  storages = settings.Backup.Enabled
                                                                                ? await client.Nodes[node].Storage.GetAsync(content: "backup", enabled: true)
                                                                                : []
                                                              });
            _backupStoragesByNode = backupStorageResults.ToDictionary(a => a.node, a => a.storages);

            // Pre-fetch VM configs once — shared by CheckQemuAsync, CheckLxcAsync, CheckStorageAsync
            var vmConfigResults = await RunParallelAsync(_resources.Where(a => a.ResourceType == ClusterResourceType.Vm),
                                                         async vm => new
                                                         {
                                                             vm.VmId,
                                                             config = await client.GetVmConfigAsync(vm.Node, vm.VmType, vm.VmId)
                                                         });
            _vmConfigs = vmConfigResults.ToDictionary(r => r.VmId, r => r.config);

            await CheckStorageAsync();
            await CheckNodesAsync(hasCluster);
            await CheckVmAsync(hasCluster);
            await CheckContainerAsync();

            foreach (var ignoredIssue in ignoredIssues)
            {
                foreach (var item in _result.Where(a => ignoredIssue.CheckIgnoreIssue(a)))
                {
                    item.IsIgnoredIssue = true;
                }
            }

            return _result;
        }
        finally
        {
            client.Timeout = originalTimeout;
        }
    }

    private async Task<TResult[]> RunParallelAsync<T, TResult>(IEnumerable<T> source, Func<T, Task<TResult>> func)
    {
        var semaphore = new SemaphoreSlim(settings.MaxParallelRequests);
        return await Task.WhenAll(source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { return await func(item); }
            finally { semaphore.Release(); }
        }));
    }
}
