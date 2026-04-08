# cv4pve-diag

```
     ______                _                      __
    / ____/___  __________(_)___ _   _____  _____/ /_
   / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
  / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
  \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Diagnostic Tool for Proxmox VE (Made in Italy)
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-diag.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-diag.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-diag/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-diag/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-diag/releases)
[![NuGet](https://img.shields.io/nuget/v/Corsinvest.ProxmoxVE.Diagnostic.Api.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Corsinvest.ProxmoxVE.Diagnostic.Api/)
[![WinGet](https://img.shields.io/winget/v/Corsinvest.cv4pve.diag?style=flat-square&logo=windows)](https://winstall.app/apps/Corsinvest.cv4pve.diag)
[![AUR](https://img.shields.io/aur/version/cv4pve-diag?style=flat-square&logo=archlinux)](https://aur.archlinux.org/packages/cv4pve-diag)

> **Health checks and diagnostics for Proxmox VE** — analyzes your entire cluster in one run and tells you what is wrong.

---

## Quick Start

```bash
wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-x64.zip
unzip cv4pve-diag-linux-x64.zip
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute
```

With API token (recommended):

```bash
./cv4pve-diag --host=YOUR_HOST --api-token=user@realm!token=uuid execute
```

---

## Installation

| Platform           | Command                                                                                                                                                                                                                                   |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Linux**          | `wget .../cv4pve-diag-linux-x64.zip && unzip cv4pve-diag-linux-x64.zip && chmod +x cv4pve-diag`                                                                                                                                           |
| **Windows WinGet** | `winget install Corsinvest.cv4pve.diag`                                                                                                                                                                                                   |
| **Windows manual** | Download `cv4pve-diag-win-x64.zip` from [Releases](https://github.com/Corsinvest/cv4pve-diag/releases)                                                                                                                                    |
| **Arch Linux**     | `yay -S cv4pve-diag`                                                                                                                                                                                                                      |
| **Debian/Ubuntu**  | `sudo dpkg -i cv4pve-diag-VERSION-ARCH.deb`                                                                                                                                                                                               |
| **RHEL/Fedora**    | `sudo rpm -i cv4pve-diag-VERSION-ARCH.rpm`                                                                                                                                                                                                |
| **macOS**          | Homebrew: `brew tap Corsinvest/homebrew-tap && brew install cv4pve-diag` ([tap repo](https://github.com/Corsinvest/homebrew-tap))<br/>Manual: `wget .../cv4pve-diag-osx-x64.zip && unzip cv4pve-diag-osx-x64.zip && chmod +x cv4pve-diag` |

All binaries on the [Releases page](https://github.com/Corsinvest/cv4pve-diag/releases).

---

## Features

- **Self-contained binary** — no runtime to install, copy and run
- **Cross-platform** — Windows, Linux, macOS
- **API-based** — no root or SSH access required
- **Cluster-aware** — analyzes all nodes, VMs, CTs and storages in one run
- **High availability** — multiple host support for automatic failover
- **Output formats** — Text, HTML, JSON, Markdown, Excel
- **Severity levels** — Critical, Warning, Info
- **Configurable thresholds** — CPU, memory, disk, network, health score, SMART, PSI pressure
- **Ignore rules** — suppress known/accepted issues by ErrorCode, Id, SubContext or Description
- **API token** support (Proxmox VE 6.2+)

---

<details>
<summary><strong>Security &amp; Permissions</strong></summary>

### Required Permissions

| Permission          | Purpose                                  | Scope            |
| ------------------- | ---------------------------------------- | ---------------- |
| **VM.Audit**        | Read VM/CT configuration and status      | Virtual machines |
| **Datastore.Audit** | Check storage capacity and content       | Storage systems  |
| **Pool.Audit**      | Access pool information                  | Resource pools   |
| **Sys.Audit**       | Node system information, services, disks | Cluster nodes    |

</details>

---

## Usage

```bash
# Basic execution
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid execute

