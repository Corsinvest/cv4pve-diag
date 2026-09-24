/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Globalization;
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

    // Backup checks on/off for this run: starts from the settings and is turned off by the permission
    // pre-check. A field, not settings.Backup.Enabled, so the caller's Settings object is never changed.
    private bool _backupChecksEnabled = settings.Backup.Enabled;

    // Orphaned image/backup checks (WS0002, WS0003) on/off: turned off by the permission pre-check
    // when the account cannot see every guest.
    private bool _orphanChecksEnabled = true;

    // False when /cluster/backup could not be read: the job list is unknown, not empty.
    private bool _clusterBackupsKnown;

    // False when /cluster/replication could not be read: _replicatedVmIds is then unknown, not empty.
    private bool _replicationKnown;

    // Ids of all guests in /cluster/resources, including those whose config could not be read.
    private HashSet<long> _existingGuestIds = [];
    private Dictionary<long, VmConfig> _vmConfigs = [];
    private Dictionary<string, IEnumerable<NodeStorage>> _backupStoragesByNode = [];

    // HA-resource ids (vmid) and guest-ids targeted by enabled replication jobs.
    // Populated by CheckClusterHaAndReplicationAsync and consumed by per-guest checks
    // (VmsWithoutHaResource, VmsWithoutReplication) — saves re-fetching for every guest.
    private readonly HashSet<long> _haVmIds = [];
    private readonly HashSet<long> _replicatedVmIds = [];

    // One entry per unique storage: shared storages appear once (deduped by name),
    // non-shared appear once per node. Used everywhere instead of filtering _resources.
    private List<ClusterResource> _storageResources = [];

    // Backup content keyed by storage name — loaded once in CheckStorageAsync, reused in CheckCommonAsync.
    // Shared storages are fetched only once regardless of how many nodes mount them.
    private readonly Dictionary<string, List<NodeStorageContent>> _backupContentByStorage = [];

    // Backup storages (same key as _backupContentByStorage) whose content could not be read.
    // The per-guest backup checks are skipped for guests relying on them: no data is not "no backup".
    private readonly HashSet<string> _backupContentUnavailable = new(StringComparer.OrdinalIgnoreCase);

    // Storage names that are shared — used in CheckCommonAsync to build the correct lookup key.
    private readonly HashSet<string> _sharedStorageNames = new(StringComparer.OrdinalIgnoreCase);

    // CPU model of each node ("Intel(R) Xeon ...", "AMD EPYC ..."), from the node status read by
    // CheckNodesAsync; CheckVmAsync picks the Spectre/Meltdown flags (WG0037) by vendor.
    private readonly Dictionary<string, string> _cpuModelByNode = new(StringComparer.OrdinalIgnoreCase);

    // QEMU machine types available on each node, keyed by node name. Populated by FetchNodeDataAsync,
    // consumed by CheckVmAsync (IG0016 outdated machine type). Empty list = node had no data or fetch failed.
    private readonly Dictionary<string, IReadOnlyList<NodeCapabilitiesQemuMachine>> _qemuMachinesByNode = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Analyze cluster by querying PVE API directly
    /// </summary>
    public async Task<ICollection<DiagnosticResult>> AnalyzeAsync(List<DiagnosticResult> ignoredIssues)
    {
        var originalTimeout = client.Timeout;
        if (settings.ApiTimeout > 0) { client.Timeout = TimeSpan.FromSeconds(settings.ApiTimeout); }

        // Numbers in the descriptions ("80.9%", "2.61 TB") must not depend on the machine's regional
        // settings: ignore rules match descriptions with a regex, and reports are compared across
        // runs. The culture flows with the async context, so every check below uses it and the
        // caller's culture is restored on return.
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            // First: PVE filters /cluster/resources by what the caller may audit, so a missing
            // privilege removes guests or storages from the analysis with no error to notice.
            // Report that before anything reads the (possibly incomplete) resource list.
            await CheckPermissionsAsync();

            IReadOnlyList<ClusterResource> allResources;
            try
            {
                allResources = (await client.Cluster.Resources.GetAsync()).ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The foundational call failed — there is nothing to analyze. Report it as
                // critical and return cleanly instead of letting the exception crash the run.
                _result.Add(new DiagnosticResult
                {
                    Id = "cluster",
                    ErrorCode = "CU0001",
                    Description = $"Unable to read cluster resources: {(ex is PveResultException pex ? DiagnosticSafeExtensions.BuildApiErrorMessage(pex.Result) : ex.Message)}",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "ApiError",
                    Gravity = DiagnosticResultGravity.Critical,
                });
                return _result;
            }

            _resources = [.. allResources.Where(a => !a.IsUnknown)];

            // Resources with unknown type are always a problem — report them all as Critical.
            // Per-item KO carries the originating context (the type-specific DecodeContext).
            foreach (var a in allResources.Where(a => a.IsUnknown))
            {
                CreateResult(
                    isOk: false,
                    id: a.GetWebUrl(),
                    errorCode: "CU0001",
                    subContext: "Status",
                    context: DiagnosticResult.DecodeContext(a.Type),
                    gravityKo: DiagnosticResultGravity.Critical,
                    descriptionKo: $"Unknown resource {a.Type}",
                    descriptionOk: "",
                    compliance: []);
            }

            // Deduplicated storage list: shared → one record per storage name, non-shared → one per node.
            // Ordered by node first: /cluster/resources lists nodes in a different order depending on
            // which node answers, and the kept record gives the finding its id (ignore rules match on it).
            _storageResources = [.. _resources.Where(a => a.ResourceType == ClusterResourceType.Storage)
                                      .OrderBy(a => a.Node, StringComparer.Ordinal)
                                      .GroupBy(a => a.Shared ? a.Storage : $"{a.Node}/{a.Storage}")
                                      .Select(g => g.First())];

            // Detect PVE major version from first online node — used to pick the correct Debian release for CVE filtering
            var firstOnlineNode = _resources.FirstOrDefault(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
            var pveMajorVersion = 8; // default to bookworm
            if (firstOnlineNode != null)
            {
                var ver = await client.Nodes[firstOnlineNode.Node].Version.GetAsync()
                                .ToSafeSingle(_result, firstOnlineNode.GetWebUrl(), DiagnosticResultContext.Node,
                                              $"PVE version on node '{firstOnlineNode.Node}'");
                // TryParse writes 0 on failure: keep the default instead.
                if (int.TryParse(ver?.Version?.Split('.')[0], out var major)) { pveMajorVersion = major; }
            }

            await FetchCveDataAsync(pveMajorVersion);

            var hasCluster = await CheckClusterAsync(pveMajorVersion);

            // Pre-fetch backup storages once per node — shared by CheckQemuAsync and CheckLxcAsync
            var backupStorageResults = await RunParallelAsync(_resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                                                        .Select(a => a.Node),
                                                              async node => new
                                                              {
                                                                  node,
                                                                  storages = _backupChecksEnabled
                                                                                ? await client.Nodes[node].Storage.GetAsync(content: "backup", enabled: true).ToSafeEnum(_result, $"nodes/{node}", DiagnosticResultContext.Node, $"backup storages on node '{node}'")
                                                                                : []
                                                              });
            _backupStoragesByNode = backupStorageResults.ToDictionary(a => a.node, a => (IEnumerable<NodeStorage>)a.storages);

            // Pre-fetch VM configs once — shared by CheckQemuAsync, CheckLxcAsync, CheckStorageAsync.
            // A guest whose config cannot be read is skipped (excluded from the dictionary) so the
            // checks that index _vmConfigs[VmId] never hit a null — the failure is recorded instead.
            var vmConfigResults = await RunParallelAsync(_resources.Where(a => a.ResourceType == ClusterResourceType.Vm),
                                                         async vm => new
                                                         {
                                                             vm.VmId,
                                                             config = await client.GetVmConfigAsync(vm.Node, vm.VmType, vm.VmId)
                                                                                  .ToSafeSingle(_result,
                                                                                                vm.GetWebUrl(),
                                                                                                DiagnosticResult.DecodeContext(vm.Type),
                                                                                                $"configuration of {vm.Type} {vm.VmId}")
                                                         });

            _vmConfigs = vmConfigResults.Where(r => r.config != null)
                                        .ToDictionary(r => r.VmId, r => r.config!);

            // Every guest that exists, before the ones with an unreadable config are dropped below:
            // the orphan checks must still count them as owners of their disks and backups.
            _existingGuestIds = [.. _resources.Where(a => a.ResourceType == ClusterResourceType.Vm).Select(a => a.VmId)];

            // Guests whose config failed to load are not present in _vmConfigs — skip them in the
            // per-guest checks below instead of indexing a missing key.
            _resources = [.. _resources.Where(a => a.ResourceType != ClusterResourceType.Vm
                                                   || _vmConfigs.ContainsKey(a.VmId))];

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
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private async Task<TResult[]> RunParallelAsync<T, TResult>(IEnumerable<T> source, Func<T, Task<TResult>> func)
    {
        // 0 or a negative value in the settings file would hang or throw: run at least one at a time.
        var semaphore = new SemaphoreSlim(Math.Max(1, settings.MaxParallelRequests));
        return await Task.WhenAll(source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { return await func(item); }
            finally { semaphore.Release(); }
        }));
    }
}
