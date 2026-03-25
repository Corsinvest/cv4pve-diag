/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// RRD fetch settings
/// </summary>
public class SettingsRrd
{
    /// <summary>
    /// Time frame for RRD data (Day or Week)
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public RrdDataTimeFrame TimeFrame { get; set; } = RrdDataTimeFrame.Day;

    /// <summary>
    /// Consolidation function: Average (default) or Max (peak detection)
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public RrdDataConsolidation Consolidation { get; set; } = RrdDataConsolidation.Average;

    /// <summary>
    /// PSI CPU pressure (some) — % of time at least one task stalled on CPU. PVE 9.0+ only.
    /// </summary>
    public SettingsThreshold<double> PressureCpu { get; set; } = new()
    {
        Warning = 40,
        Critical = 70
    };

    /// <summary>
    /// PSI I/O pressure (full) — % of time ALL tasks stalled on I/O. PVE 9.0+ only.
    /// </summary>
    public SettingsThreshold<double> PressureIoFull { get; set; } = new()
    {
        Warning = 10,
        Critical = 30
    };

    /// <summary>
    /// PSI memory pressure (full) — % of time ALL tasks stalled on memory. PVE 9.0+ only.
    /// </summary>
    public SettingsThreshold<double> PressureMemoryFull { get; set; } = new()
    {
        Warning = 5,
        Critical = 15
    };
}
