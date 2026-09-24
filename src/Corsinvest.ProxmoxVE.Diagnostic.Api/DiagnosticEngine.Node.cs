/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    // Warn when a node TLS certificate expires within this many days.
    private const int CertificateExpiringDays = 30;

    private static readonly Dictionary<int, DateTime> _pveEndOfLife = new()
    {
        { 8, new DateTime(2026, 08, 31) },
        { 7, new DateTime(2024, 07, 31) },
        { 6, new DateTime(2022, 09, 30) },
        { 5, new DateTime(2020, 07, 31) },
        { 4, new DateTime(2018, 06, 30) },
    };

    private record NodeCompareData(NodeVersion Version,
                                   string[] Hosts,
                                   NodeDns Dns,
                                   string Timezone,
                                   IEnumerable<NodeAptVersion> AptVersions,
                                   NodeStatus Status,
                                   long UtcTime,
                                   long ClockOffset,
                                   NodeAptRepositories? AptRepositories,
                                   IEnumerable<NodeNetwork> Networks);

    // Node time plus the client's UTC clock (seconds) at the moment the answer arrived.
    private static async Task<(Result Result, long ClientUtc)> ReadTimeAsync(PveClient.PveNodes.PveNodeItem api)
    {
        var result = await api.Time.Time();
        return (result, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private record NodeFetchData(ClusterResource Item,
                                 NodeSubscription? Subscription,
                                 IReadOnlyList<NodeService> Services,
                                 IReadOnlyList<NodeCertificate> Certificates,
                                 IReadOnlyList<NodeReplication> Replication,
                                 IReadOnlyList<NodeAptUpdate> AptUpdate,
                                 IReadOnlyList<NodeHardwarePci> PciDevices,
                                 IReadOnlyList<NodeTask> Tasks,
                                 IReadOnlyList<NodeDiskList> Disks,
                                 IReadOnlyList<NodeDiskZfs> ZfsList,
                                 IReadOnlyList<NodeDiskLvmThin> LvmThinList,
                                 IReadOnlyList<NodeCapabilitiesQemuMachine> QemuMachines);

    private async Task<NodeFetchData> FetchNodeDataAsync(ClusterResource item)
    {
        var api = client.Nodes[item.Node];
        var id = item.GetWebUrl();
        var node = item.Node;

        // Each call is individually safe: a single failing endpoint degrades to an empty
        // list / null and records a finding, without aborting the other node data.
        var subscriptionTask = api.Subscription.GetAsync().ToSafeSingle(_result, id, DiagnosticResultContext.Node, $"subscription on node '{node}'");
        var servicesTask = api.Services.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"services on node '{node}'");
        var certificatesTask = api.Certificates.Info.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"certificates on node '{node}'");
        var replicationTask = api.Replication.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"replication on node '{node}'");
        var aptUpdateTask = api.Apt.Update.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"APT updates on node '{node}'");
        var pciTask = api.Hardware.Pci.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"PCI devices on node '{node}'");
        var tasksTask = api.Tasks.GetAsync(errors: true, limit: 1000).ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"task history on node '{node}'");
        var disksListTask = api.Disks.List.GetAsync(include_partitions: false).ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"disks on node '{node}'");
        var zfsListTask = api.Disks.Zfs.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"ZFS pools on node '{node}'");
        var lvmThinListTask = settings.Node.NodeStorage.LvmThinMetadata
                                    ? api.Disks.Lvmthin.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"LVM-thin metadata on node '{node}'")
                                    : Task.FromResult<IReadOnlyList<NodeDiskLvmThin>>([]);
        // GetAsync is provided by the temporary shim in Helpers/PveSdkShims.cs — remove the
        // shim and this comment once cv4pve-api-dotnet > 9.2.0 ships the strong-typed extension.
        var qemuMachinesTask = api.Capabilities.Qemu.Machines.GetAsync()
                                  .ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"QEMU machines on node '{node}'");
        await Task.WhenAll(subscriptionTask, servicesTask, certificatesTask, replicationTask,
                           aptUpdateTask, pciTask, tasksTask, disksListTask, zfsListTask, lvmThinListTask,
                           qemuMachinesTask);

        return new NodeFetchData(item,
                                 subscriptionTask.Result,
                                 servicesTask.Result,
                                 certificatesTask.Result,
                                 replicationTask.Result,
                                 aptUpdateTask.Result,
                                 pciTask.Result,
                                 tasksTask.Result,
                                 disksListTask.Result,
                                 zfsListTask.Result,
                                 lvmThinListTask.Result,
                                 qemuMachinesTask.Result);
    }

    private async Task CheckNodesAsync(bool hasCluster)
    {
        var onlineNodes = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline).ToList();

        // Pre-fetch lightweight per-node data — nodes in parallel, 8 calls per node in parallel.
        // These are the node's foundational data (version, status, network, …) used together by
        // most node checks. If any of them fails the whole node is skipped (with a finding) rather
        // than carrying half-populated state into every downstream check.
        var nodeCompareResults = await RunParallelAsync(onlineNodes, async item =>
        {
            var id = item.GetWebUrl();
            try
            {
                var api = client.Nodes[item.Node];
                var versionTask = api.Version.GetAsync();
                var hostsTask = api.Hosts.GetEtcHosts();
                var dnsTask = api.Dns.GetAsync();
                var aptVersionsTask = api.Apt.Versions.GetAsync();
                var statusTask = api.Status.GetAsync();
                var aptRepositoriesTask = api.Apt.Repositories.GetAsync();
                var networksTask = api.Network.GetAsync();
                // The client clock is read when the node's time arrives, not when the checks run
                // (after every other node read): the offset must not include that wait.
                var timeTask = ReadTimeAsync(api);
                await Task.WhenAll(versionTask, hostsTask, dnsTask, aptVersionsTask,
                                   statusTask, aptRepositoriesTask, networksTask, timeTask);

                var (timeResult, clientUtcAtRead) = timeTask.Result;
                var timeRaw = timeResult.ToData();
                var nodeUtc = timeRaw.time is long t ? t : 0L;
                return (item.Node, Data: (NodeCompareData?)new NodeCompareData(versionTask.Result,
                                                             ((string)hostsTask.Result.ToData().data).Split('\n'),
                                                             dnsTask.Result,
                                                             timeRaw.timezone as string ?? "",
                                                             aptVersionsTask.Result,
                                                             statusTask.Result,
                                                             nodeUtc,
                                                             nodeUtc > 0 ? nodeUtc - clientUtcAtRead : 0L,
                                                             aptRepositoriesTask.Result,
                                                             networksTask.Result));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = DiagnosticSafeExtensions.ApiErrorCode,
                    Description = $"Unable to read node data for '{item.Node}': {(ex is PveResultException pex ? DiagnosticSafeExtensions.BuildApiErrorMessage(pex.Result) : ex.Message)}",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "ApiError",
                    Gravity = DiagnosticResultGravity.Warning,
                });
                return (item.Node, Data: (NodeCompareData?)null);
            }
        });
        // Nodes whose foundational data failed to load are dropped here (a finding was already
        // recorded). Downstream checks index/iterate this dictionary, so it holds only good nodes.
        var nodeCompareData = nodeCompareResults.Where(r => r.Data != null)
                                                .ToDictionary(r => r.Node, r => r.Data!);
        foreach (var (nodeName, data) in nodeCompareData)
        {
            _cpuModelByNode[nodeName] = data.Status?.CpuInfo?.Model ?? "";
        }

        // Pre-fetch all per-node data in parallel (subscription, services, certs, replication, apt, pci, tasks, disks)
        var nodeFetchResults = await RunParallelAsync(onlineNodes, FetchNodeDataAsync);
        var nodeFetchData = nodeFetchResults.ToDictionary(r => r.Item.Node, r => r);

        // Publish the per-node QEMU machine catalog so CheckVmAsync (IG0016) can consume it
        // without re-fetching. Keyed by node name; nodes whose fetch failed get an empty list.
        foreach (var (nodeName, fetch) in nodeFetchData)
        {
            _qemuMachinesByNode[nodeName] = fetch.QemuMachines;
        }

        #region Cluster-wide version / kernel consistency
        // Compared once across all online nodes (not per node) — mixed versions/kernels after a partial upgrade
        ComplianceMapping[] patchConsistencyControls =
        [
            ComplianceControls.Iso27001.A_8_8,
            ComplianceControls.Nis2.Art_21_e,
            ComplianceControls.PciDss.R_6_3,
            ComplianceControls.Gdpr.Art_32_1_b,
            ComplianceControls.AgId.ABSC_2_3,
            ComplianceControls.AgId.ABSC_4_1,
            ComplianceControls.AgId.ABSC_4_4,
            ComplianceControls.Ens.OP_EXP_3,
            ComplianceControls.Ens.OP_EXP_4,
            ComplianceControls.C5.OPS_23,
            ComplianceControls.C5.OPS_18,
            ComplianceControls.Soc2.CC8_1,
            ComplianceControls.Soc2.CC7_1,
            ComplianceControls.Nist80053.CM_2,
            ComplianceControls.Nist80053.SI_2,
            ComplianceControls.Iso27017.CLD_9_5_2,
            ComplianceControls.Cis.C_7,
            ComplianceControls.NistCsf.PR_PS_02,
            ComplianceControls.NistCsf.ID_RA_01,
            ComplianceControls.Acn.PR_PS_02,
            ComplianceControls.Acn.PR_PS_01,
            ComplianceControls.BsiGrundschutz.OPS_1_1_3_A15,
        ];
        if (hasCluster && nodeCompareData.Count > 1)
        {
            var pveVersions = nodeCompareData.Values
                                             .Select(a => a.Version.Version)
                                             .Where(a => !string.IsNullOrWhiteSpace(a))
                                             .Distinct()
                                             .ToList();
            CreateResult(
                isOk: pveVersions.Count <= 1,
                id: "cluster",
                errorCode: "WC0011",
                subContext: "Version",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"Nodes run different Proxmox VE versions: {string.Join(", ", pveVersions)}",
                descriptionOk: $"All nodes run the same Proxmox VE version ({string.Join(", ", pveVersions)})",
                compliance: patchConsistencyControls);

            var kernels = nodeCompareData.Values
                                         .Select(a => a.Status?.CurrentKernel?.Release)
                                         .Where(a => !string.IsNullOrWhiteSpace(a))
                                         .Distinct()
                                         .ToList();
            CreateResult(
                isOk: kernels.Count <= 1,
                id: "cluster",
                errorCode: "WC0012",
                subContext: "Version",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"Nodes run different kernel versions: {string.Join(", ", kernels)}",
                descriptionOk: $"All nodes run the same kernel version ({string.Join(", ", kernels)})",
                compliance: patchConsistencyControls);
        }
        #endregion

        foreach (var item in _resources.Where(a => a.ResourceType == ClusterResourceType.Node))
        {
            var id = item.GetWebUrl();

            CreateResult(
                isOk: item.IsOnline,
                id: id,
                errorCode: "WN0002",
                subContext: "Status",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: "Node not online",
                descriptionOk: "Node is online",
                compliance:
                [
                    ComplianceControls.Iso27001.A_5_30,
                    ComplianceControls.Iso27001.A_8_16,
                    ComplianceControls.Nis2.Art_21_c,
                    ComplianceControls.Dora.Art_11,
                    ComplianceControls.Gdpr.Art_32_1_b,
                    ComplianceControls.Ens.OP_CONT_2,
                    ComplianceControls.Ens.OP_CONT_4,
                    ComplianceControls.C5.BCM_03,
                    ComplianceControls.Soc2.A1_1,
                    ComplianceControls.Soc2.A1_2,
                    ComplianceControls.Nist80053.CP_10,
                    ComplianceControls.Iso27017.CLD_6_3_1,
                    ComplianceControls.Cis.C_11,
                    ComplianceControls.NistCsf.PR_IR_04,
                    ComplianceControls.NistCsf.RC_RP_01,
                    ComplianceControls.Acn.ID_IM_04,
                    ComplianceControls.Acn.DE_CM_01,
                    ComplianceControls.Iso22301.C_8_3_5,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
                ]);
            if (!item.IsOnline) { continue; }

            // Node data failed to load — the failure was already recorded, skip this node.
            if (!nodeCompareData.TryGetValue(item.Node, out var compareData)) { continue; }

            var nodeApi = client.Nodes[item.Node];
            var (version, hosts, dns, timezone, aptVersions, nodeStatus, nodeUtcTime, clockOffset, aptRepositories, networks) = compareData;
            if (!int.TryParse(version.Version?.Split(".")[0], out var nodeVersion)) { continue; }

            #region End Of Life
            // PVE versions with a known EOL date that has already passed
            ComplianceMapping[] patchControls =
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
                ComplianceControls.Iso27017.CLD_9_5_2,
                ComplianceControls.Cis.C_7,
                ComplianceControls.NistCsf.PR_PS_02,
                ComplianceControls.NistCsf.ID_RA_01,
                ComplianceControls.Acn.PR_PS_02,
                ComplianceControls.BsiGrundschutz.OPS_1_1_3_A15,
            ];
            if (_pveEndOfLife.TryGetValue(nodeVersion, out var eolDate))
            {
                CreateResult(
                    isOk: _now.Date < eolDate,
                    id: id,
                    errorCode: "WN0003",
                    subContext: "EOL",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"Version {version.Version} end of life {eolDate}",
                    descriptionOk: $"Version {version.Version} is still supported (EOL: {eolDate:yyyy-MM-dd})",
                    compliance: patchControls);
            }
            #endregion

            var fetch = nodeFetchData[item.Node];

            #region Subscription
            // Without an active subscription the node uses the community repo and has no enterprise support.
            // Subscription is null when its fetch failed — the failure was already recorded, so just skip.
            if (fetch.Subscription != null)
            {
                var subscriptionActive = fetch.Subscription.Status.Equals("active", StringComparison.CurrentCultureIgnoreCase);
                CreateResult(
                    isOk: subscriptionActive,
                    id: id,
                    errorCode: "WN0004",
                    subContext: "Subscription",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Node not have subscription active",
                    descriptionOk: $"Node subscription is active",
                    compliance:
                    [
                        ComplianceControls.Iso27001.A_8_8,
                        ComplianceControls.Nis2.Art_21_e,
                        ComplianceControls.PciDss.R_6_3,
                        ComplianceControls.Gdpr.Art_32_1_b,
                        ComplianceControls.AgId.ABSC_2_3,
                        ComplianceControls.Ens.OP_EXP_4,
                        ComplianceControls.Nist80053.SI_2,
                        ComplianceControls.Soc2.CC7_1,
                        ComplianceControls.C5.OPS_18,
                        ComplianceControls.AgId.ABSC_4_1,
                        ComplianceControls.AgId.ABSC_4_4,
                        ComplianceControls.Cis.C_7,
                        ComplianceControls.NistCsf.PR_PS_02,
                        ComplianceControls.NistCsf.ID_RA_01,
                        ComplianceControls.Iso27017.CLD_9_5_2,
                        ComplianceControls.Acn.PR_PS_02,
                        ComplianceControls.BsiGrundschutz.OPS_1_1_3_A15,
                    ]);
            }
            #endregion

            #region RrdData
            // Historical resource usage (CPU, RAM, network, disk) via RRD — period configurable (day/week)
            CheckNodeRrd(settings,
                         id,
                         await nodeApi.Rrddata.GetAsync(settings.Node.Rrd.TimeFrame, settings.Node.Rrd.Consolidation)
                                      .ToSafeEnum(_result, id, DiagnosticResultContext.Node, $"RRD data for node '{item.Node}'"));
            #endregion

            #region Cross-node comparisons
            // Cross-node checks compare this node against every other online node. They have no
            // meaning on a single-node setup (no peers to compare to) — skip the whole block to
            // avoid emitting Ok results that confuse the report. Single-node compliance gaps are
            // already surfaced by IC0017 / IC0002 / IC0003.
            var otherNodesData = onlineNodes.Where(a => a.Node != item.Node)
                                            .Select(a => nodeCompareData.TryGetValue(a.Node, out var od) ? od : null)
                                            .Where(od => od != null)
                                            .Select(od => od!)
                                            .ToList();
            if (otherNodesData.Count > 0)
            {
                CreateResult(
                    isOk: otherNodesData.All(od => version.IsEqual(od.Version)),
                    id: id,
                    errorCode: "CN0001",
                    subContext: "Version",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Critical,
                    descriptionKo: "Nodes version not equal",
                    descriptionOk: "Node version matches the rest of the cluster",
                    compliance: patchConsistencyControls);

                CreateResult(
                    isOk: otherNodesData.All(od => HostsEntries(hosts).SetEquals(HostsEntries(od.Hosts))),
                    id: id,
                    errorCode: "WN0005",
                    subContext: "Hosts",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Nodes hosts configuration not equal",
                    descriptionOk: "Node /etc/hosts matches the rest of the cluster",
                    compliance: patchConsistencyControls);

                CreateResult(
                    isOk: otherNodesData.All(od => dns.IsEqual(od.Dns)),
                    id: id,
                    errorCode: "WN0006",
                    subContext: "DNS",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Nodes DNS not equal",
                    descriptionOk: "Node DNS configuration matches the rest of the cluster",
                    compliance: patchConsistencyControls);

                CreateResult(
                    isOk: otherNodesData.All(od => timezone == od.Timezone),
                    id: id,
                    errorCode: "WN0007",
                    subContext: "Timezone",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Nodes Timezone not equal",
                    descriptionOk: $"Node timezone ({timezone}) matches the rest of the cluster",
                    compliance: patchConsistencyControls);

                // APT repository sources must be identical across nodes to ensure consistent upgrades.
                // Compare the enabled URIs from all repository files — order-insensitive.
                static List<string> GetUris(NodeAptRepositories? repos)
                    => [.. (repos?.Files ?? [])
                         .SelectMany(f => f.Repositories ?? [])
                         .Where(r => r.Enabled)
                         .SelectMany(r => r.URIs ?? [])
                         .Order()];
                var myUris = GetUris(aptRepositories);
                CreateResult(
                    isOk: otherNodesData.All(od => myUris.SequenceEqual(GetUris(od.AptRepositories))),
                    id: id,
                    errorCode: "WN0008",
                    subContext: "AptRepositories",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Nodes APT repositories not equal — inconsistent package sources may cause upgrade problems",
                    descriptionOk: "Node APT repositories match the rest of the cluster",
                    compliance: patchConsistencyControls);

                // MTU mismatch on physical NICs between nodes can cause packet fragmentation,
                // corosync instability and live migration failures.
                // Compare MTU of eth interfaces by name — only flag if the same interface exists on both nodes.
                var myMtus = networks.Where(a => a.Type == "eth" && a.Mtu.HasValue)
                                     .ToDictionary(a => a.Interface, a => a.Mtu!.Value);
                var mtuMismatchesList = otherNodesData
                    .SelectMany(od => od.Networks.Where(a => a.Type == "eth" && a.Mtu.HasValue)
                                                  .Where(a => myMtus.TryGetValue(a.Interface, out var myMtu) && myMtu != a.Mtu!.Value)
                                                  .Select(a => $"{a.Interface}: {myMtus[a.Interface]} vs {a.Mtu!.Value}"))
                    .Distinct()
                    .ToList();
                CreateResult(
                    isOk: mtuMismatchesList.Count == 0,
                    id: id,
                    errorCode: "WN0009",
                    subContext: "Network",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"NIC MTU mismatch with other nodes: {string.Join(", ", mtuMismatchesList)}",
                    descriptionOk: "Node NIC MTUs match the rest of the cluster",
                    compliance: patchConsistencyControls);
            }
            #endregion

            #region Network Card
            // Physical NICs (type=eth) that are down — could mean a cable/switch problem.
            // Only NICs that are in use: a spare port with no cable is down by design.
            var usedInterfaces = UsedInterfaces(networks);
            CreateResultPerItem(
                items: networks.Where(a => a.Type == "eth" && usedInterfaces.Contains(a.Interface)).ToList(),
                isItemOk: a => a.Active,
                itemId: _ => id,
                itemDescriptionKo: a => $"Network card '{a.Interface}' not active",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => "All physical NICs in use are active",
                errorCode: "WN0010",
                subContext: "Network",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance:
                [
                    ComplianceControls.Iso27001.A_5_30,
                    ComplianceControls.Iso27001.A_8_16,
                    ComplianceControls.Dora.Art_11,
                    ComplianceControls.Gdpr.Art_32_1_b,
                    ComplianceControls.Ens.OP_CONT_4,
                    ComplianceControls.Iso27017.CLD_6_3_1,
                    ComplianceControls.NistCsf.PR_IR_04,
                    ComplianceControls.Acn.ID_IM_04,
                    ComplianceControls.Acn.DE_CM_01,
                    ComplianceControls.Iso22301.C_8_3_5,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
                ]);

            // Bond with fewer than two slaves provides no link redundancy — a single NIC/cable failure takes it down
            CreateResultPerItem(
                items: networks.Where(a => a.Type == "bond").ToList(),
                isItemOk: a => (a.Slaves ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2,
                itemId: _ => id,
                itemDescriptionKo: a => $"Bond '{a.Interface}' has fewer than two slaves — no link redundancy",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => "All bonds have at least two slaves",
                errorCode: "WN0034",
                subContext: "Network",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance:
                [
                    ComplianceControls.Iso27001.A_5_30,
                    ComplianceControls.Nis2.Art_21_c,
                    ComplianceControls.Dora.Art_11,
                    ComplianceControls.Gdpr.Art_32_1_b,
                    ComplianceControls.Ens.OP_CONT_2,
                    ComplianceControls.Ens.OP_CONT_4,
                    ComplianceControls.C5.BCM_03,
                    ComplianceControls.Soc2.A1_1,
                    ComplianceControls.Soc2.A1_2,
                    ComplianceControls.Nist80053.CP_10,
                    ComplianceControls.Iso27017.CLD_6_3_1,
                    ComplianceControls.Cis.C_11,
                    ComplianceControls.NistCsf.PR_IR_04,
                    ComplianceControls.NistCsf.RC_RP_01,
                    ComplianceControls.Acn.ID_IM_04,
                    ComplianceControls.Iso22301.C_8_3_5,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ]);
            #endregion

            #region Package Versions
            // Mismatched package versions across nodes can cause subtle incompatibilities after partial upgrades.
            // Skip on single-node setups: no peer to compare to.
            if (onlineNodes.Count(a => a.Node != item.Node) > 0)
            {
                var packageDifferences = onlineNodes.Where(a => a.Node != item.Node && nodeCompareData.ContainsKey(a.Node))
                                                    .SelectMany(a => PackageVersionDifferences(aptVersions, nodeCompareData[a.Node].AptVersions)
                                                                        .Select(d => $"{d} on '{a.Node}'"))
                                                    .ToList();
                CreateResult(
                    isOk: packageDifferences.Count == 0,
                    id: id,
                    errorCode: "CN0002",
                    subContext: "PackageVersions",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Critical,
                    descriptionKo: $"Nodes package version not equal: {string.Join(", ", packageDifferences)}",
                    descriptionOk: "Node package versions match the rest of the cluster",
                    compliance: patchConsistencyControls);
            }
            #endregion

            #region Services
            // corosync is only expected on clustered setups; time sync service name changed in PVE 7+
            var serviceExcluded = new List<string>();
            if (!hasCluster) { serviceExcluded.Add("corosync"); }
            serviceExcluded.Add(nodeVersion >= 7
                                    ? "systemd-timesyncd"
                                    : "chrony");

            CreateResultPerItem(
                // unit-state 'not-found': the unit is not installed on this node (PVE lists it anyway).
                items: fetch.Services.Where(a => !serviceExcluded.Contains(a.Name) && a.UnitState != "not-found").ToList(),
                isItemOk: a => a.IsRunning,
                itemId: _ => id,
                itemDescriptionKo: a => $"Service '{a.Description}' not running",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => "All monitored services are running",
                errorCode: "WN0011",
                subContext: "Service",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance:
                [
                    ComplianceControls.Iso27001.A_8_15,
                    ComplianceControls.Iso27001.A_8_16,
                    ComplianceControls.Nis2.Art_21_f,
                    ComplianceControls.Dora.Art_10,
                    ComplianceControls.PciDss.R_10_2,
                    ComplianceControls.Gdpr.Art_32_1_d,
                    ComplianceControls.AgId.ABSC_5_2,
                    ComplianceControls.Ens.OP_EXP_8,
                    ComplianceControls.Ens.OP_MON_3,
                    ComplianceControls.C5.OPS_13,
                    ComplianceControls.C5.OPS_10,
                    ComplianceControls.Soc2.CC7_2,
                    ComplianceControls.Nist80053.AU_12,
                    ComplianceControls.Nist80053.SI_4,
                    ComplianceControls.Iso27018.A_12_4_1,
                    ComplianceControls.Cis.C_8,
                    ComplianceControls.NistCsf.DE_CM_01,
                    ComplianceControls.NistCsf.DE_CM_03,
                    ComplianceControls.Iso27017.CLD_12_4_5,
                    ComplianceControls.Acn.PR_PS_04,
                    ComplianceControls.Acn.DE_CM_01,
                    ComplianceControls.BsiGrundschutz.OPS_1_1_5_A3,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
                ]);
            #endregion

            #region Certificates
            // Expired TLS certificates break the web UI and API access.
            ComplianceMapping[] cryptoCertControls =
            [
                ComplianceControls.Iso27001.A_8_24,
                ComplianceControls.Nis2.Art_21_h,
                ComplianceControls.PciDss.R_4_2,
                ComplianceControls.Gdpr.Art_32_1_a,
                ComplianceControls.Gdpr.Art_5_1_f,
                ComplianceControls.AgId.ABSC_13_1,
                ComplianceControls.Ens.MP_COM_2,
                ComplianceControls.C5.CRY_02,
                ComplianceControls.Soc2.CC6_7,
                ComplianceControls.Nist80053.SC_8,
                ComplianceControls.Nist80053.SC_13,
                ComplianceControls.Iso27018.A_10_1_1,
                ComplianceControls.Cis.C_3,
                ComplianceControls.NistCsf.PR_DS_02,
                ComplianceControls.Acn.PR_DS_02,
                ComplianceControls.BsiGrundschutz.CON_1_A1,
            ];
            CreateResultPerItem(
                items: fetch.Certificates,
                isItemOk: a => DateTimeOffset.FromUnixTimeSeconds(a.NotAfter) >= _now,
                itemId: _ => id,
                itemDescriptionKo: cert => $"Certificate '{cert.FileName}' expired",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: certs => certs.Count == 0
                    ? "No certificates to verify"
                    : $"All {certs.Count} certificate(s) are valid",
                errorCode: "CN0003",
                subContext: "Certificates",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Critical,
                compliance: cryptoCertControls);

            // Certificates expiring within the warning window — renew before they break access
            var certExpiryLimit = _now.AddDays(CertificateExpiringDays);
            CreateResultPerItem(
                items: fetch.Certificates.Where(a =>
                {
                    var notAfter = DateTimeOffset.FromUnixTimeSeconds(a.NotAfter);
                    return notAfter >= _now;
                }).ToList(),
                isItemOk: a => DateTimeOffset.FromUnixTimeSeconds(a.NotAfter) >= certExpiryLimit,
                itemId: _ => id,
                itemDescriptionKo: a => $"Certificate '{a.FileName}' expires on {DateTimeOffset.FromUnixTimeSeconds(a.NotAfter):yyyy-MM-dd} (within {CertificateExpiringDays} days)",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => $"No certificate expires within the next {CertificateExpiringDays} days",
                errorCode: "WN0023",
                subContext: "Certificates",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance: cryptoCertControls);

            // Self-signed certificate (issuer == subject) — browsers and API clients will warn / refuse
            CreateResultPerItem(
                items: fetch.Certificates.Where(a => !string.IsNullOrWhiteSpace(a.Issuer)).ToList(),
                isItemOk: a => a.Issuer != a.Subject,
                itemId: _ => id,
                itemDescriptionKo: a => $"Certificate '{a.FileName}' is self-signed — consider a CA-signed certificate (e.g. ACME/Let's Encrypt)",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => "No self-signed certificate detected",
                errorCode: "IN0004",
                subContext: "Certificates",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Info,
                compliance: cryptoCertControls);
            #endregion

            #region Replication
            // Replication jobs with errors mean the secondary copy is out of date
            // PVE reports the last failure in 'error' and the retries in 'fail_count' (there is no
            // 'errors' key: the old lookup never matched, so CN0004 could not fire).
            var replCount = fetch.Replication.Count(a => !string.IsNullOrWhiteSpace(a.Error) || a.FailCount > 0);
            CreateResult(
                isOk: replCount == 0,
                id: id,
                errorCode: "CN0004",
                subContext: "Replication",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Critical,
                descriptionKo: $"{replCount} Replication has errors",
                descriptionOk: $"All {fetch.Replication.Count} replication job(s) on this node are healthy",
                compliance:
                [
                    ComplianceControls.Iso27001.A_5_30,
                    ComplianceControls.Nis2.Art_21_c,
                    ComplianceControls.Dora.Art_11,
                    ComplianceControls.Gdpr.Art_32_1_b,
                    ComplianceControls.Ens.OP_CONT_2,
                    ComplianceControls.Ens.OP_CONT_4,
                    ComplianceControls.C5.BCM_03,
                    ComplianceControls.Soc2.A1_1,
                    ComplianceControls.Soc2.A1_2,
                    ComplianceControls.Nist80053.CP_10,
                    ComplianceControls.Iso27017.CLD_6_3_1,
                    ComplianceControls.Cis.C_11,
                    ComplianceControls.NistCsf.PR_IR_04,
                    ComplianceControls.NistCsf.RC_RP_01,
                    ComplianceControls.Acn.ID_IM_04,
                    ComplianceControls.Iso22301.C_8_3_5,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ]);
            #endregion

            await CheckNodeDiskAsync(nodeApi, settings, id, fetch);

            #region APT Updates
            // Any pending update is informational; "important" priority updates (security) are Warning
            var aptUpdate = fetch.AptUpdate;
            var updateCount = aptUpdate.Count();
            CreateResult(
                isOk: updateCount == 0,
                id: id,
                errorCode: "IN0001",
                subContext: "Update",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Info,
                descriptionKo: $"{updateCount} Update available",
                descriptionOk: "No package updates available",
                compliance: []);

            var updateImportantCount = aptUpdate.Count(a => a.Priority == "important");
            CreateResult(
                isOk: updateImportantCount == 0,
                id: id,
                errorCode: "WN0012",
                subContext: "Update",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"{updateImportantCount} Update Important available",
                descriptionOk: "No important updates pending",
                compliance: patchControls);
            #endregion

            #region Reboot required
            // A kernel newer than the running one is installed: it only takes effect after a reboot.
            // Both kversion and current-kernel describe the running kernel, so they cannot tell this;
            // the installed kernel images come from the package list.
            var runningKernel = nodeStatus?.CurrentKernel?.Release ?? "";
            if (!string.IsNullOrWhiteSpace(runningKernel))
            {
                var newerKernel = NewerInstalledKernel(runningKernel, aptVersions.Select(a => a.Package));
                CreateResult(
                    isOk: newerKernel == null,
                    id: id,
                    errorCode: "WN0013",
                    subContext: "Reboot",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"Node requires reboot: running kernel '{runningKernel}' but newer kernel '{newerKernel}' is installed",
                    descriptionOk: $"Node is running the latest installed kernel ({runningKernel})",
                    compliance: patchControls);
            }
            #endregion

            #region NTP
            // Compare node UTC time against the clock of the machine running the analysis, read when
            // the node answered — offset > 60s indicates an NTP issue (or a wrong client clock).
            // Mapped to logging controls: accurate timestamps are a precondition for usable audit logs.
            if (nodeUtcTime > 0)
            {
                var ntpOffset = Math.Abs(clockOffset);
                CreateResult(
                    isOk: ntpOffset <= 60,
                    id: id,
                    errorCode: "WN0014",
                    subContext: "NTP",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"Node time offset is {ntpOffset}s — NTP may not be synchronized",
                    descriptionOk: $"Node time offset is {ntpOffset}s (within tolerance)",
                    compliance:
                    [
                        ComplianceControls.Iso27001.A_8_15,
                        ComplianceControls.Iso27001.A_8_16,
                        ComplianceControls.Nis2.Art_21_f,
                        ComplianceControls.Dora.Art_10,
                        ComplianceControls.PciDss.R_10_2,
                        ComplianceControls.Gdpr.Art_32_1_d,
                        ComplianceControls.AgId.ABSC_5_2,
                        ComplianceControls.Ens.OP_EXP_8,
                        ComplianceControls.Ens.OP_MON_3,
                        ComplianceControls.C5.OPS_13,
                        ComplianceControls.C5.OPS_10,
                        ComplianceControls.Soc2.CC7_2,
                        ComplianceControls.Nist80053.AU_12,
                        ComplianceControls.Nist80053.SI_4,
                        ComplianceControls.Iso27018.A_12_4_1,
                        ComplianceControls.Cis.C_8,
                        ComplianceControls.NistCsf.DE_CM_01,
                        ComplianceControls.NistCsf.DE_CM_03,
                        ComplianceControls.Iso27017.CLD_12_4_5,
                        ComplianceControls.Acn.PR_PS_04,
                        ComplianceControls.Acn.DE_CM_01,
                        ComplianceControls.BsiGrundschutz.OPS_1_1_5_A3,
                        ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
                        ComplianceControls.BsiGrundschutz.OPS_1_1_5_A4,
                    ]);
            }

            // WN0045 — time drift between cluster nodes. Even when each node looks fine vs the
            // diag client, clocks can have drifted from each other (typical sign: corosync token
            // retransmits, HA fencing instability, broken Kerberos/LDAP, replayable log timestamps).
            // Compare this node's clock against the maximum delta among the other online nodes. The
            // nodes are read at slightly different moments, so their offsets from the client clock
            // are compared rather than the raw times; the client clock itself cancels out.
            if (hasCluster && nodeUtcTime > 0)
            {
                var otherOffsets = nodeCompareData
                    .Where(kv => kv.Key != item.Node && kv.Value.UtcTime > 0)
                    .Select(kv => kv.Value.ClockOffset)
                    .ToList();
                if (otherOffsets.Count > 0)
                {
                    var maxDrift = otherOffsets.Max(o => Math.Abs(clockOffset - o));
                    CreateResult(
                        isOk: maxDrift <= 5,
                        id: id,
                        errorCode: "WN0045",
                        subContext: "NTP",
                        context: DiagnosticResultContext.Node,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: $"Node clock drifts up to {maxDrift}s from other cluster nodes — corosync/HA and log correlation may be affected",
                        descriptionOk: $"Node clock within {maxDrift}s of other cluster nodes",
                        compliance:
                        [
                            ComplianceControls.Iso27001.A_8_15,
                            ComplianceControls.Iso27001.A_8_16,
                            ComplianceControls.Nis2.Art_21_f,
                            ComplianceControls.Dora.Art_10,
                            ComplianceControls.PciDss.R_10_2,
                            ComplianceControls.Gdpr.Art_32_1_d,
                            ComplianceControls.AgId.ABSC_5_2,
                            ComplianceControls.Ens.OP_EXP_8,
                            ComplianceControls.Ens.OP_MON_3,
                            ComplianceControls.C5.OPS_13,
                            ComplianceControls.C5.OPS_10,
                            ComplianceControls.Soc2.CC7_2,
                            ComplianceControls.Nist80053.AU_12,
                            ComplianceControls.Nist80053.SI_4,
                            ComplianceControls.Iso27018.A_12_4_1,
                            ComplianceControls.Cis.C_8,
                            ComplianceControls.NistCsf.DE_CM_01,
                            ComplianceControls.NistCsf.DE_CM_03,
                            ComplianceControls.Iso27017.CLD_12_4_5,
                            ComplianceControls.Acn.PR_PS_04,
                            ComplianceControls.Acn.DE_CM_01,
                            ComplianceControls.BsiGrundschutz.OPS_1_1_5_A3,
                            ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
                        ]);
                }
            }
            #endregion

            #region IOMMU
            // IOMMU is required for PCI passthrough (GPU, NIC, etc.).
            // If all detected PCI devices report IommuGroup == -1 the kernel/firmware has IOMMU disabled.
            // Note: a node with no PCI devices at all is not flagged.
            var pciDevices = fetch.PciDevices;
            if (pciDevices.Any())
            {
                CreateResult(
                    isOk: pciDevices.Any(a => a.IommuGroup != -1),
                    id: id,
                    errorCode: "IN0002",
                    subContext: "IOMMU",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "IOMMU is not enabled — PCI passthrough will not work (enable intel_iommu=on or amd_iommu=on in kernel cmdline)",
                    descriptionOk: "IOMMU is enabled — PCI passthrough is available",
                    compliance: []);
            }
            #endregion

            #region Task history
            // Failed tasks in the last 48 hours (errors=true filters server-side for efficiency)
            var dayTask = new DateTimeOffset(_now.AddDays(-2)).ToUnixTimeSeconds();
            CheckTaskHistory(fetch.Tasks.Where(a => a.StartTime >= dayTask),
                             DiagnosticResultContext.Node, id);
            #endregion

            #region CVE
            CheckNodeCve(id, aptVersions);
            #endregion

            // TODO: backup history anomaly check — uncomment when BackupHelper.ParseVzdumpLog is available via NuGet.
            // Reads vzdump task logs for the last N days, computes per-VM average duration and size,
            // and warns when the latest backup deviates significantly.
            // Requires: using Corsinvest.ProxmoxVE.Api.Shared.Utils;
            // NOTE on codes: when activated, allocate fresh WN slots (last in use is WN0045) — the
            // codes commented below were drafted before the catalog reached its current state and
            // would collide with WN0034 (bond redundancy).
            //
            //if (settings.BackupHistory.Enabled)
            //{
            //    var since = new DateTimeOffset(_now.AddDays(-settings.BackupHistory.Days)).ToUnixTimeSeconds();
            //    var vzdumpTasks = (await nodeApi.Tasks.GetAsync(typefilter: "vzdump", limit: 1000))
            //                         .Where(a => a.StartTime >= since && a.EndTime > 0);
            //
            //    var allJobs = new List<(string VmId, TimeSpan Duration, long Size)>();
            //
            //    foreach (var t in vzdumpTasks)
            //    {
            //        var logLines = await nodeApi.Tasks[t.UniqueTaskId].Log.GetAsync(limit: 10000);
            //        var log = string.Join(Environment.NewLine, logLines);
            //        var jobs = BackupHelper.ParseVzdumpLog(log);
            //        allJobs.AddRange(jobs.Select(j => (j.VmId, j.Duration ?? TimeSpan.Zero, j.Size)));
            //    }
            //
            //    foreach (var vmGroup in allJobs.GroupBy(j => j.VmId))
            //    {
            //        var ordered = vmGroup.OrderBy(j => j.Duration).ToList();
            //        if (ordered.Count < 2) { continue; }
            //
            //        var avgDuration = TimeSpan.FromSeconds(ordered.SkipLast(1).Average(j => j.Duration.TotalSeconds));
            //        var avgSize     = ordered.SkipLast(1).Average(j => j.Size);
            //        var last        = ordered.Last();
            //
            //        if (avgDuration.TotalSeconds > 0 && last.Duration.TotalSeconds > avgDuration.TotalSeconds * settings.BackupHistory.MaxDurationMultiplier)
            //        {
            //            _result.Add(new DiagnosticResult
            //            {
            //                Id          = $"{id}/backup-duration/{vmGroup.Key}",
            //                ErrorCode   = "WN00XX-duration",   // allocate at implementation time
            //                Context     = DiagnosticResultContext.Node,
            //                SubContext  = "Backup",
            //                Gravity     = DiagnosticResultGravity.Warning,
            //                Description = $"VM {vmGroup.Key} backup duration {last.Duration:hh\\:mm\\:ss} is {last.Duration.TotalSeconds / avgDuration.TotalSeconds:F1}x the average — possible issue",
            //            });
            //        }
            //
            //        if (avgSize > 0 && last.Size < avgSize * (1 - settings.BackupHistory.MinSizeRatioPercent / 100.0))
            //        {
            //            _result.Add(new DiagnosticResult
            //            {
            //                Id          = $"{id}/backup-size/{vmGroup.Key}",
            //                ErrorCode   = "WN00XX-size",       // allocate at implementation time
            //                Context     = DiagnosticResultContext.Node,
            //                SubContext  = "Backup",
            //                Gravity     = DiagnosticResultGravity.Warning,
            //                Description = $"VM {vmGroup.Key} backup size {FormatHelper.FromBytes(last.Size)} dropped significantly vs average {FormatHelper.FromBytes((long)avgSize)}",
            //            });
            //        }
            //    }
            //}
        }

        #region Guest NICs vs host bridges
        // Guests on a node, with their config — the input of both bridge checks below.
        var guestsByNode = _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                 && !a.IsTemplate
                                                 && _vmConfigs.ContainsKey(a.VmId))
                                     .ToLookup(a => a.Node);

        // A tag on a non-VLAN-aware Linux bridge is fine: PVE uses the traditional model and builds
        // vmbrXvN on top of the uplink's .N subinterface. The real trap is a VLAN-aware bridge whose
        // bridge-vids was narrowed: a VLAN outside that list is not allowed on the bridge uplinks, so
        // the guest only reaches peers on the same bridge. No bridge-vids means no restriction.
        var vlanOutsideBridge = onlineNodes
            .Where(n => nodeCompareData.ContainsKey(n.Node))
            .SelectMany(n =>
            {
                var allowedByBridge = nodeCompareData[n.Node].Networks
                                        .Where(net => net.Type == "bridge"
                                                      && net.BridgeVlanAware is true
                                                      && !string.IsNullOrWhiteSpace(net.BridgeVids))
                                        .ToDictionary(net => net.Interface, net => (net.BridgeVids, Ranges: VlanIds.Parse(net.BridgeVids)));

                return guestsByNode[n.Node]
                    .SelectMany(vm => _vmConfigs[vm.VmId].Networks.Select(net => (Vm: vm, Net: net)))
                    .Where(x => !string.IsNullOrWhiteSpace(x.Net.Bridge) && allowedByBridge.ContainsKey(x.Net.Bridge))
                    .Select(x =>
                    {
                        var (vids, ranges) = allowedByBridge[x.Net.Bridge];
                        return (x.Vm, x.Net, Vids: vids, Outside: VlansOutsideBridge(x.Net.Tag, x.Net.Trunks, ranges));
                    })
                    .Where(x => x.Outside.Count > 0);
            })
            .ToList();
        CreateResultPerItem(
            items: vlanOutsideBridge,
            isItemOk: _ => false,
            itemId: x => x.Vm.GetWebUrl(),
            itemDescriptionKo: x => $"{(x.Vm.VmType == VmType.Lxc ? "CT" : "VM")} {x.Vm.VmId} interface '{x.Net.Id}' uses VLAN {VlanIds.Format(x.Outside)} "
                                    + $"not in bridge-vids '{x.Vids}' of VLAN-aware bridge '{x.Net.Bridge}' — that VLAN does not leave the node",
            aggregatedIdOk: "cluster/network",
            aggregatedDescriptionOk: _ => "Every guest VLAN is allowed by the bridge-vids of its VLAN-aware bridge",
            errorCode: "WN0046",
            subContext: "Network",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);

        // A bridge that exists on the guest's node but not on a peer: migrating or HA-recovering the
        // guest to that peer fails. A bridge missing on the guest's own node is skipped on purpose —
        // SDN vnets are not listed in /nodes/{node}/network, so it cannot be told apart from a vnet.
        if (nodeCompareData.Count > 1)
        {
            var bridgesByNode = nodeCompareData.ToDictionary(kv => kv.Key,
                                                             kv => kv.Value.Networks
                                                                           .Where(net => net.Type is "bridge" or "OVSBridge")
                                                                           .Select(net => net.Interface)
                                                                           .ToHashSet());
            var guests = nodeCompareData.Keys
                                        .SelectMany(node => guestsByNode[node])
                                        .Select(vm => (vm.Node, vm.VmId, Bridges: _vmConfigs[vm.VmId].Networks.Select(net => net.Bridge)));
            var nodeUrls = onlineNodes.ToDictionary(n => n.Node, n => n.GetWebUrl());

            CreateResultPerItem(
                items: FindBridgesMissingOnPeers(bridgesByNode, guests),
                isItemOk: _ => false,
                itemId: x => nodeUrls[x.Node],
                itemDescriptionKo: x => $"Bridge '{x.Bridge}' used by guest(s) {string.Join(", ", x.VmIds)} is missing on node(s) {string.Join(", ", x.MissingOn)} — migration or HA recovery there will fail",
                aggregatedIdOk: "cluster/network",
                aggregatedDescriptionOk: _ => "Every bridge used by a guest exists on all online nodes",
                errorCode: "WN0047",
                subContext: "Network",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance: []);
        }
        #endregion

        #region Memory overcommit
        // Sum of VM/CT allocated RAM on a node exceeds physical node RAM — risk of OOM
        var memOvercommitItems = onlineNodes
            .Select(n =>
            {
                var nr = _resources.FirstOrDefault(a => a.ResourceType == ClusterResourceType.Node && a.Node == n.Node);
                if (nr == null || nr.MemorySize == 0) { return null; }
                var allocated = _resources.Where(a => a.ResourceType == ClusterResourceType.Vm && a.Node == n.Node && !a.IsTemplate)
                                          .Aggregate(0UL, (acc, a) => acc + a.MemorySize);
                return new { NodeItem = n, NodeResource = nr, AllocatedMem = allocated };
            })
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();
        CreateResultPerItem(
            items: memOvercommitItems,
            isItemOk: p => p.AllocatedMem <= p.NodeResource.MemorySize,
            itemId: p => p.NodeItem.GetWebUrl(),
            itemDescriptionKo: p =>
            {
                var pct = Math.Round((p.AllocatedMem / (double)p.NodeResource.MemorySize * 100) - 100, 1);
                return $"Node '{p.NodeItem.Node}' memory overcommitted by {pct}% (allocated: {p.AllocatedMem / 1024 / 1024 / 1024} GB, physical: {p.NodeResource.MemorySize / 1024 / 1024 / 1024} GB)";
            },
            aggregatedIdOk: "cluster/nodes",
            aggregatedDescriptionOk: _ => "No node is memory-overcommitted",
            errorCode: "WN0036",
            subContext: "Memory",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);
        #endregion

        #region VM consolidation
        // Nodes with very low CPU and RAM utilization — VMs could be moved to free the node
        var consolidationCandidates = onlineNodes
            .Where(n => nodeCompareData.ContainsKey(n.Node))
            .Select(n =>
            {
                var nr = _resources.FirstOrDefault(a => a.ResourceType == ClusterResourceType.Node && a.Node == n.Node);
                if (nr == null) { return null; }
                var vmCount = _resources.Count(a => a.ResourceType == ClusterResourceType.Vm && a.Node == n.Node && !a.IsTemplate);
                if (vmCount == 0) { return null; }
                return new { NodeItem = n, NodeResource = nr, VmCount = vmCount };
            })
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();
        CreateResultPerItem(
            items: consolidationCandidates,
            isItemOk: p =>
            {
                var cpuPct = p.NodeResource.CpuUsagePercentage * 100.0;
                var memPct = p.NodeResource.MemorySize > 0 ? p.NodeResource.MemoryUsagePercentage * 100.0 : 0;
                return !(cpuPct < settings.Node.ConsolidationCpuThreshold
                          && memPct < settings.Node.ConsolidationMemThreshold);
            },
            itemId: p => p.NodeItem.GetWebUrl(),
            itemDescriptionKo: p =>
            {
                var cpuPct = p.NodeResource.CpuUsagePercentage * 100.0;
                var memPct = p.NodeResource.MemorySize > 0 ? p.NodeResource.MemoryUsagePercentage * 100.0 : 0;
                return $"Node '{p.NodeItem.Node}' has low utilization (CPU: {cpuPct:F1}%, RAM: {memPct:F1}%) — consider consolidating {p.VmCount} VM(s) to free the node";
            },
            aggregatedIdOk: "cluster/nodes",
            aggregatedDescriptionOk: _ => "No node is underutilized to the consolidation thresholds",
            errorCode: "IN0003",
            subContext: "Consolidation",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: []);
        #endregion

        #region CPU Compatibility Mode
        // Calculate the minimum common x86-64 feature level across all online nodes.
        // The level determines the safest CPU type to assign to VMs for live migration.
        // If nodes have different levels, VMs using a higher level cannot migrate to lower-level nodes.
        // Levels: v2-AES (2008+), v3 (Haswell 2013+), v4 (Skylake-X 2017+)
        if (hasCluster && onlineNodes.Count > 1)
        {
            var nodeLevels = nodeCompareData.Where(kv => kv.Value.Status?.CpuInfo?.Flags != null)
                                            .ToDictionary(kv => kv.Key, kv => NodeHelper.GetCpuX86Level(kv.Value.Status.CpuInfo.Flags));

            if (nodeLevels.Count > 1)
            {
                var minLevel = nodeLevels.Values.Min();
                var maxLevel = nodeLevels.Values.Max();
                var levelsConsistent = !(minLevel != null && maxLevel != null && minLevel.Level != maxLevel.Level);
                var lowerNodes = minLevel != null
                                    ? nodeLevels.Where(kv => kv.Value.Level == minLevel.Level).Select(kv => kv.Key)
                                    : [];
                CreateResult(
                    isOk: levelsConsistent,
                    id: "cluster",
                    errorCode: "WN0015",
                    subContext: "CPUCompatibility",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"CPU level mismatch: minimum is {minLevel?.Name}, maximum is {maxLevel?.Name}. Nodes at minimum level: {string.Join(", ", lowerNodes)}. Use cpu type '{minLevel?.Name}' for safe live migration.",
                    descriptionOk: $"All online nodes share the same x86-64 feature level ({minLevel?.Name})",
                    compliance:
                    [
                        ComplianceControls.Iso27001.A_5_30,
                        ComplianceControls.Dora.Art_11,
                        ComplianceControls.Ens.OP_CONT_2,
                        ComplianceControls.Ens.OP_EXP_3,
                        ComplianceControls.Soc2.CC8_1,
                        ComplianceControls.Nist80053.CM_2,
                        ComplianceControls.Iso27017.CLD_6_3_1,
                        ComplianceControls.NistCsf.PR_IR_04,
                        ComplianceControls.Acn.ID_IM_04,
                        ComplianceControls.Iso22301.C_8_3_5,
                        ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                    ]);
            }
        }
        #endregion
    }

    private void CheckNodeRrd(Settings settings, string id, IEnumerable<NodeRrdData> rrdData)
    {
        // No data (the RRD fetch failed, already reported as WG0042) means nothing to check.
        var rrdList = rrdData.ToList();
        if (rrdList.Count == 0) { return; }

        CheckThresholdHost(settings.Node,
                           DiagnosticResultContext.Node,
                           id,
                           rrdList.Select(a => new ThresholdRddData(a, a, a)),
                           cpuErrorCode: "WN0027",
                           memoryErrorCode: "WN0038",
                           netInErrorCode: "WN0039",
                           netOutErrorCode: "WN0040");

        // IOWait = time CPU spent waiting for I/O — high values indicate storage bottleneck
        CheckThreshold(settings.Node.Cpu,
                       "WN0028",
                       DiagnosticResultContext.Node,
                       "Usage",
                       [new ThresholdDataPoint(rrdList.Average(a => a.IoWait) * 100,
                                               0d,
                                               id,
                                               $"IOWait (rrd {settings.Node.Rrd.TimeFrame} {settings.Node.Rrd.Consolidation})")],
                       true,
                       false);

        // Root filesystem usage on the node OS disk
        CheckThreshold(settings.Storage.Threshold,
                       "WN0029",
                       DiagnosticResultContext.Node,
                       "Usage",
                       [new ThresholdDataPoint(rrdList.Average(a => a.RootUsage),
                                               rrdList.Average(a => a.RootSize),
                                               id,
                                               $"Root space (rrd {settings.Node.Rrd.TimeFrame} {settings.Node.Rrd.Consolidation})")],
                       false,
                       true);

        // SWAP usage — high swap indicates RAM pressure and causes severe performance degradation.
        // No swap (the default on ZFS-root installs) has nothing to measure: 0/0 was an Ok "NaN%".
        if (rrdList.Any(a => a.SwapSize > 0))
        {
            CheckThreshold(settings.Storage.Threshold,
                           "WN0030",
                           DiagnosticResultContext.Node,
                           "Usage",
                           [new ThresholdDataPoint(rrdList.Average(a => a.SwapUsage),
                                                   rrdList.Average(a => Convert.ToDouble(a.SwapSize)),
                                                   id,
                                                   $"SWAP (rrd {settings.Node.Rrd.TimeFrame} {settings.Node.Rrd.Consolidation})")],
                           false,
                           true);
        }

        // PSI pressure — only meaningful when non-zero (PVE 9.0+ only; older nodes always return 0).
        // PSI values are already percentages (0-100): the kernel reports /proc/pressure avgN that way
        // and pvestatd stores them unscaled — unlike CPU/memory RRD fields, which are 0-1 fractions.
        if (rrdList.Any(a => a.PressureCpuSome > 0))
        {
            CheckThreshold(settings.Node.Rrd.Pressure.Cpu,
                           "WN0031",
                           DiagnosticResultContext.Node,
                           "Pressure",
                           [new ThresholdDataPoint(rrdList.Average(a => a.PressureCpuSome),
                                                   0d,
                                                   id,
                                                   $"PSI CPU some (rrd {settings.Node.Rrd.TimeFrame} {settings.Node.Rrd.Consolidation})")],
                           true,
                           false);
        }

        if (rrdList.Any(a => a.PressureIoFull > 0))
        {
            CheckThreshold(settings.Node.Rrd.Pressure.IoFull,
                           "WN0032",
                           DiagnosticResultContext.Node,
                           "Pressure",
                           [new ThresholdDataPoint(rrdList.Average(a => a.PressureIoFull),
                                                   0d,
                                                   id,
                                                   $"PSI I/O full (rrd {settings.Node.Rrd.TimeFrame} {settings.Node.Rrd.Consolidation})")],
                           true,
                           false);
        }

        if (rrdList.Any(a => a.PressureMemoryFull > 0))
        {
            CheckThreshold(settings.Node.Rrd.Pressure.MemoryFull,
                           "WN0033",
                           DiagnosticResultContext.Node,
                           "Pressure",
                           [new ThresholdDataPoint(rrdList.Average(a => a.PressureMemoryFull),
                                                   0d,
                                                   id,
                                                   $"PSI Memory full (rrd {settings.Node.Rrd.TimeFrame} {settings.Node.Rrd.Consolidation})")],
                           true,
                           false);
        }

        // Health score for nodes: 100 - (cpu*0.4 + ram*0.4 + disk*0.2)
        var nodeCpuPct = rrdList.Average(a => a.CpuUsagePercentage) * 100.0;

        var nodeRamPct = rrdList.Any(a => a.MemorySize > 0)
                            ? rrdList.Where(a => a.MemorySize > 0).Average(a => (double)a.MemoryUsage / a.MemorySize * 100.0)
                            : 0.0;

        var nodeDiskPct = rrdList.Any(a => a.RootSize > 0)
                            ? rrdList.Where(a => a.RootSize > 0).Average(a => a.RootUsage / a.RootSize * 100.0)
                            : 0.0;

        var nodeWeightedLoad = (nodeCpuPct * 0.4) + (nodeRamPct * 0.4) + (nodeDiskPct * 0.2);
        CheckHealthScore(settings.Node.HealthScore,
                         DiagnosticResultContext.Node,
                         id,
                         nodeWeightedLoad);
    }

    // Data-integrity / storage-health controls — used for ZFS, SMART, LVM-thin metadata.
    // Subset of the RESIL family that focuses on disk-level integrity rather than full HA.
    private static readonly ComplianceMapping[] _storageIntegrityControls =
    [
        ComplianceControls.Iso27001.A_5_30,
        ComplianceControls.Iso27001.A_8_16,
        ComplianceControls.Dora.Art_11,
        ComplianceControls.Gdpr.Art_32_1_b,
        ComplianceControls.Ens.OP_CONT_4,
        ComplianceControls.C5.BCM_03,
        ComplianceControls.Soc2.A1_2,
        ComplianceControls.Nist80053.CP_10,
        ComplianceControls.Iso27017.CLD_6_3_1,
        ComplianceControls.Cis.C_11,
        ComplianceControls.NistCsf.PR_IR_04,
        ComplianceControls.NistCsf.PR_DS_11,
        ComplianceControls.Acn.ID_IM_04,
        ComplianceControls.Acn.DE_CM_01,
        ComplianceControls.Iso22301.C_8_3_5,
        ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
        ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
    ];

    private void CheckZfsChildren(string id,
                                  string poolName,
                                  IEnumerable<NodeDiskZfsDetail.Child> children)
    {
        foreach (var child in children ?? [])
        {
            // vdev not ONLINE = faulted, removed, unavail, degraded
            if (!string.IsNullOrWhiteSpace(child.State))
            {
                CreateResult(
                    // Hot spares are AVAIL (idle) or INUSE (standing in for a failed disk): both expected.
                    isOk: child.State is "ONLINE" or "AVAIL" or "INUSE",
                    id: id,
                    errorCode: "CN0012",
                    subContext: "Zfs",
                    context: DiagnosticResultContext.Node,
                    gravityKo: DiagnosticResultGravity.Critical,
                    descriptionKo: $"ZFS pool '{poolName}' vdev '{child.Name}' is {child.State}{(string.IsNullOrWhiteSpace(child.Msg) ? "" : $": {child.Msg}")}",
                    descriptionOk: $"ZFS pool '{poolName}' vdev '{child.Name}' is ONLINE",
                    compliance: _storageIntegrityControls);
            }

            // I/O errors on vdev
            CreateResult(
                isOk: child.Read == 0 && child.Write == 0 && child.Checksum == 0,
                id: id,
                errorCode: "WN0025",
                subContext: "Zfs",
                context: DiagnosticResultContext.Node,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"ZFS pool '{poolName}' vdev '{child.Name}' has I/O errors (read:{child.Read} write:{child.Write} cksum:{child.Checksum})",
                descriptionOk: $"ZFS pool '{poolName}' vdev '{child.Name}' has no I/O errors",
                compliance: _storageIntegrityControls);

            // Recurse into nested vdevs (mirrors, raidz groups)
            CheckZfsChildren(id, poolName, child.Children);
        }
    }

    private async Task CheckNodeDiskAsync(PveClient.PveNodes.PveNodeItem nodeApi,
                                          Settings settings,
                                          string id,
                                          NodeFetchData fetch)
    {
        #region Disks
        // S.M.A.R.T. status: anything other than PASSED/OK indicates a failing or failed disk
        var disksAll = fetch.Disks;

        CreateResultPerItem(
            // UNKNOWN: PVE cannot read S.M.A.R.T. (disk behind a RAID controller or a USB bridge) —
            // nothing to judge, not a failing disk.
            items: disksAll.Where(a => !string.Equals(a.Health, "UNKNOWN", StringComparison.OrdinalIgnoreCase)).ToList(),
            isItemOk: a => a.Health == "PASSED" || a.Health == "OK",
            itemId: _ => id,
            itemDescriptionKo: a => $"Disk '{a.DevPath}' S.M.A.R.T. status problem ({a.Health})",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "All disks report a healthy S.M.A.R.T. status",
            errorCode: "WN0016",
            subContext: "S.M.A.R.T.",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: _storageIntegrityControls);

        // SSD wearout reported as N/A means the drive doesn't expose wear data — worth investigating
        CreateResultPerItem(
            items: disksAll.Where(a => a.IsSsd).ToList(),
            isItemOk: a => a.Wearout != "N/A",
            itemId: _ => id,
            itemDescriptionKo: a => $"Disk ssd '{a.DevPath}' wearout not valid.",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "All SSDs expose valid wearout data",
            errorCode: "WN0017",
            subContext: "SSD Wearout",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);

        // SSD wearout percentage above threshold (100 - wearout = wear consumed)
        CheckThreshold(settings.Node.Smart.SsdWearout,
                       "WN0018",
                       DiagnosticResultContext.Node,
                       "SSD Wearout",
                       disksAll.Where(a => a.IsSsd && a.Wearout != "N/A")
                               .Select(a => new ThresholdDataPoint(100.0 - Convert.ToDouble(a.Wearout), 0d, id, $"SSD '{a.DevPath}'")),
                       true,
                       false);

        // Detailed S.M.A.R.T. attribute checks — one API call per disk, disabled by default
        if (settings.Node.Smart.Enabled)
        {
            var smartResults = await RunParallelAsync(disksAll.Where(a => !string.IsNullOrWhiteSpace(a.DevPath)),
                                                      d => nodeApi.Disks.Smart.GetAsync(disk: d.DevPath)
                                                                  .ToSafeSingle(_result, id, DiagnosticResultContext.Node,
                                                                                $"S.M.A.R.T. data of disk '{d.DevPath}' on node '{fetch.Item.Node}'"));

            foreach (var (disk, smart) in disksAll.Where(a => !string.IsNullOrWhiteSpace(a.DevPath)).ToList().Zip(smartResults))
            {
                if (smart?.Attributes == null) { continue; }

                foreach (var attr in smart.Attributes)
                {
                    switch (attr.Id)
                    {
                        // Temperature (ID 194) or Airflow Temperature (ID 190)
                        case "194" or "190" when settings.Node.Smart.Temperature.Warning > 0:
                            if (int.TryParse(attr.Raw?.Split(' ')[0], out var temp) && temp > 0)
                            {
                                var tempGravity = temp >= settings.Node.Smart.Temperature.Critical
                                                    ? DiagnosticResultGravity.Critical
                                                    : temp >= settings.Node.Smart.Temperature.Warning
                                                        ? DiagnosticResultGravity.Warning
                                                        : DiagnosticResultGravity.Info;

                                if (tempGravity != DiagnosticResultGravity.Info)
                                {
                                    CreateResult(
                                        isOk: false,
                                        id: id,
                                        errorCode: tempGravity == DiagnosticResultGravity.Critical ? "CN0007" : "WN0019",
                                        subContext: "S.M.A.R.T.",
                                        context: DiagnosticResultContext.Node,
                                        gravityKo: tempGravity,
                                        descriptionKo: $"Disk '{disk.DevPath}' temperature {temp}°C exceeds threshold",
                                        descriptionOk: "",
                                        compliance: _storageIntegrityControls);
                                }
                            }
                            break;

                        // Reallocated sectors (ID 5) — non-zero = sectors remapped due to errors
                        case "5" when int.TryParse(attr.Raw?.Split(' ')[0], out var val5) && val5 > 0:
                            CreateResult(
                                isOk: false,
                                id: id,
                                errorCode: "WN0020",
                                subContext: "S.M.A.R.T.",
                                context: DiagnosticResultContext.Node,
                                gravityKo: DiagnosticResultGravity.Warning,
                                descriptionKo: $"Disk '{disk.DevPath}' has {val5} reallocated sector(s) — disk may be failing",
                                descriptionOk: "",
                                compliance: _storageIntegrityControls);
                            break;

                        // Current pending sectors (ID 197) — unstable sectors waiting to be remapped
                        case "197" when int.TryParse(attr.Raw?.Split(' ')[0], out var val197) && val197 > 0:
                            CreateResult(
                                isOk: false,
                                id: id,
                                errorCode: "CN0008",
                                subContext: "S.M.A.R.T.",
                                context: DiagnosticResultContext.Node,
                                gravityKo: DiagnosticResultGravity.Critical,
                                descriptionKo: $"Disk '{disk.DevPath}' has {val197} pending sector(s) — imminent data loss risk",
                                descriptionOk: "",
                                compliance: _storageIntegrityControls);
                            break;

                        // Offline uncorrectable sectors (ID 198)
                        case "198" when int.TryParse(attr.Raw?.Split(' ')[0], out var val198) && val198 > 0:
                            CreateResult(
                                isOk: false,
                                id: id,
                                errorCode: "CN0009",
                                subContext: "S.M.A.R.T.",
                                context: DiagnosticResultContext.Node,
                                gravityKo: DiagnosticResultGravity.Critical,
                                descriptionKo: $"Disk '{disk.DevPath}' has {val198} offline uncorrectable sector(s)",
                                descriptionOk: "",
                                compliance: _storageIntegrityControls);
                            break;

                        // UDMA CRC errors (ID 199) — cable or controller issue
                        case "199" when int.TryParse(attr.Raw?.Split(' ')[0], out var val199) && val199 > 0:
                            CreateResult(
                                isOk: false,
                                id: id,
                                errorCode: "WN0021",
                                subContext: "S.M.A.R.T.",
                                context: DiagnosticResultContext.Node,
                                gravityKo: DiagnosticResultGravity.Warning,
                                descriptionKo: $"Disk '{disk.DevPath}' has {val199} UDMA CRC error(s) — check cable/controller",
                                descriptionOk: "",
                                compliance: _storageIntegrityControls);
                            break;

                        // Reported uncorrectable errors (ID 187)
                        case "187" when int.TryParse(attr.Raw?.Split(' ')[0], out var val187) && val187 > 0:
                            CreateResult(
                                isOk: false,
                                id: id,
                                errorCode: "WN0022",
                                subContext: "S.M.A.R.T.",
                                context: DiagnosticResultContext.Node,
                                gravityKo: DiagnosticResultGravity.Warning,
                                descriptionKo: $"Disk '{disk.DevPath}' has {val187} reported uncorrectable error(s)",
                                descriptionOk: "",
                                compliance: _storageIntegrityControls);
                            break;
                    }
                }
            }
        }
        #endregion

        #region Zfs
        // ZFS pool health: anything other than ONLINE means degraded/faulted pool
        var zfsList = fetch.ZfsList;
        CreateResultPerItem(
            items: zfsList,
            isItemOk: a => a.Health == "ONLINE",
            itemId: _ => id,
            itemDescriptionKo: a => $"Zfs '{a.Name}' health problem {a.Health}",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "All ZFS pools are ONLINE",
            errorCode: "CN0010",
            subContext: "Zfs",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance: _storageIntegrityControls);

        // ZFS pool usage above storage threshold
        CheckThreshold(settings.Storage.Threshold,
                       "WN0044",
                       DiagnosticResultContext.Storage,
                       "Zfs",
                       zfsList.Select(a => new ThresholdDataPoint(Convert.ToDouble(a.Alloc),
                                                                  Convert.ToDouble(a.Size),
                                                                  $"{id} ({a.Name})",
                                                                  $"Zfs '{a.Name}'")),
                       false,
                       true);

        // Detailed ZFS checks: pool errors and vdev state — one API call per pool
        if (settings.Node.NodeStorage.ZfsDetail && zfsList.Any())
        {
            var zfsDetails = await RunParallelAsync(zfsList, zfs => nodeApi.Disks.Zfs[zfs.Name].GetAsync()
                                                                           .ToSafeSingle(_result, id, DiagnosticResultContext.Node,
                                                                                         $"ZFS pool '{zfs.Name}' on node '{fetch.Item.Node}'"));
            foreach (var (zfs, detail) in zfsList.Zip(zfsDetails))
            {
                if (detail == null) { continue; }

                // Pool-level errors string (e.g. "1 data errors, use '-v' for a list")
                if (!string.IsNullOrWhiteSpace(detail.Errors))
                {
                    CreateResult(
                        isOk: detail.Errors.Equals("No known data errors", StringComparison.OrdinalIgnoreCase),
                        id: id,
                        errorCode: "WN0024",
                        subContext: "Zfs",
                        context: DiagnosticResultContext.Node,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: $"ZFS pool '{zfs.Name}' has errors: {detail.Errors}",
                        descriptionOk: $"ZFS pool '{zfs.Name}' has no known data errors",
                        compliance: _storageIntegrityControls);
                }

                // vdev state check — recurse through children
                CheckZfsChildren(id, zfs.Name, detail.Children);
            }
        }
        #endregion

        #region LVM-thin metadata
        // LVM-thin metadata pool full causes silent data corruption — check before it's too late
        if (settings.Node.NodeStorage.LvmThinMetadata)
        {
            var lvmThinList = fetch.LvmThinList;
            foreach (var lv in lvmThinList.Where(a => a.MetadataSize > 0))
            {
                var metaPct = (double)lv.MetadataUsed / lv.MetadataSize * 100.0;
                if (metaPct >= 90)
                {
                    CreateResult(
                        isOk: false,
                        id: id,
                        errorCode: metaPct >= 95 ? "CN0013" : "WN0026",
                        subContext: "LvmThin",
                        context: DiagnosticResultContext.Node,
                        gravityKo: metaPct >= 95 ? DiagnosticResultGravity.Critical : DiagnosticResultGravity.Warning,
                        descriptionKo: $"LVM-thin '{lv.Vg}/{lv.Lv}' metadata usage {metaPct:F1}% — metadata full causes data corruption",
                        descriptionOk: "",
                        compliance: _storageIntegrityControls);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Packages installed on both nodes with a different version, as "package X vs Y". Kernel image
    /// packages are left out: each kernel version is its own package, and old kernels kept on one
    /// node only are normal (the running and newest kernel are checked by WN0013).
    /// </summary>
    internal static List<string> PackageVersionDifferences(IEnumerable<NodeAptVersion> mine, IEnumerable<NodeAptVersion> other)
    {
        var otherByPackage = other.Where(a => !string.IsNullOrWhiteSpace(a.Package))
                                  .GroupBy(a => a.Package)
                                  .ToDictionary(g => g.Key, g => g.First().Version);
        return [.. mine.Where(a => !string.IsNullOrWhiteSpace(a.Package) && !_kernelImagePackage.IsMatch(a.Package))
                       .Where(a => otherByPackage.TryGetValue(a.Package, out var v) && v != a.Version)
                       .Select(a => $"{a.Package} {a.Version} vs {otherByPackage[a.Package]}")
                       .Distinct()
                       .Order()];
    }

    /// <summary>
    /// Entries of an /etc/hosts file, normalised for comparison: comments and blank lines dropped,
    /// whitespace collapsed. Two files with the same entries differ only cosmetically.
    /// </summary>
    internal static HashSet<string> HostsEntries(IEnumerable<string> lines)
        => [.. lines.Select(l => (l ?? "").Split('#')[0])
                    .Select(l => string.Join(' ', l.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)))
                    .Where(l => l.Length > 0)];

    // Kernel image packages: proxmox-kernel-6.8.12-43-pve-signed, proxmox-kernel-6.2.16-19-pve, pve-kernel-5.15.158-2-pve.
    private static readonly Regex _kernelImagePackage = new(@"^(?:proxmox|pve)-kernel-(\d+\.\d+\.\d+-\d+)-pve(?:-signed)?$", RegexOptions.Compiled);

    /// <summary>
    /// The newest installed kernel image newer than the running one (e.g. <c>6.8.12-43-pve</c>),
    /// or null when the node already runs the newest. <paramref name="runningKernel"/> is the
    /// uname release (<c>6.8.12-20-pve</c>). A kernel pinned with proxmox-boot-tool is not visible here.
    /// </summary>
    internal static string? NewerInstalledKernel(string runningKernel, IEnumerable<string> installedPackages)
    {
        var running = runningKernel.EndsWith("-pve", StringComparison.Ordinal) ? runningKernel[..^4] : runningKernel;
        var newest = installedPackages.Select(p => _kernelImagePackage.Match(p ?? ""))
                                      .Where(m => m.Success)
                                      .Select(m => m.Groups[1].Value)
                                      .OrderDescending(Comparer<string>.Create(DebianVersion.Compare))
                                      .FirstOrDefault();
        return newest != null && DebianVersion.Compare(newest, running) > 0 ? $"{newest}-pve" : null;
    }

    /// <summary>
    /// Interfaces that carry traffic: bridge ports, bond slaves, OVS ports and bond members, the raw
    /// device under a VLAN interface, and any interface holding an IP of its own. A VLAN name such as
    /// <c>eno1.100</c> also marks its parent <c>eno1</c> as used.
    /// </summary>
    internal static HashSet<string> UsedInterfaces(IEnumerable<NodeNetwork> networks)
    {
        var used = new HashSet<string>();
        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return; }
            used.Add(name);
            var dot = name.IndexOf('.');
            if (dot > 0) { used.Add(name[..dot]); }
        }

        foreach (var net in networks)
        {
            foreach (var name in $"{net.BridgePorts} {net.Slaves} {net.OvsPorts} {net.OvsBonds}".Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                Add(name);
            }

            if (net.Type == "vlan") { Add(string.IsNullOrWhiteSpace(net.VlanRawDevice) ? net.Interface : net.VlanRawDevice); }
            if (!string.IsNullOrWhiteSpace(net.OvsBridge)) { Add(net.Interface); }
            if (!string.IsNullOrWhiteSpace(net.Cidr)
                || !string.IsNullOrWhiteSpace(net.Address)
                || !string.IsNullOrWhiteSpace(net.Cidr6)
                || !string.IsNullOrWhiteSpace(net.Address6))
            {
                Add(net.Interface);
            }
        }
        return used;
    }

    /// <summary>
    /// VLAN ids a guest NIC uses (its <c>tag</c> plus its <c>trunks</c>) that the bridge's
    /// <c>bridge-vids</c> does not allow. VLAN 1 is always allowed: it is the bridge's default PVID.
    /// </summary>
    internal static IReadOnlyList<int> VlansOutsideBridge(int? tag, string? trunks, IReadOnlyList<(int From, int To)> allowed)
    {
        var used = VlanIds.Expand(VlanIds.Parse(trunks)).ToList();
        if (tag.HasValue) { used.Add(tag.Value); }
        return [.. used.Where(id => id != 1 && !VlanIds.Contains(allowed, id)).Distinct().Order()];
    }

    /// <summary>
    /// Bridges a guest uses that exist on the guest's own node but not on every other node in
    /// <paramref name="bridgesByNode"/>. One entry per (node, bridge) with the guests using it.
    /// Bridges absent on the guest's own node are ignored — they may be SDN vnets.
    /// </summary>
    internal static List<(string Node, string Bridge, List<long> VmIds, List<string> MissingOn)> FindBridgesMissingOnPeers(
        IReadOnlyDictionary<string, HashSet<string>> bridgesByNode,
        IEnumerable<(string Node, long VmId, IEnumerable<string> Bridges)> guests)
        => [.. guests.Where(g => bridgesByNode.ContainsKey(g.Node))
                     .SelectMany(g => g.Bridges.Where(b => !string.IsNullOrWhiteSpace(b) && bridgesByNode[g.Node].Contains(b))
                                               .Distinct()
                                               .Select(b => (g.Node, Bridge: b, g.VmId)))
                     .GroupBy(x => (x.Node, x.Bridge))
                     .Select(g => (g.Key.Node,
                                   g.Key.Bridge,
                                   VmIds: g.Select(x => x.VmId).Distinct().Order().ToList(),
                                   MissingOn: bridgesByNode.Where(kv => kv.Key != g.Key.Node && !kv.Value.Contains(g.Key.Bridge))
                                                           .Select(kv => kv.Key)
                                                           .Order()
                                                           .ToList()))
                     .Where(x => x.MissingOn.Count > 0)
                     .OrderBy(x => x.Node)
                     .ThenBy(x => x.Bridge)];
}
