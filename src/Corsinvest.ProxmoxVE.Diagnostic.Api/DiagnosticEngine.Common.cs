/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

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
                                          IEnumerable<ClusterBackup> clusterBackups,
                                          IEnumerable<NodeStorage> nodeBackupStorages)
    {
        #region VM State
        // A saved vmstate (hibernate) left in pending means the VM was suspended and never resumed properly
        _result.AddRange(pending.Where(a => a.Key == "vmstate")
                                .Select(a => new DiagnosticResult
                                {
                                    Id = id,
                                    ErrorCode = "CQ0001",
                                    Description = $"Found vmstate '{a.Value}'",
                                    Context = context,
                                    SubContext = "VM State",
                                    Gravity = DiagnosticResultGravity.Critical,
                                }));
        #endregion

        #region Pending config changes
        // Config changes applied via the API are held in "pending" until the VM is rebooted.
        // Calling out pending changes helps operators know a reboot is needed for changes to take effect.
        var pendingAll = pending.ToList();
        var pendingChanges = pendingAll.Where(a => a.Key != "vmstate"
                                                   && (a.Pending != null || a.Delete == 1)).ToList();
        if (pendingChanges.Count > 0)
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "IQ0010",
                Description = $"VM has {pendingChanges.Count} pending config change(s) that require a reboot to apply ({string.Join(", ", pendingChanges.Select(p => p.Key))})",
                Context = context,
                SubContext = "Status",
                Gravity = DiagnosticResultGravity.Info,
            });
        }
        #endregion

        #region Locked
        // A locked VM/CT cannot be started, stopped or migrated until the lock is cleared
        if (config.IsLocked)
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0015",
                Description = $"VM is locked by '{config.Lock}'",
                Context = context,
                SubContext = "Status",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
        #endregion

        #region Start on boot
        if (!config.OnBoot)
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0016",
                Description = "Start on boot not enabled",
                Context = context,
                SubContext = "StartOnBoot",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
        #endregion

        #region Protection
        if (!config.Protection)
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "IQ0011",
                Description = "For production environment is better VM Protection = enabled",
                Context = context,
                SubContext = "Protection",
                Gravity = DiagnosticResultGravity.Info,
            });
        }
        #endregion

        #region Backup config
        // Check if this VM is covered by at least one enabled backup job (all, by vmid, or by pool)
        var foundBackupConfig = clusterBackups.Any(a => a.Enabled && a.All);
        if (!foundBackupConfig)
        {
            foundBackupConfig = clusterBackups.Where(a => a.Enabled && !string.IsNullOrEmpty(a.VmId))
                                              .SelectMany(a => a.VmId.Split(","))
                                              .Any(a => long.TryParse(a.Trim(), out var id) && id == vmId);
            if (!foundBackupConfig)
            {
                foreach (var poolId in clusterBackups.Where(a => a.Enabled && !string.IsNullOrWhiteSpace(a.Pool)).Select(a => a.Pool))
                {
                    var poolDetail = await client.Pools[poolId].GetAsync();
                    foundBackupConfig = poolDetail.Members.Any(a => a.ResourceType == ClusterResourceType.Vm && a.VmId == vmId);
                    if (foundBackupConfig) { break; }
                }
            }

            if (!foundBackupConfig)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WQ0017",
                    Description = "vzdump backup not configured",
                    Context = context,
                    SubContext = "Backup",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }

        // Individual disks excluded from backup — even if the job exists, these disks won't be saved
        _result.AddRange(config.Disks.Where(a => !a.IsUnused && !a.Backup)
                                     .Select(a => new DiagnosticResult
                                     {
                                         Id = id,
                                         ErrorCode = "CQ0002",
                                         Description = $"Disk '{a.Id}' disabled for backup",
                                         Context = context,
                                         SubContext = "Backup",
                                         Gravity = DiagnosticResultGravity.Critical,
                                     }));

        #region Unused disks
        // Disks detached from the VM/CT config but still present in storage — consuming space silently
        // Size is already available in VmDisk.SizeBytes parsed from config — no extra API call needed
        _result.AddRange(config.Disks.Where(a => a.IsUnused)
                                     .Select(a => new DiagnosticResult
                                     {
                                         Id = id,
                                         ErrorCode = "WQ0018",
                                         Description = $"disk '{a.Id}' {(a.SizeBytes > 0
                                                                            ? FormatHelper.FromBytes(a.SizeBytes).ToString()
                                                                            : string.Empty)}",
                                         Context = context,
                                         SubContext = "Hardware",
                                         Gravity = DiagnosticResultGravity.Warning,
                                     }));
        #endregion

        var nodeApi = client.Nodes[node];
        if (settings.Backup.Enabled)
        {
            var backupContents = new List<NodeStorageContent>();
            foreach (var ns in nodeBackupStorages.Where(a => a.Active))
            {
                backupContents.AddRange(await nodeApi.Storage[ns.Storage].Content.GetAsync(content: "backup", vmid: (int)vmId));
            }

            // Old backups still present waste storage space
            if (settings.Backup.MaxAgeDays > 0)
            {
                var oldBackups = backupContents.Where(a => a.CreationDate.Date <= _now.Date.AddDays(-settings.Backup.MaxAgeDays)).ToList();
                if (oldBackups.Count > 0)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WQ0019",
                        Description = $"{oldBackups.Count} backup {FormatHelper.FromBytes(oldBackups.Sum(a => a.Size))} more {settings.Backup.MaxAgeDays} days are found!",
                        Context = context,
                        SubContext = "Backup",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }

            // No backup found within RecentDays — RPO violation
            if (settings.Backup.RecentDays > 0
                && !backupContents.Any(a => a.CreationDate.Date >= _now.Date.AddDays(-settings.Backup.RecentDays)))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WQ0020",
                    Description = "No recent backups found!",
                    Context = context,
                    SubContext = "Backup",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }
        #endregion

        #region Task history
        // Failed tasks for this VM in the last 48 hours — vmid filtered server-side
        var dayTask = new DateTimeOffset(_now.AddDays(-2)).ToUnixTimeSeconds();
        var tasks = (await nodeApi.Tasks.GetAsync(errors: true, limit: 1000, vmid: (int)vmId))
                    .Where(a => a.StartTime >= dayTask);
        CheckTaskHistory(_result, tasks, context, id);
        #endregion

        CheckSnapshots(_result, snapshots, settings.Snapshot, _now, id, context);

        var rrdList = rrdData.ToList();
        CheckThresholdHost(_result, thresholdHost, context, id, rrdList.Select(a => new ThresholdRddData(a, a, a)));

        // PSI pressure — only meaningful when non-zero (PVE 9.0+ only; older nodes always return 0)
        if (rrdList.Any(a => a.PressureCpuSome > 0))
        {
            CheckThreshold(_result,
                           thresholdHost.Rrd.PressureCpu,
                           "WQ0029",
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
            CheckThreshold(_result,
                           thresholdHost.Rrd.PressureIoFull,
                           "WQ0030",
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
            CheckThreshold(_result,
                           thresholdHost.Rrd.PressureMemoryFull,
                           "WQ0031",
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
        CheckHealthScore(_result, thresholdHost.HealthScore, context, id, cpuPct * 0.5 + ramPct * 0.5);
    }

    private static void CheckTaskHistory(List<DiagnosticResult> result,
                                         IEnumerable<NodeTask> tasks,
                                         DiagnosticResultContext context,
                                         string id)
    {
        var tasksCount = tasks.Count(a => !a.StatusOk);
        if (tasksCount > 0)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = context == DiagnosticResultContext.Node ? "CN0005" : "CQ0003",
                Description = $"{tasksCount} Task history has errors",
                Context = context,
                SubContext = "Tasks",
                Gravity = DiagnosticResultGravity.Critical,
            });
        }
    }

    private static void CheckSnapshots(List<DiagnosticResult> result,
                                       IEnumerable<VmSnapshot> snapshots,
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
        if (!snapshots.Any(a => a.Description == autosnapAppName || a.Description == $"{autosnapAppName}\n"))
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0021",
                Description = $"'{autosnapAppName}' not configured",
                Context = context,
                SubContext = "AutoSnapshot",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        // Old tool name — user should migrate to the current version
        if (snapshots.Any(a => a.Description == autosnapAppNameOld || a.Description == $"{autosnapAppNameOld}\n"))
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0022",
                Description = $"Old AutoSnap '{autosnapAppNameOld}' are present. Update new version",
                Context = context,
                SubContext = "AutoSnapshot",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        // Snapshots older than MaxAgeDays — likely forgotten, wasting storage
        if (snapshotSettings.MaxAgeDays > 0)
        {
            var snapOldCount = realSnapshots.Count(a => a.Date < execution.AddDays(-snapshotSettings.MaxAgeDays));
            if (snapOldCount > 0)
            {
                result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WQ0023",
                    Description = $"{snapOldCount} snapshots older than {snapshotSettings.MaxAgeDays} days",
                    Context = context,
                    SubContext = "SnapshotOld",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }

        // Too many snapshots cause long delta chains and degrade disk I/O on every read/write
        if (snapshotSettings.MaxCount > 0 && realSnapshots.Count > snapshotSettings.MaxCount)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0024",
                Description = $"{realSnapshots.Count} snapshots exceed the maximum of {snapshotSettings.MaxCount}",
                Context = context,
                SubContext = "SnapshotCount",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
    }

    private static void CheckVmFirewall(List<DiagnosticResult> result,
                                        VmFirewallOptions fwOptions,
                                        string id,
                                        DiagnosticResultContext context)
    {
        var kind = context == DiagnosticResultContext.Qemu
                        ? "VM"
                        : "Container";

        if (!fwOptions.Enable)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0013",
                Description = $"{kind} firewall is disabled — {kind.ToLower()} is exposed to all traffic on the node bridge",
                Context = context,
                SubContext = "Firewall",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        if (fwOptions.Enable && !fwOptions.Ipfilter)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "IQ0009",
                Description = $"{kind} firewall IP filter is disabled — {kind.ToLower()} can spoof source IP addresses",
                Context = context,
                SubContext = "Firewall",
                Gravity = DiagnosticResultGravity.Info,
            });
        }
    }

    private record ThresholdRddData(IMemory Memory, INetIO NetIO, ICpu Cpu);

    private static void CheckThresholdHost(List<DiagnosticResult> result,
                                           SettingsThresholdHost thresholdHost,
                                           DiagnosticResultContext context,
                                           string id,
                                           IEnumerable<ThresholdRddData> rrdData,
                                           string cpuErrorCode = "WQ0025",
                                           string memoryErrorCode = "WQ0026",
                                           string netInErrorCode = "WQ0027",
                                           string netOutErrorCode = "WQ0028")
    {
        CheckThreshold(result,
                       thresholdHost.Cpu,
                       cpuErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.Cpu.CpuUsagePercentage) * 100,
                                               0d,
                                               id,
                                               $"CPU (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       true,
                       false);

        CheckThreshold(result,
                       thresholdHost.Memory,
                       memoryErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => Convert.ToDouble(a.Memory.MemoryUsage)),
                                               rrdData.Average(a => Convert.ToDouble(a.Memory.MemorySize)),
                                               id,
                                               $"Memory (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       false,
                       true);

        CheckThreshold(result,
                       thresholdHost.Network,
                       netInErrorCode,
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.NetIO.NetIn),
                                               0d,
                                               id,
                                               $"NetIn (rrd {thresholdHost.Rrd.TimeFrame} {thresholdHost.Rrd.Consolidation})")],
                       true,
                       false);

        CheckThreshold(result,
                       thresholdHost.Network,
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


    private static void CheckHealthScore(List<DiagnosticResult> result,
                                         SettingsThreshold<double> healthScore,
                                         DiagnosticResultContext context,
                                         string id,
                                         double weightedLoad)
    {
        if (healthScore.Warning <= 0 && healthScore.Critical <= 0) { return; }

        // Score = 100 - weighted load percentage (0=idle, 100=fully saturated)
        var score = Math.Round(100.0 - weightedLoad, 1);

        DiagnosticResultGravity? gravity = null;
        if (healthScore.Critical > 0 && score < healthScore.Critical)
        {
            gravity = DiagnosticResultGravity.Critical;
        }
        else if (healthScore.Warning > 0 && score < healthScore.Warning)
        {
            gravity = DiagnosticResultGravity.Warning;
        }


        if (gravity.HasValue)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WQ0032",
                Description = $"Health score is {score}/100 (threshold: warning={healthScore.Warning}, critical={healthScore.Critical})",
                Context = context,
                SubContext = "HealthScore",
                Gravity = gravity.Value,
            });
        }
    }

    private record ThresholdDataPoint(double Usage, double Size, string Id, string PrefixDescription);

    /// <summary>
    /// Checks usage/value against Warning and Critical thresholds and adds a DiagnosticResult for each breach.
    /// isValue=true  → Usage is an absolute value (e.g. percentage already computed).
    /// isValue=false → Usage/Size are raw bytes; percentage is computed internally.
    /// formatByte=true → appends human-readable byte sizes to the description.
    /// </summary>
    private static void CheckThreshold(List<DiagnosticResult> result,
                                       SettingsThreshold<double> threshold,
                                       string errorCode,
                                       DiagnosticResultContext context,
                                       string subContext,
                                       IEnumerable<ThresholdDataPoint> data,
                                       bool isValue,
                                       bool formatByte)
    {
        if (threshold.Warning == 0 || threshold.Critical == 0) { return; }

        var ranges = new[] { threshold.Warning, threshold.Critical, threshold.Critical * 100 };
        var gravity = new[] { DiagnosticResultGravity.Warning, DiagnosticResultGravity.Critical };

        for (int i = 0; i < 2; i++)
        {
            double GetValue(double usage, double size)
                => Math.Round(isValue
                                ? usage
                                : usage / size * 100.0, 1);

            string MakeDescription(string prefix, double usage, double size)
            {
                var txt = $"{prefix} usage {GetValue(usage, size)}%";
                if (formatByte) { txt += $" - {FormatHelper.FromBytes(usage)} of {FormatHelper.FromBytes(size)}"; }
                return txt;
            }

            result.AddRange(data.Where(a => GetValue(a.Usage, a.Size) >= ranges[i]
                                            && GetValue(a.Usage, a.Size) < ranges[i + 1])
                                .Select(a => new DiagnosticResult
                                {
                                    Id = a.Id,
                                    ErrorCode = errorCode,
                                    Description = MakeDescription(a.PrefixDescription, a.Usage, a.Size),
                                    Context = context,
                                    SubContext = subContext,
                                    Gravity = gravity[i],
                                }));
        }
    }

}
