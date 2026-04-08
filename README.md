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

---

## Quick Start

```bash
wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-x64.zip
unzip cv4pve-diag-linux-x64.zip
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute
```

---

## Features

- **Self-contained binary** — no runtime to install, copy and run
- **Cross-platform** — Windows, Linux, macOS
- **API-based** — no root or SSH access required
- **Cluster-aware** — analyzes all nodes, VMs, CTs and storages in one run
- **High availability** — multiple host support for automatic failover
- **Output formats** — Text, HTML, JSON, Markdown, Excel
- **Severity levels** — Critical, Warning, Info
- **Configurable thresholds** — via `settings.json`
- **Ignore rules** — suppress known/accepted issues per VM or check
- **API token** support (Proxmox VE 6.2+)

---

## Installation

| Platform | Command                                                                                                                                                                         |
|----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Linux** | `wget .../cv4pve-diag-linux-x64.zip && unzip cv4pve-diag-linux-x64.zip && chmod +x cv4pve-diag`                                                                                 |
| **Windows WinGet** | `winget install Corsinvest.cv4pve.diag`                                                                                                                                         |
| **Windows manual** | Download `cv4pve-diag-win-x64.zip` from [Releases](https://github.com/Corsinvest/cv4pve-diag/releases)                                                                          |
| **Arch Linux** | `yay -S cv4pve-diag`                                                                                                                                                            |
| **Debian/Ubuntu** | `sudo dpkg -i cv4pve-diag-VERSION-ARCH.deb`                                                                                                                                     |
| **RHEL/Fedora** | `sudo rpm -i cv4pve-diag-VERSION-ARCH.rpm`                                                                                                                                      |
| **macOS** | Homebrew<br/>`brew tap Corsinvest/tap`<br/>`brew install cv4pe-diag`<br/>Manual<br/>`wget .../cv4pve-diag-osx-x64.zip && unzip cv4pve-diag-osx-x64.zip && chmod +x cv4pve-diag` |

All binaries on the [Releases page](https://github.com/Corsinvest/cv4pve-diag/releases).

---

## Configuration

### Authentication Methods

#### **Username/Password**

```bash
cv4pve-diag --host=192.168.1.100 --username=root@pam --password=your_password execute
```

#### **API Token (Recommended)**

```bash
cv4pve-diag --host=192.168.1.100 --api-token=diagnostic@pve!token1=uuid-here execute
```

#### **Password from File**

```bash
# Store password in file
cv4pve-diag --host=192.168.1.100 --username=root@pam --password=file:/etc/cv4pve/password execute

# First run: prompts for password and saves to file
# Subsequent runs: reads password from file automatically
```

---

## Usage Examples

### Basic Diagnostic Operations

<details>
<summary><strong>Execute Diagnostics</strong></summary>

#### Simple Execution

```bash
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret execute
```

#### With Output Format

```bash
# HTML output
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --output=Html execute

# JSON output for automation
cv4pve-diag --host=pve.domain.com --api-token=diag@pve!token=uuid --output=Json execute

# Markdown format
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --output=Markdown execute

# Excel output (saves to file)
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --output=Excel execute

# Excel output with custom file name
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --output=Excel --output-file=report.xlsx execute
```

</details>

<details>
<summary><strong>Settings Management</strong></summary>

#### Create Settings File

```bash
# Generate default settings file
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret create-settings

# This creates settings.json with customizable diagnostic rules
```

#### Use Custom Settings

```bash
# Run diagnostics with custom settings
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --settings-file=settings.json execute
```

</details>

<details>
<summary><strong>Ignore Issues</strong></summary>

#### Create Ignored Issues File

```bash
# Generate ignored issues template
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret create-ignored-issues

# This creates ignored-issues.json
```

#### Use Ignored Issues

```bash
# Run diagnostics with ignored issues
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --ignored-issues-file=ignored-issues.json execute

# Show ignored issues in separate table
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret --ignored-issues-file=ignored-issues.json --ignored-issues-show execute
```

</details>

### Configuration Files

<details>
<summary><strong>Parameter Files for Complex Setups</strong></summary>

#### Create Parameter File

```bash
# /etc/cv4pve/production.conf
--host=pve-cluster.company.com
--api-token=diagnostic@pve!production=uuid-here
--settings-file=/etc/cv4pve/settings.json
--ignored-issues-file=/etc/cv4pve/ignored-issues.json
--validate-certificate
```

#### Execute with Parameter File

```bash
cv4pve-diag @/etc/cv4pve/production.conf execute --output=Html
```

</details>

---

## Security & Permissions

### Required Permissions

| Permission          | Purpose                 | Scope            |
| ------------------- | ----------------------- | ---------------- |
| **VM.Audit**        | Read VM/CT information  | Virtual machines |
| **Datastore.Audit** | Check storage capacity  | Storage systems  |
| **Pool.Audit**      | Access pool information | Resource pools   |
| **Sys.Audit**       | Node system information | Cluster nodes    |

### API Token Setup (Recommended)

<details>
<summary><strong>Creating API Tokens</strong></summary>

#### 1. Generate API Token with Proper Permissions

```bash
# Follow Proxmox VE documentation for:
# - API token creation with proper privilege separation
# - Permission assignment for required roles
# - Required permissions: VM.Audit, Datastore.Audit, Pool.Audit, Sys.Audit
# Refer to official Proxmox VE API documentation for detailed steps
```

#### 2. Use Token in Commands

```bash
cv4pve-diag --host=pve.local --api-token=diagnostic@pve!diag-token=uuid-from-creation execute
```

</details>

---

## Diagnostic Checks

<details>
<summary><strong>Cluster Checks</strong></summary>

### Cluster Checks

| Check                               | Code   | SubContext  | Gravity  | Description                                                                        |
| ----------------------------------- | ------ | ----------- | -------- | ---------------------------------------------------------------------------------- |
| No backup job configured            | WC0001 | Backup      | Warning  | No automated backup job for any VM/CT                                              |
| Backup job no compression           | IC0001 | Backup      | Info     | Backup job has no compression configured                                           |
| Backup job no retention             | WC0002 | Backup      | Warning  | Backup job has no maxfiles/prune policy — storage will fill up                     |
| No quorum                           | CC0001 | Quorum      | Critical | Cluster has lost quorum — VM operations may be blocked                             |
| Corosync expected_votes mismatch    | CC0002 | Quorum      | Critical | Corosync expected votes does not match online node count                           |
| HA group with offline nodes         | CC0003 | HA          | Critical | HA group references nodes that are currently offline                               |
| No HA resources configured          | IC0002 | HA          | Info     | No VMs/CTs protected by HA — no automatic failover on node failure                 |
| No replication jobs configured      | IC0003 | Replication | Info     | No storage replication configured — no redundant copy across nodes                 |
| Pool empty                          | IC0004 | Pool        | Info     | Resource pool exists but has no VMs or storage assigned                            |
| Cluster firewall disabled           | WC0003 | Firewall    | Warning  | Cluster-level firewall is completely disabled                                      |
| Firewall inbound policy not DROP    | WC0004 | Firewall    | Warning  | Inbound policy allows traffic not matched by rules                                 |
| Firewall outbound policy not DROP   | WC0004 | Firewall    | Warning  | Outbound policy allows traffic not matched by rules                                |
| Node firewall disabled              | WN0001 | Firewall    | Warning  | Cluster firewall enabled but individual node has it disabled                       |
| root@pam no TFA                     | CC0004 | Access      | Critical | root@pam has no two-factor authentication configured                               |
| Admin user no TFA                   | WC0007 | Access      | Warning  | User with Administrator role has no two-factor authentication                      |
| Overly broad permissions            | WC0005 | Access      | Warning  | User has Administrator role at root path `/` — prefer pool/node-scoped permissions |
| Disabled user with active API token | WC0006 | Access      | Warning  | Disabled user still has valid API tokens that should be revoked                    |
| Local user no expiration            | IC0005 | Access      | Info     | Local user has no expiration date configured                                       |
| API token no expiration             | IC0006 | Access      | Info     | API token has no expiration date configured                                        |

</details>

<details>
<summary><strong>Node Checks</strong></summary>

### Node Checks

| Check                              | Code          | SubContext       | Gravity          | Description                                                            |
| ---------------------------------- | ------------- | ---------------- | ---------------- | ---------------------------------------------------------------------- |
| Node offline                       | WN0002        | Status           | Warning          | Node is not reachable                                                  |
| PVE version end of life            | WN0003        | EOL              | Warning          | Installed PVE version has reached end of life                          |
| No active subscription             | WN0004        | Subscription     | Warning          | Node has no active Proxmox VE subscription                             |
| Nodes version not equal            | CN0001        | Version          | Critical         | Nodes in cluster have different PVE versions                           |
| Nodes hosts config not equal       | WN0005        | Hosts            | Warning          | `/etc/hosts` content differs between nodes                             |
| Nodes DNS not equal                | WN0006        | DNS              | Warning          | DNS configuration differs between nodes                                |
| Nodes timezone not equal           | WN0007        | Timezone         | Warning          | Timezone differs between nodes                                         |
| Nodes APT repos not equal          | WN0008        | AptRepositories  | Warning          | APT repository sources differ between nodes                            |
| NIC MTU mismatch                   | WN0009        | Network          | Warning          | Physical NIC MTU differs between nodes                                 |
| NIC not active                     | WN0010        | Network          | Warning          | Physical NIC is down                                                   |
| Package versions not equal         | CN0002        | PackageVersions  | Critical         | Nodes have different package versions installed                        |
| Service not running                | WN0011        | Service          | Warning          | A required system service is not running                               |
| Certificate expired                | CN0003        | Certificates     | Critical         | TLS certificate has expired                                            |
| Replication errors                 | CN0004        | Replication      | Critical         | Replication job has errors                                             |
| Updates available                  | IN0001        | Update           | Info             | Packages available for update                                          |
| Important updates available        | WN0012        | Update           | Warning          | Security/important packages available for update                       |
| Node requires reboot               | WN0013        | Reboot           | Warning          | Running kernel differs from installed kernel                           |
| NTP offset too large               | WN0014        | NTP              | Warning          | Node time is out of sync with NTP                                      |
| IOMMU not enabled                  | IN0002        | IOMMU            | Info             | IOMMU disabled — PCI passthrough will not work                         |
| CPU level mismatch                 | WN0015        | CPUCompatibility | Warning          | Nodes have different x86-64 feature levels — live migration may fail   |
| CPU usage above threshold          | WN0027        | Usage            | Warning/Critical | CPU usage above configured threshold                                   |
| Memory usage above threshold       | WN0027        | Usage            | Warning/Critical | Memory usage above configured threshold                                |
| IOWait above threshold             | WN0028        | Usage            | Warning/Critical | CPU IOWait above configured threshold                                  |
| Disk usage above threshold         | WN0029        | Usage            | Warning/Critical | Node root disk usage above configured threshold                        |
| SWAP usage above threshold         | WN0030        | Usage            | Warning/Critical | SWAP usage above configured threshold                                  |
| PSI CPU pressure high              | WN0031        | Pressure         | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                      |
| PSI I/O pressure high              | WN0032        | Pressure         | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                 |
| PSI memory pressure high           | WN0033        | Pressure         | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)              |
| Health score low                   | WQ0032        | HealthScore      | Warning/Critical | Composite health score below threshold                                 |
| Task history errors                | CN0005        | Tasks            | Critical         | Failed tasks found in the last 48 hours                                |
| Disk SMART problem                 | WN0016        | S.M.A.R.T.       | Warning          | Disk reports a SMART health problem                                    |
| Disk temperature                   | WN0018/CN0006 | S.M.A.R.T.       | Warning/Critical | Disk temperature exceeds configured threshold                          |
| Disk reallocated sectors           | WN0020        | S.M.A.R.T.       | Warning          | Disk has reallocated sectors — disk may be failing                     |
| Disk pending sectors               | CN0008        | S.M.A.R.T.       | Critical         | Disk has pending sectors — imminent data loss risk                     |
| Disk uncorrectable sectors         | CN0009        | S.M.A.R.T.       | Critical         | Disk has offline uncorrectable sectors                                 |
| Disk UDMA CRC errors               | WN0021        | S.M.A.R.T.       | Warning          | Disk has UDMA CRC errors — check cable/controller                      |
| Disk reported uncorrectable errors | WN0022        | S.M.A.R.T.       | Warning          | Disk has reported uncorrectable errors                                 |
| SSD wearout not valid              | WN0017        | SSD Wearout      | Warning          | SSD does not expose wear data                                          |
| SSD wearout above threshold        | WN0023/CN0007 | SSD Wearout      | Warning/Critical | SSD wearout consumed above threshold                                   |
| ZFS pool health problem            | CN0010        | Zfs              | Critical         | ZFS pool is not in ONLINE state                                        |
| ZFS vdev degraded/faulted          | CN0012        | Zfs              | Critical         | ZFS pool vdev is in a degraded or faulted state                        |
| ZFS vdev I/O errors                | WN0025        | Zfs              | Warning          | ZFS pool vdev has accumulated read/write/checksum errors               |
| ZFS pool errors                    | WN0024        | Zfs              | Warning          | ZFS pool reports errors                                                |
| LVM-thin metadata usage            | WN0026/CN0011 | Storage          | Warning/Critical | LVM-thin metadata usage is high — full metadata causes data corruption |

</details>

<details>
<summary><strong>Storage Checks</strong></summary>

### Storage Checks

| Check                                | Code   | SubContext | Gravity          | Description                                                                         |
| ------------------------------------ | ------ | ---------- | ---------------- | ----------------------------------------------------------------------------------- |
| Storage unavailable                  | CS0001 | Status     | Critical         | Storage is not accessible                                                           |
| Storage usage above threshold        | WS0001 | Usage      | Warning/Critical | Storage usage above configured threshold                                            |
| Orphaned backup                      | WS0003 | Backup     | Warning          | Backup file whose VMID no longer exists                                             |
| Orphaned disk image                  | WS0002 | Image      | Warning          | Disk image not attached to any VM/CT                                                |
| Storage overcommitted                | WS0004 | Usage      | Warning/Critical | Allocated disk space exceeds physical capacity                                      |
| Shared storage not on all nodes      | WS0005 | Status     | Warning          | Shared storage only mounted on one node                                             |
| No backup storage configured         | WS0006 | Backup     | Warning          | No storage has 'backup' content type — backups cannot be stored                     |
| Backup storage unreachable from node | WS0007 | Backup     | Warning          | Backup job storage not available on a node — VMs on that node will not be backed up |

</details>

<details>
<summary><strong>VM (QEMU) Checks</strong></summary>

### VM (QEMU) Checks

| Check                           | Code          | SubContext      | Gravity          | Description                                                                              |
| ------------------------------- | ------------- | --------------- | ---------------- | ---------------------------------------------------------------------------------------- |
| VM state found (vmstate)        | CQ0001        | VM State        | Critical         | Hibernated VM state left in pending — VM was suspended and never resumed                 |
| VM state in snapshot            | WQ0023        | Snapshot        | Warning          | Snapshot includes RAM state — increases size and restore time significantly              |
| Pending config changes          | IQ0010        | Pending         | Info             | Config changes pending reboot to take effect                                             |
| VM locked                       | WQ0015        | Status          | Warning          | VM is locked and cannot be managed                                                       |
| OsType not set                  | WQ0001        | OS              | Warning          | VM OS type is not configured                                                             |
| OS not maintained               | WQ0002        | OSNotMaintained | Warning          | Guest OS has reached end of life                                                         |
| QEMU agent not enabled          | WQ0003        | Agent           | Warning          | Guest agent not configured                                                               |
| QEMU agent not running in guest | WQ0004        | Agent           | Warning          | Agent enabled but not responding inside guest                                            |
| Controller not VirtIO SCSI      | IQ0001        | VirtIO          | Info             | SCSI controller is not VirtIO — lower performance                                        |
| Disk not VirtIO                 | IQ0002        | VirtIO          | Info             | Disk not using VirtIO bus                                                                |
| Network not VirtIO              | IQ0003        | VirtIO          | Info             | Network interface not using VirtIO driver                                                |
| CD-ROM mounted                  | WQ0005        | Hardware        | Warning          | CD-ROM drive has an image mounted                                                        |
| CPU type 'host'                 | WQ0006        | CPU             | Warning          | CPU type 'host' prevents live migration                                                  |
| CPU type outdated               | IQ0004        | CPU             | Info             | CPU type is outdated (kvm64)                                                             |
| CPU hotplug on Windows          | WQ0007        | CPU             | Warning          | CPU hotplug enabled on Windows guest — not supported                                     |
| Balloon driver disabled         | IQ0005        | Balloon         | Info             | RAM is statically allocated — no memory ballooning                                       |
| Disk cache=unsafe               | WQ0008        | Hardware        | Warning          | Disk uses cache=unsafe — data loss risk on host crash                                    |
| Disk cache=writeback no backup  | WQ0009        | Hardware        | Warning          | Disk uses writeback cache but backup is disabled                                         |
| Windows 11 no UEFI              | WQ0010        | SecureBoot      | Warning          | Windows 11 requires UEFI (bios=ovmf)                                                     |
| Windows 11 no TPM 2.0           | WQ0011        | SecureBoot      | Warning          | Windows 11 requires TPM 2.0                                                              |
| Balloon memory overcommit       | IQ0006        | Balloon         | Info             | Balloon has no room to reclaim memory                                                    |
| RNG device configured           | IQ0007        | Hardware        | Info             | VM has virtio-rng device — verify this is intentional                                    |
| Serial console configured       | IQ0008        | Hardware        | Info             | VM has serial console configured — verify this is intentional                            |
| USB/PCI passthrough configured  | WQ0012        | Hardware        | Warning          | Passthrough configured — live migration and HA not possible                              |
| Template with agent enabled     | WQ0014        | Agent           | Warning          | Template has QEMU agent enabled — unused on templates                                    |
| VM firewall disabled            | WQ0013        | Firewall        | Warning          | VM firewall is disabled — exposed to all bridge traffic                                  |
| VM IP filter disabled           | IQ0009        | Firewall        | Info             | VM can spoof source IP addresses                                                         |
| Start on boot not enabled       | WQ0016        | StartOnBoot     | Warning          | VM will not start automatically after host reboot                                        |
| Protection not enabled          | IQ0011        | Protection      | Info             | VM protection flag not set                                                               |
| No backup configured            | WQ0017        | Backup          | Warning          | VM not included in any backup job                                                        |
| Disk excluded from backup       | CQ0002        | Backup          | Critical         | A disk has backup disabled                                                               |
| Unused disk                     | WQ0018        | Hardware        | Warning          | Disk detached from VM but still in storage                                               |
| Old backups found               | WQ0019        | Backup          | Warning          | Backup files older than configured days found                                            |
| No recent backups               | WQ0020        | Backup          | Warning          | No backup found in the last configured days                                              |
| Task history errors             | CQ0003        | Tasks           | Critical         | Failed tasks found in the last 48 hours                                                  |
| AutoSnapshot not configured     | WQ0021        | AutoSnapshot    | Warning          | cv4pve-autosnap not configured                                                           |
| Old AutoSnap version            | WQ0022        | AutoSnapshot    | Warning          | Old AutoSnap snapshots present — update required                                         |
| Snapshots too old               | WQ0024        | SnapshotOld     | Warning          | Snapshots older than configured age                                                      |
| Snapshots exceed max count      | WQ0023        | SnapshotCount   | Warning          | Snapshot count exceeds configured limit                                                  |
| CPU usage above threshold       | WQ0025        | Usage           | Warning/Critical | CPU usage above configured threshold                                                     |
| Memory usage above threshold    | WQ0026        | Usage           | Warning/Critical | Memory usage above configured threshold                                                  |
| Network usage above threshold   | WQ0027/WQ0028 | Usage           | Warning/Critical | Network throughput above configured threshold                                            |
| PSI CPU pressure high           | WQ0029        | Pressure        | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                                        |
| PSI I/O pressure high           | WQ0030        | Pressure        | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                                   |
| PSI memory pressure high        | WQ0031        | Pressure        | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)                                |
| Health score low                | WQ0032        | HealthScore     | Warning/Critical | Composite health score below threshold                                                   |
| CPU type 'host' with HA         | CQ0004        | CPU             | Critical         | CPU type 'host' is incompatible with HA — live migration required by HA is impossible    |
| Disk on local storage with HA   | CQ0005        | HA              | Critical         | Disk is on non-shared storage but VM is managed by HA — live migration will fail         |
| vCPU overcommit                 | WQ0036        | CPU             | Warning          | Node vCPU overcommit ratio exceeds configured threshold                                  |
| Machine type not set            | IQ0012        | Hardware        | Info             | Machine type not configured — QEMU will use default which may change across PVE upgrades |
| No network interface            | WQ0034        | Network         | Warning          | VM has no network interface — completely isolated from network                           |
| Duplicate MAC address           | WQ0033        | Network         | Warning          | MAC address shared with another VM — causes network conflicts                            |
| Snapshot with RAM state         | WQ0035        | Snapshot        | Warning          | Snapshot includes RAM state — wastes disk space and blocks storage migration             |

