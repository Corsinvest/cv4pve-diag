/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
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
    public SettingsThresholdHost Node { get; set; } = new();

    /// <summary>
    /// Qemu
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdHost Qemu { get; set; } = new()
    {
        HealthScore = new()
        {
            Warning = 60,
            Critical = 40
        }
    };

    /// <summary>
    /// Lxc
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdHost Lxc { get; set; } = new()
    {
        HealthScore = new()
        {
            Warning = 60,
            Critical = 40
        }
    };

    /// <summary>
    /// Threshold
    /// </summary>
    public SettingsThresholdPercentual SsdWearoutThreshold { get; set; } = new();

    /// <summary>
    /// Snapshot checks configuration
    /// </summary>
    public SettingsSnapshot Snapshot { get; set; } = new();
}
