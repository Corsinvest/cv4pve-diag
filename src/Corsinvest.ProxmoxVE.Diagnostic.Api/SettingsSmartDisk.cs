/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for S.M.A.R.T. disk checks.
/// Requires one API call per disk per node — disabled by default.
/// </summary>
public class SettingsSmartDisk
{
    /// <summary>
    /// Enable detailed S.M.A.R.T. attribute checks (temperature, bad sectors, CRC errors).
    /// Disabled by default because it requires one API call per disk per node.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Disk temperature threshold in Celsius.
    /// Warning at 55°C, Critical at 65°C. Set Warning to 0 to disable.
    /// </summary>
    public SettingsThreshold<int> Temperature { get; set; } = new()
    {
        Warning = 55,
        Critical = 65
    };

    /// <summary>
    /// SSD wearout threshold — percentage of life consumed (100 - wearout reported by PVE).
    /// Warning at 70%, Critical at 80%.
    /// </summary>
    public SettingsThreshold<double> SsdWearout { get; set; } = new() { Warning = 70, Critical = 85 };
}
