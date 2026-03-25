/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings Threshold Host
/// </summary>
public class SettingsThresholdHost
{
    /// <summary>
    /// RRD fetch settings (time frame, consolidation function and PSI pressure thresholds)
    /// </summary>
    public SettingsRrd Rrd { get; set; } = new();

    /// <summary>
    /// Cpu
    /// </summary>
    /// <returns></returns>
    public SettingsThreshold<double> Cpu { get; set; } = new() { Warning = 70, Critical = 85 };

    /// <summary>
    /// Memory
    /// </summary>
    /// <returns></returns>
    public SettingsThreshold<double> Memory { get; set; } = new() { Warning = 70, Critical = 85 };

    /// <summary>
    /// Network
    /// </summary>
    public SettingsThreshold<double> Network { get; set; } = new();

    /// <summary>
    /// Health Score
    /// </summary>
    public SettingsThreshold<double> HealthScore { get; set; } = new() { Warning = 70, Critical = 50 };
}
