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
 
 using System;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api
{
    /// <summary>
    /// Diagnostic result
    /// </summary>
    public class DiagnosticResult
    {
        /// <summary>
        /// Id
        /// </summary>
        /// <value></value>
        public string Id { get; set; }

        /// <summary>
        /// Tag
        /// </summary>
        /// <value></value>
        public object Tag { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        /// <value></value>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        /// <value></value>
        public DiagnosticResultContext Context { get; set; }

        /// <summary>
        /// Subcontext
        /// </summary>
        /// <value></value>
        public string SubContext { get; set; }

        /// <summary>
        /// Secription
        /// </summary>
        /// <value></value>
        public string Description { get; set; }

        /// <summary>
        /// Gravity
        /// </summary>
        /// <value></value>
        public DiagnosticResultGravity Gravity { get; set; }

        /// <summary>
        /// Decode context
        /// </summary>
        public static DiagnosticResultContext DecodeContext(string text) 
            => Enum.TryParse<DiagnosticResultContext>(text, true, out var ret) ?
                                (DiagnosticResultContext)ret :
                                DiagnosticResultContext.Cluster;
    }
}