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


---

## Quick Start

```bash
# Check available releases at: https://github.com/Corsinvest/cv4pve-diag/releases
# Download specific version (replace VERSION with actual version number)
wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-x64.zip
unzip cv4pve-diag-linux-x64.zip

# Run diagnostic
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute
```

---

## Features

### Core Capabilities

#### **Performance & Reliability**
- **Native C#** implementation
- **Cross-platform** (Windows, Linux, macOS)
- **API-based** operation (no root access required)
- **Cluster support** with automatic analysis
- **High availability** with multiple host support

#### **Flexible Analysis**
- **Comprehensive checks** for VMs and Containers
- **Storage monitoring** and capacity analysis
- **Node health** verification
- **Replication status** checking
- **Backup configuration** validation
- **Thin provisioning** overcommit detection

#### **Advanced Reporting**
- **Multiple output formats** (Text, HTML, JSON, Markdown)
- **Severity levels** (Critical, Warning, Info)
- **Customizable settings** via configuration files
- **Issue filtering** with ignore rules
- **Export capabilities** for automation

#### **Enterprise Features**
- **API token** support (Proxmox VE 6.2+)
- **SSL validation** options
- **Custom settings** management
- **Ignored issues** tracking
- **Comprehensive logging** and status reporting

---

## Installation

### Linux Installation

```bash
# Check available releases and get the specific version number
# Visit: https://github.com/Corsinvest/cv4pve-diag/releases

# Download specific version (replace VERSION with actual version like v1.9.0)
wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-x64.zip

# Alternative: Get latest release URL programmatically
LATEST_URL=$(curl -s https://api.github.com/repos/Corsinvest/cv4pve-diag/releases/latest | grep browser_download_url | grep linux-x64 | cut -d '"' -f 4)
wget "$LATEST_URL"

# Extract and make executable
unzip cv4pve-diag-linux-x64.zip
chmod +x cv4pve-diag

# Optional: Move to system path
sudo mv cv4pve-diag /usr/local/bin/
```

### Windows Installation

**Option 1: WinGet (Recommended)**
```powershell
# Install using Windows Package Manager
winget install Corsinvest.cv4pve.diag
```

**Option 2: Manual Installation**
```powershell
# Check available releases at: https://github.com/Corsinvest/cv4pve-diag/releases
# Download specific version (replace VERSION with actual version)
Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag.exe-win-x64.zip" -OutFile "cv4pve-diag.zip"

# Extract
Expand-Archive cv4pve-diag.zip -DestinationPath "C:\Tools\cv4pve-diag"

# Add to PATH (optional)
$env:PATH += ";C:\Tools\cv4pve-diag"
```

### macOS Installation

```bash
# Check available releases at: https://github.com/Corsinvest/cv4pve-diag/releases
# Download specific version (replace VERSION with actual version)
wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-osx-x64.zip
unzip cv4pve-diag-osx-x64.zip
chmod +x cv4pve-diag

# Move to applications
sudo mv cv4pve-diag /usr/local/bin/
```

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

| Permission | Purpose | Scope |
|------------|---------|-------|
| **VM.Audit** | Read VM/CT information | Virtual machines |
| **Datastore.Audit** | Check storage capacity | Storage systems |
| **Pool.Audit** | Access pool information | Resource pools |
| **Sys.Audit** | Node system information | Cluster nodes |

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

### Cluster Checks

| Check | SubContext | Gravity | Description | Ignorable |
|-------|-----------|---------|-------------|-----------|
| Unknown resource type | Status | Critical | Resource with unrecognized type in cluster | No |
| Backup compression disabled | BackupCompression | Warning | Cluster-level backup has no compression set | Yes |
| No quorum | Quorum | Critical | Cluster has no quorum | No |
| HA groups with no resources | HAGroups | Warning | HA group defined but has no VMs/CTs assigned | Yes |
| Pools with no VMs/CTs | Pools | Info | Resource pool exists but is empty | Yes |
| Firewall disabled | Firewall | Warning | Cluster-level firewall is disabled | Yes |
| Two-factor auth not enforced | TwoFactor | Warning | No two-factor authentication policy on cluster | Yes |