</details>

<details>
<summary><strong>LXC (Container) Checks</strong></summary>

### LXC (Container) Checks

| Check                       | Code   | SubContext    | Gravity          | Description                                                           |
| --------------------------- | ------ | ------------- | ---------------- | --------------------------------------------------------------------- |
| Pending config changes      | IQ0010 | Pending       | Info             | Config changes pending reboot to take effect                          |
| CT locked                   | WQ0015 | Status        | Warning          | Container is locked and cannot be managed                             |
| CT firewall disabled        | WQ0013 | Firewall      | Warning          | Container firewall is disabled — exposed to all bridge traffic        |
| CT IP filter disabled       | IQ0009 | Firewall      | Info             | Container can spoof source IP addresses                               |
| Privileged container        | WL0020 | Security      | Warning          | Container runs as privileged — root inside has host-level access      |
| Privileged without AppArmor | CL0004 | Security      | Critical         | Privileged container has AppArmor disabled — no kernel confinement    |
| Nesting without keyctl      | WL0019 | Features      | Warning          | `nesting=1` set but `keyctl=1` missing — keyring isolation incomplete |
| Raw LXC config              | WL0021 | Config        | Warning          | Container has raw LXC config entries that bypass PVE abstractions     |
| Swap = 0                    | IL0003 | Config        | Info             | Container has swap disabled — OOM killer risk under memory pressure   |
| No hostname                 | IL0004 | Config        | Info             | Container has no hostname configured                                  |
| Start on boot not enabled   | WQ0016 | StartOnBoot   | Warning          | CT will not start automatically after host reboot                     |
| Protection not enabled      | IQ0011 | Protection    | Info             | CT protection flag not set                                            |
| No backup configured        | WQ0017 | Backup        | Warning          | CT not included in any backup job                                     |
| Disk excluded from backup   | CQ0002 | Backup        | Critical         | A disk has backup disabled                                            |
| Unused disk                 | WQ0018 | Hardware      | Warning          | Disk detached from CT but still in storage                            |
| Old backups found           | WQ0019 | Backup        | Warning          | Backup files older than configured days found                         |
| No recent backups           | WQ0020 | Backup        | Warning          | No backup found in the last configured days                           |
| Task history errors         | CQ0003 | Tasks         | Critical         | Failed tasks found in the last 48 hours                               |
| AutoSnapshot not configured | WQ0021 | AutoSnapshot  | Warning          | cv4pve-autosnap not configured                                        |
| Old AutoSnap version        | WQ0022 | AutoSnapshot  | Warning          | Old AutoSnap snapshots present — update required                      |
| Snapshots too old           | WQ0024 | SnapshotOld   | Warning          | Snapshots older than configured age                                   |
| Snapshots exceed max count  | WQ0023 | SnapshotCount | Warning          | Snapshot count exceeds configured limit                               |
| PSI CPU pressure high       | WQ0029 | Pressure      | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+)                     |
| PSI I/O pressure high       | WQ0030 | Pressure      | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+)                |
| PSI memory pressure high    | WQ0031 | Pressure      | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+)             |
| Health score low            | WQ0032 | HealthScore   | Warning/Critical | Composite health score below threshold                                |
| No memory limit             | WL0022 | Memory        | Warning          | Container has no memory limit (Memory=0) — can consume all host RAM   |