# With output format
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid --output=Html execute
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid --output=Json execute
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid --output=Excel execute

# With settings and ignore rules
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --settings-file=settings.json \
  --ignored-issues-file=ignored-issues.json \
  execute

# Parameter file (recommended for complex setups)
cv4pve-diag @/etc/cv4pve/production.conf execute
```

### Example Output

```
+-----------------------------+--------+--------------------------------------------------------------------+---------+-----------------+----------+
| Id                          | Code   | Description                                                        | Context | SubContext      | Gravity  |
+-----------------------------+--------+--------------------------------------------------------------------+---------+-----------------+----------+
| nodes/pve02                 | CN0002 | Nodes package version not equal                                    | Node    | PackageVersions | Critical |
| nodes/pve02/qemu/203        | CG0002 | Disk 'scsi0' disabled for backup                                   | Qemu    | Backup          | Critical |
| nodes/pve01/lxc/100         | CG0002 | Disk 'rootfs' disabled for backup                                  | Lxc     | Backup          | Critical |
| nodes/pve01/qemu/1030       | WG0026 | Memory (rrd Day AVERAGE) usage 92.9% - 5.99 GB of 6.44 GB         | Qemu    | Usage           | Critical |
| nodes/pve02                 | WN0008 | Nodes hosts configuration not equal                                | Node    | Hosts           | Warning  |
| nodes/pve01/storage/local   | WS0001 | Image Orphaned 51.54 GB file vm-106-disk-1                         | Storage | Image           | Warning  |
| nodes/pve01/storage/pbs01   | WS0003 | Storage usage 75% - 2.42 TB of 3.22 TB                            | Storage | Usage           | Warning  |
| nodes/pve02/qemu/106        | WG0003 | Qemu Agent not enabled                                             | Qemu    | Agent           | Warning  |
| nodes/pve02/qemu/999        | WG0017 | vzdump backup not configured                                       | Qemu    | Backup          | Warning  |
| nodes/pve01/qemu/1030       | WG0005 | Cdrom mounted                                                      | Qemu    | Hardware        | Warning  |
| nodes/pve01/qemu/1010       | WG0002 | OS 'Microsoft Windows 10/2016/2019' not maintained from vendor!    | Qemu    | OSNotMaintained | Warning  |
| nodes/pve02                 | IN0001 | 26 Update available                                                | Node    | Update          | Info     |
| nodes/pve01/qemu/1000       | IG0011 | For production environment is better VM Protection = enabled       | Qemu    | Protection      | Info     |
+-----------------------------+--------+--------------------------------------------------------------------+---------+-----------------+----------+
```

---

## Diagnostic Checks

<details>
<summary><strong>Cluster Checks</strong></summary>

| Check                               | Code   | SubContext  | Gravity  | Description                                                              |
| ----------------------------------- | ------ | ----------- | -------- | ------------------------------------------------------------------------ |
| No backup job configured            | WC0001 | Backup      | Warning  | No automated backup job for any VM/CT                                    |
| Backup job no compression           | IC0001 | Backup      | Info     | Backup job has no compression configured                                 |
| Backup job no retention             | WC0002 | Backup      | Warning  | Backup job has no maxfiles/prune policy — storage will fill up           |
| No quorum                           | CC0001 | Quorum      | Critical | Cluster has lost quorum — VM operations may be blocked                   |
| Corosync expected_votes mismatch    | CC0002 | Quorum      | Critical | Corosync expected votes does not match online node count                 |
| HA group with offline nodes         | CC0003 | HA          | Critical | HA group references nodes that are currently offline                     |
| No HA resources configured          | IC0002 | HA          | Info     | No VMs/CTs protected by HA — no automatic failover on node failure       |
| No replication jobs configured      | IC0003 | Replication | Info     | No storage replication configured — no redundant copy across nodes       |
| Pool empty                          | IC0004 | Pool        | Info     | Resource pool exists but has no VMs or storage assigned                  |
| Cluster firewall disabled           | WC0003 | Firewall    | Warning  | Cluster-level firewall is completely disabled                            |
| Firewall policy not DROP            | WC0004 | Firewall    | Warning  | Inbound or outbound policy allows unmatched traffic through              |
| Node firewall disabled              | WN0001 | Firewall    | Warning  | Cluster firewall enabled but individual node has it disabled             |
| Firewall rule allows 0.0.0.0/0      | WC0008 | Firewall    | Warning  | Cluster firewall rule with overly permissive source or destination       |
| root@pam no TFA                     | CC0004 | Access      | Critical | root@pam has no two-factor authentication configured                     |
| Admin user no TFA                   | WC0007 | Access      | Warning  | User with Administrator role has no two-factor authentication            |
| Overly broad permissions            | WC0005 | Access      | Warning  | User has Administrator role at root path `/` — prefer scoped permissions |
| Disabled user with active API token | WC0006 | Access      | Warning  | Disabled user still has valid API tokens that should be revoked          |
| Local user no expiration            | IC0005 | Access      | Info     | Local user has no expiration date configured                             |
| API token no expiration             | IC0006 | Access      | Info     | API token has no expiration date configured                              |

</details>

<details>
<summary><strong>Node Checks</strong></summary>

| Check                              | Code          | SubContext       | Gravity          | Description                                                                    |
| ---------------------------------- | ------------- | ---------------- | ---------------- | ------------------------------------------------------------------------------ |
| Node offline                       | WN0002        | Status           | Warning          | Node is not reachable                                                          |
| PVE version end of life            | WN0003        | EOL              | Warning          | Installed PVE version has reached end of life                                  |
| No active subscription             | WN0004        | Subscription     | Warning          | Node has no active Proxmox VE subscription                                     |
| Nodes version not equal            | CN0001        | Version          | Critical         | Nodes in cluster have different PVE versions                                   |
| Nodes hosts config not equal       | WN0005        | Hosts            | Warning          | `/etc/hosts` content differs between nodes                                     |
| Nodes DNS not equal                | WN0006        | DNS              | Warning          | DNS configuration differs between nodes                                        |
| Nodes timezone not equal           | WN0007        | Timezone         | Warning          | Timezone differs between nodes                                                 |
| Nodes APT repos not equal          | WN0008        | AptRepositories  | Warning          | APT repository sources differ between nodes                                    |
| NIC MTU mismatch                   | WN0009        | Network          | Warning          | Physical NIC MTU differs between nodes                                         |
| NIC not active                     | WN0010        | Network          | Warning          | Physical NIC is down                                                           |
| Package versions not equal         | CN0002        | PackageVersions  | Critical         | Nodes have different package versions installed                                |
| Service not running                | WN0011        | Service          | Warning          | A required system service is not running                                       |
| Certificate expired                | CN0003        | Certificates     | Critical         | TLS certificate has expired                                                    |
| Replication errors                 | CN0004        | Replication      | Critical         | Replication job has errors                                                     |
| Updates available                  | IN0001        | Update           | Info             | Packages available for update                                                  |
| Important updates available        | WN0012        | Update           | Warning          | Security/important packages available for update                               |
| Node requires reboot               | WN0013        | Reboot           | Warning          | Running kernel differs from installed kernel                                   |
| NTP offset too large               | WN0014        | NTP              | Warning          | Node time is out of sync with NTP                                              |
| IOMMU not enabled                  | IN0002        | IOMMU            | Info             | IOMMU disabled — PCI passthrough will not work                                 |
| VM consolidation suggested         | IN0003        | Consolidation    | Info             | Node CPU and RAM utilization both below threshold — consider consolidating VMs |
| CPU level mismatch                 | WN0015        | CPUCompatibility | Warning          | Nodes have different x86-64 feature levels — live migration may fail           |
| Memory overcommit                  | WN0036        | Memory           | Warning          | Sum of VM allocated RAM exceeds physical node RAM                              |
| Bridge not VLAN-aware              | WN0037        | Network          | Warning          | VM/CT uses VLAN tag on a bridge that is not VLAN-aware — tag silently ignored  |
| CPU usage above threshold          | WN0027        | Usage            | Warning/Critical | CPU usage above configured threshold                                           |
| Memory usage above threshold       | WN0038        | Usage            | Warning/Critical | Memory usage above configured threshold                                        |
| Network usage above threshold      | WN0039/WN0040 | Usage            | Warning/Critical | Network throughput above configured threshold                                  |
| PSI CPU pressure high              | WN0031        | Pressure         | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                              |
| PSI I/O pressure high              | WN0032        | Pressure         | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                         |
| PSI memory pressure high           | WN0033        | Pressure         | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                      |
| Health score low                   | WG0032        | HealthScore      | Warning/Critical | Composite health score below threshold                                         |
| Task history errors                | CN0005        | Tasks            | Critical         | Failed tasks found in the last 48 hours                                        |
| Disk SMART problem                 | WN0016        | S.M.A.R.T.       | Warning          | Disk reports a SMART health problem                                            |
| Disk temperature                   | WN0018/CN0006 | S.M.A.R.T.       | Warning/Critical | Disk temperature exceeds configured threshold                                  |
| Disk reallocated sectors           | WN0020        | S.M.A.R.T.       | Warning          | Disk has reallocated sectors — disk may be failing                             |
| Disk pending sectors               | CN0008        | S.M.A.R.T.       | Critical         | Disk has pending sectors — imminent data loss risk                             |
| Disk uncorrectable sectors         | CN0009        | S.M.A.R.T.       | Critical         | Disk has offline uncorrectable sectors                                         |
| Disk UDMA CRC errors               | WN0021        | S.M.A.R.T.       | Warning          | Disk has UDMA CRC errors — check cable/controller                              |
| Disk reported uncorrectable errors | WN0022        | S.M.A.R.T.       | Warning          | Disk has reported uncorrectable errors                                         |
| SSD wearout not valid              | WN0017        | SSD Wearout      | Warning          | SSD does not expose wear data                                                  |
| SSD wearout above threshold        | WN0023/CN0007 | SSD Wearout      | Warning/Critical | SSD wearout consumed above threshold                                           |
| ZFS pool health problem            | CN0010        | Zfs              | Critical         | ZFS pool is not in ONLINE state                                                |
| ZFS vdev degraded/faulted          | CN0012        | Zfs              | Critical         | ZFS pool vdev is in a degraded or faulted state                                |
| ZFS vdev I/O errors                | WN0025        | Zfs              | Warning          | ZFS pool vdev has accumulated read/write/checksum errors                       |
| ZFS pool errors                    | WN0024        | Zfs              | Warning          | ZFS pool reports errors                                                        |
| LVM-thin metadata usage            | WN0026/CN0011 | Storage          | Warning/Critical | LVM-thin metadata usage is high — full metadata causes data corruption         |

</details>

<details>
<summary><strong>Storage Checks</strong></summary>

| Check                                | Code   | SubContext | Gravity          | Description                                                                         |
| ------------------------------------ | ------ | ---------- | ---------------- | ----------------------------------------------------------------------------------- |
| Storage unavailable                  | CS0001 | Status     | Critical         | Storage is not accessible                                                           |
| Storage usage above threshold        | WS0001 | Usage      | Warning/Critical | Storage usage above configured threshold                                            |
| Orphaned backup                      | WS0003 | Backup     | Warning          | Backup file whose VMID no longer exists                                             |
| Orphaned disk image                  | WS0002 | Image      | Warning          | Disk image not attached to any VM/CT                                                |
| Storage overcommitted                | WS0004 | Usage      | Warning          | Allocated disk space exceeds physical capacity (thin provisioning)                  |
| Shared storage not on all nodes      | WS0005 | Shared     | Warning          | Shared storage only mounted on one node                                             |
| No backup storage configured         | WS0006 | Backup     | Warning          | No storage has 'backup' content type — backups cannot be stored                     |
| Backup storage unreachable from node | WS0007 | Backup     | Warning          | Backup job storage not available on a node — VMs on that node will not be backed up |

</details>

<details>
<summary><strong>VM (QEMU) Checks</strong></summary>

| Check                           | Code          | SubContext      | Gravity          | Description                                                                              |
| ------------------------------- | ------------- | --------------- | ---------------- | ---------------------------------------------------------------------------------------- |
| VM state found (vmstate)        | CG0001        | VM State        | Critical         | Hibernated VM state left in pending — VM was suspended and never resumed                 |
| Pending config changes          | IG0010        | Status          | Info             | Config changes pending reboot to take effect                                             |
| VM locked                       | WG0015        | Status          | Warning          | VM is locked and cannot be managed                                                       |
| OsType not set                  | WG0001        | OS              | Warning          | VM OS type is not configured                                                             |
| OS not maintained               | WG0002        | OSNotMaintained | Warning          | Guest OS has reached end of life                                                         |
| QEMU agent not enabled          | WG0003        | Agent           | Warning          | Guest agent not configured                                                               |
| QEMU agent not running in guest | WG0004        | Agent           | Warning          | Agent enabled but not responding inside guest                                            |
| Controller not VirtIO SCSI      | IG0001        | VirtIO          | Info             | SCSI controller is not VirtIO — lower performance                                        |
| Disk not VirtIO                 | IG0002        | VirtIO          | Info             | Disk not using VirtIO bus                                                                |
| Network not VirtIO              | IG0003        | VirtIO          | Info             | Network interface not using VirtIO driver                                                |
| CD-ROM mounted                  | WG0005        | Hardware        | Warning          | CD-ROM drive has an image mounted                                                        |
| CPU type 'host'                 | WG0006        | CPU             | Warning          | CPU type 'host' prevents live migration                                                  |
| CPU type outdated               | IG0004        | CPU             | Info             | CPU type is outdated (kvm64)                                                             |
| CPU security flags missing      | WG0037        | CPU             | Warning          | Non-host CPU type missing +spec-ctrl/+ssbd/+pcid/+md-clear flags                         |
| CPU hotplug on Windows          | WG0007        | CPU             | Warning          | CPU hotplug enabled on Windows guest — not supported                                     |
| CPU type 'host' with HA         | CG0004        | CPU             | Critical         | CPU type 'host' is incompatible with HA — live migration required by HA is impossible    |
| vCPU overcommit                 | WG0036        | CPU             | Warning          | Node vCPU overcommit ratio exceeds configured threshold                                  |
| Balloon driver disabled         | IG0005        | Balloon         | Info             | RAM is statically allocated — no memory ballooning                                       |
| Balloon memory overcommit       | IG0006        | Balloon         | Info             | Balloon has no room to reclaim memory                                                    |
| Disk cache=unsafe               | WG0008        | Hardware        | Warning          | Disk uses cache=unsafe — data loss risk on host crash                                    |
| Disk cache=writeback no backup  | WG0009        | Hardware        | Warning          | Disk uses writeback cache but backup is disabled                                         |
| Windows 11 no UEFI              | WG0010        | SecureBoot      | Warning          | Windows 11 requires UEFI (bios=ovmf)                                                     |
| Windows 11 no TPM 2.0           | WG0011        | SecureBoot      | Warning          | Windows 11 requires TPM 2.0                                                              |
| RNG device configured           | IG0007        | Hardware        | Info             | VM has virtio-rng device — verify this is intentional                                    |
| Serial console configured       | IG0008        | Hardware        | Info             | VM has serial console configured — verify this is intentional                            |
| Machine type not set            | IG0012        | Hardware        | Info             | Machine type not configured — QEMU will use default which may change across PVE upgrades |
| USB/PCI passthrough configured  | WG0012        | Hardware        | Warning          | Passthrough configured — live migration and HA not possible                              |
| Disk on local storage with HA   | CG0005        | HA              | Critical         | Disk is on non-shared storage but VM is managed by HA — live migration will fail         |
| No network interface            | WG0034        | Network         | Warning          | VM has no network interface — completely isolated from network                           |
| Duplicate MAC address           | WG0033        | Network         | Warning          | MAC address shared with another VM — causes network conflicts                            |
| VM firewall disabled            | WG0013        | Firewall        | Warning          | VM firewall is disabled — exposed to all bridge traffic                                  |
| VM IP filter disabled           | IG0009        | Firewall        | Info             | VM can spoof source IP addresses                                                         |
| Start on boot not enabled       | WG0016        | StartOnBoot     | Warning          | VM will not start automatically after host reboot                                        |
| Protection not enabled          | IG0011        | Protection      | Info             | VM protection flag not set                                                               |
| No backup configured            | WG0017        | Backup          | Warning          | VM not included in any backup job                                                        |
| Disk excluded from backup       | CG0002        | Backup          | Critical         | A disk has backup disabled                                                               |
| Unused disk                     | WG0018        | Hardware        | Warning          | Disk detached from VM but still in storage                                               |
| Old backups found               | WG0019        | Backup          | Warning          | Backup files older than configured days found                                            |
| No recent backups               | WG0020        | Backup          | Warning          | No backup found in the last configured days                                              |
| Task history errors             | CG0003        | Tasks           | Critical         | Failed tasks found in the last 48 hours                                                  |
| AutoSnapshot not configured     | WG0021        | AutoSnapshot    | Warning          | cv4pve-autosnap not configured                                                           |
| Old AutoSnap version            | WG0022        | AutoSnapshot    | Warning          | Old AutoSnap snapshots present — update required                                         |
| Snapshots too old               | WG0024        | SnapshotOld     | Warning          | Snapshots older than configured age                                                      |
| Snapshot with RAM state         | WG0035        | Snapshot        | Warning          | Snapshot includes RAM state — wastes disk space and blocks storage migration             |
| Snapshots exceed max count      | WG0023        | SnapshotCount   | Warning          | Snapshot count exceeds configured limit                                                  |
| CPU usage above threshold       | WG0025        | Usage           | Warning/Critical | CPU usage above configured threshold                                                     |
| Memory usage above threshold    | WG0026        | Usage           | Warning/Critical | Memory usage above configured threshold                                                  |
| Network usage above threshold   | WG0027/WG0028 | Usage           | Warning/Critical | Network throughput above configured threshold                                            |
| PSI CPU pressure high           | WG0029        | Pressure        | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                                        |
| PSI I/O pressure high           | WG0030        | Pressure        | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                                   |
| PSI memory pressure high        | WG0031        | Pressure        | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                                |
| Health score low                | WG0032        | HealthScore     | Warning/Critical | Composite health score below threshold                                                   |
| Template with agent enabled     | WG0014        | Agent           | Warning          | Template has QEMU agent enabled — unused on templates                                    |

</details>

<details>
<summary><strong>LXC (Container) Checks</strong></summary>

| Check                       | Code   | SubContext    | Gravity          | Description                                                                  |
| --------------------------- | ------ | ------------- | ---------------- | ---------------------------------------------------------------------------- |
| Pending config changes      | IG0010 | Status        | Info             | Config changes pending reboot to take effect                                 |
| CT locked                   | WG0015 | Status        | Warning          | Container is locked and cannot be managed                                    |
| Privileged container        | WG0039 | Security      | Warning          | Container runs as privileged — root inside has host-level access             |
| Privileged without AppArmor | CG0006 | Security      | Critical         | Privileged container has AppArmor disabled — no kernel confinement           |
| Nesting without keyctl      | WG0038 | Features      | Warning          | `nesting=1` set but `keyctl=1` missing — keyring isolation incomplete        |
| Raw LXC config              | WG0041 | Config        | Warning          | Container has raw LXC config entries that bypass PVE abstractions            |
| No memory limit             | WG0040 | Memory        | Warning          | Container has no memory limit (Memory=0) — can consume all host RAM          |
| Swap = 0                    | IG0013 | Config        | Info             | Container has swap disabled — OOM killer risk under memory pressure          |
| No hostname                 | IG0014 | Config        | Info             | Container has no hostname configured                                         |
| CT firewall disabled        | WG0013 | Firewall      | Warning          | Container firewall is disabled — exposed to all bridge traffic               |
| CT IP filter disabled       | IG0009 | Firewall      | Info             | Container can spoof source IP addresses                                      |
| Start on boot not enabled   | WG0016 | StartOnBoot   | Warning          | CT will not start automatically after host reboot                            |
| Protection not enabled      | IG0011 | Protection    | Info             | CT protection flag not set                                                   |
| No backup configured        | WG0017 | Backup        | Warning          | CT not included in any backup job                                            |
| Disk excluded from backup   | CG0002 | Backup        | Critical         | A disk has backup disabled                                                   |
| Unused disk                 | WG0018 | Hardware      | Warning          | Disk detached from CT but still in storage                                   |
| Old backups found           | WG0019 | Backup        | Warning          | Backup files older than configured days found                                |
| No recent backups           | WG0020 | Backup        | Warning          | No backup found in the last configured days                                  |
| Task history errors         | CG0003 | Tasks         | Critical         | Failed tasks found in the last 48 hours                                      |
| AutoSnapshot not configured | WG0021 | AutoSnapshot  | Warning          | cv4pve-autosnap not configured                                               |
| Old AutoSnap version        | WG0022 | AutoSnapshot  | Warning          | Old AutoSnap snapshots present — update required                             |
| Snapshots too old           | WG0024 | SnapshotOld   | Warning          | Snapshots older than configured age                                          |
| Snapshot with RAM state     | WG0035 | Snapshot      | Warning          | Snapshot includes RAM state — wastes disk space and blocks storage migration |
| Snapshots exceed max count  | WG0023 | SnapshotCount | Warning          | Snapshot count exceeds configured limit                                      |
| PSI CPU pressure high       | WG0029 | Pressure      | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                            |
| PSI I/O pressure high       | WG0030 | Pressure      | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                       |
| PSI memory pressure high    | WG0031 | Pressure      | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                    |
| Health score low            | WG0032 | HealthScore   | Warning/Critical | Composite health score below threshold                                       |

</details>

> All checks can be suppressed via ignore rules. Use the `Code` field to target specific checks precisely.

---

## Settings Reference

```bash
# Generate default settings file
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid create-settings

