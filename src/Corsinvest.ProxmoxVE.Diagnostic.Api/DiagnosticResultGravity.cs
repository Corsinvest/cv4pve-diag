/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Diagnostic result gravity
/// </summary>
public enum DiagnosticResultGravity
{
    /// <summary>
    /// Info
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Critical
    /// </summary>
    Critical = 2
}
