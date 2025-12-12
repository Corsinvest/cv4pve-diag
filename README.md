<div align="center">

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

</div>

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

## Table of Contents

<details>
<summary><strong>Click to expand navigation</strong></summary>

- [Features](#-features)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage Examples](#-usage-examples)
- [Security & Permissions](#-security--permissions)
- [Advanced Features](#-advanced-features)
- [Diagnostic Checks](#-diagnostic-checks)
- [Troubleshooting](#-troubleshooting)
- [Resources](#-resources)

</details>

---

## Features

### Core Capabilities

<table>
<tr>
<td width="50%">

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

</td>
<td width="50%">

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

</td>
</tr>
</table>

---

## Installation

<div align="center">
  <img src="https://img.shields.io/badge/INSTALLATION-GUIDE-green?style=for-the-badge&logo=download" alt="Installation Guide">
</div>

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

<div align="center">
  <img src="https://img.shields.io/badge/CONFIGURATION-SETUP-blue?style=for-the-badge&logo=settings" alt="Configuration Setup">
</div>

### Authentication Methods

<table>
<tr>
<td width="50%">

#### **Username/Password**
```bash
cv4pve-diag --host=192.168.1.100 --username=root@pam --password=your_password execute
```

#### **API Token (Recommended)**
```bash
cv4pve-diag --host=192.168.1.100 --api-token=diagnostic@pve!token1=uuid-here execute
```

</td>
<td width="50%">

#### **Password from File**
```bash
# Store password in file
cv4pve-diag --host=192.168.1.100 --username=root@pam --password=file:/etc/cv4pve/password execute

# First run: prompts for password and saves to file
# Subsequent runs: reads password from file automatically
```

</td>
</tr>
</table>

---

## Usage Examples

<div align="center">
  <img src="https://img.shields.io/badge/USAGE-EXAMPLES-orange?style=for-the-badge&logo=terminal" alt="Usage Examples">
</div>

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

<details>
<summary><strong>Export Data</strong></summary>

#### Export Collected Data
```bash
# Export diagnostic data to JSON file
cv4pve-diag --host=pve.domain.com --username=root@pam --password=secret export-collect
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

<div align="center">
  <img src="https://img.shields.io/badge/SECURITY-PERMISSIONS-red?style=for-the-badge&logo=shield" alt="Security & Permissions">
</div>

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

### Security Best Practices

<table>
<tr>
<td width="50%">

#### **Do's**
- Use API tokens instead of passwords
- Enable privilege separation for tokens
- Store credentials in secure files with proper permissions
- Use dedicated user accounts for diagnostics
- Enable SSL certificate validation in production

</td>
<td width="50%">

#### **Don'ts**
- Use root credentials for automation
- Store passwords in plain text scripts
- Disable SSL validation without good reason
- Grant excessive permissions
- Share API tokens between different applications

</td>
</tr>
</table>

---

## Advanced Features

<div align="center">
  <img src="https://img.shields.io/badge/ADVANCED-FEATURES-purple?style=for-the-badge&logo=rocket" alt="Advanced Features">
</div>

### Custom Settings

<details>
<summary><strong>Customize Diagnostic Rules</strong></summary>

The settings file allows you to customize thresholds and rules for diagnostics:

```json
{
  "MinPercentageVmUsageDisk": 95,
  "MinPercentageVmUsageMemory": 95,
  "MaxDaysSnapshotOutdate": 30,
  "ConsiderBackupExtension": true,
  "BackupExtension": [
    "vma",
    "vma.gz",
    "vma.lzo"
  ]
}
```

#### Example Usage
```bash
# Create default settings
cv4pve-diag --host=pve.local --username=root@pam --password=secret create-settings

# Edit settings.json with your preferences
# Run with custom settings
cv4pve-diag --host=pve.local --username=root@pam --password=secret --settings-file=settings.json execute
```

</details>

### Ignore Specific Issues

<details>
<summary><strong>Filter Unwanted Warnings</strong></summary>

Create rules to ignore specific issues using regex patterns:

```json
[
  {
    "Id": "105",
    "Context": "Qemu",
    "SubContext": "Protection",
    "Description": null,
    "Gravity": "Critical"
  },
  {
    "Id": ".*",
    "Context": "Qemu",
    "SubContext": "Agent",
    "Description": ".*test.*",
    "Gravity": "Warning"
  }
]
```

The regex patterns match against Id, SubContext, and Description fields.

</details>

---

## Diagnostic Checks

<div align="center">
  <img src="https://img.shields.io/badge/DIAGNOSTIC-CHECKS-teal?style=for-the-badge&logo=check" alt="Diagnostic Checks">
</div>

### Check Categories

<table>
<tr>
<td width="33%">

#### **Node Checks**
- Node online status
- Update availability
- Replication status
- ZFS health
- CPU/Memory usage
- Storage capacity

</td>
<td width="33%">

#### **VM/CT Checks**
- VM/CT status
- Resource usage
- QEMU agent status
- Backup configuration
- Snapshot age
- AutoSnapshot configuration
- Protection status
- Start on boot
- Hardware configuration
- VirtIO usage

</td>
<td width="33%">

#### **Storage Checks**
- Storage capacity
- Orphaned images
- Disk allocation
- Replication errors
- Backup file validation

</td>
</tr>
</table>

### Example Output

```txt
-----------------------------------------------------------------------------------------------------------------------------------------
| Id                             | Description                                                  | Context | SubContext   | Gravity  |
-----------------------------------------------------------------------------------------------------------------------------------------
| pve2                           | 1 Replication has errors                                     | Node    | Replication  | Critical |
| pve2                           | Zfs 'rpool' health problem                                   | Node    | Zfs          | Critical |
| 312                            | Unknown resource qemu                                        | Qemu    | Status       | Critical |
| pve3                           | Node not online                                              | Node    | Status       | Warning  |
| local-zfs:vm-117-disk-1        | Image Orphaned                                               | Storage | Image        | Warning  |
| 121                            | Qemu Agent not enabled                                       | Qemu    | Agent        | Warning  |
| 103                            | cv4pve-autosnap not configured                               | Qemu    | AutoSnapshot | Warning  |
| 115                            | vzdump backup not configured                                 | Qemu    | Backup       | Warning  |
| 117                            | Unused disk0                                                 | Qemu    | Hardware     | Warning  |
| 121                            | 10 snapshots older than 1 month                              | Qemu    | Snapshot     | Warning  |
| pve1                           | 3 Update availble                                            | Node    | Update       | Info     |
| 109                            | For more performance switch 'scsi0' hdd to VirtIO            | Qemu    | VirtIO       | Info     |
-----------------------------------------------------------------------------------------------------------------------------------------
```

---

## Troubleshooting

<div align="center">
  <img src="https://img.shields.io/badge/TROUBLESHOOTING-HELP-red?style=for-the-badge&logo=tools" alt="Troubleshooting">
</div>

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

<div align="center">
  <img src="https://img.shields.io/badge/RESOURCES-LEARN%20MORE-teal?style=for-the-badge&logo=video" alt="Resources">
</div>

### Video Tutorials

<table>
<tr>
<td align="center" width="50%">

#### **Official Tutorial**

[![cv4pve-diag Tutorial](http://img.youtube.com/vi/hn1nw9KXlsg/maxresdefault.jpg)](https://www.youtube.com/watch?v=hn1nw9KXlsg)

**Complete setup and usage guide**

</td>
<td align="center" width="50%">

#### **Web GUI Version**

[![cv4pve-admin](https://raw.githubusercontent.com/Corsinvest/cv4pve-admin/main/src/Corsinvest.ProxmoxVE.Admin/wwwroot/doc/images/screenshot/modules/diagnostic/results.png)](https://github.com/Corsinvest/cv4pve-admin)

**Web interface for cv4pve-diag**

</td>
</tr>
</table>

### Documentation Links

| Resource | Description |
|----------|-------------|
| **[API Documentation](https://pve.proxmox.com/pve-docs/api-viewer/index.html)** | Proxmox VE API reference |
| **[cv4pve-tools Suite](https://www.cv4pve-tools.com)** | Complete cv4pve tools ecosystem |
| **[API Token Guide](https://pve.proxmox.com/pve-docs/pveum-plain.html)** | Proxmox VE API token documentation |

---

## Command Reference

<div align="center">
  <img src="https://img.shields.io/badge/COMMAND-REFERENCE-navy?style=for-the-badge&logo=terminal" alt="Command Reference">
</div>

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

#### export-collect
Export collected diagnostic data

```bash
cv4pve-diag --host=pve.local --username=root@pam --password=secret export-collect
```

</details>

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

<div align="center">
  <sub>Part of <a href="https://www.corsinvest.it/cv4pve">cv4pve</a> suite | Made with ❤️ in Italy by <a href="https://www.corsinvest.it">Corsinvest</a></sub>
  <br>
  <sub>Copyright © Corsinvest Srl</sub>
</div>
