/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

/// <summary>
/// Normative standards a diagnostic check can map to.
/// A single check may map to several controls across multiple standards —
/// see <see cref="ComplianceMapping"/>.
/// </summary>
public enum ComplianceStandard
{
    /// <summary>ISO/IEC 27001:2022 — Information security management systems.</summary>
    Iso27001,

    /// <summary>ISO/IEC 27017 — Security controls for cloud services.</summary>
    Iso27017,

    /// <summary>EU NIS2 Directive — Network and Information Security.</summary>
    Nis2,

    /// <summary>EU DORA — Digital Operational Resilience Act.</summary>
    Dora,

    /// <summary>EU GDPR — General Data Protection Regulation.</summary>
    Gdpr,

    /// <summary>PCI DSS v4.0 — Payment Card Industry Data Security Standard.</summary>
    PciDss,

    /// <summary>NIST Cybersecurity Framework v2.0.</summary>
    NistCsf,

    /// <summary>CIS Controls v8 — Center for Internet Security.</summary>
    Cis,

    /// <summary>AgID — Misure minime di sicurezza ICT per le Pubbliche Amministrazioni (Italy).</summary>
    AgId,
}