# Run with custom settings
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid --settings-file=settings.json execute
```

<details>
<summary><strong>Full settings.json with all defaults</strong></summary>

```jsonc
{
  "Storage": {
    "Rrd": {
      "TimeFrame": "Day", // Hour, Day, Week, Month, Year
      "Consolidation": "Average", // Average, Maximum
    },
    "Threshold": {
      "Warning": 70, // storage usage % warning threshold
      "Critical": 85, // storage usage % critical threshold
    },
  },
  "Node": {
    "Smart": {
      "Enabled": false, // enable per-disk SMART checks
      "Temperature": { "Warning": 55, "Critical": 65 }, // disk temperature °C
      "SsdWearout": { "Warning": 70, "Critical": 85 }, // SSD wearout % consumed
    },
    "NodeStorage": {
      "ZfsDetail": false, // enable per-vdev ZFS error checks
      "LvmThinMetadata": true, // enable LVM-thin metadata usage check
    },
    "MaxVCpuRatio": 4.0, // max vCPU/physical-CPU ratio before warning
    "ConsolidationCpuThreshold": 10.0, // CPU % below which node is flagged as consolidation candidate
    "ConsolidationMemThreshold": 20.0, // RAM % below which node is flagged as consolidation candidate
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu": { "Warning": 40, "Critical": 70 }, // PSI CPU some pressure %
        "IoFull": { "Warning": 10, "Critical": 30 }, // PSI I/O full pressure %
        "MemoryFull": { "Warning": 5, "Critical": 15 }, // PSI memory full pressure %
      },
    },
    "Cpu": { "Warning": 70, "Critical": 85 },
    "Memory": { "Warning": 70, "Critical": 85 },
    "Network": { "Warning": 0, "Critical": 0 }, // bytes/s, 0 = disabled
    "HealthScore": { "Warning": 70, "Critical": 50 },
  },
  "Qemu": {
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu": { "Warning": 50, "Critical": 80 },
        "IoFull": { "Warning": 20, "Critical": 50 },
        "MemoryFull": { "Warning": 10, "Critical": 30 },
      },
    },
    "Cpu": { "Warning": 70, "Critical": 85 },
    "Memory": { "Warning": 70, "Critical": 85 },
    "Network": { "Warning": 0, "Critical": 0 },
    "HealthScore": { "Warning": 60, "Critical": 40 },
  },
  "Lxc": {
    // same structure as Qemu
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu": { "Warning": 50, "Critical": 80 },
        "IoFull": { "Warning": 20, "Critical": 50 },
        "MemoryFull": { "Warning": 10, "Critical": 30 },
      },
    },
    "Cpu": { "Warning": 70, "Critical": 85 },
    "Memory": { "Warning": 70, "Critical": 85 },
    "Network": { "Warning": 0, "Critical": 0 },
    "HealthScore": { "Warning": 60, "Critical": 40 },
  },
  "Snapshot": {
    "Enabled": true,
    "MaxCount": 10, // max snapshots per VM/CT, 0 = disabled
    "MaxAgeDays": 30, // warn if snapshot older than N days, 0 = disabled
  },
  "Backup": {
    "Enabled": true,
    "MaxAgeDays": 60, // warn if backup older than N days, 0 = disabled
    "RecentDays": 7, // warn if no backup in last N days, 0 = disabled
  },
}
```

#### Health Score Formula

```
Node  score = 100 - (cpu% × 0.4 + ram% × 0.4 + disk% × 0.2)
VM/CT score = 100 - (cpu% × 0.5 + ram% × 0.5)
```

Set `Warning` and `Critical` to `0` to disable health score checks entirely.

> **PSI Pressure** (PVE 9.0+): Linux Pressure Stall Information metrics. Checks are automatically skipped on older PVE versions where the values are always zero.

</details>

---

## Ignore Rules

Suppress known or accepted issues using regex patterns:

```bash
# Generate ignored issues template
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid create-ignored-issues