</details>

> **Note:** All checks can be suppressed via ignore rules. Use the `Code` field to target specific checks precisely.

### Example Output

```txt
+-----------------------------+--------------------------------------------------------------------+---------+-----------------+----------+
| Id                          | Description                                                        | Context | SubContext      | Gravity  |
+-----------------------------+--------------------------------------------------------------------+---------+-----------------+----------+
| nodes/pve02                 | Nodes package version not equal                                    | Node    | PackageVersions | Critical |
| nodes/pve02/qemu/203        | Disk 'scsi0' disabled for backup                                   | Qemu    | Backup          | Critical |
| nodes/pve01/lxc/100         | Disk 'rootfs' disabled for backup                                  | Lxc     | Backup          | Critical |
| nodes/pve01/qemu/1030       | Memory (rrd Day AVERAGE) usage 92.9% - 5.99 GB of 6.44 GB          | Qemu    | Usage           | Critical |
| nodes/pve02                 | Nodes hosts configuration not equal                                | Node    | Hosts           | Warning  |
| nodes/pve01/storage/local   | Image Orphaned 51.54 GB file vm-106-disk-1                         | Storage | Image           | Warning  |
| nodes/pve01/storage/pbs01   | Storage usage 75% - 2.42 TB of 3.22 TB                             | Storage | Usage           | Warning  |
| nodes/pve02/qemu/106        | Qemu Agent not enabled                                             | Qemu    | Agent           | Warning  |
| nodes/pve02/qemu/999        | vzdump backup not configured                                       | Qemu    | Backup          | Warning  |
| nodes/pve02/lxc/101         | 'cv4pve-autosnap' not configured                                   | Lxc     | AutoSnapshot    | Warning  |
| nodes/pve01/qemu/1030       | Cdrom mounted                                                      | Qemu    | Hardware        | Warning  |
| nodes/pve01/qemu/1010       | OS 'Microsoft Windows 10/2016/2019' not maintained from vendor!    | Qemu    | OSNotMaintained | Warning  |
| nodes/pve01/qemu/1006       | 4 snapshots older than 1 month                                     | Qemu    | SnapshotOld     | Warning  |
| nodes/pve01/qemu/1010       | Start on boot not enabled                                          | Qemu    | StartOnBoot     | Warning  |
| nodes/pve02                 | 26 Update available                                                | Node    | Update          | Info     |
| nodes/pve01/qemu/1000       | For production environment is better VM Protection = enabled       | Qemu    | Protection      | Info     |
| nodes/pve01/qemu/1006       | For more performance switch 'net0' network to VirtIO               | Qemu    | VirtIO          | Info     |
+-----------------------------+--------------------------------------------------------------------+---------+-----------------+----------+
```

