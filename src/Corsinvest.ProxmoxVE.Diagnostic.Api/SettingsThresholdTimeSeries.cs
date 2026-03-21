/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings Threshold TimeSeries
/// </summary>
public class SettingsThresholdTimeSeries
{
    /// <summary>
    /// TimeSeries
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public SettingsTimeSeriesType TimeSeries { get; set; } = SettingsTimeSeriesType.Day;

    /// <summary>
    /// Threshold
    /// </summary>
    public SettingsThresholdPercentual Threshold { get; set; } = new SettingsThresholdPercentual();
}
