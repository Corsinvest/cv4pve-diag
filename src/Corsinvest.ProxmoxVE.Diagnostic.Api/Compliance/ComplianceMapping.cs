/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

/// <summary>
/// One normative control that a diagnostic check maps to.
/// A <see cref="DiagnosticResult"/> may carry multiple mappings — even several within
/// the same standard — when the check satisfies more than one control.
/// </summary>
/// <param name="Standard">The standard family (ISO 27001, NIS2, DORA, …).</param>
/// <param name="ControlId">The control identifier as published by the standard (e.g. "A.5.17", "Art.21(j)").</param>
/// <param name="ControlTitle">Short human-readable title of the control.</param>
public sealed record ComplianceMapping(ComplianceStandard Standard, string ControlId, string ControlTitle);
