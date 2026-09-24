/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings threshold for node — extends host thresholds with node-specific checks
/// </summary>
public class SettingsThresholdNode : SettingsThresholdHost
{
    /// <summary>
    /// Maximum vCPU overcommit ratio (total vCPUs / physical CPUs) before a warning is raised.
    /// Default 4.0 — e.g. 32 vCPUs on an 8-core node triggers the warning.
    /// </summary>
    public double MaxVCpuRatio { get; set; } = 4.0;

    /// <summary>
    /// CPU utilization threshold (%) below which a node is considered underutilized for consolidation.
    /// </summary>
    public double ConsolidationCpuThreshold { get; set; } = 10.0;

    /// <summary>
    /// Memory utilization threshold (%) below which a node is considered underutilized for consolidation.
    /// </summary>
    public double ConsolidationMemThreshold { get; set; } = 20.0;

    /// <summary>
    /// CPU IOWait threshold (% of CPU time spent waiting for I/O, averaged over the RRD time frame).
    /// Sustained values above 10% point to a storage bottleneck (WN0028).
    /// </summary>
    public SettingsThreshold<double> IoWait { get; set; } = new() { Warning = 10, Critical = 25 };

    /// <summary>
    /// S.M.A.R.T. disk checks configuration
    /// </summary>
    public SettingsSmartDisk Smart { get; set; } = new();

    /// <summary>
    /// Node-local storage checks (ZFS detail, LVM-thin metadata)
    /// </summary>
    public SettingsNodeStorage NodeStorage { get; set; } = new();

    /// <summary>
    /// Node-specific PSI pressure thresholds (lower than VM defaults), set on the inherited
    /// <see cref="SettingsThresholdHost.Rrd"/>. A 'new Rrd' property here used to hide it: the
    /// data fetch read this one, the threshold labels read the base one, never deserialized.
    /// </summary>
    public SettingsThresholdNode()
    {
        Rrd = new()
        {
            Pressure = new()
            {
                Cpu = new() { Warning = 40, Critical = 70 },
                IoFull = new() { Warning = 10, Critical = 30 },
                MemoryFull = new() { Warning = 5, Critical = 15 },
            }
        };
    }
}
