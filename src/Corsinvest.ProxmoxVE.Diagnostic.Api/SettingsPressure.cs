/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// PSI pressure thresholds (PVE 9.0+)
/// </summary>
public class SettingsPressure
{
    /// <summary>
    /// PSI CPU pressure (some) — % of time at least one task stalled on CPU.
    /// </summary>
    public SettingsThreshold<double> Cpu { get; set; } = new() { Warning = 50, Critical = 80 };

    /// <summary>
    /// PSI I/O pressure (full) — % of time ALL tasks stalled on I/O.
    /// </summary>
    public SettingsThreshold<double> IoFull { get; set; } = new() { Warning = 20, Critical = 50 };

    /// <summary>
    /// PSI memory pressure (full) — % of time ALL tasks stalled on memory.
    /// </summary>
    public SettingsThreshold<double> MemoryFull { get; set; } = new() { Warning = 10, Critical = 30 };
}
