# Compliance Mapping

`cv4pve-diag` tags many of its diagnostic checks with the normative controls they satisfy. A failed check on two-factor authentication, for example, is also a documented gap against ISO 27001 A.5.17, NIS2 Art. 21(j), DORA Art. 9 and PCI DSS 8.4.2 — the report doubles as **evidence for auditors**.

The diagnostic logic is unchanged: same checks, same severity, same descriptions. Compliance is an additional **mapping** layer attached to the findings.

---

## How findings are produced

- Checks **without** a compliance mapping are reported as before (e.g. performance thresholds, hardware health, configuration hygiene).
- Checks **with** a compliance mapping include the list of normative controls (standard + control id + title) on every result they produce.
- When the top-level `IncludeOkResult` flag is enabled in [settings.md](settings.md), every check also emits an `Ok` result on success — useful for full audit reports where you need to prove that controls were verified, not only that they were violated.

---

## Single-node setups and compliance

A single-node Proxmox VE host is, by design, **not compliant** with the resilience and business-continuity controls that most standards require (ISO 27001 A.5.30, NIS2 Art. 21(c), DORA Art. 12, …). With only one node:

- There is **no HA failover** — if the host goes down, every guest goes down with it.
- There is **no replication target** between nodes.
- There is **no live migration** for planned maintenance.

For this reason `cv4pve-diag` **always emits** the related findings on single-node hosts, even though they may look noisy on a lab or dev setup:

| Code | Meaning on a single node |
|---|---|
| `IC0017` | Cluster has a single node — HA/quorum/replication ineffective |
| `IC0002` | No HA resources configured — VMs will not restart automatically on failure |
| `IC0003` | No replication jobs configured — no redundant copy of VM data |
| `IG0015` | Guest is not managed by any HA resource |
| `WG0043` | HA guest has no enabled replication job *(only fires if HA itself is configured, which on single-node is unusual)* |

This is **intentional**: on a production single-node deployment those findings ARE the audit evidence that the resilience controls are not in place. The remediation is to add a second node, not to silence the check.

If you run `cv4pve-diag` on a lab or dev single-node setup and the noise bothers you, the right tool is the **ignore rules**: add the codes you do not want to see in `ignored-issues.json` and the next runs will skip them (or list them in a separate table with `--ignored-issues-show`). See [docs/ignored-issues.md](ignored-issues.md).

> **Cross-node checks** (`CN0001`, `CN0002`, `WN0005`–`WN0009`) are different: they compare each node against its peers. On a single-node setup there is nothing to compare to, so these checks are skipped entirely (no KO and no OK emitted) — they would have no compliance value.

---

## CLI usage: the `--compliance` flag

When you pass `--compliance=<standard>` on the `execute` command, two things happen:

1. The output is **filtered**: only findings that have at least one mapping for the selected standard are kept.
2. A `ControlId` column is **added** to the report (or to the Excel `Summary` sheet), listing the control identifiers of that standard for each finding (comma-separated when more than one).

