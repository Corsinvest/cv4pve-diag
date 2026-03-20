/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for snapshot checks
/// </summary>
public class SettingsSnapshot
{
    /// <summary>
    /// Maximum number of snapshots per VM/LXC before raising a warning.
    /// Too many snapshots degrade I/O performance due to long delta chains.
    /// Set to 0 to disable the check.
    /// </summary>
    [DisplayName("Max Count")]
    public int MaxCount { get; set; } = 10;

    /// <summary>
    /// Maximum age in days for snapshots before raising a warning.
    /// Old snapshots are likely forgotten and waste storage space.
    /// Set to 0 to disable the check.
    /// </summary>
    [DisplayName("Max Age Days")]
    public int MaxAgeDays { get; set; } = 30;
}
