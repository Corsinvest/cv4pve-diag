/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for health score checks.
/// Score formula: Node = 100 - (cpu*0.4 + ram*0.4 + disk*0.2)
///                VM   = 100 - (cpu*0.5 + ram*0.5)
/// </summary>
public class SettingsHealthScore
{
    /// <summary>
    /// Score threshold below which a Warning is raised (default 60).
    /// Set to 0 to disable the check.
    /// </summary>
    [DisplayName("Warning Threshold")]
    public double WarningThreshold { get; set; } = 60;

    /// <summary>
    /// Score threshold below which a Critical issue is raised (default 40).
    /// Set to 0 to disable the check.
    /// </summary>
    [DisplayName("Critical Threshold")]
    public double CriticalThreshold { get; set; } = 40;
}