Accepted values match the [Standards supported](#standards-supported) list — for example: `Iso27001`, `Nis2`, `Dora`, `PciDss`, `Iso27017`, `Gdpr`, `NistCsf`, `Cis`, `AgId`.

Omit the flag (default) to get the legacy output: no filter applied, no `ControlId` column.

### Examples

```bash
# All findings mapped to ISO 27001, with their control ids in the report
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --compliance=Iso27001 execute

# Same, exported to Excel — the header table shows "Compliance: Nis2"
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --compliance=Nis2 --output=Excel execute

# Combine with --output and --output-file to produce an HTML auditor report
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --compliance=PciDss --output=Html --output-file=pcidss-report.html execute

# Combine with IncludeOkResult in the settings file to also show passing controls
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --settings-file=audit-settings.json --compliance=Dora execute
```

---

## Standards supported

| Standard | Coverage |
|---|---|
| **ISO/IEC 27001:2022** | Access, backup, crypto, logging, monitoring, vulnerability, network security |
| **NIS2** (EU Directive 2022/2555) | Art. 21(c/d/e/f/h/i/j) |
| **DORA** (EU Regulation 2022/2554) | Art. 9, 10, 11, 12 |
| **PCI DSS v4.0** | 1.2, 4.2, 6.3, 7.2, 8.2, 8.4.2, 10.2 |
| **GDPR** (EU Regulation 2016/679) | Art. 5(1)(f), Art. 32(1)(a/b/c/d) — technical security of processing only |
| **AgID** — Misure minime ICT (Italian PA) | ABSC 2.3, 3.1, 3.2, 4.1, 4.4, 5.1, 5.2, 5.7, 5.10, 8.1, 10.1, 10.3, 10.4, 13.1 |
| **ISO/IEC 27017:2015** | CLD.6.3.1, CLD.8.1.5, CLD.9.5.1/2, CLD.12.1.5, CLD.12.4.5, CLD.13.1.4 |
| **CIS Controls v8** | CIS 3, 4, 5, 6, 7, 8, 10, 11, 12, 13 |
| **NIST CSF 2.0** | ID.AM/RA, PR.AA/DS/IR/PS, DE.CM, RC.RP — relevant subcategories |

---

## Control catalog

### ISO/IEC 27001:2022

| Control | Title | Where it appears |
|---|---|---|
| A.5.15 | Access control | Privileged ACL, container isolation, asset/pool management |
| A.5.16 | Identity management | User lifecycle, notification email |
| A.5.17 | Authentication information | TFA on admin / root / external realms |
| A.5.18 | Access rights | Lifecycle (expiration, disabled users, groups, roles) |
| A.5.30 | ICT readiness for business continuity | HA, replication, single-node cluster, NIC bond, storage availability |
| A.8.2 | Privileged access rights | Admin ACL on root, privileged containers, token privilege separation |
| A.8.5 | Secure authentication | TFA |
| A.8.8 | Management of technical vulnerabilities | Patch level, OS EOL, kernel/version mismatch, CPU security flags, CVE |
| A.8.13 | Information backup | Backup config, retention, schedule, recent backups, disk inclusion, storage |
| A.8.15 | Logging | Cluster log errors, firewall rule logging, task failure rate, NTP, metric server |
| A.8.16 | Monitoring activities | Task history, services, NTP, storage availability, metric server, user notification |
| A.8.20 | Networks security | Cluster firewall, node firewall, policy, 0.0.0.0/0 rules, duplicate MAC |
| A.8.22 | Segregation of networks | VM/CT firewall, duplicate MAC |
| A.8.24 | Use of cryptography | Certificates (expired, expiring, self-signed) |

### NIS2 — Art. 21

| Article | Title | Where it appears |
|---|---|---|
| Art. 21(c) | Backup management and disaster recovery | Backup, HA, replication, single-node, storage capacity |
| Art. 21(d) | Identity / access lifecycle management | Lifecycle, tokens |
| Art. 21(e) | Vulnerability handling and disclosure | Patch, CVE, firewall, OS EOL, CPU security flags |
| Art. 21(f) | Effectiveness assessment (logging / monitoring) | Cluster log, task failures, NTP, services, metric server |
| Art. 21(h) | Cryptography and encryption | Certificates |
| Art. 21(i) | Access control policies and asset management | ACL, container isolation, pools |
| Art. 21(j) | Multi-factor authentication | TFA (all variants) |

### DORA

| Article | Title | Where it appears |
|---|---|---|
| Art. 9 | ICT security policies | TFA |
| Art. 10 | Detection of anomalous activities | Cluster log, task failures, metric server |
| Art. 11 | Backup policies and recovery procedures | Backup, storage backup config |
| Art. 12 | ICT business continuity policy | HA, replication, single-node, storage availability, replication errors |

### PCI DSS v4.0

| Control | Title | Where it appears |
|---|---|---|
| 1.2 | Network security controls configuration | Firewall enable / policy / rules |
| 4.2 | Strong cryptography over open, public networks | Certificates |
| 6.3 | Security vulnerabilities are identified and addressed | Patch, CVE, CPU security flags |
| 7.2 | Access definition and assignment | Container privileged access |
| 8.2 | User identification and account lifecycle | Lifecycle, tokens |
| 8.4.2 | MFA for all access into the cardholder data environment | TFA |
| 10.2 | Audit logs for anomaly detection | Cluster log, NTP, firewall logging |

### GDPR — EU Regulation 2016/679

Only technical articles relevant to a virtualisation cluster. Procedural / organisational requirements (DPIA, breach notification, data subject rights, ...) are out of scope.

| Article | Title | Where it appears |
|---|---|---|
| Art. 5(1)(f) | Integrity and confidentiality (security principle) | TFA, access privilege, certificates, firewall, account lifecycle, duplicate MAC |
| Art. 32(1)(a) | Pseudonymisation and encryption of personal data | Certificates |
| Art. 32(1)(b) | Confidentiality, integrity, availability and resilience of processing systems | HA, replication, single-node, storage/node availability, patch, CVE, disk cache integrity, TFA |
| Art. 32(1)(c) | Timely restoration of availability after an incident | Backup (all areas), backup storage availability |
| Art. 32(1)(d) | Regular testing of the effectiveness of security measures | Cluster log, task failures, NTP, services, metric server, user notification, firewall audit logging |

### AgID — Misure minime ICT per le PA (Italian Public Administration baseline)

Subset of ABSC (AgID Basic Security Controls) verifiable on a virtualisation cluster.

| Control | Title | Where it appears |
|---|---|---|
| ABSC 2.3 | Authorised software list and EOL tracking | OS not maintained, PVE EOL, CVE |
| ABSC 3.1 | Use secure standard configurations | Container privileged, raw lxc config |
| ABSC 3.2 | Keep configurations aligned and up to date | Patch, version/kernel/package mismatch |
| ABSC 4.1 / 4.4 | Vulnerability scanning and remediation | CVE, important updates |
| ABSC 5.1 | Limit administrative privileges | ACL, container privileged, root@pam token privsep |
| ABSC 5.2 | Track administrator actions | Cluster log, task history, firewall audit logging |
| ABSC 5.7 | MFA for administrators | TFA (root@pam, admin, group, realm) |
| ABSC 5.10 | Limit local authentication and credential lifetime | Local user expiration, API token expiration |
| ABSC 8.1 | Defences against malware (network baseline) | Firewall, patch |
| ABSC 10.1 / 10.3 / 10.4 | Backup execution, integrity, and protection | All backup checks, backup storage availability |
| ABSC 13.1 | Encrypt sensitive data in transit and at rest | Certificates |

### ISO/IEC 27017:2015 — Cloud-specific extensions

Adds the CLD.* controls to ISO 27001. The base ISO 27001 controls are listed above.

| Control | Title | Where it appears |
|---|---|---|
| CLD.6.3.1 | Shared roles and responsibilities in cloud | HA, replication, single-node, NIC bond |
| CLD.8.1.5 | Removal of cloud service customer assets | (declared, not currently mapped) |
| CLD.9.5.1 / 9.5.2 | Segregation in virtual computing environments / VM hardening | Container isolation, CPU security flags, patch consistency |
| CLD.12.1.5 | Administrator's operational security | Privileged ACL |
| CLD.12.4.5 | Monitoring of cloud services | Cluster log, task failures, NTP, services, metric server |
| CLD.13.1.4 | Alignment of security for virtual and physical networks | Firewall, duplicate MAC |

### CIS Controls v8

| Control | Title | Where it appears |
|---|---|---|
| CIS 3 | Data Protection | Certificates, disk cache integrity |
| CIS 4 | Secure Configuration of Enterprise Assets | Container isolation |
| CIS 5 | Account Management | Account lifecycle, user notifications |
| CIS 6 | Access Control Management | TFA, ACL, container privileged |
| CIS 7 | Continuous Vulnerability Management | Patch, CVE |
| CIS 8 | Audit Log Management | Cluster log, task failures, NTP, firewall audit logging |
| CIS 11 | Data Recovery | Backup, HA, replication, single-node |
| CIS 12 | Network Infrastructure Management | Firewall, duplicate MAC |
| CIS 13 | Network Monitoring and Defense | Firewall |

### NIST CSF 2.0

Subcategories chosen for relevance to virtualisation cluster diagnostics.

| Subcategory | Title | Where it appears |
|---|---|---|
| ID.AM-02 | Software/services/systems inventory is maintained | Empty pools (asset/inventory cleanliness) |
| ID.RA-01 | Asset vulnerabilities are identified and recorded | Patch, CVE |
| PR.AA-01 | Identities and credentials are managed | TFA, account lifecycle |
| PR.AA-03 | Users, services and hardware are authenticated | TFA |
| PR.AA-05 | Access permissions and entitlements are managed | ACL, container privileged |
| PR.DS-01 | Data-at-rest is protected | Disk cache integrity |
| PR.DS-02 | Data-in-transit is protected | Certificates |
| PR.DS-11 | Backups are conducted, protected and tested | Backup |
| PR.IR-01 | Networks protected from unauthorized access | Firewall, duplicate MAC |
| PR.IR-04 | Adequate resource capacity is maintained | HA, replication, single-node, storage availability |
| PR.PS-02 | Software is maintained commensurate with risk | Patch, CVE |
| DE.CM-01 | Networks and services are monitored | Cluster log, task failures, NTP, services |
| DE.CM-03 | Personnel activity is monitored | Cluster log, task failures, user notification |
| RC.RP-01 | Recovery procedures are in place and exercised | Backup, HA, replication, single-node |

---

## Coverage by area

The mapping is concentrated where compliance frameworks actually demand verifiable controls. The full check list per area is in [checks.md](checks.md); the table below shows where compliance tags are attached.

| Area | Examples of mapped checks |
|---|---|
| **Access / Identity** | root@pam TFA, admin TFA (direct and via group), ACL scoping, account expiration, API tokens, external realm TFA, privilege separation |
| **Backup** | Job presence, retention, schedule, recent backups, disk inclusion, backup storage availability, recent task failures |
| **Resilience / HA** | HA resource state, replication configuration and errors, single-node topology, NIC bond redundancy, HA on shared storage |
| **Firewall** | Cluster firewall on/off, default policy, node firewall, overly permissive rules, audit logging, VM/CT firewall, IP filter |
| **Cryptography** | Certificates expired, expiring soon, self-signed, disk cache safety |
| **Patch / Vulnerability** | PVE EOL, subscription, important updates, reboot required, version/kernel/package consistency, OS not maintained, CPU security flags, CVE |
| **Logging / Monitoring** | Cluster log errors, task failure rate, services not running, NTP offset, storage availability, metric server, user notification |
| **Container isolation** | Nesting/keyctl, privileged containers, AppArmor, raw lxc config |

---

## Disclaimer & limits

The compliance mapping in `cv4pve-diag` is **automated and technical only** — it reads the cluster state and tags findings against the verifiable subset of each standard. Procedural, organisational and physical controls are out of scope and require manual review.

The report **does not constitute formal certification**. Use it as a continuous self-assessment input alongside your audit programme, never as the sole evidence of conformity.

Specifically:

- **Scope is technical.** Policies, training, supplier management, business continuity testing, physical security, incident response procedures and similar organisational requirements are **not** assessed here.
- **Coverage is partial.** A standard may include dozens or hundreds of controls; this tool maps only the ones that can be verified from the Proxmox VE API state. For example: ISO 27001 has 93 Annex A controls; only the technical subset relevant to a virtualisation cluster is mapped. NIS2 Art. 21(2) defines ten measures; only the technically verifiable ones produce findings.
- **A passing check does not equal compliance with the control.** It only confirms that the specific automated rule passed. Full conformity usually requires manual evidence (policies, procedures, evidence of operation) that this tool cannot validate.
- **Coverage is not symmetrical across standards.** PCI DSS requirements that involve cardholder-data flows, GDPR requirements on lawful processing bases, DORA testing/reporting obligations and HIPAA-like requirements on PHI are not enforceable from PVE state alone.
- **Controls evolve.** Standard revisions (ISO 27001 minor updates, NIS2 secondary acts, PCI DSS minor versions, …) may shift control numbering or scope. Re-check the catalog when a new release of `cv4pve-diag` is published.
- **Mapping is best-effort and informative.** The association between an automated check and a normative control reflects the project maintainers' interpretation; an auditor may disagree on specific mappings. Treat findings as inputs to a conversation, not as authoritative pronouncements.

In short: a clean `cv4pve-diag --compliance=…` report means *the cluster passes the technical controls this tool can verify for that standard*. It is a strong signal, but it is not — and cannot be — a substitute for a formal audit.
