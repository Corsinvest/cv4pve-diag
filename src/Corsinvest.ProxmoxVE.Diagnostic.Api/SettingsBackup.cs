/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for backup checks
/// </summary>
public class SettingsBackup
{
    /// <summary>
    /// Enable backup age checks. Disable to skip the API call per storage per VM/CT.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum age in days for old backup files before raising a warning.
    /// Old backups still present on storage waste space.
    /// Set to 0 to disable the check.
    /// </summary>
    public int MaxAgeDays { get; set; } = 60;

    /// <summary>
    /// Maximum age in days to consider a backup "recent".
    /// If no backup exists within this window, a warning is raised (RPO violation).
    /// Set to 0 to disable the check.
    /// </summary>
    public int RecentDays { get; set; } = 7;
}