### Node Checks

| Check | SubContext | Gravity | Description | Ignorable |
|-------|-----------|---------|-------------|-----------|
| Node offline | Status | Critical | Node is not reachable | No |
| Package versions differ | PackageVersions | Critical | Nodes have different package versions installed | Yes |
| Hosts file differs | Hosts | Warning | `/etc/hosts` content differs between nodes | Yes |
| Corosync ring errors | Corosync | Critical | Corosync reports ring errors | No |
| Replication errors | Replication | Warning | Replication job has errors | Yes |
| ZFS pool degraded/faulted | ZFS | Critical | ZFS pool is not in ONLINE state | No |
| SSD wearout below threshold | SSD | Warning/Critical | SSD wearout indicator below threshold | Yes |
| Updates available | Update | Info | Packages available for update | Yes |
| CPU usage above threshold | Usage | Warning/Critical | CPU usage above configured threshold | Yes |
| Memory usage above threshold | Usage | Warning/Critical | Memory usage above configured threshold | Yes |
| Disk (storage) usage above threshold | Usage | Warning/Critical | Node storage usage above configured threshold | Yes |
| Health score low | HealthScore | Warning/Critical | Composite health score below threshold | Yes |
| Time series period | TimeSeries | — | Configurable: Hour, Day, Week, Month, Year | — |

### Storage Checks

| Check | SubContext | Gravity | Description | Ignorable |
|-------|-----------|---------|-------------|-----------|
| Storage unavailable | Status | Critical | Storage is not accessible | No |
| Storage usage above threshold | Usage | Warning/Critical | Storage usage above configured threshold | Yes |
| Orphaned disk image | Image | Warning | Disk image not attached to any VM/CT | Yes |
| Thin provisioning overcommit | ThinOvercommit | Warning/Critical | Allocated disk space exceeds physical capacity on thin pool (lvmthin, zfspool, rbd, cephfs) | Yes |
| Old backup files | Backup | Warning | Backup files older than configured retention | Yes |

### VM (QEMU) Checks

| Check | SubContext | Gravity | Description | Ignorable |
|-------|-----------|---------|-------------|-----------|
| VM status unknown | Status | Critical | VM is in unknown/error state | No |
| No backup configured | Backup | Warning | VM not included in any backup job | Yes |
| Disk excluded from backup | Backup | Critical | A disk has backup disabled | Yes |
| No snapshot | Snapshot | Info | VM has no snapshots | Yes |
| Too many snapshots | SnapshotCount | Warning | Snapshot count exceeds configured limit | Yes |
| Old snapshots | SnapshotOld | Warning | Snapshots older than configured age | Yes |
| CPU usage above threshold | Usage | Warning/Critical | CPU usage above configured threshold | Yes |
| Memory usage above threshold | Usage | Warning/Critical | Memory usage above configured threshold | Yes |
| Health score low | HealthScore | Warning/Critical | Composite health score below threshold | Yes |
| QEMU agent not enabled | Agent | Warning | Guest agent not configured | Yes |
| Start on boot not enabled | StartOnBoot | Warning | VM will not start automatically after host reboot | Yes |
| Protection not enabled | Protection | Info | VM protection flag not set | Yes |
| VirtIO not used | VirtIO | Info | Network interface not using VirtIO driver | Yes |
| CD-ROM mounted | Hardware | Warning | CD-ROM drive has an image mounted | Yes |
| Unsafe disk cache | DiskCache | Warning | Disk configured with `cache=unsafe` | Yes |
| OS no longer maintained | OSNotMaintained | Warning | Guest OS has reached end of life | Yes |
| AutoSnapshot not configured | AutoSnapshot | Warning | cv4pve-autosnap not configured | Yes |
| ScsiHw not virtio-scsi-pci | Hardware | Info | SCSI controller is not the recommended type | Yes |

### LXC (Container) Checks