# Run with ignored issues
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --ignored-issues-file=ignored-issues.json execute

# Show ignored issues in a separate table
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --ignored-issues-file=ignored-issues.json --ignored-issues-show execute
```

<details>
<summary><strong>ignored-issues.json format</strong></summary>

```json
[
  {
    "ErrorCode": "IG0011"
  },
  {
    "Id": "nodes/pve01/qemu/105",
    "SubContext": "Protection"
  },
  {
    "Id": "nodes/pve01/.*",
    "Context": "Qemu"
  }
]
```

**Fields (all support regex):**

| Field         | Description               | Example            |
| ------------- | ------------------------- | ------------------ |
| `ErrorCode`   | Match by check code       | `"IG0011"`         |
| `Id`          | Match by resource URL     | `"nodes/pve01/.*"` |
| `SubContext`  | Match by sub-context      | `"Protection"`     |
| `Description` | Match by description text | `".*test.*"`       |
| `Context`     | Filter by context type    | `"Qemu"`           |
| `Gravity`     | Filter by severity        | `"Info"`           |

All fields are optional — only specified fields are matched.

</details>

---

## Resources

[![cv4pve-diag Tutorial](http://img.youtube.com/vi/hn1nw9KXlsg/maxresdefault.jpg)](https://www.youtube.com/watch?v=hn1nw9KXlsg)

**Web GUI version:** [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin)

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