---

## Settings Reference

Generate the default settings file with:

```bash
cv4pve-diag --host=pve.local --username=root@pam --password=secret create-settings
```

Full `settings.json` structure with all defaults:

```jsonc
{
  "Storage": {
    "Rrd": {
      "TimeFrame": "Day",        // Hour, Day, Week, Month, Year
      "Consolidation": "Average" // Average, Maximum
    },
    "Threshold": {
      "Warning": 70,             // storage usage % warning threshold
      "Critical": 85             // storage usage % critical threshold
    }
  },
  "Node": {
    "Smart": {
      "Enabled": false,                                  // enable per-disk SMART checks
      "Temperature": { "Warning": 55, "Critical": 65 }, // disk temperature °C
      "SsdWearout":  { "Warning": 70, "Critical": 85 }  // SSD wearout % consumed
    },
    "NodeStorage": {
      "ZfsDetail": false,        // enable per-vdev ZFS error checks
      "LvmThinMetadata": true    // enable LVM-thin metadata usage check
    },
    "MaxVCpuRatio": 4.0,         // max vCPU/physical-CPU ratio before warning (e.g. 4.0 = 32 vCPU on 8 cores)
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {                                             // PSI pressure thresholds (PVE 9.0+)
        "Cpu":        { "Warning": 40, "Critical": 70 },       // PSI CPU some pressure %
        "IoFull":     { "Warning": 10, "Critical": 30 },       // PSI I/O full pressure %
        "MemoryFull": { "Warning": 5,  "Critical": 15 }        // PSI memory full pressure %
      }
    },
    "Cpu":       { "Warning": 70, "Critical": 85 }, // node CPU usage %
    "Memory":    { "Warning": 70, "Critical": 85 }, // node memory usage %
    "Network":   { "Warning": 0,  "Critical": 0  }, // node network throughput bytes/s, 0 = disabled
    "HealthScore": { "Warning": 70, "Critical": 50 } // composite health score (100 - weighted usage)
  },
  "Qemu": {
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu":        { "Warning": 50, "Critical": 80 },
        "IoFull":     { "Warning": 20, "Critical": 50 },
        "MemoryFull": { "Warning": 10, "Critical": 30 }
      }
    },
    "Cpu":       { "Warning": 70, "Critical": 85 },
    "Memory":    { "Warning": 70, "Critical": 85 },
    "Network":   { "Warning": 0,  "Critical": 0  }, // 0 = disabled
    "HealthScore": { "Warning": 60, "Critical": 40 }
  },
  "Lxc": {                       // same structure as Qemu
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu":        { "Warning": 50, "Critical": 80 },
        "IoFull":     { "Warning": 20, "Critical": 50 },
        "MemoryFull": { "Warning": 10, "Critical": 30 }
      }
    },
    "Cpu":       { "Warning": 70, "Critical": 85 },
    "Memory":    { "Warning": 70, "Critical": 85 },
    "Network":   { "Warning": 0,  "Critical": 0  },
    "HealthScore": { "Warning": 60, "Critical": 40 }
  },
  "Snapshot": {
    "Enabled": true,
    "MaxCount": 10,              // max snapshots per VM/CT, 0 = disabled
    "MaxAgeDays": 30             // warn if snapshot older than N days, 0 = disabled
  },
  "Backup": {
    "Enabled": true,
    "MaxAgeDays": 60,            // warn if backup older than N days, 0 = disabled
    "RecentDays": 7              // warn if no backup in last N days, 0 = disabled
  }
}
```

