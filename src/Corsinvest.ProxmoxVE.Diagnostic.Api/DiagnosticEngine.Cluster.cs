/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task<bool> CheckClusterAsync(int pveMajorVersion)
    {
        var clusterConfigNodesTask = client.Cluster.Config.Nodes.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster config nodes");
        var clusterBackupTask = client.Cluster.Backup.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster backup jobs");
        await Task.WhenAll(clusterConfigNodesTask, clusterBackupTask);
        var clusterConfigNodes = clusterConfigNodesTask.Result;
        var hasCluster = clusterConfigNodes.Any();
        _clusterBackups = clusterBackupTask.Result;

        await CheckClusterBackupAsync();

        if (hasCluster)
        {
            var clusterStatus = await client.Cluster.Status.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster status");
            await CheckClusterQuorumAndHaAsync(clusterStatus, pveMajorVersion);
            await CheckClusterHaAndReplicationAsync();
        }

        await CheckClusterPoolsAsync();
        await CheckClusterFirewallAsync();
        await CheckClusterAccessAsync();
        await CheckClusterLogAsync();
        await CheckClusterMetricsAsync();

        // Single-node "cluster": HA, quorum and replication are not effective.
        // Counted independently of hasCluster so even a non-clustered single host gets the hint.
        var nodeCount = _resources.Count(a => a.ResourceType == ClusterResourceType.Node);
        CreateResult(
            isOk: nodeCount > 1,
            id: "cluster",
            errorCode: "IC0017",
            subContext: "Topology",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: "Cluster has a single node — HA, quorum and replication provide no real protection",
            descriptionOk: $"Cluster has {nodeCount} nodes",
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Nis2.Art_21_c,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.Cis.C_11,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.NistCsf.RC_RP_01,
                ComplianceControls.Iso27017.CLD_6_3_1,
                ComplianceControls.Ens.OP_CONT_2,
                ComplianceControls.Nist80053.CP_10,
                ComplianceControls.Soc2.A1_2,
                ComplianceControls.C5.BCM_03,
                ComplianceControls.Ens.MP_S_1,
                ComplianceControls.Soc2.A1_1,
                ComplianceControls.C5.PI_02,
            ]);

        return hasCluster;
    }

    private async Task CheckClusterMetricsAsync()
    {
        // External metric server (InfluxDB / Graphite) — required for persistent long-term
        // monitoring beyond the in-node RRD. Auditors want historical evidence of system
        // behaviour for incident investigation; RRD data is short-lived and lost on reboot.
        var servers = (await client.Cluster.Metrics.Server.GetAsync().ToSafeEnum(_result, "cluster/metrics", DiagnosticResultContext.Cluster, "cluster metric servers")).ToList();

        ComplianceMapping[] observabilityControls =
        [
            ComplianceControls.Iso27001.A_8_15,
            ComplianceControls.Iso27001.A_8_16,
            ComplianceControls.Nis2.Art_21_f,
            ComplianceControls.Dora.Art_10,
            ComplianceControls.Gdpr.Art_32_1_d,
            ComplianceControls.Ens.OP_EXP_8,
            ComplianceControls.Nist80053.AU_12,
            ComplianceControls.Soc2.CC7_2,
            ComplianceControls.Iso27018.A_12_4_1,
            ComplianceControls.C5.OPS_09,
            ComplianceControls.Ens.OP_MON_1,
            ComplianceControls.Nist80053.SI_4,
            ComplianceControls.C5.OPS_10,
        ];

        // IC0018 — no metric server configured at all.
        CreateResult(
            isOk: servers.Count > 0,
            id: "cluster/metrics",
            errorCode: "IC0018",
            subContext: "Metrics",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: "No external metric server configured — long-term monitoring relies only on volatile RRD data",
            descriptionOk: $"{servers.Count} metric server(s) configured at cluster level",
            compliance: observabilityControls);
        if (servers.Count == 0) { return; }

        // IC0019 — servers are configured but every one of them is disabled.
        // The 'disable' field is 1 when the server is off; treat missing/0 as enabled.
        bool IsEnabled(object server)
        {
            var disableProp = server.GetType().GetProperty("Disable");
            var v = disableProp?.GetValue(server);
            return v switch
            {
                null => true,
                bool b => !b,
                int i => i == 0,
                long l => l == 0,
                _ => !v.ToString()!.Equals("1", StringComparison.Ordinal),
            };
        }

        var enabledCount = servers.Count(s => IsEnabled(s!));
        CreateResult(
            isOk: enabledCount > 0,
            id: "cluster/metrics",
            errorCode: "IC0019",
            subContext: "Metrics",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: $"All {servers.Count} configured metric server(s) are disabled — no metrics are being exported",
            descriptionOk: $"{enabledCount} of {servers.Count} metric server(s) are enabled",
            compliance: observabilityControls);
    }

    private async Task CheckClusterLogAsync()
    {
        // 200 recent entries — enough to catch a burst of errors without dragging the whole journal.
        var entries = await client.Cluster.Log.GetAsync(max: 200).ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster log");

        // syslog priorities 0..3 are emerg/alert/crit/err — anything above is warning/info/debug.
        var errors = entries.Count(e => e.Severity >= 0 && e.Severity <= 3);
        CreateResult(
            isOk: errors < 10,
            id: "cluster",
            errorCode: "IC0015",
            subContext: "Log",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: $"Cluster log has {errors} error-level entries in the last 200 — review the journal",
            descriptionOk: $"Cluster log has {errors} error-level entries in the last 200 (below threshold)",
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
                ComplianceControls.Nist80053.AU_12,
                ComplianceControls.Soc2.CC7_2,
                ComplianceControls.Iso27018.A_12_4_1,
                ComplianceControls.C5.OPS_09,
                ComplianceControls.Cis.C_8,
                ComplianceControls.NistCsf.DE_CM_01,
                ComplianceControls.NistCsf.DE_CM_03,
                ComplianceControls.Iso27017.CLD_12_4_5,
            ]);
    }

    private async Task CheckClusterBackupAsync()
    {
        var backupList = _clusterBackups.ToList();

        // No backup jobs defined at all — entire cluster has no automated backup
        CreateResult(
            isOk: backupList.Count > 0,
            id: "cluster/backup",
            errorCode: "WC0001",
            subContext: "Backup",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: "No backup job configured — no automated backup for any VM/CT",
            descriptionOk: $"{backupList.Count} backup job(s) configured at cluster level",
            compliance:
            [
                ComplianceControls.Iso27001.A_8_13,
                ComplianceControls.Nis2.Art_21_c,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.Gdpr.Art_32_1_c,
                ComplianceControls.AgId.ABSC_10_1,
                ComplianceControls.Ens.MP_INFO_6,
                ComplianceControls.Nist80053.CP_9,
                ComplianceControls.Soc2.A1_2,
                ComplianceControls.Iso27018.A_12_3_1,
                ComplianceControls.C5.OPS_21,
                ComplianceControls.AgId.ABSC_10_3,
                ComplianceControls.AgId.ABSC_10_4,
                ComplianceControls.Cis.C_11,
                ComplianceControls.NistCsf.PR_DS_11,
                ComplianceControls.NistCsf.RC_RP_01,
            ]);
        if (backupList.Count == 0) { return; }

        // Backup jobs without compression waste storage space (zstd recommended).
        // PBS targets are skipped: Proxmox Backup Server compresses chunks server-side
        // (always zstd) and exposes no 'compress' option on the job.
        CreateResultPerItem(
            items: backupList.Where(a => a.Enabled && !IsPbsStorage(a.Storage)).ToList(),
            isItemOk: a => !string.IsNullOrWhiteSpace(a.Compress),
            itemId: a => $"cluster/backup/{a.Id}",
            itemDescriptionKo: a => $"Backup job '{a.Id}' has no compression configured",
            aggregatedIdOk: "cluster/backup",
            aggregatedDescriptionOk: _ => "All enabled non-PBS backup jobs have compression configured",
            errorCode: "IC0001",
            subContext: "Backup",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: []);

        // Backup compliance controls — reused for WC0002/WC0017/WC0018/IC0012.
        ComplianceMapping[] backupControls =
        [
        ];

        // Backup jobs without retention policy — storage will fill up indefinitely
        CreateResultPerItem(
            items: backupList.Where(a => a.Enabled).ToList(),
            isItemOk: a => a.ExtensionData?.ContainsKey("maxfiles") is true
                           || a.ExtensionData?.ContainsKey("prune-backups") is true,
            itemId: a => $"cluster/backup/{a.Id}",
            itemDescriptionKo: a => $"Backup job '{a.Id}' has no retention policy (maxfiles/prune) — storage will fill up",
            aggregatedIdOk: "cluster/backup",
            aggregatedDescriptionOk: _ => "All enabled backup jobs have a retention policy",
            errorCode: "WC0002",
            subContext: "Backup",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: backupControls);

        // Enabled backup job with no schedule: it will never run automatically.
        CreateResultPerItem(
            items: backupList.Where(a => a.Enabled).ToList(),
            isItemOk: a => !string.IsNullOrWhiteSpace(a.Schedule),
            itemId: a => $"cluster/backup/{a.Id}",
            itemDescriptionKo: a => $"Backup job '{a.Id}' is enabled but has no schedule — it will never run automatically",
            aggregatedIdOk: "cluster/backup",
            aggregatedDescriptionOk: _ => "All enabled backup jobs have a schedule",
            errorCode: "WC0017",
            subContext: "Backup",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: backupControls);

        // Disabled backup jobs: informational, often leftover configuration worth reviewing.
        CreateResultPerItem(
            items: backupList,
            isItemOk: a => a.Enabled,
            itemId: a => $"cluster/backup/{a.Id}",
            itemDescriptionKo: a => $"Backup job '{a.Id}' is currently disabled",
            aggregatedIdOk: "cluster/backup",
            aggregatedDescriptionOk: _ => "No disabled backup jobs",
            errorCode: "IC0012",
            subContext: "Backup",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: backupControls);

        // Cluster-wide task feed — used here for recent backup failures and below for task error rate.
        var clusterTasks = (await client.Cluster.Tasks.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster task feed")).ToList();

        // Recent vzdump task that did not complete successfully — backup likely failed.
        var vzdumpTasks = clusterTasks.Where(t => (t.Type?.StartsWith("vzdump", StringComparison.OrdinalIgnoreCase) ?? false)
                                                   && t.EndTime > 0)
                                       .ToList();
        CreateResultPerItem(
            items: vzdumpTasks,
            isItemOk: t => string.Equals(t.Status, "OK", StringComparison.OrdinalIgnoreCase),
            itemId: t => $"nodes/{t.Node}",
            itemDescriptionKo: t => $"Backup task on node '{t.Node}' by '{t.User}' ended with status '{t.Status}'",
            aggregatedIdOk: "cluster/backup",
            aggregatedDescriptionOk: _ => "No recent backup task failures",
            errorCode: "WC0018",
            subContext: "Backup",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: backupControls);

        // Overall task failure rate — sustained failures across the cluster usually indicate a systemic issue.
        var finishedTasks = clusterTasks.Where(t => t.EndTime > 0).ToList();
        if (finishedTasks.Count >= 10)
        {
            var failed = finishedTasks.Count(t => !string.Equals(t.Status, "OK", StringComparison.OrdinalIgnoreCase));
            var ratio = (double)failed / finishedTasks.Count;
            CreateResult(
                isOk: ratio < 0.10,
                id: "cluster",
                errorCode: "IC0016",
                subContext: "Tasks",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Info,
                descriptionKo: $"Cluster task failure rate is {ratio:P0} ({failed}/{finishedTasks.Count}) — investigate recurring errors",
                descriptionOk: $"Cluster task failure rate is {ratio:P0} ({failed}/{finishedTasks.Count})",
                compliance:
                [
                    ComplianceControls.Iso27001.A_8_15,
                    ComplianceControls.Iso27001.A_8_16,
                    ComplianceControls.Nis2.Art_21_f,
                    ComplianceControls.Dora.Art_10,
                    ComplianceControls.Gdpr.Art_32_1_d,
                    ComplianceControls.AgId.ABSC_5_2,
                    ComplianceControls.Ens.OP_EXP_8,
                    ComplianceControls.Nist80053.AU_12,
                    ComplianceControls.Soc2.CC7_2,
                    ComplianceControls.Iso27018.A_12_4_1,
                    ComplianceControls.C5.OPS_09,
                    ComplianceControls.Cis.C_8,
                    ComplianceControls.NistCsf.DE_CM_01,
                    ComplianceControls.NistCsf.DE_CM_03,
                    ComplianceControls.Iso27017.CLD_12_4_5,
                ]);
        }
    }

    private async Task CheckClusterHaAndReplicationAsync()
    {
        // Cluster without any HA resource configured — no automatic failover on node failure
        var haResourcesTask = client.Cluster.Ha.Resources.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "HA resources");
        var haStatusTask = client.Cluster.Ha.Status.Current.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "HA status");
        var replJobsTask = client.Cluster.Replication.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "replication jobs");
        await Task.WhenAll(haResourcesTask, haStatusTask, replJobsTask);

        // Cache the guest ids referenced by HA / enabled replication so per-guest checks don't re-walk them.
        foreach (var h in haResourcesTask.Result)
        {
            // Sid format is "<type>:<vmid>" — e.g. "vm:100", "ct:200".
            var parts = (h.Sid ?? "").Split(':');
            if (parts.Length == 2 && long.TryParse(parts[1], out var vmid)) { _haVmIds.Add(vmid); }
        }
        foreach (var r in replJobsTask.Result.Where(r => !r.Disable && !string.IsNullOrWhiteSpace(r.Guest)))
        {
            if (long.TryParse(r.Guest, out var vmid)) { _replicatedVmIds.Add(vmid); }
        }

        // No HA configured — without HA resources, guests won't automatically restart on node failure.
        // Emitted regardless of node count: a single-node host is itself non-compliant with the
        // resilience controls this check maps to (A.5.30, DORA Art.12). IC0017 reports the
        // single-node topology in addition to this finding.
        var haResourceCount = haResourcesTask.Result.Count;
        CreateResult(
            isOk: haResourceCount > 0,
            id: "cluster",
            errorCode: "IC0002",
            subContext: "HA",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: "No HA resources configured — VMs will not automatically restart on node failure",
            descriptionOk: $"{haResourceCount} HA resource(s) configured",
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.Iso27017.CLD_6_3_1,
            ]);

        // Resilience / business continuity controls.
        ComplianceMapping[] resilienceControls =
        [
            ComplianceControls.Nis2.Art_21_c,
            ComplianceControls.Ens.OP_CONT_2,
            ComplianceControls.Nist80053.CP_10,
            ComplianceControls.Soc2.A1_2,
            ComplianceControls.C5.BCM_03,
            ComplianceControls.Ens.MP_S_1,
            ComplianceControls.Soc2.A1_1,
            ComplianceControls.C5.PI_02,
        ];

        // HA service in error state — the resource is not running and will not be recovered automatically
        CreateResultPerItem(
            items: haStatusTask.Result.Where(a => a.Type == "service").ToList(),
            isItemOk: a => string.IsNullOrWhiteSpace(a.State)
                            || !a.State.Equals("error", StringComparison.OrdinalIgnoreCase),
            itemId: a => $"cluster/ha/{a.Sid}",
            itemDescriptionKo: a => $"HA resource '{a.Sid}' is in error state on node '{a.Node}' — manual recovery required",
            aggregatedIdOk: "cluster/ha",
            aggregatedDescriptionOk: _ => "No HA resource in error state",
            errorCode: "CC0005",
            subContext: "HA",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance: resilienceControls);

        // Cluster without any replication job — no storage redundancy between nodes.
        // Emitted regardless of node count: like IC0002, a single-node deployment is itself
        // non-compliant with the resilience controls this check maps to.
        CreateResult(
            isOk: replJobsTask.Result.Any(),
            id: "cluster",
            errorCode: "IC0003",
            subContext: "Replication",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: "No storage replication jobs configured — no redundant copy of VM data across nodes",
            descriptionOk: $"{replJobsTask.Result.Count} storage replication job(s) configured",
            compliance: resilienceControls);

        // Disabled replication job — the guest's data is no longer kept in sync on the target node
        CreateResultPerItem(
            items: replJobsTask.Result,
            isItemOk: a => !a.Disable,
            itemId: a => $"cluster/replication/{a.Id}",
            itemDescriptionKo: a => $"Replication job '{a.Id}' (guest {a.Guest} → {a.Target}) is disabled — data is no longer replicated",
            aggregatedIdOk: "cluster/replication",
            aggregatedDescriptionOk: _ => "No disabled replication jobs",
            errorCode: "WC0009",
            subContext: "Replication",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: resilienceControls);

        // Enabled replication job without a schedule — it will never run automatically
        CreateResultPerItem(
            items: replJobsTask.Result.Where(a => !a.Disable).ToList(),
            isItemOk: a => !string.IsNullOrWhiteSpace(a.Schedule),
            itemId: a => $"cluster/replication/{a.Id}",
            itemDescriptionKo: a => $"Replication job '{a.Id}' (guest {a.Guest} → {a.Target}) has no schedule — it will never run automatically",
            aggregatedIdOk: "cluster/replication",
            aggregatedDescriptionOk: _ => "All enabled replication jobs have a schedule",
            errorCode: "WC0010",
            subContext: "Replication",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: resilienceControls);
    }

    private async Task CheckClusterQuorumAndHaAsync(IEnumerable<ClusterStatus> clusterStatus,
                                                    int pveMajorVersion)
    {
        ComplianceMapping[] resilienceControls =
        [
            ComplianceControls.Iso27001.A_5_30,
            ComplianceControls.Nis2.Art_21_c,
            ComplianceControls.Dora.Art_12,
            ComplianceControls.Gdpr.Art_32_1_b,
            ComplianceControls.Ens.OP_CONT_2,
            ComplianceControls.Nist80053.CP_10,
            ComplianceControls.Soc2.A1_2,
            ComplianceControls.C5.BCM_03,
            ComplianceControls.Ens.MP_S_1,
            ComplianceControls.Soc2.A1_1,
            ComplianceControls.C5.PI_02,
        ];

        // Quorum lost means the cluster cannot make decisions — VMs may not start or migrate
        var clusterInfo = clusterStatus.FirstOrDefault(a => a.Type == "cluster");
        if (clusterInfo != null)
        {
            CreateResult(
                isOk: clusterInfo.Quorate != 0,
                id: "cluster",
                errorCode: "CC0001",
                subContext: "Quorum",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Critical,
                descriptionKo: "Cluster has lost quorum — VM operations may be blocked",
                descriptionOk: "Cluster has quorum",
                compliance: resilienceControls);

            // Corosync expected_votes must match the number of online nodes.
            // A mismatch means Corosync still expects votes from nodes that are gone,
            // which can prevent quorum even when all remaining nodes are online.
            if (clusterInfo.ExpectedVotes.HasValue)
            {
                var onlineCount = _resources.Count(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
                CreateResult(
                    isOk: clusterInfo.ExpectedVotes.Value == onlineCount,
                    id: "cluster",
                    errorCode: "CC0002",
                    subContext: "Quorum",
                    context: DiagnosticResultContext.Cluster,
                    gravityKo: DiagnosticResultGravity.Critical,
                    descriptionKo: $"Corosync expected_votes ({clusterInfo.ExpectedVotes.Value}) does not match online node count ({onlineCount}) — quorum may be unstable",
                    descriptionOk: $"Corosync expected_votes ({clusterInfo.ExpectedVotes.Value}) matches online node count",
                    compliance: resilienceControls);
            }
        }

        if (pveMajorVersion < 9)
        {
            // HA groups referencing nodes that are currently offline — failover may not work
            var onlineNodeNames = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                            .Select(a => a.Node)
                                            .ToHashSet();
            var haGroups = (await client.Cluster.Ha.Groups.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "HA groups")).ToList();
            CreateResultPerItem(
                items: haGroups.Where(g => !string.IsNullOrWhiteSpace(g.Nodes)).ToList(),
                isItemOk: g => g.Nodes.Split(',')
                                .Select(n => n.Split(':')[0].Trim())
                                .All(n => onlineNodeNames.Contains(n)),
                itemId: g => $"cluster/ha/groups/{g.Group}",
                itemDescriptionKo: g =>
                {
                    var offline = g.Nodes.Split(',').Select(n => n.Split(':')[0].Trim())
                                   .Where(n => !onlineNodeNames.Contains(n));
                    return $"HA group '{g.Group}' has offline node(s): {string.Join(", ", offline)}";
                },
                aggregatedIdOk: "cluster/ha/groups",
                aggregatedDescriptionOk: _ => "All HA groups reference only online nodes",
                errorCode: "CC0003",
                subContext: "HA",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Critical,
                compliance: resilienceControls);
        }
    }

    // Pools with no VMs and no storage assigned serve no purpose
    private async Task CheckClusterPoolsAsync()
    {
        CreateResultPerItem(
            items: (await client.Pools.GetAsync().ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "pools")).ToList(),
            isItemOk: a => _resources.Any(r => r.Pool == a.Id),
            itemId: a => $"cluster/pool/{a.Id}",
            itemDescriptionKo: a => $"Pool '{a.Id}' is empty (no VMs or storage assigned)",
            aggregatedIdOk: "cluster/pools",
            aggregatedDescriptionOk: _ => "No empty pools",
            errorCode: "IC0004",
            subContext: "Pool",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_15,
                ComplianceControls.Nis2.Art_21_i,
                ComplianceControls.Gdpr.Art_5_1_f,
                ComplianceControls.AgId.ABSC_5_1,
                ComplianceControls.Ens.OP_ACC_2,
                ComplianceControls.Nist80053.AC_6,
                ComplianceControls.Soc2.CC6_3,
                ComplianceControls.C5.IDM_09,
                ComplianceControls.NistCsf.ID_AM_02,
            ]);
    }

    private async Task CheckClusterFirewallAsync()
    {
        var clusterFwOptions = await client.Cluster.Firewall.Options.GetAsync()
                                     .ToSafeSingle(_result, "cluster", DiagnosticResultContext.Cluster, "cluster firewall options");
        // If the fetch failed we already recorded a finding — nothing else this method can do.
        if (clusterFwOptions == null) { return; }

        // Cluster firewall completely disabled — no traffic filtering at all
        CreateResult(
            isOk: clusterFwOptions.Enable,
            id: "cluster",
            errorCode: "WC0003",
            subContext: "Firewall",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: "Cluster firewall is disabled — no traffic filtering is active",
            descriptionOk: "Cluster firewall is enabled",
            compliance:
            [
                ComplianceControls.Iso27001.A_8_20,
                ComplianceControls.Nis2.Art_21_e,
                ComplianceControls.PciDss.R_1_2,
                ComplianceControls.AgId.ABSC_8_1,
                ComplianceControls.Ens.MP_COM_1,
                ComplianceControls.Nist80053.SC_7,
                ComplianceControls.Soc2.CC6_6,
                ComplianceControls.C5.KOS_01,
                ComplianceControls.Cis.C_12,
                ComplianceControls.Cis.C_13,
                ComplianceControls.NistCsf.PR_IR_01,
                ComplianceControls.Iso27017.CLD_13_1_4,
            ]);
        if (!clusterFwOptions.Enable) { return; }

        // Firewall / network security controls.
        ComplianceMapping[] firewallControls =
        [
        ];

        // Inbound and outbound policies should be DROP — ACCEPT allows unmatched traffic through
        CreateResultPerItem(
            items: new[] {
                (Policy: clusterFwOptions.PolicyIn, Direction: "inbound"),
                (Policy: clusterFwOptions.PolicyOut, Direction: "outbound"),
            }.Where(p => !string.IsNullOrWhiteSpace(p.Policy)).ToList(),
            isItemOk: p => p.Policy.Equals("DROP", StringComparison.OrdinalIgnoreCase),
            itemId: _ => "cluster",
            itemDescriptionKo: p => $"Cluster firewall {p.Direction} policy is '{p.Policy}' — recommended value is DROP",
            aggregatedIdOk: "cluster",
            aggregatedDescriptionOk: _ => "Cluster firewall inbound and outbound policies are both DROP",
            errorCode: "WC0004",
            subContext: "Firewall",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: firewallControls);

        // Firewall enabled at cluster level but disabled on individual nodes — inconsistent protection.
        // The per-node fetch is wrapped: a single faulty node degrades to null and is silently skipped
        // (the failure was already recorded as a finding by ToSafeSingle).
        var onlineNodes = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline).ToList();
        var nodeFwResults = await RunParallelAsync(onlineNodes,
            node => client.Nodes[node.Node].Firewall.Options.GetAsync()
                          .ToSafeSingle(_result, node.GetWebUrl(), DiagnosticResultContext.Node, $"firewall options on node '{node.Node}'"));
        CreateResultPerItem(
            items: onlineNodes.Zip(nodeFwResults).Where(p => p.Second != null).ToList(),
            isItemOk: p => p.Second!.Enable,
            itemId: p => p.First.GetWebUrl(),
            itemDescriptionKo: p => $"Cluster firewall is enabled but node '{p.First.Node}' has firewall disabled",
            aggregatedIdOk: "cluster",
            aggregatedDescriptionOk: _ => "All online nodes have the firewall enabled",
            errorCode: "WN0001",
            subContext: "Firewall",
            context: DiagnosticResultContext.Node,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: firewallControls);

        // Cluster firewall rules with source or dest 0.0.0.0/0 — overly permissive
        var clusterRules = (await client.Cluster.Firewall.Rules.GetAsync().ToSafeEnum(_result, "cluster/firewall/rules", DiagnosticResultContext.Cluster, "cluster firewall rules")).ToList();
        CreateResultPerItem(
            items: clusterRules.Where(r => r.Enable).ToList(),
            isItemOk: r => r.Source != "0.0.0.0/0" && r.Dest != "0.0.0.0/0",
            itemId: _ => "cluster/firewall/rules",
            itemDescriptionKo: r => $"Firewall rule #{r.Positon} allows traffic from/to 0.0.0.0/0 — overly permissive",
            aggregatedIdOk: "cluster/firewall/rules",
            aggregatedDescriptionOk: _ => "No enabled firewall rule allows traffic from/to 0.0.0.0/0",
            errorCode: "WC0008",
            subContext: "Firewall",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: firewallControls);

        // Cluster firewall is enabled but no enabled rule has logging configured — no audit trail.
        // "nolog" or empty disables logging; anything else (warning, info, debug, …) is considered logging.
        var enabledRules = clusterRules.Where(r => r.Enable).ToList();
        if (enabledRules.Count > 0)
        {
            var anyLogging = enabledRules.Any(r => !string.IsNullOrWhiteSpace(r.Log)
                                                    && !string.Equals(r.Log, "nolog", StringComparison.OrdinalIgnoreCase));
            CreateResult(
                isOk: anyLogging,
                id: "cluster/firewall/rules",
                errorCode: "IC0013",
                subContext: "Firewall",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Info,
                descriptionKo: $"Cluster firewall has {enabledRules.Count} enabled rules but none have logging configured — no audit trail",
                descriptionOk: $"At least one of {enabledRules.Count} enabled firewall rules has logging configured",
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
                    ComplianceControls.Nist80053.AU_12,
                    ComplianceControls.Soc2.CC7_2,
                    ComplianceControls.Iso27018.A_12_4_1,
                    ComplianceControls.C5.OPS_09,
                    ComplianceControls.Cis.C_8,
                    ComplianceControls.NistCsf.DE_CM_01,
                    ComplianceControls.NistCsf.DE_CM_03,
                    ComplianceControls.Iso27017.CLD_12_4_5,
                ]);
        }

        // Many disabled rules cluster-wide — stale configuration accumulating noise.
        var disabledCount = clusterRules.Count(r => !r.Enable);
        CreateResult(
            isOk: disabledCount < 10,
            id: "cluster/firewall/rules",
            errorCode: "IC0014",
            subContext: "Firewall",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: $"Cluster firewall has {disabledCount} disabled rules — consider cleaning up stale configuration",
            descriptionOk: $"Cluster firewall has {disabledCount} disabled rules (below clutter threshold)",
            compliance: firewallControls);
    }

    private async Task CheckClusterAccessAsync()
    {
        // Local users (pam/pve realm) without expiration and tokens without expiration are a security risk
        var accessUsersTask = client.Access.Users.GetAsync().ToSafeEnum(_result, "access/users", DiagnosticResultContext.Cluster, "access users");
        var tfaEntriesTask = client.Access.Tfa.GetAsync().ToSafeEnum(_result, "access/tfa", DiagnosticResultContext.Cluster, "TFA entries");
        var aclsTask = client.Access.Acl.GetAsync().ToSafeEnum(_result, "access/acl", DiagnosticResultContext.Cluster, "ACL entries");
        var groupsTask = client.Access.Groups.GetAsync().ToSafeEnum(_result, "access/groups", DiagnosticResultContext.Cluster, "access groups");
        var rolesTask = client.Access.Roles.GetAsync().ToSafeEnum(_result, "access/roles", DiagnosticResultContext.Cluster, "access roles");
        var domainsTask = client.Access.Domains.GetAsync().ToSafeEnum(_result, "access/domains", DiagnosticResultContext.Cluster, "access domains");
        await Task.WhenAll(accessUsersTask, tfaEntriesTask, aclsTask, groupsTask, rolesTask, domainsTask);
        var accessUsers = accessUsersTask.Result;
        var tfaEntries = tfaEntriesTask.Result;
        var acls = aclsTask.Result;
        var groups = groupsTask.Result;
        var roles = rolesTask.Result;
        var domains = domainsTask.Result;

        var usersWithTfa = tfaEntries.Where(t => t.Entries?.Any() is true)
                                     .Select(t => t.UserId)
                                     .ToHashSet();

        // root@pam without TFA is a critical security risk — full access with a single password.
        var root = accessUsers.FirstOrDefault(u => u.Id == "root@pam" && u.Enable);
        // Check is "Ok" when root@pam is not enabled (so the check does not apply) OR TFA is set.
        CreateResult(
            isOk: root == null || usersWithTfa.Contains("root@pam"),
            id: "access/users/root@pam",
            errorCode: "CC0004",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Critical,
            descriptionKo: "root@pam has no TFA configured — full access protected only by password",
            descriptionOk: "root@pam has TFA configured",
            compliance:
            [
                ComplianceControls.Iso27001.A_5_17,
                ComplianceControls.Iso27001.A_8_5,
                ComplianceControls.Nis2.Art_21_j,
                ComplianceControls.Dora.Art_9,
                ComplianceControls.PciDss.R_8_4_2,
                ComplianceControls.Gdpr.Art_5_1_f,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.AgId.ABSC_5_7,
                ComplianceControls.Ens.OP_ACC_4,
                ComplianceControls.Nist80053.IA_2,
                ComplianceControls.Soc2.CC6_1,
                ComplianceControls.Iso27018.A_9_4_2,
                ComplianceControls.C5.IDM_08,
                ComplianceControls.Cis.C_6,
                ComplianceControls.NistCsf.PR_AA_01,
                ComplianceControls.NistCsf.PR_AA_03,
            ]);

        // Admin users without TFA — fetch ACLs once to find users with Administrator role
        var adminUserIds = acls.Where(a => a.Roleid == "Administrator" && a.Type == "user")
                               .Select(a => a.UsersGroupid)
                               .ToHashSet();

        // ACL / privilege scoping controls (WC0005, WC0014, IC0010, WC0015): least-privilege baseline.
        ComplianceMapping[] accessPrivilegeControls =
        [
            ComplianceControls.Iso27001.A_5_15,
            ComplianceControls.Iso27001.A_8_2,
            ComplianceControls.Nis2.Art_21_i,
            ComplianceControls.PciDss.R_7_2,
            ComplianceControls.Ens.OP_ACC_2,
            ComplianceControls.Nist80053.AC_6,
            ComplianceControls.Soc2.CC6_3,
            ComplianceControls.C5.IDM_09,
        ];
        // TFA controls (WC0007, WC0013, IC0011): aligned with CC0004.
        ComplianceMapping[] tfaControls =
        [
            ComplianceControls.Iso27001.A_5_17,
            ComplianceControls.Iso27001.A_8_5,
            ComplianceControls.Nis2.Art_21_j,
            ComplianceControls.Dora.Art_9,
            ComplianceControls.PciDss.R_8_4_2,
            ComplianceControls.Gdpr.Art_5_1_f,
            ComplianceControls.Gdpr.Art_32_1_b,
            ComplianceControls.Ens.OP_ACC_4,
            ComplianceControls.Nist80053.IA_2,
            ComplianceControls.Soc2.CC6_1,
            ComplianceControls.Iso27018.A_9_4_2,
            ComplianceControls.C5.IDM_08,
        ];
        // Account / identity lifecycle (WC0006, WC0016, IC0005, IC0006).
        ComplianceMapping[] accountLifecycleControls =
        [
            ComplianceControls.Iso27001.A_5_16,
            ComplianceControls.Iso27001.A_5_18,
            ComplianceControls.Nis2.Art_21_d,
            ComplianceControls.PciDss.R_8_2,
            ComplianceControls.Gdpr.Art_5_1_f,
            ComplianceControls.Ens.OP_ACC_1,
            ComplianceControls.Nist80053.IA_2,
            ComplianceControls.Soc2.CC6_2,
            ComplianceControls.C5.IDM_01,
        ];

        // ACL Administrator role assigned at root path '/' — too permissive, prefer scoped permissions
        CreateResultPerItem(
            items: acls.Where(a => a.Roleid == "Administrator" && a.Path == "/" && a.Type == "user").ToList(),
            isItemOk: _ => false,
            itemId: _ => "access/acl",
            itemDescriptionKo: a => $"User '{a.UsersGroupid}' has Administrator role at root path '/' — prefer pool/node-scoped permissions",
            aggregatedIdOk: "access/acl",
            aggregatedDescriptionOk: _ => "No user has Administrator role directly on '/'",
            errorCode: "WC0005",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: accessPrivilegeControls);

        // Disabled users with active API tokens — tokens remain valid even when user is disabled
        CreateResultPerItem(
            items: accessUsers.Where(u => !u.Enable && u.Tokens.Any())
                              .SelectMany(u => u.Tokens.Select(t => (User: u, Token: t)))
                              .ToList(),
            isItemOk: _ => false,
            itemId: ut => $"access/users/{ut.User.Id}",
            itemDescriptionKo: ut => $"Disabled user '{ut.User.Id}' has active API token '{ut.User.Id}!{ut.Token.Id}' — token remains valid and should be revoked",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "No disabled user has active API tokens",
            errorCode: "WC0006",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: accountLifecycleControls);

        // Admin users without TFA (excluding root@pam, handled by CC0004)
        CreateResultPerItem(
            items: accessUsers.Where(u => u.Enable && adminUserIds.Contains(u.Id) && u.Id != "root@pam").ToList(),
            isItemOk: u => usersWithTfa.Contains(u.Id),
            itemId: u => $"access/users/{u.Id}",
            itemDescriptionKo: u => $"Admin user '{u.Id}' has no TFA configured",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "All admin users (besides root@pam) have TFA configured",
            errorCode: "WC0007",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: tfaControls);

        // Local users (pam/pve) — expiration and tokens analysed separately on the same set.
        var localUsers = accessUsers.Where(a => a.Enable && (a.RealmType == "pam" || a.RealmType == "pve")).ToList();

        CreateResultPerItem(
            items: localUsers,
            isItemOk: u => u.Expire != 0,
            itemId: u => $"access/users/{u.Id}",
            itemDescriptionKo: u => $"Local user '{u.Id}' has no expiration date configured",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "All local users have an expiration date configured",
            errorCode: "IC0005",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: accountLifecycleControls);

        // API tokens without expiration remain valid indefinitely — security risk
        CreateResultPerItem(
            items: localUsers.SelectMany(u => u.Tokens.Select(t => (User: u, Token: t))).ToList(),
            isItemOk: ut => ut.Token.Expire != 0,
            itemId: ut => $"access/users/{ut.User.Id}",
            itemDescriptionKo: ut => $"API token '{ut.User.Id}!{ut.Token.Id}' has no expiration date configured",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "All API tokens on local users have an expiration date",
            errorCode: "IC0006",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: accountLifecycleControls);

        // Enabled users without an email — notifications (backup failures, fencing, etc.) cannot reach them.
        // Mapped to monitoring/audit: without notification channels, security-relevant events go unseen.
        CreateResultPerItem(
            items: accessUsers.Where(a => a.Enable).ToList(),
            isItemOk: a => !string.IsNullOrWhiteSpace(a.Email),
            itemId: a => $"access/users/{a.Id}",
            itemDescriptionKo: a => $"User '{a.Id}' has no email configured — will not receive notifications",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "All enabled users have an email address configured",
            errorCode: "IC0007",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_16,
                ComplianceControls.Iso27001.A_8_16,
                ComplianceControls.Nis2.Art_21_f,
                ComplianceControls.Gdpr.Art_32_1_d,
                ComplianceControls.Cis.C_5,
                ComplianceControls.Cis.C_8,
                ComplianceControls.NistCsf.DE_CM_03,
                ComplianceControls.AgId.ABSC_5_2,
                ComplianceControls.Ens.OP_EXP_8,
                ComplianceControls.Nist80053.AU_12,
                ComplianceControls.Soc2.CC7_2,
                ComplianceControls.Iso27018.A_12_4_1,
                ComplianceControls.C5.OPS_09,
            ]);

        // Empty groups — no users assigned, usually leftover configuration
        CreateResultPerItem(
            items: groups,
            isItemOk: a => !string.IsNullOrWhiteSpace(a.Users),
            itemId: a => $"access/groups/{a.Id}",
            itemDescriptionKo: a => $"Group '{a.Id}' has no members",
            aggregatedIdOk: "access/groups",
            aggregatedDescriptionOk: _ => "No empty access groups",
            errorCode: "IC0008",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: accountLifecycleControls);

        // Custom roles not referenced by any ACL — dead configuration
        var rolesInUse = acls.Where(a => !string.IsNullOrWhiteSpace(a.Roleid))
                             .Select(a => a.Roleid)
                             .ToHashSet();
        CreateResultPerItem(
            items: roles.Where(a => a.Special == 0).ToList(),
            isItemOk: a => rolesInUse.Contains(a.Id),
            itemId: a => $"access/roles/{a.Id}",
            itemDescriptionKo: a => $"Custom role '{a.Id}' is not assigned in any ACL — unused",
            aggregatedIdOk: "access/roles",
            aggregatedDescriptionOk: _ => "All custom roles are referenced by at least one ACL entry",
            errorCode: "IC0009",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: accountLifecycleControls);

        // Groups holding Administrator role on '/': any enabled member without TFA is a security risk.
        // WC0007 covers users with direct ACL; this covers users that get admin transitively via group.
        var privilegedGroupIds = acls.Where(a => a.Path == "/" && a.Type == "group" && a.Roleid == "Administrator")
                                     .Select(a => a.UsersGroupid)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var transitiveAdmins = new List<(string Member, string GroupId)>();
        foreach (var g in groups.Where(g => privilegedGroupIds.Contains(g.Id) && !string.IsNullOrWhiteSpace(g.Users)))
        {
            foreach (var member in g.Users.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var u = accessUsers.FirstOrDefault(x => string.Equals(x.Id, member, StringComparison.OrdinalIgnoreCase));
                if (u != null && !u.Enable) { continue; }
                transitiveAdmins.Add((member, g.Id));
            }
        }

        CreateResultPerItem(
            items: transitiveAdmins,
            isItemOk: mg => usersWithTfa.Contains(mg.Member),
            itemId: mg => $"access/users/{mg.Member}",
            itemDescriptionKo: mg => $"User '{mg.Member}' has Administrator role via group '{mg.GroupId}' but no TFA configured",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "No transitive admin (via group) is missing TFA",
            errorCode: "WC0013",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: tfaControls);

        // Disabled user that still has Administrator ACL on '/': leftover privilege from before deactivation.
        var disabledUserIds = accessUsers.Where(u => !u.Enable)
                                         .Select(u => u.Id)
                                         .ToHashSet(StringComparer.OrdinalIgnoreCase);
        CreateResultPerItem(
            items: acls.Where(a => a.Path == "/" && a.Type == "user" && a.Roleid == "Administrator"
                                    && disabledUserIds.Contains(a.UsersGroupid)).ToList(),
            isItemOk: _ => false,
            itemId: a => $"access/users/{a.UsersGroupid}",
            itemDescriptionKo: a => $"Disabled user '{a.UsersGroupid}' still has Administrator role on '/' — revoke the ACL entry",
            aggregatedIdOk: "access/acl",
            aggregatedDescriptionOk: _ => "No disabled user retains Administrator role on '/'",
            errorCode: "WC0014",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: accessPrivilegeControls);

        // Administrator ACL on '/' with Propagate disabled: unusual, often a misconfiguration.
        CreateResultPerItem(
            items: acls.Where(a => a.Path == "/" && a.Roleid == "Administrator" && a.Propagate == 0).ToList(),
            isItemOk: _ => false,
            itemId: _ => "access/acl",
            itemDescriptionKo: a => $"{a.Type} '{a.UsersGroupid}' has Administrator role on '/' but Propagate is disabled — children resources do not inherit it",
            aggregatedIdOk: "access/acl",
            aggregatedDescriptionOk: _ => "All Administrator ACLs on '/' have Propagate enabled",
            errorCode: "IC0010",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: accessPrivilegeControls);

        // External realm (LDAP/AD/OpenID) without realm-level TFA: weaker baseline than pve/pam where per-user TFA can be enforced.
        CreateResultPerItem(
            items: domains.Where(d => string.Equals(d.Type, "ldap", StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(d.Type, "ad", StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(d.Type, "openid", StringComparison.OrdinalIgnoreCase)).ToList(),
            isItemOk: d => !string.IsNullOrWhiteSpace(d.Tfa),
            itemId: d => $"access/domains/{d.Realm}",
            itemDescriptionKo: d => $"External realm '{d.Realm}' ({d.Type}) does not enforce TFA at realm level",
            aggregatedIdOk: "access/domains",
            aggregatedDescriptionOk: _ => "All external realms enforce TFA at realm level",
            errorCode: "IC0011",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Info,
            compliance: tfaControls);

        // root@pam API tokens without privilege separation inherit full root rights — they should always be priv-separated.
        if (root != null)
        {
            CreateResultPerItem(
                items: root.Tokens.ToList(),
                isItemOk: t => t.Privsep != 0,
                itemId: _ => "access/users/root@pam",
                itemDescriptionKo: t => $"root@pam token '{t.Id}' has no privilege separation — it has full root rights",
                aggregatedIdOk: "access/users/root@pam",
                aggregatedDescriptionOk: _ => "All root@pam tokens have privilege separation enabled",
                errorCode: "WC0015",
                subContext: "Access",
                context: DiagnosticResultContext.Cluster,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance: accessPrivilegeControls);
        }

        // Enabled user whose expiration has already passed: account should have been deactivated.
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CreateResultPerItem(
            items: accessUsers.Where(u => u.Enable && u.Expire > 0).ToList(),
            isItemOk: u => u.Expire >= nowUnix,
            itemId: u => $"access/users/{u.Id}",
            itemDescriptionKo: u => $"User '{u.Id}' is enabled but expired on {DateTimeOffset.FromUnixTimeSeconds(u.Expire):yyyy-MM-dd} — account should be deactivated",
            aggregatedIdOk: "access/users",
            aggregatedDescriptionOk: _ => "No enabled user has an expiration date in the past",
            errorCode: "WC0016",
            subContext: "Access",
            context: DiagnosticResultContext.Cluster,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: accountLifecycleControls);
    }
}
