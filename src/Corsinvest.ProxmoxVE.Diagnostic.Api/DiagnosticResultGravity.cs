/*
 * This file is part of the cv4pve-diag https://github.com/Corsinvest/cv4pve-diag,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */
 
 namespace Corsinvest.ProxmoxVE.Diagnostic.Api
{
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
}