#### Health Score Formula

```
Node  score = 100 - (cpu% × 0.4 + ram% × 0.4 + disk% × 0.2)
VM/CT score = 100 - (cpu% × 0.5 + ram% × 0.5)
```

Set `Warning` and `Critical` to `0` to disable health score checks entirely.

> **PSI Pressure** (PVE 9.0+): Linux Pressure Stall Information metrics. Checks are automatically skipped on older PVE versions where the values are always zero.

---

## Advanced Features

### Ignore Specific Issues

<details>
<summary><strong>Filter Unwanted Warnings</strong></summary>

Create rules to ignore specific issues using regex patterns:

```json
[
  {
    "Id": "nodes/pve01/qemu/105",
    "Context": "Qemu",
    "SubContext": "Protection"
  }
]
```

**Pattern matching (regex):**

- Exact match: `"Id": "nodes/pve01/qemu/105"` — ignore only VM 105 on pve01
- Node match: `"Id": "nodes/pve01/.*"` — ignore all issues on pve01
- Partial match: `"Description": ".*test.*"` — ignore if description contains "test"
- All match: `"Id": ".*"` — ignore all IDs (use with specific Context/SubContext)

</details>

---

## Troubleshooting

### Common Issues & Solutions

<details>
<summary><strong>Authentication Problems</strong></summary>

