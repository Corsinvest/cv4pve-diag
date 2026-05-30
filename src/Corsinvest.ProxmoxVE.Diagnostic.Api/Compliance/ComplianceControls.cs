/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Collections.Frozen;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

/// <summary>
/// Catalog of compliance controls referenced by diagnostic checks.
/// Each entry is declared once as a static readonly singleton and reused from any check
/// that maps to that control — avoids string duplication and keeps a single source of truth
/// for the wording of each normative reference.
/// </summary>
public static class ComplianceControls
{
    /// <summary>ISO/IEC 27001:2022 — Information security management systems.</summary>
    public static class Iso27001
    {
        /// <summary>A.5.15 — Access control.</summary>
        public static readonly ComplianceMapping A_5_15 =
            new(ComplianceStandard.Iso27001, "A.5.15", "Access control");

        /// <summary>A.5.16 — Identity management.</summary>
        public static readonly ComplianceMapping A_5_16 =
            new(ComplianceStandard.Iso27001, "A.5.16", "Identity management");

        /// <summary>A.5.17 — Authentication information.</summary>
        public static readonly ComplianceMapping A_5_17 =
            new(ComplianceStandard.Iso27001, "A.5.17", "Authentication information");

        /// <summary>A.5.18 — Access rights.</summary>
        public static readonly ComplianceMapping A_5_18 =
            new(ComplianceStandard.Iso27001, "A.5.18", "Access rights");

        /// <summary>A.5.30 — ICT readiness for business continuity.</summary>
        public static readonly ComplianceMapping A_5_30 =
            new(ComplianceStandard.Iso27001, "A.5.30", "ICT readiness for business continuity");

        /// <summary>A.8.2 — Privileged access rights.</summary>
        public static readonly ComplianceMapping A_8_2 =
            new(ComplianceStandard.Iso27001, "A.8.2", "Privileged access rights");

        /// <summary>A.8.5 — Secure authentication.</summary>
        public static readonly ComplianceMapping A_8_5 =
            new(ComplianceStandard.Iso27001, "A.8.5", "Secure authentication");

        /// <summary>A.8.8 — Management of technical vulnerabilities.</summary>
        public static readonly ComplianceMapping A_8_8 =
            new(ComplianceStandard.Iso27001, "A.8.8", "Management of technical vulnerabilities");

        /// <summary>A.8.13 — Information backup.</summary>
        public static readonly ComplianceMapping A_8_13 =
            new(ComplianceStandard.Iso27001, "A.8.13", "Information backup");

        /// <summary>A.8.15 — Logging.</summary>
        public static readonly ComplianceMapping A_8_15 =
            new(ComplianceStandard.Iso27001, "A.8.15", "Logging");

        /// <summary>A.8.16 — Monitoring activities.</summary>
        public static readonly ComplianceMapping A_8_16 =
            new(ComplianceStandard.Iso27001, "A.8.16", "Monitoring activities");

        /// <summary>A.8.20 — Networks security.</summary>
        public static readonly ComplianceMapping A_8_20 =
            new(ComplianceStandard.Iso27001, "A.8.20", "Networks security");

        /// <summary>A.8.22 — Segregation of networks.</summary>
        public static readonly ComplianceMapping A_8_22 =
            new(ComplianceStandard.Iso27001, "A.8.22", "Segregation of networks");

