/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

 namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Diagnostic Result Context
/// </summary>
public enum DiagnosticResultContext
{
    /// <summary>
    /// Node
    /// </summary>
    Node,

    /// <summary>
    /// Cluster
    /// </summary>
    Cluster,

    /// <summary>
    /// Storage
    /// </summary>
    Storage,

    /// <summary>
    /// Qemu
    /// </summary>

    Qemu,

    /// <summary>
    /// Lxc
    /// </summary>
    Lxc
}
