/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings
/// </summary>
public class Settings
{
    /// <summary>
    /// Store
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdTimeSeries Storage { get; set; } = new SettingsThresholdTimeSeries();

    /// <summary>
    /// Node
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdNode Node { get; set; } = new();

    /// <summary>
    /// Qemu
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdQemu Qemu { get; set; } = new()
    {
        HealthScore = new() { Warning = 60, Critical = 40 },
        Rrd = new()
        {
            Pressure = new()
            {
                Cpu = new() { Warning = 50, Critical = 80 },
                IoFull = new() { Warning = 20, Critical = 50 },
                MemoryFull = new() { Warning = 10, Critical = 30 },
            }
        },
    };

    /// <summary>
    /// Lxc
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdHost Lxc { get; set; } = new()
    {
        HealthScore = new() { Warning = 60, Critical = 40 },
        Rrd = new()
        {
            Pressure = new()
            {
                Cpu = new() { Warning = 50, Critical = 80 },
                IoFull = new() { Warning = 20, Critical = 50 },
                MemoryFull = new() { Warning = 10, Critical = 30 },
            }
        },
    };

    /// <summary>
    /// Snapshot checks configuration
    /// </summary>
    public SettingsSnapshot Snapshot { get; set; } = new();

    /// <summary>
    /// Backup checks configuration
    /// </summary>
    public SettingsBackup Backup { get; set; } = new();

}
