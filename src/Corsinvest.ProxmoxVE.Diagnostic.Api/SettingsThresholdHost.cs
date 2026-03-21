/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings Threshold Host
/// </summary>
public class SettingsThresholdHost
{
    /// <summary>
    /// TimeSeries
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public SettingsTimeSeriesType TimeSeries { get; set; } = SettingsTimeSeriesType.Day;

    /// <summary>
    /// Cpu
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdPercentual Cpu { get; set; } = new();

    /// <summary>
    /// Memory
    /// </summary>
    /// <returns></returns>
    public SettingsThresholdPercentual Memory { get; set; } = new();

    /// <summary>
    /// Network
    /// </summary>
    public SettingsThreshold<double> Network { get; set; } = new();

    /// <summary>
    /// Health Score
    /// </summary>
    public SettingsThresholdPercentual HealthScore { get; set; } = new();
}
