/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task CheckCommonVmAsync(Settings settings,
                                          SettingsThresholdHost thresholdHost,
                                          VmConfig config,
                                          IEnumerable<KeyValue> pending,
                                          IEnumerable<VmSnapshot> snapshots,
                                          IEnumerable<VmRrdData> rrdData,
                                          DiagnosticResultContext context,
                                          string node,
                                          long vmId,
                                          string id,
                                          IEnumerable<NodeStorage> nodeBackupStorages)
    {
        #region VM State
        // A saved vmstate (hibernate) left in pending means the VM was suspended and never resumed properly
        CreateResultPerItem(
            items: pending.Where(a => a.Key == "vmstate").ToList(),
            isItemOk: _ => false,
            itemId: _ => id,
            itemDescriptionKo: a => $"Found vmstate '{a.Value}'",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "No leftover vmstate (hibernate) entries",
            errorCode: "CG0001",
            subContext: "VM State",
            context: context,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance: []);
        #endregion

        #region Pending config changes
        // Config changes applied via the API are held in "pending" until the VM is rebooted.
        // Calling out pending changes helps operators know a reboot is needed for changes to take effect.
        var pendingChanges = pending.Where(a => a.Key != "vmstate"
                                                && (a.Pending != null || a.Delete == 1)).ToList();
        CreateResult(
            isOk: pendingChanges.Count == 0,
            id: id,
            errorCode: "IG0010",
            subContext: "Status",
            context: context,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: $"VM has {pendingChanges.Count} pending config change(s) that require a reboot to apply ({string.Join(", ", pendingChanges.Select(p => p.Key))})",
            descriptionOk: "No pending config changes",
            compliance: []);
        #endregion

        #region Locked
        // A locked VM/CT cannot be started, stopped or migrated until the lock is cleared
        CreateResult(
            isOk: !config.IsLocked,
            id: id,
            errorCode: "WG0015",
            subContext: "Status",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: $"VM is locked by '{config.Lock}'",
            descriptionOk: "VM is not locked",
            compliance: []);
        #endregion

        #region Start on boot
        CreateResult(
            isOk: config.OnBoot,
            id: id,
            errorCode: "WG0016",
            subContext: "StartOnBoot",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: "Start on boot not enabled",
            descriptionOk: "Start on boot is enabled",
            compliance: []);
        #endregion

        #region Protection
        CreateResult(
            isOk: config.Protection,
            id: id,
            errorCode: "IG0011",
            subContext: "Protection",
            context: context,
            gravityKo: DiagnosticResultGravity.Info,
            descriptionKo: "For production environment is better VM Protection = enabled",
            descriptionOk: "VM Protection is enabled",
            compliance: []);
        #endregion

        #region Backup config
        ComplianceMapping[] backupGuestControls =
        [
            ComplianceControls.Iso27001.A_8_13,
            ComplianceControls.Nis2.Art_21_c,
            ComplianceControls.Dora.Art_11,
            ComplianceControls.Dora.Art_12,
            ComplianceControls.Gdpr.Art_32_1_c,
            ComplianceControls.Ens.MP_INFO_6,
            ComplianceControls.Iso27018.A_12_3_1,
            ComplianceControls.C5.OPS_21,
        ];

        // Check if this VM is covered by at least one enabled backup job (all, by vmid, or by pool)
        var foundBackupConfig = _clusterBackups.Any(a => a.Enabled && a.All);
        if (!foundBackupConfig)
        {
            foundBackupConfig = _clusterBackups.Where(a => a.Enabled && !string.IsNullOrEmpty(a.VmId))
                                               .SelectMany(a => a.VmId.Split(","))
                                               .Any(a => long.TryParse(a.Trim(), out var bid) && bid == vmId);
            if (!foundBackupConfig)
            {
                foreach (var poolId in _clusterBackups.Where(a => a.Enabled && !string.IsNullOrWhiteSpace(a.Pool)).Select(a => a.Pool))
                {
                    var poolDetail = await client.Pools[poolId].GetAsync()
                                           .ToSafeSingle(_result, $"pools/{poolId}", DiagnosticResultContext.Cluster, $"members of pool '{poolId}'");
                    if (poolDetail == null) { continue; }
                    foundBackupConfig = poolDetail.Members.Any(a => a.ResourceType == ClusterResourceType.Vm && a.VmId == vmId);
                    if (foundBackupConfig) { break; }
                }
            }
        }
        CreateResult(
            isOk: foundBackupConfig,
            id: id,
            errorCode: "WG0017",
            subContext: "Backup",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: "vzdump backup not configured",
            descriptionOk: "Guest is covered by at least one enabled backup job",
            compliance: backupGuestControls);

        // Individual disks excluded from backup — even if the job exists, these disks won't be saved
        CreateResultPerItem(
            items: config.Disks.Where(a => !a.IsUnused).ToList(),
            isItemOk: a => a.Backup,
            itemId: _ => id,
            itemDescriptionKo: a => $"Disk '{a.Id}' disabled for backup",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "All active disks are included in backup",
            errorCode: "CG0002",
            subContext: "Backup",
            context: context,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance: backupGuestControls);

        #region Unused disks
        // Disks detached from the VM/CT config but still present in storage — consuming space silently
        // Size is already available in VmDisk.SizeBytes parsed from config — no extra API call needed
        CreateResultPerItem(
            items: config.Disks.ToList(),
            isItemOk: a => !a.IsUnused,
            itemId: _ => id,
            itemDescriptionKo: a => $"Unused disk '{a.Id}'{(a.SizeBytes > 0 ? $" ({FormatHelper.FromBytes(a.SizeBytes)})" : "")} — detached from VM but still in storage",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "No unused disks left attached to storage",
            errorCode: "WG0018",
            subContext: "Hardware",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);
        #endregion

        var nodeApi = client.Nodes[node];
        if (settings.Backup.Enabled)
        {
            // Reuse already-fetched backup content — filter by vmId in memory, no extra API call.
            // Key is storage name for shared storage, node/storage for non-shared.
            var backupContents = nodeBackupStorages.Where(a => a.Active)
                                                   .SelectMany(a => _backupContentByStorage.TryGetValue(BackupStorageKey(node, a.Storage), out var list)
                                                                       ? list.Where(c => c.VmId == vmId)
                                                                       : [])
                                                   .ToList();

            // Old backups still present waste storage space
            if (settings.Backup.MaxAgeDays > 0)
            {
                var oldBackups = backupContents.Where(a => a.CreationDate.Date <= _now.Date.AddDays(-settings.Backup.MaxAgeDays)).ToList();
                CreateResult(
                    isOk: oldBackups.Count == 0,
                    id: id,
                    errorCode: "WG0019",
                    subContext: "Backup",
                    context: context,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"{oldBackups.Count} {(oldBackups.Count == 1 ? "backup" : "backups")} older than {settings.Backup.MaxAgeDays} days ({FormatHelper.FromBytes(oldBackups.Sum(a => a.Size))})",
                    descriptionOk: $"No backups older than {settings.Backup.MaxAgeDays} days",
                    compliance: backupGuestControls);
            }

            // No backup found within RecentDays — RPO violation
            if (settings.Backup.RecentDays > 0)
            {
                var hasRecent = backupContents.Any(a => a.CreationDate.Date >= _now.Date.AddDays(-settings.Backup.RecentDays));
                CreateResult(
                    isOk: hasRecent,
                    id: id,
                    errorCode: "WG0020",
                    subContext: "Backup",
                    context: context,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "No recent backups found!",
                    descriptionOk: $"At least one backup within the last {settings.Backup.RecentDays} day(s)",
                    compliance: backupGuestControls);
            }
        }
        #endregion

        #region Task history
        // Failed tasks for this VM in the last 48 hours — vmid filtered server-side
        var dayTask = new DateTimeOffset(_now.AddDays(-2)).ToUnixTimeSeconds();
        var tasks = (await nodeApi.Tasks.GetAsync(errors: true, limit: 1000, vmid: (int)vmId))
                    .Where(a => a.StartTime >= dayTask);
        CheckTaskHistory(tasks, context, id);
        #endregion

        CheckSnapshots(snapshots, settings.Snapshot, _now, id, context);

        var rrdList = rrdData.ToList();
        CheckThresholdHost(thresholdHost,
                           context,
                           id,
                           rrdList.Select(a => new ThresholdRddData(a, a, a)),
                           cpuErrorCode: "WG0025",
                           memoryErrorCode: "WG0026",
                           netInErrorCode: "WG0027",
                           netOutErrorCode: "WG0028");

        // PSI pressure — only meaningful when non-zero (PVE 9.0+ only; older nodes always return 0)
        if (rrdList.Any(a => a.PressureCpuSome > 0))
        {
            CheckThreshold(thresholdHost.Rrd.Pressure.Cpu,
                           "WG0029",
                           context,
                           "Pressure",
                           [new ThresholdDataPoint(rrdList.Average(a => a.PressureCpuSome) * 100,
                                                   0d,
                                                   id,
                                                   $"PSI CPU some (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                           true,
                           false);
        }

        if (rrdList.Any(a => a.PressureIoFull > 0))
        {
            CheckThreshold(thresholdHost.Rrd.Pressure.IoFull,
                           "WG0030",
                           context,
                           "Pressure",
                           [new ThresholdDataPoint(rrdList.Average(a => a.PressureIoFull) * 100,
                                                   0d,
                                                   id,
                                                   $"PSI I/O full (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                           true,
                           false);
        }

        if (rrdList.Any(a => a.PressureMemoryFull > 0))
        {
            CheckThreshold(thresholdHost.Rrd.Pressure.MemoryFull,
                           "WG0031",
                           context,
                           "Pressure",
                           [new ThresholdDataPoint(rrdList.Average(a => a.PressureMemoryFull) * 100,
                                                   0d,
                                                   id,
                                                   $"PSI Memory full (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                           true,
                           false);
        }

        // Health score for VM/LXC: 100 - (cpu*0.5 + ram*0.5)
        var cpuPct = rrdList.Average(a => a.CpuUsagePercentage) * 100.0;
        var ramPct = rrdList.Any(a => Convert.ToDouble(a.MemorySize) > 0)
                        ? rrdList.Average(a => Convert.ToDouble(a.MemoryUsage) / Convert.ToDouble(a.MemorySize) * 100.0)
                        : 0.0;
        CheckHealthScore(thresholdHost.HealthScore, context, id, (cpuPct * 0.5) + (ramPct * 0.5));

        // HA / Replication coverage — only meaningful for running, non-template guests.
        // IC0002 / IC0003 already cover the "no HA at all / no replication at all" cluster-wide
        // cases; these two flag individual guests that are NOT covered when the cluster has
        // HA/replication in use. Both findings are also compliance-relevant (A.5.30, Nis2 Art.21(c),
        // Dora Art.12) so they are emitted even on single-node hosts: a single-node setup is
        // itself non-compliant with the resilience controls.
        var resource = _resources.FirstOrDefault(r => r.VmId == vmId);
        if (resource != null && !resource.IsTemplate && resource.IsRunning)
        {
            ComplianceMapping[] resilienceControls =
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Nis2.Art_21_c,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.Ens.OP_CONT_2,
                ComplianceControls.C5.BCM_03,
                ComplianceControls.Ens.MP_S_1,
                ComplianceControls.C5.PI_02,
            ];

            if (_haVmIds.Count > 0)
            {
                CreateResult(
                    isOk: _haVmIds.Contains(vmId),
                    id: id,
                    errorCode: "IG0015",
                    subContext: "HA",
                    context: context,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "Guest is not managed by any HA resource — it will not be restarted automatically on node failure",
                    descriptionOk: "Guest is managed by an HA resource",
                    compliance: resilienceControls);
            }

            // If the guest is in HA on non-shared storage, replication is the only way the failover target
            // has a recent copy. Flag HA guests with no enabled replication job.
            if (_haVmIds.Contains(vmId))
            {
                CreateResult(
                    isOk: _replicatedVmIds.Contains(vmId),
                    id: id,
                    errorCode: "WG0043",
                    subContext: "Replication",
                    context: context,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "HA guest has no enabled replication job — on non-shared storage the failover target will have no recent data",
                    descriptionOk: "HA guest is covered by an enabled replication job",
                    compliance: resilienceControls);
            }
        }
    }

    private void CheckTaskHistory(IEnumerable<NodeTask> tasks,
                                  DiagnosticResultContext context,
                                  string id)
    {
        var tasksCount = tasks.Count(a => !a.StatusOk);
        CreateResult(
            isOk: tasksCount == 0,
            id: id,
            errorCode: context == DiagnosticResultContext.Node ? "CN0005" : "CG0003",
            subContext: "Tasks",
            context: context,
            gravityKo: DiagnosticResultGravity.Critical,
            descriptionKo: $"{tasksCount} Task history has errors",
            descriptionOk: "No task errors in recent history",
            compliance:
            [
                ComplianceControls.Iso27001.A_8_15,
                ComplianceControls.Iso27001.A_8_16,
                ComplianceControls.Nis2.Art_21_f,
                ComplianceControls.Dora.Art_10,
                ComplianceControls.Gdpr.Art_32_1_d,
                ComplianceControls.AgId.ABSC_5_2,
                ComplianceControls.Ens.OP_EXP_8,
                ComplianceControls.Iso27018.A_12_4_1,
                ComplianceControls.C5.OPS_09,
                ComplianceControls.Cis.C_8,
                ComplianceControls.NistCsf.DE_CM_01,
                ComplianceControls.NistCsf.DE_CM_03,
                ComplianceControls.Iso27017.CLD_12_4_5,
            ]);
    }

    private void CheckSnapshots(IEnumerable<VmSnapshot> snapshots,
                                SettingsSnapshot snapshotSettings,
                                DateTime execution,
                                string id,
                                DiagnosticResultContext context)
    {
        const string autosnapAppName = "cv4pve-autosnap";
        const string autosnapAppNameOld = "eve4pve-autosnap";

        // Exclude the implicit "current" entry which is always present and is not a real snapshot
        var realSnapshots = snapshots.Where(a => a.Name != "current").ToList();

        // cv4pve-autosnap is the recommended tool for automated rolling snapshots
        CreateResult(
            isOk: snapshots.Any(a => a.Description == autosnapAppName || a.Description == $"{autosnapAppName}\n"),
            id: id,
            errorCode: "WG0021",
            subContext: "AutoSnapshot",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: $"'{autosnapAppName}' not configured",
            descriptionOk: $"'{autosnapAppName}' is configured for automated rolling snapshots",
            compliance: []);

        // Old tool name — user should migrate to the current version
        CreateResult(
            isOk: !snapshots.Any(a => a.Description == autosnapAppNameOld || a.Description == $"{autosnapAppNameOld}\n"),
            id: id,
            errorCode: "WG0022",
            subContext: "AutoSnapshot",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: $"Old AutoSnap '{autosnapAppNameOld}' are present. Update new version",
            descriptionOk: $"No legacy '{autosnapAppNameOld}' snapshots present",
            compliance: []);

        // Snapshots older than MaxAgeDays — likely forgotten, wasting storage
        if (snapshotSettings.MaxAgeDays > 0)
        {
            var snapOldCount = realSnapshots.Count(a => a.Date < execution.AddDays(-snapshotSettings.MaxAgeDays));
            CreateResult(
                isOk: snapOldCount == 0,
                id: id,
                errorCode: "WG0023",
                subContext: "SnapshotOld",
                context: context,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"{snapOldCount} {(snapOldCount == 1 ? "snapshot" : "snapshots")} older than {snapshotSettings.MaxAgeDays} days",
                descriptionOk: $"No snapshot older than {snapshotSettings.MaxAgeDays} days",
                compliance: []);
        }

        // Snapshots with RAM state (vmstate=1) save the full guest memory to disk.
        // This wastes significant storage and blocks certain operations (e.g. storage migration).
        CreateResultPerItem(
            items: realSnapshots,
            isItemOk: s => !s.VmStatus,
            itemId: _ => id,
            itemDescriptionKo: snap => $"Snapshot '{snap.Name}' includes RAM state — wastes disk space and blocks storage migration",
            aggregatedIdOk: id,
            aggregatedDescriptionOk: _ => "No snapshot includes RAM state",
            errorCode: "WG0035",
            subContext: "Snapshot",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);

        // Too many snapshots cause long delta chains and degrade disk I/O on every read/write
        if (snapshotSettings.MaxCount > 0)
        {
            CreateResult(
                isOk: realSnapshots.Count <= snapshotSettings.MaxCount,
                id: id,
                errorCode: "WG0024",
                subContext: "SnapshotCount",
                context: context,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"{realSnapshots.Count} snapshots exceed the maximum of {snapshotSettings.MaxCount}",
                descriptionOk: $"Snapshot count ({realSnapshots.Count}) is within the configured maximum ({snapshotSettings.MaxCount})",
                compliance: []);
        }
    }

    private void CheckVmFirewall(VmFirewallOptions fwOptions,
                                 string id,
                                 DiagnosticResultContext context)
    {
        var kind = context == DiagnosticResultContext.Qemu
                        ? "VM"
                        : "Container";

        ComplianceMapping[] firewallControls =
        [
            ComplianceControls.Iso27001.A_8_20,
            ComplianceControls.Iso27001.A_8_22,
            ComplianceControls.Nis2.Art_21_e,
            ComplianceControls.PciDss.R_1_2,
            ComplianceControls.Gdpr.Art_5_1_f,
            ComplianceControls.Ens.MP_COM_1,
            ComplianceControls.C5.KOS_01,
        ];

        CreateResult(
            isOk: fwOptions.Enable,
            id: id,
            errorCode: "WG0013",
            subContext: "Firewall",
            context: context,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: $"{kind} firewall is disabled — {kind.ToLower()} is exposed to all traffic on the node bridge",
            descriptionOk: $"{kind} firewall is enabled",
            compliance: firewallControls);

        if (fwOptions.Enable)
        {
            CreateResult(
                isOk: fwOptions.Ipfilter,
                id: id,
                errorCode: "IG0009",
                subContext: "Firewall",
                context: context,
                gravityKo: DiagnosticResultGravity.Info,
                descriptionKo: $"{kind} firewall IP filter is disabled — {kind.ToLower()} can spoof source IP addresses",
                descriptionOk: $"{kind} firewall IP filter is enabled",
                compliance: firewallControls);
        }
    }

    private string BackupStorageKey(string node, string storage)
        => _sharedStorageNames.Contains(storage)
            ? storage
            : $"{node}/{storage}";

    private record ThresholdRddData(IMemory Memory, INetIO NetIO, ICpu Cpu);

    private void CheckThresholdHost(SettingsThresholdHost thresholdHost,
                                    DiagnosticResultContext context,
                                    string id,
                                    IEnumerable<ThresholdRddData> rrdData,
                                    string cpuErrorCode,
                                    string memoryErrorCode,
                                    string netInErrorCode,
                                    string netOutErrorCode)
    {
        CheckThreshold(thresholdHost.Cpu,
                       cpuErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.Cpu.CpuUsagePercentage) * 100,
                                               0d,
                                               id,
                                               $"CPU (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       true,
                       false);

        CheckThreshold(thresholdHost.Memory,
                       memoryErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => Convert.ToDouble(a.Memory.MemoryUsage)),
                                               rrdData.Average(a => Convert.ToDouble(a.Memory.MemorySize)),
                                               id,
                                               $"Memory (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       false,
                       true);

        CheckThreshold(thresholdHost.Network,
                       netInErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.NetIO.NetIn),
                                               0d,
                                               id,
                                               $"NetIn (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       true,
                       false);

        CheckThreshold(thresholdHost.Network,
                       netOutErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.NetIO.NetOut),
                                               0d,
                                               id,
                                               $"NetOut (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       true,
                       false);

    }


    private void CheckHealthScore(SettingsThreshold<double> healthScore,
                                  DiagnosticResultContext context,
                                  string id,
                                  double weightedLoad)
    {
        // Both thresholds disabled → skip entirely (no result, not even Ok).
        if (healthScore.Warning <= 0 && healthScore.Critical <= 0) { return; }

        // Score = 100 - weighted load percentage (0=idle, 100=fully saturated)
        var score = Math.Round(100.0 - weightedLoad, 1);

        var isCritical = healthScore.Critical > 0 && score < healthScore.Critical;
        var isWarning = !isCritical && healthScore.Warning > 0 && score < healthScore.Warning;
        var isOk = !isCritical && !isWarning;

        CreateResult(
            isOk: isOk,
            id: id,
            errorCode: "WG0032",
            subContext: "HealthScore",
            context: context,
            gravityKo: isCritical
                        ? DiagnosticResultGravity.Critical
                        : DiagnosticResultGravity.Warning,
            descriptionKo: $"Health score is {score}/100 (threshold: warning={healthScore.Warning}, critical={healthScore.Critical})",
            descriptionOk: $"Health score is {score}/100 (above warning threshold {healthScore.Warning})",
            compliance: []);
    }

    private record ThresholdDataPoint(double Usage, double Size, string Id, string PrefixDescription);

    /// <summary>
    /// Checks each datapoint against Warning/Critical thresholds and routes through <see cref="CreateResult"/>.
    /// One call per datapoint: Critical when pct ≥ Critical, Warning when pct ≥ Warning (but below Critical),
    /// Ok otherwise (emitted only if <c>settings.IncludeOkResult</c> is true).
    /// isValue=true  → Usage is an absolute value (e.g. percentage already computed).
    /// isValue=false → Usage/Size are raw bytes; percentage is computed internally.
    /// formatByte=true → appends human-readable byte sizes to the description.
    /// </summary>
    private void CheckThreshold(SettingsThreshold<double> threshold,
                                string errorCode,
                                DiagnosticResultContext context,
                                string subContext,
                                IEnumerable<ThresholdDataPoint> data,
                                bool isValue,
                                bool formatByte,
                                IReadOnlyList<ComplianceMapping>? compliance = null)
    {
        // Both thresholds disabled → skip entirely (no result, not even Ok).
        if (threshold.Warning == 0 || threshold.Critical == 0) { return; }

        var complianceList = compliance ?? [];

        foreach (var a in data)
        {
            var pct = Math.Round(isValue ? a.Usage : a.Usage / a.Size * 100.0, 1);
            var description = $"{a.PrefixDescription} usage {pct}%";
            if (formatByte)
            {
                description += $" - {FormatHelper.FromBytes(a.Usage)} of {FormatHelper.FromBytes(a.Size)}";
            }

            var isCritical = pct >= threshold.Critical;
            var isWarning = !isCritical && pct >= threshold.Warning;
            var isOk = !isCritical && !isWarning;

            CreateResult(
                isOk: isOk,
                id: a.Id,
                errorCode: errorCode,
                subContext: subContext,
                context: context,
                gravityKo: isCritical ? DiagnosticResultGravity.Critical : DiagnosticResultGravity.Warning,
                descriptionKo: description,
                descriptionOk: $"{description} (within threshold: warning={threshold.Warning}, critical={threshold.Critical})",
                compliance: complianceList);
        }
    }

}
