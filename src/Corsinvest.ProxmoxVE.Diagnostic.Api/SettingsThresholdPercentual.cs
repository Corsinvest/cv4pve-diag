/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Settings Threshold Percentual
/// </summary>
public class SettingsThresholdPercentual : SettingsThreshold<double>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public SettingsThresholdPercentual()
    {
        Warning = 70;
        Critical = 80;
    }
}
