/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings Threshold TimeSeries
/// </summary>
public class SettingsThresholdTimeSeries
{
    /// <summary>
    /// RRD fetch settings (time frame and consolidation function)
    /// </summary>
    public SettingsRrd Rrd { get; set; } = new();

    /// <summary>
    /// Threshold
    /// </summary>
    public SettingsThreshold<double> Threshold { get; set; } = new() { Warning = 70, Critical = 85 };
}
