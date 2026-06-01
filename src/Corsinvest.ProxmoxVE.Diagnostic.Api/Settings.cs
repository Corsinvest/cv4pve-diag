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
    /// Max parallel requests when fetching (1 = sequential)
    /// </summary>
    public int MaxParallelRequests { get; set; } = 5;

    /// <summary>
    /// API timeout in seconds (0 = use default)
    /// </summary>
    public int ApiTimeout { get; set; } = 0;

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

    /// <summary>
    /// CVE checks configuration
    /// </summary>
    public SettingsCve Cve { get; set; } = new();

    /// <summary>
    /// When true, every diagnostic check that uses CreateResult / CreateResultPerItem
    /// also emits an Ok result on success (Gravity = Ok). Useful for full audit-style
    /// reports where you need to prove that controls were verified, not only violated.
    /// When false (default), output is identical to the legacy mode — only failures appear.
    /// </summary>
    public bool IncludeOkResult { get; set; }
}
