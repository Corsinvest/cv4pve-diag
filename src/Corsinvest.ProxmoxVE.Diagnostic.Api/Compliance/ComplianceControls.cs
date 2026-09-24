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
            Art_21_c, Art_21_e, Art_21_f, Art_21_h, Art_21_i, Art_21_j,
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

        /// <summary>Art.11 — Response and recovery (ICT business continuity policy).</summary>
        public static readonly ComplianceMapping Art_11 =
            new(ComplianceStandard.Dora, "Art.11", "Response and recovery (ICT business continuity)");

        /// <summary>Art.12 — Backup policies and procedures, restoration and recovery procedures and methods.</summary>
        public static readonly ComplianceMapping Art_12 =
            new(ComplianceStandard.Dora, "Art.12", "Backup policies, restoration and recovery procedures");

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
    /// ACN — Italian NIS2 basic security measures (Determinazione ACN n. 379907 del 19 dicembre 2025,
    /// replacing n. 164179/2025; D.Lgs. 138/2024 art. 24). Identifiers are the Framework Nazionale /
    /// NIST CSF 2.0 subcategory codes used by the annexes (Allegato 1 soggetti importanti, Allegato 2
    /// soggetti essenziali). Measures that apply to essential entities only say so in the title.
    /// </summary>
    public static class Acn
    {
        /// <summary>ID.AM-02 — Inventories of software, services and systems are maintained.</summary>
        public static readonly ComplianceMapping ID_AM_02 =
            new(ComplianceStandard.Acn, "ID.AM-02", "Inventory of software, services and systems");

        /// <summary>ID.RA-08 — Processes for receiving, analysing and responding to vulnerability disclosures.</summary>
        public static readonly ComplianceMapping ID_RA_08 =
            new(ComplianceStandard.Acn, "ID.RA-08", "Vulnerability disclosures received, analysed and remediated");

        /// <summary>ID.IM-04 — Business continuity, disaster recovery and crisis plans, including backups and redundancy.</summary>
        public static readonly ComplianceMapping ID_IM_04 =
            new(ComplianceStandard.Acn, "ID.IM-04", "Business continuity and disaster recovery plans (backups, redundancy)");

        /// <summary>PR.AA-01 — Identities and credentials of users, services and hardware are managed (recorded, individual, reviewed, revoked).</summary>
        public static readonly ComplianceMapping PR_AA_01 =
            new(ComplianceStandard.Acn, "PR.AA-01", "Identity and credential management");

        /// <summary>PR.AA-03 — Users, services and hardware are authenticated; multi-factor authentication at least for relevant systems.</summary>
        public static readonly ComplianceMapping PR_AA_03 =
            new(ComplianceStandard.Acn, "PR.AA-03", "Authentication, multi-factor for relevant systems");

        /// <summary>PR.AA-05 — Access permissions follow least privilege and separation of duties; separate privileged accounts.</summary>
        public static readonly ComplianceMapping PR_AA_05 =
            new(ComplianceStandard.Acn, "PR.AA-05", "Least privilege and separate privileged accounts");

        /// <summary>PR.DS-02 — Confidentiality, integrity and availability of data in transit are protected.</summary>
        public static readonly ComplianceMapping PR_DS_02 =
            new(ComplianceStandard.Acn, "PR.DS-02", "Protection of data in transit (encryption)");

        /// <summary>PR.DS-11 — Backups of data are created, protected, maintained and tested (data and configurations, offline copies).</summary>
        public static readonly ComplianceMapping PR_DS_11 =
            new(ComplianceStandard.Acn, "PR.DS-11", "Backups created, protected, maintained and tested");

        /// <summary>PR.PS-01 — Configuration management practices: hardened baseline configurations (essential entities only).</summary>
        public static readonly ComplianceMapping PR_PS_01 =
            new(ComplianceStandard.Acn, "PR.PS-01", "Secure configuration baselines (essential entities)");

        /// <summary>PR.PS-02 — Software is maintained, replaced and removed according to risk: supported software only, security updates without undue delay.</summary>
        public static readonly ComplianceMapping PR_PS_02 =
            new(ComplianceStandard.Acn, "PR.PS-02", "Supported software and timely security updates");

        /// <summary>PR.PS-04 — Log records are generated and made available for continuous monitoring (administrative access, retention).</summary>
        public static readonly ComplianceMapping PR_PS_04 =
            new(ComplianceStandard.Acn, "PR.PS-04", "Logs generated and kept for continuous monitoring");

        /// <summary>PR.IR-01 — Networks and environments are protected from unauthorised logical access (perimeter systems, firewalls).</summary>
        public static readonly ComplianceMapping PR_IR_01 =
            new(ComplianceStandard.Acn, "PR.IR-01", "Networks protected from unauthorised access (firewalls)");

        /// <summary>DE.CM-01 — Networks and network services are monitored to find potentially adverse events.</summary>
        public static readonly ComplianceMapping DE_CM_01 =
            new(ComplianceStandard.Acn, "DE.CM-01", "Networks and services monitored");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            ID_AM_02, ID_RA_08, ID_IM_04,
            PR_AA_01, PR_AA_03, PR_AA_05,
            PR_DS_02, PR_DS_11,
            PR_PS_01, PR_PS_02, PR_PS_04,
            PR_IR_01,
            DE_CM_01,
        ];
    }

    /// <summary>
    /// Commission Implementing Regulation (EU) 2024/2690 — technical and methodological requirements
    /// of the NIS2 risk-management measures (Art. 21(5) of Directive (EU) 2022/2555) for DNS service
    /// providers, TLD name registries, cloud computing, data centre and content delivery network
    /// providers, managed (security) service providers, online marketplaces, search engines, social
    /// networks and trust service providers. Identifiers are the points of the Annex, at the level
    /// that carries a title (numbering as in the ENISA technical implementation guidance).
    /// </summary>
    public static class Nis2Ir
    {
        /// <summary>3.2 — Monitoring and logging (incl. 3.2.6 synchronised time sources).</summary>
        public static readonly ComplianceMapping C_3_2 =
            new(ComplianceStandard.Nis2Ir, "3.2", "Monitoring and logging");

        /// <summary>4.1 — Business continuity and disaster recovery plan.</summary>
        public static readonly ComplianceMapping C_4_1 =
            new(ComplianceStandard.Nis2Ir, "4.1", "Business continuity and disaster recovery plan");

        /// <summary>4.2 — Backup and redundancy management (backup copies, integrity checks, redundancy).</summary>
        public static readonly ComplianceMapping C_4_2 =
            new(ComplianceStandard.Nis2Ir, "4.2", "Backup and redundancy management");

        /// <summary>6.3 — Configuration management (secure configurations of hardware, software, services and networks).</summary>
        public static readonly ComplianceMapping C_6_3 =
            new(ComplianceStandard.Nis2Ir, "6.3", "Configuration management");

        /// <summary>6.6 — Security patch management.</summary>
        public static readonly ComplianceMapping C_6_6 =
            new(ComplianceStandard.Nis2Ir, "6.6", "Security patch management");

        /// <summary>6.7 — Network security.</summary>
        public static readonly ComplianceMapping C_6_7 =
            new(ComplianceStandard.Nis2Ir, "6.7", "Network security");

        /// <summary>6.8 — Network segmentation.</summary>
        public static readonly ComplianceMapping C_6_8 =
            new(ComplianceStandard.Nis2Ir, "6.8", "Network segmentation");

        /// <summary>6.10 — Vulnerability handling and disclosure.</summary>
        public static readonly ComplianceMapping C_6_10 =
            new(ComplianceStandard.Nis2Ir, "6.10", "Vulnerability handling and disclosure");

        /// <summary>9 — Cryptography.</summary>
        public static readonly ComplianceMapping C_9 =
            new(ComplianceStandard.Nis2Ir, "9", "Cryptography");

        /// <summary>11.2 — Management of access rights.</summary>
        public static readonly ComplianceMapping C_11_2 =
            new(ComplianceStandard.Nis2Ir, "11.2", "Management of access rights");

        /// <summary>11.3 — Privileged accounts and system administration accounts.</summary>
        public static readonly ComplianceMapping C_11_3 =
            new(ComplianceStandard.Nis2Ir, "11.3", "Privileged accounts and system administration accounts");

        /// <summary>11.5 — Identification (unique identities, deactivated when no longer needed).</summary>
        public static readonly ComplianceMapping C_11_5 =
            new(ComplianceStandard.Nis2Ir, "11.5", "Identification");

        /// <summary>11.7 — Multi-factor authentication.</summary>
        public static readonly ComplianceMapping C_11_7 =
            new(ComplianceStandard.Nis2Ir, "11.7", "Multi-factor authentication");

        /// <summary>12.4 — Asset inventory.</summary>
        public static readonly ComplianceMapping C_12_4 =
            new(ComplianceStandard.Nis2Ir, "12.4", "Asset inventory");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            C_3_2, C_4_1, C_4_2,
            C_6_3, C_6_6, C_6_7, C_6_8, C_6_10,
            C_9,
            C_11_2, C_11_3, C_11_5, C_11_7,
            C_12_4,
        ];
    }

    /// <summary>
    /// ISO 22301:2019 — Security and resilience — Business continuity management systems — Requirements.
    /// Clause numbers and titles as in the published table of contents (the requirement text is not
    /// public). Only clauses a cluster configuration can give evidence for are listed.
    /// </summary>
    public static class Iso22301
    {
        /// <summary>8.3.4 — Resource requirements (capacity for the business continuity solutions).</summary>
        public static readonly ComplianceMapping C_8_3_4 =
            new(ComplianceStandard.Iso22301, "8.3.4", "Resource requirements");

        /// <summary>8.3.5 — Implementation of solutions (backup, HA, replication in place).</summary>
        public static readonly ComplianceMapping C_8_3_5 =
            new(ComplianceStandard.Iso22301, "8.3.5", "Implementation of solutions");

        /// <summary>8.4.5 — Recovery.</summary>
        public static readonly ComplianceMapping C_8_4_5 =
            new(ComplianceStandard.Iso22301, "8.4.5", "Recovery");

        /// <summary>8.5 — Exercise programme (restore and failover tests).</summary>
        public static readonly ComplianceMapping C_8_5 =
            new(ComplianceStandard.Iso22301, "8.5", "Exercise programme");

        /// <summary>9.1 — Monitoring, measurement, analysis and evaluation.</summary>
        public static readonly ComplianceMapping C_9_1 =
            new(ComplianceStandard.Iso22301, "9.1", "Monitoring, measurement, analysis and evaluation");

        internal static IEnumerable<ComplianceMapping> All => [C_8_3_4, C_8_3_5, C_8_4_5, C_8_5, C_9_1];
    }

    /// <summary>
    /// BSI IT-Grundschutz-Kompendium, Edition 2023 (Germany). Requirement identifiers are
    /// &lt;Baustein&gt;.A&lt;n&gt;; titles are the German ones of the Kompendium, with the protection
    /// level: (B) Basis, (S) Standard, (H) erhöhter Schutzbedarf. The Kompendium is superseded by
    /// Grundschutz++ (published 2026, certifiable from 2027) but remains certifiable until 2031.
    /// </summary>
    public static class BsiGrundschutz
    {
        /// <summary>CON.1.A1 — Auswahl geeigneter kryptografischer Verfahren (selection of suitable cryptographic methods).</summary>
        public static readonly ComplianceMapping CON_1_A1 =
            new(ComplianceStandard.BsiGrundschutz, "CON.1.A1", "Auswahl geeigneter kryptografischer Verfahren (B)");

        /// <summary>CON.3.A5 — Regelmäßige Datensicherung (regular data backup).</summary>
        public static readonly ComplianceMapping CON_3_A5 =
            new(ComplianceStandard.BsiGrundschutz, "CON.3.A5", "Regelmäßige Datensicherung (B)");

        /// <summary>OPS.1.1.3.A15 — Regelmäßige Aktualisierung von IT-Systemen und Software (regular updates).</summary>
        public static readonly ComplianceMapping OPS_1_1_3_A15 =
            new(ComplianceStandard.BsiGrundschutz, "OPS.1.1.3.A15", "Regelmäßige Aktualisierung von IT-Systemen und Software (B)");

        /// <summary>OPS.1.1.5.A3 — Konfiguration der Protokollierung auf System- und Netzebene (logging configuration).</summary>
        public static readonly ComplianceMapping OPS_1_1_5_A3 =
            new(ComplianceStandard.BsiGrundschutz, "OPS.1.1.5.A3", "Konfiguration der Protokollierung auf System- und Netzebene (B)");

        /// <summary>OPS.1.1.5.A4 — Zeitsynchronisation der IT-Systeme (time synchronisation).</summary>
        public static readonly ComplianceMapping OPS_1_1_5_A4 =
            new(ComplianceStandard.BsiGrundschutz, "OPS.1.1.5.A4", "Zeitsynchronisation der IT-Systeme (B)");

        /// <summary>ORP.4.A10 — Schutz von Benutzendenkennungen mit weitreichenden Berechtigungen (protection of privileged accounts).</summary>
        public static readonly ComplianceMapping ORP_4_A10 =
            new(ComplianceStandard.BsiGrundschutz, "ORP.4.A10", "Schutz von Benutzendenkennungen mit weitreichenden Berechtigungen (S)");

        /// <summary>ORP.4.A21 — Mehr-Faktor-Authentisierung (multi-factor authentication).</summary>
        public static readonly ComplianceMapping ORP_4_A21 =
            new(ComplianceStandard.BsiGrundschutz, "ORP.4.A21", "Mehr-Faktor-Authentisierung (H)");

        /// <summary>SYS.1.1.A19 — Einrichtung lokaler Paketfilter (local packet filters: node and guest firewall).</summary>
        public static readonly ComplianceMapping SYS_1_1_A19 =
            new(ComplianceStandard.BsiGrundschutz, "SYS.1.1.A19", "Einrichtung lokaler Paketfilter (S)");

        /// <summary>SYS.1.5.A4 — Sichere Konfiguration eines Netzes für virtuelle Infrastrukturen (virtual infrastructure network).</summary>
        public static readonly ComplianceMapping SYS_1_5_A4 =
            new(ComplianceStandard.BsiGrundschutz, "SYS.1.5.A4", "Sichere Konfiguration eines Netzes für virtuelle Infrastrukturen (B)");

        /// <summary>SYS.1.5.A17 — Überwachung des Betriebszustands und der Konfiguration der virtuellen Infrastruktur (monitoring).</summary>
        public static readonly ComplianceMapping SYS_1_5_A17 =
            new(ComplianceStandard.BsiGrundschutz, "SYS.1.5.A17", "Überwachung des Betriebszustands und der Konfiguration der virtuellen Infrastruktur (S)");

        /// <summary>SYS.1.5.A20 — Verwendung von hochverfügbaren Architekturen (high-availability architectures).</summary>
        public static readonly ComplianceMapping SYS_1_5_A20 =
            new(ComplianceStandard.BsiGrundschutz, "SYS.1.5.A20", "Verwendung von hochverfügbaren Architekturen (H)");

        /// <summary>SYS.1.6.A17 — Ausführung von Containern ohne Privilegien (unprivileged containers).</summary>
        public static readonly ComplianceMapping SYS_1_6_A17 =
            new(ComplianceStandard.BsiGrundschutz, "SYS.1.6.A17", "Ausführung von Containern ohne Privilegien (S)");

        /// <summary>SYS.1.8.A13 — Überwachung und Verwaltung von Speicherlösungen (storage monitoring and management).</summary>
        public static readonly ComplianceMapping SYS_1_8_A13 =
            new(ComplianceStandard.BsiGrundschutz, "SYS.1.8.A13", "Überwachung und Verwaltung von Speicherlösungen (S)");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            CON_1_A1, CON_3_A5,
            OPS_1_1_3_A15, OPS_1_1_5_A3, OPS_1_1_5_A4,
            ORP_4_A10, ORP_4_A21,
            SYS_1_1_A19, SYS_1_5_A4, SYS_1_5_A17, SYS_1_5_A20, SYS_1_6_A17, SYS_1_8_A13,
        ];
    }

    /// <summary>
    /// ENS — Esquema Nacional de Seguridad (Real Decreto 311/2022, Spain).
    /// Control identifiers follow the official ENS taxonomy: op.* (operational framework),
    /// mp.* (protection measures). Only the subset technically verifiable on a Proxmox VE
    /// cluster is listed here.
    /// </summary>
    public static class Ens
    {
        /// <summary>op.acc.1 — Identification (unique user identity for every access).</summary>
        public static readonly ComplianceMapping OP_ACC_1 =
            new(ComplianceStandard.Ens, "op.acc.1", "Identification");

        /// <summary>op.acc.2 — Access requirements (Requisitos de acceso: least privilege, role-based).</summary>
        public static readonly ComplianceMapping OP_ACC_2 =
            new(ComplianceStandard.Ens, "op.acc.2", "Access requirements");

        /// <summary>op.acc.6 — Authentication mechanism, organisation users (Mecanismo de autenticación: usuarios de la organización).</summary>
        public static readonly ComplianceMapping OP_ACC_6 =
            new(ComplianceStandard.Ens, "op.acc.6", "Authentication mechanism (organisation users)");

        /// <summary>op.exp.1 — Inventory of assets.</summary>
        public static readonly ComplianceMapping OP_EXP_1 =
            new(ComplianceStandard.Ens, "op.exp.1", "Inventory of assets");

        /// <summary>op.exp.2 — Security configuration (hardening baseline).</summary>
        public static readonly ComplianceMapping OP_EXP_2 =
            new(ComplianceStandard.Ens, "op.exp.2", "Security configuration");

        /// <summary>op.exp.3 — Security configuration management (drift detection).</summary>
        public static readonly ComplianceMapping OP_EXP_3 =
            new(ComplianceStandard.Ens, "op.exp.3", "Security configuration management");

        /// <summary>op.exp.4 — Maintenance and software updates (patch management).</summary>
        public static readonly ComplianceMapping OP_EXP_4 =
            new(ComplianceStandard.Ens, "op.exp.4", "Maintenance and software updates");

        /// <summary>op.exp.5 — Change management.</summary>
        public static readonly ComplianceMapping OP_EXP_5 =
            new(ComplianceStandard.Ens, "op.exp.5", "Change management");

        /// <summary>op.exp.8 — Activity logging (Registro de la actividad).</summary>
        public static readonly ComplianceMapping OP_EXP_8 =
            new(ComplianceStandard.Ens, "op.exp.8", "Activity logging");

        /// <summary>op.exp.9 — Incident management logging (Registro de la gestión de incidentes).</summary>
        public static readonly ComplianceMapping OP_EXP_9 =
            new(ComplianceStandard.Ens, "op.exp.9", "Incident management logging");

        /// <summary>op.cont.2 — Continuity plan (HA / failover provisions).</summary>
        public static readonly ComplianceMapping OP_CONT_2 =
            new(ComplianceStandard.Ens, "op.cont.2", "Continuity plan");

        /// <summary>op.cont.3 — Periodic tests (Pruebas periódicas) of the continuity plan.</summary>
        public static readonly ComplianceMapping OP_CONT_3 =
            new(ComplianceStandard.Ens, "op.cont.3", "Periodic tests");

        /// <summary>op.cont.4 — Alternative means (Medios alternativos): redundancy, HA, failover.</summary>
        public static readonly ComplianceMapping OP_CONT_4 =
            new(ComplianceStandard.Ens, "op.cont.4", "Alternative means");

        /// <summary>op.pl.4 — Capacity sizing and management (Dimensionamiento/gestión de la capacidad).</summary>
        public static readonly ComplianceMapping OP_PL_4 =
            new(ComplianceStandard.Ens, "op.pl.4", "Capacity sizing and management");

        /// <summary>op.mon.3 — Surveillance (Vigilancia): continuous monitoring of the system state and events.</summary>
        public static readonly ComplianceMapping OP_MON_3 =
            new(ComplianceStandard.Ens, "op.mon.3", "Surveillance");

        /// <summary>mp.com.1 — Secure perimeter (Perímetro seguro: firewall, network segregation).</summary>
        public static readonly ComplianceMapping MP_COM_1 =
            new(ComplianceStandard.Ens, "mp.com.1", "Secure perimeter");

        /// <summary>mp.com.2 — Protection of confidentiality (Protección de la confidencialidad: encryption in transit).</summary>
        public static readonly ComplianceMapping MP_COM_2 =
            new(ComplianceStandard.Ens, "mp.com.2", "Protection of confidentiality");

        /// <summary>mp.info.6 — Backup copies (Copias de seguridad).</summary>
        public static readonly ComplianceMapping MP_INFO_6 =
            new(ComplianceStandard.Ens, "mp.info.6", "Backup copies");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            OP_ACC_1, OP_ACC_2, OP_ACC_6,
            OP_EXP_1, OP_EXP_2, OP_EXP_3, OP_EXP_4, OP_EXP_5, OP_EXP_8, OP_EXP_9,
            OP_CONT_2, OP_CONT_3, OP_CONT_4,
            OP_PL_4,
            OP_MON_3,
            MP_COM_1, MP_COM_2,
            MP_INFO_6,
        ];
    }

    /// <summary>
    /// C5 — Cloud Computing Compliance Criteria Catalogue (BSI Germany, C5:2020).
    /// Identifiers and titles as in the English edition of the catalogue: OIS, SP, HR, AM, PS, OPS,
    /// IDM, CRY (cryptography), COS (communication security), PI (portability and interoperability),
    /// DEV, SSO, SIM, BCM, COM, INQ, PSS. Only the subset technically verifiable on a Proxmox VE
    /// cluster is listed.
    /// </summary>
    public static class C5
    {
        /// <summary>IDM-01 — Policy for user accounts and access rights.</summary>
        public static readonly ComplianceMapping IDM_01 =
            new(ComplianceStandard.C5, "IDM-01", "Policy for user accounts and access rights");

        /// <summary>IDM-02 — Granting and change of user accounts and access rights.</summary>
        public static readonly ComplianceMapping IDM_02 =
            new(ComplianceStandard.C5, "IDM-02", "Granting and change of user accounts and access rights");

        /// <summary>IDM-03 — Locking and withdrawal of user accounts in the event of inactivity or multiple failed logins.</summary>
        public static readonly ComplianceMapping IDM_03 =
            new(ComplianceStandard.C5, "IDM-03", "Locking and withdrawal of user accounts");

        /// <summary>IDM-06 — Privileged access rights.</summary>
        public static readonly ComplianceMapping IDM_06 =
            new(ComplianceStandard.C5, "IDM-06", "Privileged access rights");

        /// <summary>IDM-09 — Authentication mechanisms (strong / multi-factor authentication).</summary>
        public static readonly ComplianceMapping IDM_09 =
            new(ComplianceStandard.C5, "IDM-09", "Authentication mechanisms");

        /// <summary>CRY-01 — Policy for the use of encryption procedures and key management.</summary>
        public static readonly ComplianceMapping CRY_01 =
            new(ComplianceStandard.C5, "CRY-01", "Policy for the use of encryption procedures and key management");

        /// <summary>CRY-02 — Encryption of data for transmission (transport encryption).</summary>
        public static readonly ComplianceMapping CRY_02 =
            new(ComplianceStandard.C5, "CRY-02", "Encryption of data for transmission (transport encryption)");

        /// <summary>COS-01 — Technical safeguards (firewalls, segregation).</summary>
        public static readonly ComplianceMapping COS_01 =
            new(ComplianceStandard.C5, "COS-01", "Technical safeguards");

        /// <summary>OPS-06 — Data Backup and Recovery – Concept.</summary>
        public static readonly ComplianceMapping OPS_06 =
            new(ComplianceStandard.C5, "OPS-06", "Data Backup and Recovery – Concept");

        /// <summary>OPS-10 — Logging and Monitoring – Concept.</summary>
        public static readonly ComplianceMapping OPS_10 =
            new(ComplianceStandard.C5, "OPS-10", "Logging and Monitoring – Concept");

        /// <summary>OPS-13 — Logging and Monitoring – Identification of Events.</summary>
        public static readonly ComplianceMapping OPS_13 =
            new(ComplianceStandard.C5, "OPS-13", "Logging and Monitoring – Identification of Events");

        /// <summary>OPS-18 — Managing Vulnerabilities, Malfunctions and Errors – Concept.</summary>
        public static readonly ComplianceMapping OPS_18 =
            new(ComplianceStandard.C5, "OPS-18", "Managing Vulnerabilities, Malfunctions and Errors – Concept");

        /// <summary>OPS-23 — Managing Vulnerabilities, Malfunctions and Errors – System Hardening.</summary>
        public static readonly ComplianceMapping OPS_23 =
            new(ComplianceStandard.C5, "OPS-23", "Managing Vulnerabilities, Malfunctions and Errors – System Hardening");

        /// <summary>BCM-03 — Planning business continuity (redundancy, HA, replication).</summary>
        public static readonly ComplianceMapping BCM_03 =
            new(ComplianceStandard.C5, "BCM-03", "Planning business continuity");

        /// <summary>BCM-04 — Verification, updating and testing of the business continuity.</summary>
        public static readonly ComplianceMapping BCM_04 =
            new(ComplianceStandard.C5, "BCM-04", "Verification, updating and testing of the business continuity");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            IDM_01, IDM_02, IDM_03, IDM_06, IDM_09,
            CRY_01, CRY_02,
            COS_01,
            OPS_06, OPS_10, OPS_13, OPS_18, OPS_23,
            BCM_03, BCM_04,
        ];
    }

    /// <summary>
    /// SOC 2 — AICPA Trust Services Criteria (2017 + 2022 revision).
    /// Categories: CC = Common Criteria, A = Availability, C = Confidentiality, PI = Processing Integrity, P = Privacy.
    /// Only the subset technically verifiable on a Proxmox VE cluster is listed.
    /// </summary>
    public static class Soc2
    {
        /// <summary>CC6.1 — Logical access security software and infrastructure.</summary>
        public static readonly ComplianceMapping CC6_1 =
            new(ComplianceStandard.Soc2, "CC6.1", "Logical access security");

        /// <summary>CC6.2 — Authentication and authorization of internal and external users.</summary>
        public static readonly ComplianceMapping CC6_2 =
            new(ComplianceStandard.Soc2, "CC6.2", "Authentication and authorization");

        /// <summary>CC6.3 — Authorization for access requests, additions and modifications.</summary>
        public static readonly ComplianceMapping CC6_3 =
            new(ComplianceStandard.Soc2, "CC6.3", "Access request authorization");

        /// <summary>CC6.6 — Logical access security measures to protect against threats from sources outside system boundaries.</summary>
        public static readonly ComplianceMapping CC6_6 =
            new(ComplianceStandard.Soc2, "CC6.6", "Boundary protection");

        /// <summary>CC6.7 — Restrict the transmission, movement and removal of information.</summary>
        public static readonly ComplianceMapping CC6_7 =
            new(ComplianceStandard.Soc2, "CC6.7", "Information transmission controls");

        /// <summary>CC6.8 — Prevent or detect and act upon introduction of unauthorized or malicious software.</summary>
        public static readonly ComplianceMapping CC6_8 =
            new(ComplianceStandard.Soc2, "CC6.8", "Malicious software prevention");

        /// <summary>CC7.1 — Detection of new vulnerabilities and configuration changes.</summary>
        public static readonly ComplianceMapping CC7_1 =
            new(ComplianceStandard.Soc2, "CC7.1", "Vulnerability and configuration monitoring");

        /// <summary>CC7.2 — Monitoring of system components and operation for anomalies.</summary>
        public static readonly ComplianceMapping CC7_2 =
            new(ComplianceStandard.Soc2, "CC7.2", "System monitoring");

        /// <summary>CC7.3 — Evaluation of security events to determine response.</summary>
        public static readonly ComplianceMapping CC7_3 =
            new(ComplianceStandard.Soc2, "CC7.3", "Security event evaluation");

        /// <summary>CC8.1 — Change management process for infrastructure and software.</summary>
        public static readonly ComplianceMapping CC8_1 =
            new(ComplianceStandard.Soc2, "CC8.1", "Change management");

        /// <summary>A1.1 — Capacity planning and management to meet availability commitments.</summary>
        public static readonly ComplianceMapping A1_1 =
            new(ComplianceStandard.Soc2, "A1.1", "Capacity planning");

        /// <summary>A1.2 — Environmental protections, software, data backup and recovery infrastructure.</summary>
        public static readonly ComplianceMapping A1_2 =
            new(ComplianceStandard.Soc2, "A1.2", "Backup and recovery infrastructure");

        /// <summary>A1.3 — Recovery plan testing.</summary>
        public static readonly ComplianceMapping A1_3 =
            new(ComplianceStandard.Soc2, "A1.3", "Recovery plan testing");

        /// <summary>C1.1 — Identification and maintenance of confidential information.</summary>
        public static readonly ComplianceMapping C1_1 =
            new(ComplianceStandard.Soc2, "C1.1", "Confidential information management");

        /// <summary>C1.2 — Disposal of confidential information.</summary>
        public static readonly ComplianceMapping C1_2 =
            new(ComplianceStandard.Soc2, "C1.2", "Confidential information disposal");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            CC6_1, CC6_2, CC6_3, CC6_6, CC6_7, CC6_8,
            CC7_1, CC7_2, CC7_3,
            CC8_1,
            A1_1, A1_2, A1_3,
            C1_1, C1_2,
        ];
    }

    /// <summary>
    /// NIST SP 800-53 rev.5 — Security and Privacy Controls for Information Systems.
    /// Subset of the Moderate baseline that is technically verifiable on a Proxmox VE cluster.
    /// Identifiers follow the official family-numeric format (e.g. AC-2, AU-12, SC-7). Families used here:
    /// AC (Access Control), AU (Audit and Accountability), CM (Configuration Management),
    /// CP (Contingency Planning), IA (Identification and Authentication),
    /// SC (System and Communications Protection), SI (System and Information Integrity).
    /// </summary>
    public static class Nist80053
    {
        /// <summary>AC-2 — Account management (account lifecycle, disabled accounts, tokens).</summary>
        public static readonly ComplianceMapping AC_2 =
            new(ComplianceStandard.Nist80053, "AC-2", "Account management");

        /// <summary>AC-3 — Access enforcement (authorisation policy in effect).</summary>
        public static readonly ComplianceMapping AC_3 =
            new(ComplianceStandard.Nist80053, "AC-3", "Access enforcement");

        /// <summary>AC-6 — Least privilege.</summary>
        public static readonly ComplianceMapping AC_6 =
            new(ComplianceStandard.Nist80053, "AC-6", "Least privilege");

        /// <summary>AU-2 — Event logging (what events to capture).</summary>
        public static readonly ComplianceMapping AU_2 =
            new(ComplianceStandard.Nist80053, "AU-2", "Event logging");

        /// <summary>AU-6 — Audit record review, analysis and reporting.</summary>
        public static readonly ComplianceMapping AU_6 =
            new(ComplianceStandard.Nist80053, "AU-6", "Audit record review");

        /// <summary>AU-12 — Audit record generation by system components.</summary>
        public static readonly ComplianceMapping AU_12 =
            new(ComplianceStandard.Nist80053, "AU-12", "Audit record generation");

        /// <summary>CM-2 — Baseline configuration of the system.</summary>
        public static readonly ComplianceMapping CM_2 =
            new(ComplianceStandard.Nist80053, "CM-2", "Baseline configuration");

        /// <summary>CM-6 — Configuration settings (hardening baselines applied).</summary>
        public static readonly ComplianceMapping CM_6 =
            new(ComplianceStandard.Nist80053, "CM-6", "Configuration settings");

        /// <summary>CM-7 — Least functionality (disable unneeded services and ports).</summary>
        public static readonly ComplianceMapping CM_7 =
            new(ComplianceStandard.Nist80053, "CM-7", "Least functionality");

        /// <summary>CP-9 — System backup.</summary>
        public static readonly ComplianceMapping CP_9 =
            new(ComplianceStandard.Nist80053, "CP-9", "System backup");

        /// <summary>CP-10 — System recovery and reconstitution (HA, replication).</summary>
        public static readonly ComplianceMapping CP_10 =
            new(ComplianceStandard.Nist80053, "CP-10", "System recovery and reconstitution");

        /// <summary>IA-2 — Identification and authentication of organisational users (MFA on privileged accounts).</summary>
        public static readonly ComplianceMapping IA_2 =
            new(ComplianceStandard.Nist80053, "IA-2", "Identification and authentication");

        /// <summary>IA-5 — Authenticator management (passwords / tokens / certificates lifecycle).</summary>
        public static readonly ComplianceMapping IA_5 =
            new(ComplianceStandard.Nist80053, "IA-5", "Authenticator management");

        /// <summary>SC-7 — Boundary protection (firewall, network segregation).</summary>
        public static readonly ComplianceMapping SC_7 =
            new(ComplianceStandard.Nist80053, "SC-7", "Boundary protection");

        /// <summary>SC-8 — Transmission confidentiality and integrity (TLS).</summary>
        public static readonly ComplianceMapping SC_8 =
            new(ComplianceStandard.Nist80053, "SC-8", "Transmission confidentiality and integrity");

        /// <summary>SC-13 — Cryptographic protection (certificates, ciphers).</summary>
        public static readonly ComplianceMapping SC_13 =
            new(ComplianceStandard.Nist80053, "SC-13", "Cryptographic protection");

        /// <summary>SI-2 — Flaw remediation (patch management).</summary>
        public static readonly ComplianceMapping SI_2 =
            new(ComplianceStandard.Nist80053, "SI-2", "Flaw remediation");

        /// <summary>SI-4 — System monitoring (intrusion detection, anomaly).</summary>
        public static readonly ComplianceMapping SI_4 =
            new(ComplianceStandard.Nist80053, "SI-4", "System monitoring");

        /// <summary>SI-5 — Security alerts, advisories and directives (CVE awareness).</summary>
        public static readonly ComplianceMapping SI_5 =
            new(ComplianceStandard.Nist80053, "SI-5", "Security alerts and advisories");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            AC_2, AC_3, AC_6,
            AU_2, AU_6, AU_12,
            CM_2, CM_6, CM_7,
            CP_9, CP_10,
            IA_2, IA_5,
            SC_7, SC_8, SC_13,
            SI_2, SI_4, SI_5,
        ];
    }

    /// <summary>
    /// ISO/IEC 27018:2019 — Code of practice for protection of Personally Identifiable Information
    /// (PII) in public clouds acting as PII processors. Extends ISO 27001/27017 with PII-specific
    /// controls. Only the subset technically verifiable on a Proxmox VE cluster is listed; many
    /// 27018 controls are contractual/organisational and out of scope here.
    /// </summary>
    public static class Iso27018
    {
        /// <summary>A.9.4.2 — Secure log-on procedures for accounts that can access PII.</summary>
        public static readonly ComplianceMapping A_9_4_2 =
            new(ComplianceStandard.Iso27018, "A.9.4.2", "Secure log-on for PII access");

        /// <summary>A.10.1.1 — Use of cryptography to protect PII in transit.</summary>
        public static readonly ComplianceMapping A_10_1_1 =
            new(ComplianceStandard.Iso27018, "A.10.1.1", "Cryptography for PII in transit");

        /// <summary>A.12.1.4 — Separation of development, test and operational environments handling PII.</summary>
        public static readonly ComplianceMapping A_12_1_4 =
            new(ComplianceStandard.Iso27018, "A.12.1.4", "Separation of environments handling PII");

        /// <summary>A.12.3.1 — Backup of PII data (existence, retention, restore-ability).</summary>
        public static readonly ComplianceMapping A_12_3_1 =
            new(ComplianceStandard.Iso27018, "A.12.3.1", "Backup of PII");

        /// <summary>A.12.4.1 — Event logging for processing of PII.</summary>
        public static readonly ComplianceMapping A_12_4_1 =
            new(ComplianceStandard.Iso27018, "A.12.4.1", "Event logging for PII processing");

        /// <summary>A.13.2.1 — Secure transfer of PII over networks.</summary>
        public static readonly ComplianceMapping A_13_2_1 =
            new(ComplianceStandard.Iso27018, "A.13.2.1", "Secure transfer of PII");

        /// <summary>A.16.1.2 — Reporting of information-security events involving PII.</summary>
        public static readonly ComplianceMapping A_16_1_2 =
            new(ComplianceStandard.Iso27018, "A.16.1.2", "Reporting of PII-related events");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            A_9_4_2, A_10_1_1, A_12_1_4, A_12_3_1, A_12_4_1, A_13_2_1, A_16_1_2,
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
        new[] { Iso27001.All, Nis2.All, Dora.All, PciDss.All, Gdpr.All, AgId.All, Ens.All, C5.All, Soc2.All, Nist80053.All, Iso27017.All, Iso27018.All, Cis.All, NistCsf.All, Acn.All, Iso22301.All, BsiGrundschutz.All, Nis2Ir.All }
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
