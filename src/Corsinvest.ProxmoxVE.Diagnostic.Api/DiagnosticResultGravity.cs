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
    /// Ok — a check that was evaluated and passed.
    /// Emitted only when <c>Settings.IncludeOkResult</c> is enabled.
    /// Value is -1 so adding it does not shift the existing numeric ordering of Info/Warning/Critical.
    /// </summary>
    Ok = -1,

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