        /// <summary>A.8.24 — Use of cryptography.</summary>
        public static readonly ComplianceMapping A_8_24 =
            new(ComplianceStandard.Iso27001, "A.8.24", "Use of cryptography");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            A_5_15, A_5_16, A_5_17, A_5_18, A_5_30,
            A_8_2, A_8_5, A_8_8, A_8_13, A_8_15, A_8_16, A_8_20, A_8_22, A_8_24,
        ];
    }

    /// <summary>EU NIS2 Directive — Network and Information Security.</summary>
    public static class Nis2
    {
        /// <summary>Art.21(c) — Backup management and disaster recovery.</summary>
        public static readonly ComplianceMapping Art_21_c =
            new(ComplianceStandard.Nis2, "Art.21(c)", "Backup management and disaster recovery");

        /// <summary>Art.21(d) — Supply chain security, including security-related aspects of relationships with suppliers and service providers.</summary>
        public static readonly ComplianceMapping Art_21_d =
            new(ComplianceStandard.Nis2, "Art.21(d)", "Identity / access lifecycle management");

        /// <summary>Art.21(e) — Security in network and information systems acquisition, development and maintenance, including vulnerability handling and disclosure.</summary>
        public static readonly ComplianceMapping Art_21_e =
            new(ComplianceStandard.Nis2, "Art.21(e)", "Vulnerability handling and disclosure");

        /// <summary>Art.21(f) — Policies and procedures to assess the effectiveness of cybersecurity risk-management measures.</summary>
        public static readonly ComplianceMapping Art_21_f =
            new(ComplianceStandard.Nis2, "Art.21(f)", "Effectiveness assessment (logging/monitoring)");

        /// <summary>Art.21(h) — Cryptography and encryption.</summary>
        public static readonly ComplianceMapping Art_21_h =
            new(ComplianceStandard.Nis2, "Art.21(h)", "Cryptography and encryption");

        /// <summary>Art.21(i) — Human resources security, access control policies and asset management.</summary>
        public static readonly ComplianceMapping Art_21_i =
            new(ComplianceStandard.Nis2, "Art.21(i)", "Access control policies and asset management");

        /// <summary>Art.21(j) — Multi-factor authentication.</summary>
        public static readonly ComplianceMapping Art_21_j =
            new(ComplianceStandard.Nis2, "Art.21(j)", "Multi-factor authentication");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            Art_21_c, Art_21_d, Art_21_e, Art_21_f, Art_21_h, Art_21_i, Art_21_j,
        ];
    }

    /// <summary>EU DORA — Digital Operational Resilience Act.</summary>
    public static class Dora
    {
        /// <summary>Art.9 — ICT security policies.</summary>
        public static readonly ComplianceMapping Art_9 =
            new(ComplianceStandard.Dora, "Art.9", "ICT security policies");

        /// <summary>Art.10 — Detection of anomalous activities (logging / monitoring).</summary>
        public static readonly ComplianceMapping Art_10 =
            new(ComplianceStandard.Dora, "Art.10", "Detection of anomalous activities");

        /// <summary>Art.11 — Backup policies and recovery procedures.</summary>
        public static readonly ComplianceMapping Art_11 =
            new(ComplianceStandard.Dora, "Art.11", "Backup policies and recovery procedures");

        /// <summary>Art.12 — ICT business continuity policy.</summary>
        public static readonly ComplianceMapping Art_12 =
            new(ComplianceStandard.Dora, "Art.12", "ICT business continuity policy");

        internal static IEnumerable<ComplianceMapping> All => [Art_9, Art_10, Art_11, Art_12];
    }

    /// <summary>PCI DSS v4.0 — Payment Card Industry Data Security Standard.</summary>
    public static class PciDss
    {
        /// <summary>1.2 — Network security controls configuration.</summary>
        public static readonly ComplianceMapping R_1_2 =
            new(ComplianceStandard.PciDss, "1.2", "Network security controls configuration");

        /// <summary>4.2 — Strong cryptography over open, public networks.</summary>
        public static readonly ComplianceMapping R_4_2 =
            new(ComplianceStandard.PciDss, "4.2", "Strong cryptography over open, public networks");

        /// <summary>6.3 — Security vulnerabilities are identified and addressed.</summary>
        public static readonly ComplianceMapping R_6_3 =
            new(ComplianceStandard.PciDss, "6.3", "Security vulnerabilities are identified and addressed");

        /// <summary>7.2 — Access to system components and data is appropriately defined and assigned.</summary>
        public static readonly ComplianceMapping R_7_2 =
            new(ComplianceStandard.PciDss, "7.2", "Access definition and assignment");

        /// <summary>8.2 — User identification and related accounts for users and administrators are strictly managed.</summary>
        public static readonly ComplianceMapping R_8_2 =
            new(ComplianceStandard.PciDss, "8.2", "User identification and account lifecycle");

        /// <summary>8.4.2 — MFA for all access into the cardholder data environment.</summary>
        public static readonly ComplianceMapping R_8_4_2 =
            new(ComplianceStandard.PciDss, "8.4.2", "MFA for all access into the cardholder data environment");

        /// <summary>10.2 — Audit logs are implemented to support detection of anomalies.</summary>
        public static readonly ComplianceMapping R_10_2 =
            new(ComplianceStandard.PciDss, "10.2", "Audit logs for anomaly detection");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            R_1_2, R_4_2, R_6_3, R_7_2, R_8_2, R_8_4_2, R_10_2,
        ];
    }

    /// <summary>
    /// EU GDPR — General Data Protection Regulation (Regulation (EU) 2016/679).
    /// Only the articles with verifiable technical requirements are listed.
    /// Procedural / organisational requirements (DPIA, breach notification, data subject rights, …)
    /// are out of scope for a virtualisation cluster diagnostic tool.
    /// </summary>
    public static class Gdpr
    {
        /// <summary>Art. 5(1)(f) — Integrity and confidentiality: personal data shall be processed in a manner that ensures appropriate security, including protection against unauthorised or unlawful processing and against accidental loss, destruction or damage.</summary>
        public static readonly ComplianceMapping Art_5_1_f =
            new(ComplianceStandard.Gdpr, "Art.5(1)(f)", "Integrity and confidentiality (security principle)");

        /// <summary>Art. 32(1)(a) — Pseudonymisation and encryption of personal data.</summary>
        public static readonly ComplianceMapping Art_32_1_a =
            new(ComplianceStandard.Gdpr, "Art.32(1)(a)", "Pseudonymisation and encryption of personal data");

        /// <summary>Art. 32(1)(b) — Ability to ensure the ongoing confidentiality, integrity, availability and resilience of processing systems and services.</summary>
        public static readonly ComplianceMapping Art_32_1_b =
            new(ComplianceStandard.Gdpr, "Art.32(1)(b)", "Confidentiality, integrity, availability and resilience of processing systems");

        /// <summary>Art. 32(1)(c) — Ability to restore the availability and access to personal data in a timely manner in the event of a physical or technical incident.</summary>
        public static readonly ComplianceMapping Art_32_1_c =
            new(ComplianceStandard.Gdpr, "Art.32(1)(c)", "Timely restoration of availability and access after an incident");

        /// <summary>Art. 32(1)(d) — Process for regularly testing, assessing and evaluating the effectiveness of technical and organisational measures.</summary>
        public static readonly ComplianceMapping Art_32_1_d =
            new(ComplianceStandard.Gdpr, "Art.32(1)(d)", "Regular testing of the effectiveness of security measures");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            Art_5_1_f, Art_32_1_a, Art_32_1_b, Art_32_1_c, Art_32_1_d,
        ];
    }

    /// <summary>
    /// AgID — Misure minime di sicurezza ICT per le Pubbliche Amministrazioni
    /// (Circolare AgID n. 2/2017). Subset of ABSC (AgID Basic Security Controls).
    /// </summary>
    public static class AgId
    {
        /// <summary>ABSC 2.3 — Authorised software list, EOL tracking.</summary>
        public static readonly ComplianceMapping ABSC_2_3 =
            new(ComplianceStandard.AgId, "ABSC 2.3", "Authorised software list and EOL tracking");

        /// <summary>ABSC 3.1 — Use secure standard configurations.</summary>
        public static readonly ComplianceMapping ABSC_3_1 =
            new(ComplianceStandard.AgId, "ABSC 3.1", "Use secure standard configurations");

        /// <summary>ABSC 3.2 — Keep configurations aligned across systems.</summary>
        public static readonly ComplianceMapping ABSC_3_2 =
            new(ComplianceStandard.AgId, "ABSC 3.2", "Keep configurations aligned and up to date");

        /// <summary>ABSC 4.1 — Perform regular vulnerability scans.</summary>
        public static readonly ComplianceMapping ABSC_4_1 =
            new(ComplianceStandard.AgId, "ABSC 4.1", "Perform regular vulnerability scans");

        /// <summary>ABSC 4.4 — Verify that detected vulnerabilities are remediated.</summary>
        public static readonly ComplianceMapping ABSC_4_4 =
            new(ComplianceStandard.AgId, "ABSC 4.4", "Verify vulnerability remediation");

        /// <summary>ABSC 5.1 — Limit administrative privileges.</summary>
        public static readonly ComplianceMapping ABSC_5_1 =
            new(ComplianceStandard.AgId, "ABSC 5.1", "Limit administrative privileges");

        /// <summary>ABSC 5.2 — Track administrators' actions (audit logging).</summary>
        public static readonly ComplianceMapping ABSC_5_2 =
            new(ComplianceStandard.AgId, "ABSC 5.2", "Track administrator actions");

        /// <summary>ABSC 5.7 — Multi-factor authentication for administrators.</summary>
        public static readonly ComplianceMapping ABSC_5_7 =
            new(ComplianceStandard.AgId, "ABSC 5.7", "Multi-factor authentication for administrators");

        /// <summary>ABSC 5.10 — Limit local authentication and credential lifetime.</summary>
        public static readonly ComplianceMapping ABSC_5_10 =
            new(ComplianceStandard.AgId, "ABSC 5.10", "Limit local authentication and credential lifetime");

        /// <summary>ABSC 8.1 — Defences against malware (network filtering and patching baseline).</summary>
        public static readonly ComplianceMapping ABSC_8_1 =
            new(ComplianceStandard.AgId, "ABSC 8.1", "Defences against malware");

        /// <summary>ABSC 10.1 — Perform regular backups.</summary>
        public static readonly ComplianceMapping ABSC_10_1 =
            new(ComplianceStandard.AgId, "ABSC 10.1", "Perform regular backups");

        /// <summary>ABSC 10.3 — Verify backup integrity and availability.</summary>
        public static readonly ComplianceMapping ABSC_10_3 =
            new(ComplianceStandard.AgId, "ABSC 10.3", "Verify backup integrity and availability");

        /// <summary>ABSC 10.4 — Protect backup storage from unauthorised access and loss.</summary>
        public static readonly ComplianceMapping ABSC_10_4 =
            new(ComplianceStandard.AgId, "ABSC 10.4", "Protect backup storage");

        /// <summary>ABSC 13.1 — Encrypt sensitive data in transit and at rest.</summary>
        public static readonly ComplianceMapping ABSC_13_1 =
            new(ComplianceStandard.AgId, "ABSC 13.1", "Encrypt sensitive data in transit and at rest");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            ABSC_2_3, ABSC_3_1, ABSC_3_2,
            ABSC_4_1, ABSC_4_4,
            ABSC_5_1, ABSC_5_2, ABSC_5_7, ABSC_5_10,
            ABSC_8_1,
            ABSC_10_1, ABSC_10_3, ABSC_10_4,
            ABSC_13_1,
        ];
    }

    /// <summary>
    /// ISO/IEC 27017:2015 — Security controls for cloud services.
    /// Only the cloud-specific CLD.* extensions to ISO 27001 are listed here;
    /// the base ISO 27001 controls are in <see cref="Iso27001"/>.
    /// </summary>
    public static class Iso27017
    {
        /// <summary>CLD.6.3.1 — Shared roles and responsibilities within a cloud computing environment.</summary>
        public static readonly ComplianceMapping CLD_6_3_1 =
            new(ComplianceStandard.Iso27017, "CLD.6.3.1", "Shared roles and responsibilities in cloud");

        /// <summary>CLD.8.1.5 — Removal of cloud service customer assets (asset lifecycle in shared infra).</summary>
        public static readonly ComplianceMapping CLD_8_1_5 =
            new(ComplianceStandard.Iso27017, "CLD.8.1.5", "Removal of cloud service customer assets");

        /// <summary>CLD.9.5.1 — Segregation in virtual computing environments.</summary>
        public static readonly ComplianceMapping CLD_9_5_1 =
            new(ComplianceStandard.Iso27017, "CLD.9.5.1", "Segregation in virtual computing environments");

        /// <summary>CLD.9.5.2 — Virtual machine hardening.</summary>
        public static readonly ComplianceMapping CLD_9_5_2 =
            new(ComplianceStandard.Iso27017, "CLD.9.5.2", "Virtual machine hardening");

        /// <summary>CLD.12.1.5 — Administrator's operational security.</summary>
        public static readonly ComplianceMapping CLD_12_1_5 =
            new(ComplianceStandard.Iso27017, "CLD.12.1.5", "Administrator's operational security");

        /// <summary>CLD.12.4.5 — Monitoring of cloud services (logging/alerting in the cloud stack).</summary>
        public static readonly ComplianceMapping CLD_12_4_5 =
            new(ComplianceStandard.Iso27017, "CLD.12.4.5", "Monitoring of cloud services");

        /// <summary>CLD.13.1.4 — Alignment of security management for virtual and physical networks.</summary>
        public static readonly ComplianceMapping CLD_13_1_4 =
            new(ComplianceStandard.Iso27017, "CLD.13.1.4", "Alignment of security for virtual and physical networks");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            CLD_6_3_1, CLD_8_1_5, CLD_9_5_1, CLD_9_5_2, CLD_12_1_5, CLD_12_4_5, CLD_13_1_4,
        ];
    }

    /// <summary>
    /// CIS Controls v8 — Center for Internet Security.
    /// Top-level controls; safeguards (CIS-X.Y) are referenced via descriptive titles only.
    /// </summary>
    public static class Cis
    {
        /// <summary>CIS 3 — Data Protection.</summary>
        public static readonly ComplianceMapping C_3 =
            new(ComplianceStandard.Cis, "CIS 3", "Data Protection");

        /// <summary>CIS 4 — Secure Configuration of Enterprise Assets and Software.</summary>
        public static readonly ComplianceMapping C_4 =
            new(ComplianceStandard.Cis, "CIS 4", "Secure Configuration of Enterprise Assets and Software");

        /// <summary>CIS 5 — Account Management.</summary>
        public static readonly ComplianceMapping C_5 =
            new(ComplianceStandard.Cis, "CIS 5", "Account Management");

        /// <summary>CIS 6 — Access Control Management.</summary>
        public static readonly ComplianceMapping C_6 =
            new(ComplianceStandard.Cis, "CIS 6", "Access Control Management");

        /// <summary>CIS 7 — Continuous Vulnerability Management.</summary>
        public static readonly ComplianceMapping C_7 =
            new(ComplianceStandard.Cis, "CIS 7", "Continuous Vulnerability Management");

        /// <summary>CIS 8 — Audit Log Management.</summary>
        public static readonly ComplianceMapping C_8 =
            new(ComplianceStandard.Cis, "CIS 8", "Audit Log Management");

        /// <summary>CIS 10 — Malware Defenses.</summary>
        public static readonly ComplianceMapping C_10 =
            new(ComplianceStandard.Cis, "CIS 10", "Malware Defenses");

        /// <summary>CIS 11 — Data Recovery.</summary>
        public static readonly ComplianceMapping C_11 =
            new(ComplianceStandard.Cis, "CIS 11", "Data Recovery");

        /// <summary>CIS 12 — Network Infrastructure Management.</summary>
        public static readonly ComplianceMapping C_12 =
            new(ComplianceStandard.Cis, "CIS 12", "Network Infrastructure Management");

        /// <summary>CIS 13 — Network Monitoring and Defense.</summary>
        public static readonly ComplianceMapping C_13 =
            new(ComplianceStandard.Cis, "CIS 13", "Network Monitoring and Defense");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            C_3, C_4, C_5, C_6, C_7, C_8, C_10, C_11, C_12, C_13,
        ];
    }

    /// <summary>
    /// NIST Cybersecurity Framework 2.0 (CSF 2.0).
    /// Subcategories chosen for relevance to virtualisation cluster diagnostics.
    /// </summary>
    public static class NistCsf
    {
        /// <summary>ID.AM-02 — Inventories of software, services and systems managed by the organization are maintained.</summary>
        public static readonly ComplianceMapping ID_AM_02 =
            new(ComplianceStandard.NistCsf, "ID.AM-02", "Software/services/systems inventory is maintained");

        /// <summary>ID.RA-01 — Vulnerabilities in assets are identified, validated, and recorded.</summary>
        public static readonly ComplianceMapping ID_RA_01 =
            new(ComplianceStandard.NistCsf, "ID.RA-01", "Asset vulnerabilities are identified and recorded");

        /// <summary>PR.AA-01 — Identities and credentials for authorized users are managed.</summary>
        public static readonly ComplianceMapping PR_AA_01 =
            new(ComplianceStandard.NistCsf, "PR.AA-01", "Identities and credentials are managed");

        /// <summary>PR.AA-03 — Users, services, and hardware are authenticated.</summary>
        public static readonly ComplianceMapping PR_AA_03 =
            new(ComplianceStandard.NistCsf, "PR.AA-03", "Users, services and hardware are authenticated");

        /// <summary>PR.AA-05 — Access permissions, entitlements and authorisations are defined, managed and enforced.</summary>
        public static readonly ComplianceMapping PR_AA_05 =
            new(ComplianceStandard.NistCsf, "PR.AA-05", "Access permissions and entitlements are managed");

        /// <summary>PR.DS-01 — Confidentiality, integrity and availability of data-at-rest are protected.</summary>
        public static readonly ComplianceMapping PR_DS_01 =
            new(ComplianceStandard.NistCsf, "PR.DS-01", "Data-at-rest is protected");

        /// <summary>PR.DS-02 — Confidentiality, integrity and availability of data-in-transit are protected.</summary>
        public static readonly ComplianceMapping PR_DS_02 =
            new(ComplianceStandard.NistCsf, "PR.DS-02", "Data-in-transit is protected");

        /// <summary>PR.DS-11 — Backups of data are conducted, protected, maintained and tested.</summary>
        public static readonly ComplianceMapping PR_DS_11 =
            new(ComplianceStandard.NistCsf, "PR.DS-11", "Backups are conducted, protected and tested");

        /// <summary>PR.IR-01 — Networks and environments are protected from unauthorized logical access and usage.</summary>
        public static readonly ComplianceMapping PR_IR_01 =
            new(ComplianceStandard.NistCsf, "PR.IR-01", "Networks and environments are protected from unauthorized access");

        /// <summary>PR.IR-04 — Adequate resource capacity to ensure availability is maintained.</summary>
        public static readonly ComplianceMapping PR_IR_04 =
            new(ComplianceStandard.NistCsf, "PR.IR-04", "Adequate resource capacity is maintained");

        /// <summary>PR.PS-02 — Software is maintained, replaced and removed commensurate with risk.</summary>
        public static readonly ComplianceMapping PR_PS_02 =
            new(ComplianceStandard.NistCsf, "PR.PS-02", "Software is maintained commensurate with risk");

        /// <summary>DE.CM-01 — Networks and network services are monitored to find potentially adverse events.</summary>
        public static readonly ComplianceMapping DE_CM_01 =
            new(ComplianceStandard.NistCsf, "DE.CM-01", "Networks and services are monitored");

        /// <summary>DE.CM-03 — Personnel activity and technology usage are monitored.</summary>
        public static readonly ComplianceMapping DE_CM_03 =
            new(ComplianceStandard.NistCsf, "DE.CM-03", "Personnel activity is monitored");

        /// <summary>RC.RP-01 — The recovery portion of the incident response plan is executed once initiated.</summary>
        public static readonly ComplianceMapping RC_RP_01 =
            new(ComplianceStandard.NistCsf, "RC.RP-01", "Recovery procedures are in place and exercised");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            ID_AM_02, ID_RA_01,
            PR_AA_01, PR_AA_03, PR_AA_05,
            PR_DS_01, PR_DS_02, PR_DS_11,
            PR_IR_01, PR_IR_04,
            PR_PS_02,
            DE_CM_01, DE_CM_03,
            RC_RP_01,
        ];
    }

    // ──────── Lookup ────────

    private static readonly FrozenDictionary<(ComplianceStandard, string), ComplianceMapping> _byKey =
        new[] { Iso27001.All, Nis2.All, Dora.All, PciDss.All, Gdpr.All, AgId.All, Iso27017.All, Cis.All, NistCsf.All }
            .SelectMany(x => x)
            .ToFrozenDictionary(m => (m.Standard, m.ControlId));

    /// <summary>
    /// All compliance mappings declared in this catalog, across every standard.
    /// </summary>
    public static IReadOnlyCollection<ComplianceMapping> All => _byKey.Values;

    /// <summary>
    /// Look up a mapping by (standard, control id). Returns <c>null</c> if not found.
    /// </summary>
    public static ComplianceMapping? Find(ComplianceStandard standard, string controlId)
        => _byKey.TryGetValue((standard, controlId), out var m) ? m : null;

    /// <summary>
    /// Returns the human-readable title for the given control. Falls back to the
    /// <paramref name="controlId"/> itself when the mapping is not found (e.g. a
    /// historical report that references a control later removed from the catalog).
    /// </summary>
    public static string GetTitle(ComplianceStandard standard, string controlId)
        => Find(standard, controlId)?.ControlTitle ?? controlId;
}
