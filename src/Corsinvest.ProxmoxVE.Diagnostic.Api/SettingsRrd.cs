/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
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
    /// PSI pressure thresholds (CPU, I/O, Memory). PVE 9.0+ only.
    /// </summary>
    public SettingsPressure Pressure { get; set; } = new();
}
