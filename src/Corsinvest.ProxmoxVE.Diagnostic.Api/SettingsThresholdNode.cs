/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings threshold for node — extends host thresholds with node-specific checks
/// </summary>
public class SettingsThresholdNode : SettingsThresholdHost
{
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
        PressureCpu = new() { Warning = 40, Critical = 70 },
        PressureIoFull = new() { Warning = 10, Critical = 30 },
        PressureMemoryFull = new() { Warning = 5, Critical = 15 },
    };
}