| Check | SubContext | Gravity | Description | Ignorable |
|-------|-----------|---------|-------------|-----------|
| CT status unknown | Status | Critical | Container is in unknown/error state | No |
| No backup configured | Backup | Warning | CT not included in any backup job | Yes |
| Disk excluded from backup | Backup | Critical | A disk has backup disabled | Yes |
| No snapshot | Snapshot | Info | CT has no snapshots | Yes |
| Too many snapshots | SnapshotCount | Warning | Snapshot count exceeds configured limit | Yes |
| Old snapshots | SnapshotOld | Warning | Snapshots older than configured age | Yes |
| CPU usage above threshold | Usage | Warning/Critical | CPU usage above configured threshold | Yes |
| Memory usage above threshold | Usage | Warning/Critical | Memory usage above configured threshold | Yes |
| Health score low | HealthScore | Warning/Critical | Composite health score below threshold | Yes |
| Start on boot not enabled | StartOnBoot | Warning | CT will not start automatically after host reboot | Yes |
| Protection not enabled | Protection | Info | CT protection flag not set | Yes |
| AutoSnapshot not configured | AutoSnapshot | Warning | cv4pve-autosnap not configured | Yes |
| Nesting enabled without keyctl | Features | Warning | `nesting=1` is set but `keyctl=1` is missing (Docker inside LXC) | Yes |

> **Note:** All checks marked as *Ignorable* can be suppressed by adding matching rules to the ignored-issues file. This is useful for intentional configurations that do not represent real problems.

### Example Output