#### Issue: "Authentication failed"

```bash
# Verify credentials
cv4pve-diag --host=pve.local --username=root@pam --password=test execute

# Check API token format
cv4pve-diag --host=pve.local --api-token=user@realm!tokenid=uuid execute
```

#### Solution: Verify permissions

```bash
# Check user permissions in Proxmox
pveum user list
pveum user permissions diagnostic@pve
```

</details>

<details>
<summary><strong>Connection Issues</strong></summary>

#### Issue: "Connection timeout" or "Host unreachable"

```bash
# Test connectivity
ping pve.local
telnet pve.local 8006

# Try with SSL validation disabled (testing only)
cv4pve-diag --host=pve.local --username=root@pam --password=secret execute
```

#### Solution: Use SSL validation

```bash
# Enable SSL validation for production
cv4pve-diag --host=pve.local --validate-certificate --username=root@pam --password=secret execute
```

</details>

---

## Resources

### Video Tutorials

#### **Official Tutorial**

[![cv4pve-diag Tutorial](http://img.youtube.com/vi/hn1nw9KXlsg/maxresdefault.jpg)](https://www.youtube.com/watch?v=hn1nw9KXlsg)

**Complete setup and usage guide**

#### **Web GUI Version**

[![cv4pve-admin](https://raw.githubusercontent.com/Corsinvest/cv4pve-admin/main/src/Corsinvest.ProxmoxVE.Admin/wwwroot/doc/images/screenshot/modules/diagnostic/results.png)](https://github.com/Corsinvest/cv4pve-admin)

### Documentation Links

| Resource                                                                        | Description                        |
| ------------------------------------------------------------------------------- | ---------------------------------- |
| **[API Documentation](https://pve.proxmox.com/pve-docs/api-viewer/index.html)** | Proxmox VE API reference           |
| **[API Token Guide](https://pve.proxmox.com/pve-docs/pveum-plain.html)**        | Proxmox VE API token documentation |

---

## Command Reference

### Global Options

<details>
<summary><strong>Complete Parameter List</strong></summary>

```bash
cv4pve-diag [global-options] [command]
```

#### Authentication Options

| Parameter     | Description      | Example                                        |
| ------------- | ---------------- | ---------------------------------------------- |
| `--host`      | Proxmox host(s)  | `--host=pve.local:8006`                        |
| `--username`  | Username@realm   | `--username=diagnostic@pve`                    |
| `--password`  | Password or file | `--password=secret` or `--password=file:/path` |
| `--api-token` | API token        | `--api-token=user@realm!token=uuid`            |

#### Connection Options

| Parameter                | Description              | Default |
| ------------------------ | ------------------------ | ------- |
| `--validate-certificate` | Validate SSL certificate | `false` |

#### Diagnostic Options

| Parameter               | Description               | Example                              |
| ----------------------- | ------------------------- | ------------------------------------ |
| `--settings-file`       | Custom settings file      | `--settings-file=settings.json`      |
| `--ignored-issues-file` | Ignored issues file       | `--ignored-issues-file=ignored.json` |
| `--ignored-issues-show` | Show ignored issues table | Flag                                 |

#### Output Options

| Parameter       | Description              | Options                                                   |
| --------------- | ------------------------ | --------------------------------------------------------- |
| `--output`      | Output format            | `Text`, `Html`, `Json`, `JsonPretty`, `Markdown`, `Excel` |
| `--output-file` | Save output to file      | Any file path; for Excel defaults to `cv4pve-diagnostic-<timestamp>.xlsx` |

</details>

### Commands

<details>
<summary><strong>Available Commands</strong></summary>

#### execute

Execute diagnostic and display results

```bash
cv4pve-diag --host=pve.local --username=root@pam --password=secret execute
```

#### create-settings

Create default settings file

```bash
cv4pve-diag --host=pve.local --username=root@pam --password=secret create-settings
```

#### create-ignored-issues

Create ignored issues template

```bash
cv4pve-diag --host=pve.local --username=root@pam --password=secret create-ignored-issues
```

</details>

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
