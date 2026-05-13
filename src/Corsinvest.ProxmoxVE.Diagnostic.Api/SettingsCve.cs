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
    /// Enable Debian Security Tracker checks.
    /// Downloads CVE data for all Debian packages installed on each node.
    /// Covers system packages (kernel, qemu, lxc, openssl, ...).
    /// </summary>
    public bool DebianTrackerEnabled { get; set; }

    /// <summary>
    /// Enable NVD checks for Proxmox-specific CVE.
    /// Queries NVD API once with keywordSearch=proxmox to find CVE affecting PVE directly.
    /// Optional API key speeds up the request (free at https://nvd.nist.gov/developers/request-an-api-key).
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
