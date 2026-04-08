/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings for node-local storage checks (ZFS, LVM-thin, etc.)
/// </summary>
public class SettingsNodeStorage
{
    /// <summary>
    /// Enable detailed ZFS pool checks (vdev state, I/O errors).
    /// Requires one API call per ZFS pool per node.
    /// </summary>
    public bool ZfsDetail { get; set; } = false;

    /// <summary>
    /// Enable LVM-thin metadata usage check.
    /// Full metadata pool causes data corruption. One API call per node.
    /// </summary>
    public bool LvmThinMetadata { get; set; } = true;
}
