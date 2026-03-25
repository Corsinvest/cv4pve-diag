/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */


namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for snapshot checks
/// </summary>
public class SettingsSnapshot
{
    /// <summary>
    /// Enable snapshot checks. Disable to skip the API call per VM/CT.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of snapshots per VM/LXC before raising a warning.
    /// Too many snapshots degrade I/O performance due to long delta chains.
    /// Set to 0 to disable the check.
    /// </summary>
    public int MaxCount { get; set; } = 10;

    /// <summary>
    /// Maximum age in days for snapshots before raising a warning.
    /// Old snapshots are likely forgotten and waste storage space.
    /// Set to 0 to disable the check.
    /// </summary>
    public int MaxAgeDays { get; set; } = 30;
}
