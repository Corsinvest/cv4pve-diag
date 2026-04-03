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
    /// S.M.A.R.T. disk checks configuration
    /// </summary>
    public SettingsSmartDisk Smart { get; set; } = new();

    /// <summary>
    /// Node-local storage checks (ZFS detail, LVM-thin metadata)
    /// </summary>
    public SettingsNodeStorage NodeStorage { get; set; } = new();

    /// <summary>
    /// RRD fetch settings with node-specific PSI pressure thresholds (lower than VM defaults)
    /// </summary>
    public new SettingsRrd Rrd { get; set; } = new()
    {
        Pressure = new()
        {
            Cpu = new() { Warning = 40, Critical = 70 },
            IoFull = new() { Warning = 10, Critical = 30 },
            MemoryFull = new() { Warning = 5, Critical = 15 },
        }
    };
}
