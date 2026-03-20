/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api;
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
                                          IEnumerable<ClusterBackup> clusterBackups)
    {
        #region VM State
        // A saved vmstate (hibernate) left in pending means the VM was suspended and never resumed properly
        _result.AddRange(pending.Where(a => a.Key == "vmstate")
                                .Select(a => new DiagnosticResult
                                {
                                    Id = id,
                                    ErrorCode = "WV0001",
                                    Description = $"Found vmstate '{a.Value}'",
                                    Context = context,
                                    SubContext = "VM State",
                                    Gravity = DiagnosticResultGravity.Critical,
                                }));
        #endregion

        #region Locked
        // A locked VM/CT cannot be started, stopped or migrated until the lock is cleared
        if (config.IsLocked)
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WV0001",
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
                ErrorCode = "WV0001",
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
                ErrorCode = "WV0001",
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
                    ErrorCode = "CC0001",
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
                                         ErrorCode = "WV0001",
                                         Description = $"Disk '{a.Id}' disabled for backup",
                                         Context = context,
                                         SubContext = "Backup",
                                         Gravity = DiagnosticResultGravity.Critical,
                                     }));

        // Check backup age via storage content
        var nodeApi = client.Nodes[node];
        var nodeStorages = await nodeApi.Storage.GetAsync();

        #region Unused disks
        // Disks detached from the VM/CT config but still present in storage — consuming space silently
        foreach (var unusedDisk in config.Disks.Where(a => a.IsUnused))
        {
            var volume = $"{unusedDisk.Storage}:{unusedDisk.FileName}";
            var ns = nodeStorages.FirstOrDefault(a => a.Storage == unusedDisk.Storage);
            var size = string.Empty;
            if (ns != null && ns.Active)
            {
                var contents = await nodeApi.Storage[ns.Storage].Content.GetAsync();
                size = FormatHelper.FromBytes(contents.FirstOrDefault(a => a.Volume == volume)?.Size ?? 0).ToString();
            }
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "IV0001",
                Description = $"disk '{unusedDisk.Id}' {size}",
                Context = context,
                SubContext = "Hardware",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
        #endregion
        var backupContents = new List<NodeStorageContent>();
        foreach (var ns in nodeStorages.Where(a => a.Content != null && a.Content.Split(",").Contains("backup") && a.Active))
        {
            var contents = await nodeApi.Storage[ns.Storage].Content.GetAsync();
            backupContents.AddRange(contents.Where(a => a.VmId == vmId && a.Content == "backup"));
        }

        // Old backups (>60 days) still present waste storage space
        const int dayOld = 60;
        const int dayRecent = 7;
        var oldBackups = backupContents.Where(a => a.CreationDate.Date <= _now.Date.AddDays(-dayOld)).ToList();
        if (oldBackups.Count > 0)
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "CC0001",
                Description = $"{oldBackups.Count} backup {FormatHelper.FromBytes(oldBackups.Sum(a => a.Size))} more {dayOld} days are found!",
                Context = context,
                SubContext = "Backup",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        // No backup found in the last 7 days — RPO violation
        if (!backupContents.Any(a => a.CreationDate.Date >= _now.Date.AddDays(-dayRecent)))
        {
            _result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "CC0001",
                Description = "No recent backups found!",
                Context = context,
                SubContext = "Backup",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
        #endregion

        #region Task history
        // Failed tasks for this VM in the last 48 hours (filtered server-side with errors=true)
        var dayTask = new DateTimeOffset(_now.AddDays(-2)).ToUnixTimeSeconds();
        var tasks = (await nodeApi.Tasks.GetAsync(errors: true, limit: 1000))
                    .Where(a => a.StartTime >= dayTask && (a.VmId?.Equals(vmId.ToString()) ?? false));
        CheckTaskHistory(_result, tasks, context, id);
        #endregion

        CheckSnapshots(_result, snapshots, settings.Snapshot, _now, id, context);

        CheckThresholdHost(_result, settings, thresholdHost, context, id, rrdData.Select(a => new ThresholdRddData(a, a, a)));
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
                ErrorCode = "IN0001",
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
                ErrorCode = "WV0003",
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
                ErrorCode = "WV0003",
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
                    ErrorCode = "WV0003",
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
                ErrorCode = "WV0003",
                Description = $"{realSnapshots.Count} snapshots exceed the maximum of {snapshotSettings.MaxCount}",
                Context = context,
                SubContext = "SnapshotCount",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
    }

    private record ThresholdRddData(IMemory Memory, INetIO NetIO, ICpu Cpu);

    private static void CheckThresholdHost(List<DiagnosticResult> result,
                                           Settings settings,
                                           SettingsThresholdHost thresholdHost,
                                           DiagnosticResultContext context,
                                           string id,
                                           IEnumerable<ThresholdRddData> rrdData)
    {
        CheckThreshold(result,
                       thresholdHost.Cpu,
                       "WV0002",
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.Cpu.CpuUsagePercentage) * 100,
                                               0d,
                                               id,
                                               $"CPU (rrd {thresholdHost.TimeSeries} AVERAGE)")],
                       true,
                       false);

        CheckThreshold(result,
                       thresholdHost.Memory,
                       "WV0002",
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => Convert.ToDouble(a.Memory.MemoryUsage)),
                                               rrdData.Average(a => Convert.ToDouble(a.Memory.MemorySize)),
                                               id,
                                               $"Memory (rrd {thresholdHost.TimeSeries} AVERAGE)")],
                       false,
                       true);

        CheckThreshold(result,
                       thresholdHost.Network,
                       "WV0002",
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.NetIO.NetIn),
                                               0d,
                                               id,
                                               $"NetIn (rrd {thresholdHost.TimeSeries} AVERAGE)")],
                       true,
                       false);

        CheckThreshold(result,
                       thresholdHost.Network,
                       "WV0002",
                       context,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.NetIO.NetOut),
                                               0d,
                                               id,
                                               $"NetOut (rrd {thresholdHost.TimeSeries} AVERAGE)")],
                       true,
                       false);

        // Health score for VM/LXC: 100 - (cpu*0.5 + ram*0.5)
        var cpuPct = rrdData.Average(a => a.Cpu.CpuUsagePercentage) * 100.0;
        var ramPct = rrdData.Any(a => Convert.ToDouble(a.Memory.MemorySize) > 0)
                        ? rrdData.Average(a => Convert.ToDouble(a.Memory.MemoryUsage) / Convert.ToDouble(a.Memory.MemorySize) * 100.0)
                        : 0.0;
        CheckHealthScore(result, settings.HealthScore, context, id, cpuPct * 0.5 + ramPct * 0.5);
    }

    private static void CheckNodeRrd(List<DiagnosticResult> result,
                                     Settings settings,
                                     string id,
                                     IEnumerable<NodeRrdData> rrdData)
    {
        CheckThresholdHost(result,
                           settings,
                           settings.Node,
                           DiagnosticResultContext.Node,
                           id,
                           rrdData.Select(a => new ThresholdRddData(a, a, a)));

        // IOWait = time CPU spent waiting for I/O — high values indicate storage bottleneck
        CheckThreshold(result,
                       settings.Node.Cpu,
                       "WV0002",
                       DiagnosticResultContext.Node,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.IoWait) * 100,
                                               0d,
                                               id,
                                               $"IOWait (rrd {settings.Node.TimeSeries} AVERAGE)")],
                       true,
                       false);

        // Root filesystem usage on the node OS disk
        CheckThreshold(result,
                       settings.Storage.Threshold,
                       "WV0002",
                       DiagnosticResultContext.Node,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.RootUsage),
                                               rrdData.Average(a => a.RootSize),
                                               id,
                                               $"Root space (rrd {settings.Node.TimeSeries} AVERAGE)")],
                       false,
                       true);

        // SWAP usage — high swap indicates RAM pressure and causes severe performance degradation
        CheckThreshold(result,
                       settings.Storage.Threshold,
                       "WV0002",
                       DiagnosticResultContext.Node,
                       "Usage",
                       [new ThresholdDataPoint(rrdData.Average(a => a.SwapUsage),
                                               rrdData.Average(a => Convert.ToDouble(a.SwapSize)),
                                               id,
                                               $"SWAP (rrd {settings.Node.TimeSeries} AVERAGE)")],
                       false,
                       true);

        // Health score for nodes: 100 - (cpu*0.4 + ram*0.4 + disk*0.2)
        var nodeCpuPct = rrdData.Average(a => a.CpuUsagePercentage) * 100.0;
        var nodeRamPct = rrdData.Any(a => a.MemorySize > 0)
                            ? rrdData.Average(a => (double)a.MemoryUsage / a.MemorySize * 100.0)
                            : 0.0;

        var nodeDiskPct = rrdData.Any(a => a.RootSize > 0)
                            ? rrdData.Average(a => a.RootUsage / a.RootSize * 100.0)
                            : 0.0;

        CheckHealthScore(result,
                         settings.HealthScore,
                         DiagnosticResultContext.Node,
                         id,
                         nodeCpuPct * 0.4 + nodeRamPct * 0.4 + nodeDiskPct * 0.2);
    }

    private static void CheckHealthScore(List<DiagnosticResult> result,
                                         SettingsHealthScore healthScore,
                                         DiagnosticResultContext context,
                                         string id,
                                         double weightedLoad)
    {
        if (healthScore.WarningThreshold <= 0 && healthScore.CriticalThreshold <= 0) { return; }

        // Score = 100 - weighted load percentage (0=idle, 100=fully saturated)
        var score = Math.Round(100.0 - weightedLoad, 1);

        DiagnosticResultGravity? gravity = null;
        if (healthScore.CriticalThreshold > 0 && score < healthScore.CriticalThreshold)
        {
            gravity = DiagnosticResultGravity.Critical;
        }
        else if (healthScore.WarningThreshold > 0 && score < healthScore.WarningThreshold)
        {
            gravity = DiagnosticResultGravity.Warning;
        }


        if (gravity.HasValue)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "HS0001",
                Description = $"Health score is {score}/100 (threshold: warning={healthScore.WarningThreshold}, critical={healthScore.CriticalThreshold})",
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
            double GetValue(double usage, double size) => Math.Round(isValue ? usage : usage / size * 100.0, 1);

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