```txt
+-----------------------------+--------------------------------------------------------------------+---------+-----------------+----------+
| Id                          | Description                                                        | Context | SubContext      | Gravity  |
+-----------------------------+--------------------------------------------------------------------+---------+-----------------+----------+
| nodes/pve02                 | Nodes package version not equal                                    | Node    | PackageVersions | Critical |
| nodes/pve02/qemu/203        | Disk 'scsi0' disabled for backup                                   | Qemu    | Backup          | Critical |
| nodes/pve01/lxc/100         | Disk 'rootfs' disabled for backup                                  | Lxc     | Backup          | Critical |
| nodes/pve01/qemu/1030       | Memory (rrd Day AVERAGE) usage 92.9% - 5.99 GB of 6.44 GB         | Qemu    | Usage           | Critical |
| nodes/pve02                 | Nodes hosts configuration not equal                                | Node    | Hosts           | Warning  |
| nodes/pve01/storage/local   | Image Orphaned 51.54 GB file vm-106-disk-1                         | Storage | Image           | Warning  |
| nodes/pve01/storage/pbs01   | Storage usage 75% - 2.42 TB of 3.22 TB                            | Storage | Usage           | Warning  |
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

```json
{
  "Storage": {
    "TimeSeries": "Day",
    "Threshold": {
      "Warning": 70,
      "Critical": 80
    }
  },
  "Node": {
    "TimeSeries": "Day",
    "Cpu": { "Warning": 70, "Critical": 80 },
    "Memory": { "Warning": 70, "Critical": 80 },
    "Network": { "Warning": 0, "Critical": 0 }
  },
  "Qemu": {
    "TimeSeries": "Day",
    "Cpu": { "Warning": 70, "Critical": 80 },
    "Memory": { "Warning": 70, "Critical": 80 },
    "Network": { "Warning": 0, "Critical": 0 }
  },
  "Lxc": {
    "TimeSeries": "Day",
    "Cpu": { "Warning": 70, "Critical": 80 },
    "Memory": { "Warning": 70, "Critical": 80 },
    "Network": { "Warning": 0, "Critical": 0 }
  },
  "SsdWearoutThreshold": {
    "Warning": 70,
    "Critical": 80
  },
  "Snapshot": {
    "MaxCount": 10,
    "MaxAgeDays": 30
  },
  "HealthScore": {
    "WarningThreshold": 60,
    "CriticalThreshold": 40
  }
}
```

### Settings Description

| Section | Property | Default | Description |
|---------|----------|---------|-------------|
| `Storage.TimeSeries` | — | `Day` | RRD time window for storage metrics (`Hour`, `Day`, `Week`, `Month`, `Year`) |
| `Storage.Threshold` | `Warning` / `Critical` | `70` / `80` | Storage usage percentage thresholds |
| `Node.TimeSeries` | — | `Day` | RRD time window for node metrics |
| `Node.Cpu` | `Warning` / `Critical` | `70` / `80` | Node CPU usage % thresholds |
| `Node.Memory` | `Warning` / `Critical` | `70` / `80` | Node memory usage % thresholds |
| `Node.Network` | `Warning` / `Critical` | `0` / `0` | Node network throughput thresholds (bytes/s), `0` = disabled |
| `Qemu.TimeSeries` | — | `Day` | RRD time window for VM metrics |
| `Qemu.Cpu` | `Warning` / `Critical` | `70` / `80` | VM CPU usage % thresholds |
| `Qemu.Memory` | `Warning` / `Critical` | `70` / `80` | VM memory usage % thresholds |
| `Qemu.Network` | `Warning` / `Critical` | `0` / `0` | VM network throughput thresholds (bytes/s), `0` = disabled |
| `Lxc.TimeSeries` | — | `Day` | RRD time window for CT metrics |
| `Lxc.Cpu` | `Warning` / `Critical` | `70` / `80` | CT CPU usage % thresholds |
| `Lxc.Memory` | `Warning` / `Critical` | `70` / `80` | CT memory usage % thresholds |
| `Lxc.Network` | `Warning` / `Critical` | `0` / `0` | CT network throughput thresholds (bytes/s), `0` = disabled |
| `SsdWearoutThreshold` | `Warning` / `Critical` | `70` / `80` | SSD wearout % thresholds (below = alert) |
| `Snapshot.MaxCount` | — | `10` | Max snapshots per VM/CT before warning; `0` = disabled |
| `Snapshot.MaxAgeDays` | — | `30` | Max snapshot age in days before warning; `0` = disabled |
| `HealthScore.WarningThreshold` | — | `60` | Health score below which a Warning is raised |
| `HealthScore.CriticalThreshold` | — | `40` | Health score below which a Critical is raised |

#### Health Score Formula

```
Node  score = 100 - (cpu × 0.4 + ram × 0.4 + disk × 0.2)
VM/CT score = 100 - (cpu × 0.5 + ram × 0.5)
```

Set `WarningThreshold` and `CriticalThreshold` to `0` to disable health score checks entirely.

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

| Resource | Description |
|----------|-------------|
| **[API Documentation](https://pve.proxmox.com/pve-docs/api-viewer/index.html)** | Proxmox VE API reference |
| **[API Token Guide](https://pve.proxmox.com/pve-docs/pveum-plain.html)** | Proxmox VE API token documentation |

---

## Command Reference

### Global Options

<details>
<summary><strong>Complete Parameter List</strong></summary>

```bash
cv4pve-diag [global-options] [command]
```

#### Authentication Options
| Parameter | Description | Example |
|-----------|-------------|---------|
| `--host` | Proxmox host(s) | `--host=pve.local:8006` |
| `--username` | Username@realm | `--username=diagnostic@pve` |
| `--password` | Password or file | `--password=secret` or `--password=file:/path` |
| `--api-token` | API token | `--api-token=user@realm!token=uuid` |

#### Connection Options
| Parameter | Description | Default |
|-----------|-------------|---------|
| `--validate-certificate` | Validate SSL certificate | `false` |

#### Diagnostic Options
| Parameter | Description | Example |
|-----------|-------------|---------|
| `--settings-file` | Custom settings file | `--settings-file=settings.json` |
| `--ignored-issues-file` | Ignored issues file | `--ignored-issues-file=ignored.json` |
| `--ignored-issues-show` | Show ignored issues table | Flag |

#### Output Options
| Parameter | Description | Options |
|-----------|-------------|---------|
| `--output` | Output format | `Text`, `Html`, `Json`, `JsonPretty`, `Markdown` |

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
