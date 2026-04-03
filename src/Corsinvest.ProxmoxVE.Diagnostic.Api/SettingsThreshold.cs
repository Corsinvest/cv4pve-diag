/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */


namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings Threshold
/// </summary>
public class SettingsThreshold<T>
{
    /// <summary>
    /// Warning
    /// </summary>
    public T Warning { get; set; } = default!;

    /// <summary>
    /// Critical
    /// </summary>
    /// <value></value>
    public T Critical { get; set; } = default!;
}
