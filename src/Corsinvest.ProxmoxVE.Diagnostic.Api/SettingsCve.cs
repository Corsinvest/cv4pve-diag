/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for CVE checks
/// </summary>
public class SettingsCve
{
    /// <summary>
    /// Enable NVD checks for Proxmox VE specific CVEs.
    /// Queries the NVD API once with a Proxmox VE CPE filter.
    /// </summary>
    public bool NvdEnabled { get; set; }

    /// <summary>
    /// Minimum CVSS v3 base score to report (0.0–10.0).
    /// CVE with a score below this threshold are silently ignored.
    /// Default: 7.0 (HIGH and CRITICAL only).
    /// Set to 0 to report all CVE regardless of score.
    /// </summary>
    public double MinCvssScore { get; set; } = 7.0;
}
