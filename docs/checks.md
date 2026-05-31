# Diagnostic Checks

This is the catalog of every check `cv4pve-diag` runs against a Proxmox VE cluster, grouped by area.

## Reading the tables

- **Code** — short alphanumeric identifier. The first letter encodes severity (`C` = Critical, `W` = Warning, `I` = Info), the second the area (`C` = Cluster, `N` = Node, `S` = Storage, `G` = Guest). Use this code with [ignore rules](ignored-issues.md) to suppress specific findings.
- **SubContext** — a finer-grained classifier surfaced in the JSON output and Excel report.
- **Gravity** — the severity emitted when the check fails. Some threshold checks (CPU, memory, PSI, disk usage, …) can emit either Warning or Critical depending on the value.

> Checks tagged with compliance controls (ISO 27001 / NIS2 / DORA / PCI DSS) attach the mapping to the finding. See [compliance.md](compliance.md).

> With `IncludeOkResult: true` in [settings.md](settings.md#including-ok-results), every check also emits an Ok result when it passes — useful for full audit reports.

---

<details>
<summary><strong>Cluster Checks</strong></summary>

| Code   | SubContext  | Gravity  | Description                                                              |
| ------ | ----------- | -------- | ------------------------------------------------------------------------ |
| WC0001 | Backup      | Warning  | No automated backup job for any VM/CT                                    |
| IC0001 | Backup      | Info     | Backup job has no compression configured                                 |
| WC0002 | Backup      | Warning  | Backup job has no maxfiles/prune policy — storage will fill up           |
| CC0001 | Quorum      | Critical | Cluster has lost quorum — VM operations may be blocked                   |
| CC0002 | Quorum      | Critical | Corosync expected votes does not match online node count                 |
| CC0003 | HA          | Critical | HA group references nodes that are currently offline                     |
| CC0005 | HA          | Critical | HA service is in error state — manual recovery required                  |
| IC0002 | HA          | Info     | No VMs/CTs protected by HA — no automatic failover on node failure       |
| IC0003 | Replication | Info     | No storage replication configured — no redundant copy across nodes       |
| WC0009 | Replication | Warning  | Replication job is disabled — guest data is no longer replicated         |
| WC0010 | Replication | Warning  | Enabled replication job has no schedule — it will never run              |
| IC0004 | Pool        | Info     | Resource pool exists but has no VMs or storage assigned                  |
| WC0003 | Firewall    | Warning  | Cluster-level firewall is completely disabled                            |
| WC0004 | Firewall    | Warning  | Inbound or outbound policy allows unmatched traffic through              |
| WN0001 | Firewall    | Warning  | Cluster firewall enabled but individual node has it disabled             |
| WC0008 | Firewall    | Warning  | Cluster firewall rule with overly permissive source or destination       |
| CC0004 | Access      | Critical | root@pam has no two-factor authentication configured                     |
| WC0007 | Access      | Warning  | User with Administrator role has no two-factor authentication            |
| WC0005 | Access      | Warning  | User has Administrator role at root path `/` — prefer scoped permissions |
| WC0006 | Access      | Warning  | Disabled user still has valid API tokens that should be revoked          |
| IC0005 | Access      | Info     | Local user has no expiration date configured                             |
| IC0006 | Access      | Info     | API token has no expiration date configured                              |
| IC0007 | Access      | Info     | Enabled user has no email — will not receive notifications               |
| IC0008 | Access      | Info     | Group has no members                                                     |
| IC0009 | Access      | Info     | Custom role is not assigned in any ACL                                   |
| WC0013 | Access      | Warning  | User has Administrator role via a group but no TFA configured            |
| WC0014 | Access      | Warning  | Disabled user still has Administrator role on `/`                        |
| IC0010 | Access      | Info     | Administrator role on `/` with Propagate disabled                        |
| IC0011 | Access      | Info     | LDAP/AD/OpenID realm does not enforce TFA at realm level                 |
| WC0015 | Access      | Warning  | root@pam API token has no privilege separation                           |
| WC0016 | Access      | Warning  | User has expiration date in the past but is still enabled                |
| WC0017 | Backup      | Warning  | Enabled backup job has no schedule — will never run                      |
| IC0012 | Backup      | Info     | Backup job is disabled                                                   |
| WC0018 | Backup      | Warning  | Recent vzdump task ended with non-OK status                              |
| IC0013 | Firewall    | Info     | Cluster firewall has enabled rules but none configure logging            |
| IC0014 | Firewall    | Info     | Cluster firewall has 10+ disabled rules — stale configuration            |
| IC0015 | Log         | Info     | 10+ error-level entries in the cluster journal                           |
| IC0016 | Tasks       | Info     | 10%+ of recent cluster tasks failed                                      |
| IC0017 | Topology    | Info     | Cluster has a single node — HA / quorum / replication ineffective        |
| IC0018 | Metrics     | Info     | No external metric server configured — only volatile RRD data            |
| IC0019 | Metrics     | Info     | Metric servers exist but every one of them is disabled                   |
| WC0011 | Version     | Warning  | Online nodes run different Proxmox VE versions                           |
| WC0012 | Version     | Warning  | Online nodes run different kernel versions                               |

</details>

<details>
<summary><strong>Node Checks</strong></summary>

| Code          | SubContext       | Gravity          | Description                                                                    |
| ------------- | ---------------- | ---------------- | ------------------------------------------------------------------------------ |
| WN0002        | Status           | Warning          | Node is not reachable                                                          |
| WN0003        | EOL              | Warning          | Installed PVE version has reached end of life                                  |
| WN0004        | Subscription     | Warning          | Node has no active Proxmox VE subscription                                     |
| CN0001        | Version          | Critical         | Nodes in cluster have different PVE versions                                   |
| WN0005        | Hosts            | Warning          | `/etc/hosts` content differs between nodes                                     |
| WN0006        | DNS              | Warning          | DNS configuration differs between nodes                                        |
| WN0007        | Timezone         | Warning          | Timezone differs between nodes                                                 |
| WN0008        | AptRepositories  | Warning          | APT repository sources differ between nodes                                    |
| WN0009        | Network          | Warning          | Physical NIC MTU differs between nodes                                         |
| WN0010        | Network          | Warning          | Physical NIC is down                                                           |
| WN0034        | Network          | Warning          | Bond has fewer than two slaves — no link redundancy                            |
| CN0002        | PackageVersions  | Critical         | Nodes have different package versions installed                                |
| WN0011        | Service          | Warning          | A required system service is not running                                       |
| CN0003        | Certificates     | Critical         | TLS certificate has expired                                                    |
| WN0023        | Certificates     | Warning          | TLS certificate expires within 30 days                                         |
| IN0004        | Certificates     | Info             | TLS certificate is self-signed — consider a CA-signed certificate              |
| CN0004        | Replication      | Critical         | Replication job has errors                                                     |
| IN0001        | Update           | Info             | Packages available for update                                                  |
| WN0012        | Update           | Warning          | Security/important packages available for update                               |
| WN0013        | Reboot           | Warning          | Running kernel differs from installed kernel                                   |
| WN0014        | NTP              | Warning          | Node time is out of sync with NTP                                              |
| IN0002        | IOMMU            | Info             | IOMMU disabled — PCI passthrough will not work                                 |
| IN0003        | Consolidation    | Info             | Node CPU and RAM utilization both below threshold — consider consolidating VMs |
| WN0015        | CPUCompatibility | Warning          | Nodes have different x86-64 feature levels — live migration may fail           |
| WN0036        | Memory           | Warning          | Sum of VM allocated RAM exceeds physical node RAM                              |
| WN0037        | Network          | Warning          | VM/CT uses VLAN tag on a bridge that is not VLAN-aware — tag silently ignored  |
| WN0027        | Usage            | Warning/Critical | CPU usage above configured threshold                                           |
| WN0038        | Usage            | Warning/Critical | Memory usage above configured threshold                                        |
| WN0039/WN0040 | Usage            | Warning/Critical | Network throughput above configured threshold                                  |
| WN0031        | Pressure         | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                              |
| WN0032        | Pressure         | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                         |
| WN0033        | Pressure         | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                      |
| WG0032        | HealthScore      | Warning/Critical | Composite health score below threshold                                         |
| CN0005        | Tasks            | Critical         | Failed tasks found in the last 48 hours                                        |
| WN0016        | S.M.A.R.T.       | Warning          | Disk reports a SMART health problem                                            |
| WN0019/CN0007 | S.M.A.R.T.       | Warning/Critical | Disk temperature exceeds configured threshold                                  |
| WN0020        | S.M.A.R.T.       | Warning          | Disk has reallocated sectors — disk may be failing                             |
| CN0008        | S.M.A.R.T.       | Critical         | Disk has pending sectors — imminent data loss risk                             |
| CN0009        | S.M.A.R.T.       | Critical         | Disk has offline uncorrectable sectors                                         |
| WN0021        | S.M.A.R.T.       | Warning          | Disk has UDMA CRC errors — check cable/controller                              |
| WN0022        | S.M.A.R.T.       | Warning          | Disk has reported uncorrectable errors                                         |
| WN0017        | SSD Wearout      | Warning          | SSD does not expose wear data                                                  |
| WN0018        | SSD Wearout      | Warning/Critical | SSD wearout consumed above threshold                                           |
| WN0044        | Zfs              | Warning/Critical | ZFS pool disk usage above configured threshold                                 |
| CN0010        | Zfs              | Critical         | ZFS pool is not in ONLINE state                                                |
| CN0012        | Zfs              | Critical         | ZFS pool vdev is in a degraded or faulted state                                |
| WN0025        | Zfs              | Warning          | ZFS pool vdev has accumulated read/write/checksum errors                       |
| WN0024        | Zfs              | Warning          | ZFS pool reports errors                                                        |
| WN0026/CN0013 | LvmThin          | Warning/Critical | LVM-thin metadata usage is high — full metadata causes data corruption         |
| WN0028        | Usage            | Warning/Critical | CPU IOWait above configured threshold — indicates storage bottleneck           |
| WN0029        | Usage            | Warning/Critical | Node root filesystem usage above configured threshold                          |
| WN0030        | Usage            | Warning/Critical | Node SWAP usage above configured threshold — indicates RAM pressure            |
| CU0001        | Status           | Critical         | A cluster resource with an unknown type was detected                           |
| WN0042/CN0015 | CVE              | Warning/Critical | A known CVE affects the installed Proxmox VE version                           |

</details>

<details>
<summary><strong>Storage Checks</strong></summary>

| Code   | SubContext | Gravity          | Description                                                                         |
| ------ | ---------- | ---------------- | ----------------------------------------------------------------------------------- |
| CS0001 | Status     | Critical         | Storage is not accessible (excludes storages disabled on purpose)                   |
| WS0008 | Status     | Warning          | Storage is disabled — backup jobs or guests may still point at it                   |
| WS0001 | Usage      | Warning/Critical | Storage usage above configured threshold                                            |
| WS0003 | Backup     | Warning          | Backup file whose VMID no longer exists                                             |
| WS0002 | Image      | Warning          | Disk image not attached to any VM/CT                                                |
| WS0004 | Usage      | Warning          | Allocated disk space exceeds physical capacity (thin provisioning)                  |
| WS0005 | Shared     | Warning          | Shared storage only mounted on one node                                             |
| WS0006 | Backup     | Warning          | No storage has 'backup' content type — backups cannot be stored                     |
| WS0007 | Backup     | Warning          | Backup job storage not available on a node — VMs on that node will not be backed up |

</details>

<details>
<summary><strong>VM (QEMU) Checks</strong></summary>

| Code          | SubContext      | Gravity          | Description                                                                              |
| ------------- | --------------- | ---------------- | ---------------------------------------------------------------------------------------- |
| CG0001        | VM State        | Critical         | Hibernated VM state left in pending — VM was suspended and never resumed                 |
| IG0010        | Status          | Info             | Config changes pending reboot to take effect                                             |
| WG0015        | Status          | Warning          | VM is locked and cannot be managed                                                       |
| WG0001        | OS              | Warning          | VM OS type is not configured                                                             |
| WG0002        | OSNotMaintained | Warning          | Guest OS has reached end of life                                                         |
| WG0003        | Agent           | Warning          | Guest agent not configured                                                               |
| WG0004        | Agent           | Warning          | Agent enabled but not responding inside guest                                            |
| IG0001        | VirtIO          | Info             | SCSI controller is not VirtIO — lower performance                                        |
| IG0002        | VirtIO          | Info             | Disk not using VirtIO bus                                                                |
| IG0003        | VirtIO          | Info             | Network interface not using VirtIO driver                                                |
| WG0005        | Hardware        | Warning          | CD-ROM drive has an image mounted                                                        |
| WG0006        | CPU             | Warning          | CPU type 'host' prevents live migration                                                  |
| IG0004        | CPU             | Info             | CPU type is outdated (kvm64)                                                             |
| WG0037        | CPU             | Warning          | Non-host CPU type missing +spec-ctrl/+ssbd/+pcid/+md-clear flags                         |
| WG0007        | CPU             | Warning          | CPU hotplug enabled on Windows guest — not supported                                     |
| CG0004        | CPU             | Critical         | CPU type 'host' is incompatible with HA — live migration required by HA is impossible    |
| WG0036        | CPU             | Warning          | Node vCPU overcommit ratio exceeds configured threshold                                  |
| IG0005        | Balloon         | Info             | RAM is statically allocated — no memory ballooning                                       |
| IG0006        | Balloon         | Info             | Balloon has no room to reclaim memory                                                    |
| WG0008        | Hardware        | Warning          | Disk uses cache=unsafe — data loss risk on host crash                                    |
| WG0009        | Hardware        | Warning          | Disk uses writeback cache but backup is disabled                                         |
| WG0010        | SecureBoot      | Warning          | Windows 11 requires UEFI (bios=ovmf)                                                     |
| WG0011        | SecureBoot      | Warning          | Windows 11 requires TPM 2.0                                                              |
| IG0007        | Hardware        | Info             | VM has virtio-rng device — verify this is intentional                                    |
| IG0008        | Hardware        | Info             | VM has serial console configured — verify this is intentional                            |
| IG0012        | Hardware        | Info             | Machine type not configured — QEMU will use default which may change across PVE upgrades |
| IG0016        | Hardware        | Info             | Machine type pinned to an old version — newer version available on the node              |
| WG0012        | Hardware        | Warning          | Passthrough configured — live migration and HA not possible                              |
| CG0005        | HA              | Critical         | Disk is on non-shared storage but VM is managed by HA — live migration will fail         |
| IG0015        | HA              | Info             | Guest is not managed by any HA resource — will not be restarted on node failure          |
| WG0043        | Replication     | Warning          | HA guest has no enabled replication job — failover target will have no recent data       |
| WG0034        | Network         | Warning          | VM has no network interface — completely isolated from network                           |
| WG0033        | Network         | Warning          | MAC address shared with another VM — causes network conflicts                            |
| WG0013        | Firewall        | Warning          | VM firewall is disabled — exposed to all bridge traffic                                  |
| IG0009        | Firewall        | Info             | VM can spoof source IP addresses                                                         |
| WG0016        | StartOnBoot     | Warning          | VM will not start automatically after host reboot                                        |
| IG0011        | Protection      | Info             | VM protection flag not set                                                               |
| WG0017        | Backup          | Warning          | VM not included in any backup job                                                        |
| CG0002        | Backup          | Critical         | A disk has backup disabled                                                               |
| WG0018        | Hardware        | Warning          | Disk detached from VM but still in storage                                               |
| WG0019        | Backup          | Warning          | Backup files older than configured days found                                            |
| WG0020        | Backup          | Warning          | No backup found in the last configured days                                              |
| CG0003        | Tasks           | Critical         | Failed tasks found in the last 48 hours                                                  |
| WG0021        | AutoSnapshot    | Warning          | cv4pve-autosnap not configured                                                           |
| WG0022        | AutoSnapshot    | Warning          | Old AutoSnap snapshots present — update required                                         |
| WG0024        | SnapshotOld     | Warning          | Snapshots older than configured age                                                      |
| WG0035        | Snapshot        | Warning          | Snapshot includes RAM state — wastes disk space and blocks storage migration             |
| WG0023        | SnapshotCount   | Warning          | Snapshot count exceeds configured limit                                                  |
| WG0025        | Usage           | Warning/Critical | CPU usage above configured threshold                                                     |
| WG0026        | Usage           | Warning/Critical | Memory usage above configured threshold                                                  |
| WG0027/WG0028 | Usage           | Warning/Critical | Network throughput above configured threshold                                            |
| WG0029        | Pressure        | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                                        |
| WG0030        | Pressure        | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                                   |
| WG0031        | Pressure        | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                                |
| WG0032        | HealthScore     | Warning/Critical | Composite health score below threshold                                                   |
| WG0014        | Agent           | Warning          | Template has QEMU agent enabled — unused on templates                                    |

</details>

<details>
<summary><strong>LXC (Container) Checks</strong></summary>

| Code   | SubContext    | Gravity          | Description                                                                  |
| ------ | ------------- | ---------------- | ---------------------------------------------------------------------------- |
| IG0010 | Status        | Info             | Config changes pending reboot to take effect                                 |
| WG0015 | Status        | Warning          | Container is locked and cannot be managed                                    |
| WG0039 | Security      | Warning          | Container runs as privileged — root inside has host-level access             |
| CG0006 | Security      | Critical         | Privileged container has AppArmor disabled — no kernel confinement           |
| WG0038 | Features      | Warning          | `nesting=1` set but `keyctl=1` missing — keyring isolation incomplete        |
| WG0041 | Config        | Warning          | Container has raw LXC config entries that bypass PVE abstractions            |
| WG0040 | Memory        | Warning          | Container has no memory limit (Memory=0) — can consume all host RAM          |
| IG0013 | Config        | Info             | Container has swap disabled — OOM killer risk under memory pressure          |
| IG0014 | Config        | Info             | Container has no hostname configured                                         |
| WG0013 | Firewall      | Warning          | Container firewall is disabled — exposed to all bridge traffic               |
| IG0009 | Firewall      | Info             | Container can spoof source IP addresses                                      |
| WG0016 | StartOnBoot   | Warning          | CT will not start automatically after host reboot                            |
| IG0011 | Protection    | Info             | CT protection flag not set                                                   |
| WG0017 | Backup        | Warning          | CT not included in any backup job                                            |
| CG0002 | Backup        | Critical         | A disk has backup disabled                                                   |
| WG0018 | Hardware      | Warning          | Disk detached from CT but still in storage                                   |
| WG0019 | Backup        | Warning          | Backup files older than configured days found                                |
| WG0020 | Backup        | Warning          | No backup found in the last configured days                                  |
| CG0003 | Tasks         | Critical         | Failed tasks found in the last 48 hours                                      |
| IG0015 | HA            | Info             | Container is not managed by any HA resource — will not be restarted on node failure |
| WG0043 | Replication   | Warning          | HA container has no enabled replication job — failover target will have no recent data |
| WG0021 | AutoSnapshot  | Warning          | cv4pve-autosnap not configured                                               |
| WG0022 | AutoSnapshot  | Warning          | Old AutoSnap snapshots present — update required                             |
| WG0024 | SnapshotOld   | Warning          | Snapshots older than configured age                                          |
| WG0035 | Snapshot      | Warning          | Snapshot includes RAM state — wastes disk space and blocks storage migration |
| WG0023 | SnapshotCount | Warning          | Snapshot count exceeds configured limit                                      |
| WG0025 | Usage         | Warning/Critical | CPU usage above configured threshold                                         |
| WG0026 | Usage         | Warning/Critical | Memory usage above configured threshold                                      |
| WG0027/WG0028 | Usage  | Warning/Critical | Network throughput above configured threshold                                |
| WG0029 | Pressure      | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                            |
| WG0030 | Pressure      | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                       |
| WG0031 | Pressure      | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                    |
| WG0032 | HealthScore   | Warning/Critical | Composite health score below threshold                                       |

</details>

---

## Meta codes

These codes are not regular checks — they are emitted by the engine itself when something goes wrong outside the diagnostic logic.

| Code   | SubContext | Gravity | Description                                                                                                  |
| ------ | ---------- | ------- | ------------------------------------------------------------------------------------------------------------ |
| WG0042 | ApiError   | Warning | A Proxmox VE API call failed during analysis (network error, permission denied, endpoint unavailable, …). The affected check was skipped; the underlying call/endpoint is reported in the description. |

---

> All checks can be suppressed via [ignore rules](ignored-issues.md). Use the `Code` field to target specific checks precisely.
