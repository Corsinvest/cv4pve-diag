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

        /// <summary>op.acc.2 — Access rights (least privilege, role-based).</summary>
        public static readonly ComplianceMapping OP_ACC_2 =
            new(ComplianceStandard.Ens, "op.acc.2", "Access rights");

        /// <summary>op.acc.4 — Local access process (authentication mechanism, MFA on admin).</summary>
        public static readonly ComplianceMapping OP_ACC_4 =
            new(ComplianceStandard.Ens, "op.acc.4", "Local access process");

        /// <summary>op.acc.5 — Remote access (segregated, controlled remote administration).</summary>
        public static readonly ComplianceMapping OP_ACC_5 =
            new(ComplianceStandard.Ens, "op.acc.5", "Remote access");

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

        /// <summary>op.exp.8 — Activity log recording.</summary>
        public static readonly ComplianceMapping OP_EXP_8 =
            new(ComplianceStandard.Ens, "op.exp.8", "Activity log recording");

        /// <summary>op.exp.9 — Incident management records.</summary>
        public static readonly ComplianceMapping OP_EXP_9 =
            new(ComplianceStandard.Ens, "op.exp.9", "Incident management records");

        /// <summary>op.cont.2 — Continuity plan (HA / failover provisions).</summary>
        public static readonly ComplianceMapping OP_CONT_2 =
            new(ComplianceStandard.Ens, "op.cont.2", "Continuity plan");

        /// <summary>op.cont.3 — Periodic test of continuity procedures.</summary>
        public static readonly ComplianceMapping OP_CONT_3 =
            new(ComplianceStandard.Ens, "op.cont.3", "Periodic continuity tests");

        /// <summary>op.mon.1 — Intrusion detection / monitoring of system activity.</summary>
        public static readonly ComplianceMapping OP_MON_1 =
            new(ComplianceStandard.Ens, "op.mon.1", "System activity monitoring");

        /// <summary>mp.com.1 — Secure communications perimeter (firewall, network segregation).</summary>
        public static readonly ComplianceMapping MP_COM_1 =
            new(ComplianceStandard.Ens, "mp.com.1", "Secure communications perimeter");

        /// <summary>mp.com.2 — Protection of confidentiality in communications (encryption in transit).</summary>
        public static readonly ComplianceMapping MP_COM_2 =
            new(ComplianceStandard.Ens, "mp.com.2", "Protection of confidentiality in communications");

        /// <summary>mp.info.6 — Backup copies of information.</summary>
        public static readonly ComplianceMapping MP_INFO_6 =
            new(ComplianceStandard.Ens, "mp.info.6", "Information backup");

        /// <summary>mp.s.1 — Service protection (availability, isolation).</summary>
        public static readonly ComplianceMapping MP_S_1 =
            new(ComplianceStandard.Ens, "mp.s.1", "Service protection");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            OP_ACC_1, OP_ACC_2, OP_ACC_4, OP_ACC_5,
            OP_EXP_1, OP_EXP_2, OP_EXP_3, OP_EXP_4, OP_EXP_5, OP_EXP_8, OP_EXP_9,
            OP_CONT_2, OP_CONT_3,
            OP_MON_1,
            MP_COM_1, MP_COM_2,
            MP_INFO_6,
            MP_S_1,
        ];
    }

    /// <summary>
    /// C5 — Cloud Computing Compliance Criteria Catalogue (BSI Germany, C5:2020).
    /// Identifiers follow the official BSI taxonomy: OIS (organisation of information security),
    /// HR (human resources), AM (asset management), PS (physical security), RB (regulatory/business),
    /// IDM (identity / access management), KRY (cryptography), KOS (communication security),
    /// PI (portability / interoperability), OPS (operations), BCM (business continuity management),
    /// SIM (incident management), COM (compliance), INQ (investigation requests), DEV (development),
    /// SSO (service supplier), POR (portability of customer data). Only the subset technically
    /// verifiable on a Proxmox VE cluster is listed.
    /// </summary>
    public static class C5
    {
        /// <summary>IDM-01 — Policy for system and data access (least privilege, role separation).</summary>
        public static readonly ComplianceMapping IDM_01 =
            new(ComplianceStandard.C5, "IDM-01", "Policy for system and data access");

        /// <summary>IDM-02 — User registration and approval workflow.</summary>
        public static readonly ComplianceMapping IDM_02 =
            new(ComplianceStandard.C5, "IDM-02", "User registration");

        /// <summary>IDM-03 — Locking, deactivation and deletion of accounts (lifecycle).</summary>
        public static readonly ComplianceMapping IDM_03 =
            new(ComplianceStandard.C5, "IDM-03", "Account lifecycle");

        /// <summary>IDM-08 — Authentication mechanisms (strong / multi-factor for privileged users).</summary>
        public static readonly ComplianceMapping IDM_08 =
            new(ComplianceStandard.C5, "IDM-08", "Authentication mechanisms");

        /// <summary>IDM-09 — Authorisation mechanisms (role-based, least privilege).</summary>
        public static readonly ComplianceMapping IDM_09 =
            new(ComplianceStandard.C5, "IDM-09", "Authorisation mechanisms");

        /// <summary>KRY-01 — Policy for the use of cryptography.</summary>
        public static readonly ComplianceMapping KRY_01 =
            new(ComplianceStandard.C5, "KRY-01", "Policy for use of cryptography");

        /// <summary>KRY-03 — Encryption of data in transit (TLS configuration, certificate hygiene).</summary>
        public static readonly ComplianceMapping KRY_03 =
            new(ComplianceStandard.C5, "KRY-03", "Encryption of data in transit");

        /// <summary>KOS-01 — Technical safeguards for the cloud network (firewalls, segregation).</summary>
        public static readonly ComplianceMapping KOS_01 =
            new(ComplianceStandard.C5, "KOS-01", "Technical safeguards for cloud network");

        /// <summary>KOS-03 — Logging of communication-layer events (firewall log).</summary>
        public static readonly ComplianceMapping KOS_03 =
            new(ComplianceStandard.C5, "KOS-03", "Logging of communication events");

        /// <summary>OPS-09 — Audit logging of administrative activity.</summary>
        public static readonly ComplianceMapping OPS_09 =
            new(ComplianceStandard.C5, "OPS-09", "Audit logging");

        /// <summary>OPS-10 — Monitoring of audit logs and abnormal events.</summary>
        public static readonly ComplianceMapping OPS_10 =
            new(ComplianceStandard.C5, "OPS-10", "Monitoring of audit logs");

        /// <summary>OPS-16 — Handling of vulnerabilities (scanning + remediation).</summary>
        public static readonly ComplianceMapping OPS_16 =
            new(ComplianceStandard.C5, "OPS-16", "Vulnerability handling");

        /// <summary>OPS-18 — Patch management (timely application of security updates).</summary>
        public static readonly ComplianceMapping OPS_18 =
            new(ComplianceStandard.C5, "OPS-18", "Patch management");

        /// <summary>OPS-21 — Backup of customer data (job existence, retention, restore tests).</summary>
        public static readonly ComplianceMapping OPS_21 =
            new(ComplianceStandard.C5, "OPS-21", "Backup of customer data");

        /// <summary>OPS-23 — Storage of backups (separated, protected, available).</summary>
        public static readonly ComplianceMapping OPS_23 =
            new(ComplianceStandard.C5, "OPS-23", "Storage of backups");

        /// <summary>BCM-01 — Top-level business continuity policy.</summary>
        public static readonly ComplianceMapping BCM_01 =
            new(ComplianceStandard.C5, "BCM-01", "Business continuity policy");

        /// <summary>BCM-03 — Redundancy of system components (HA, replication).</summary>
        public static readonly ComplianceMapping BCM_03 =
            new(ComplianceStandard.C5, "BCM-03", "Redundancy of system components");

        /// <summary>BCM-04 — Periodic testing of continuity arrangements.</summary>
        public static readonly ComplianceMapping BCM_04 =
            new(ComplianceStandard.C5, "BCM-04", "Periodic testing of continuity");

        /// <summary>PI-02 — Hardening of virtualisation infrastructure (segregation, machine type, CPU flags).</summary>
        public static readonly ComplianceMapping PI_02 =
            new(ComplianceStandard.C5, "PI-02", "Hardening of virtualisation infrastructure");

        internal static IEnumerable<ComplianceMapping> All =>
        [
            IDM_01, IDM_02, IDM_03, IDM_08, IDM_09,
            KRY_01, KRY_03,
            KOS_01, KOS_03,
            OPS_09, OPS_10, OPS_16, OPS_18, OPS_21, OPS_23,
            BCM_01, BCM_03, BCM_04,
            PI_02,
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
        new[] { Iso27001.All, Nis2.All, Dora.All, PciDss.All, Gdpr.All, AgId.All, Ens.All, C5.All, Soc2.All, Nist80053.All, Iso27017.All, Iso27018.All, Cis.All, NistCsf.All }
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
