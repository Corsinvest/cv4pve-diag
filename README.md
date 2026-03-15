# cv4pve-diag

```
     ______                _                      __
    / ____/___  __________(_)___ _   _____  _____/ /_
   / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
  / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
  \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Diagnostic Tool for Proxmox VE (Made in Italy)

cv4pve-diag is a part of the Suite - CV4PVE
For more information visit https://www.corsinvest.it/cv4pve
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-diag.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-diag.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-diag/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-diag/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-diag/releases)
[![NuGet](https://img.shields.io/nuget/v/Corsinvest.ProxmoxVE.Diagnostic.Api.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Corsinvest.ProxmoxVE.Diagnostic.Api/)


---

## Quick Start

```bash

Step 1 - # Check available releases at:
         https://github.com/Corsinvest/cv4pve-diag/releases

Step 2 - # Download specific version (replace VERSION with actual version number)
         wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-x64.zip

Step 3 - # Unzip the Download
         unzip cv4pve-diag-linux-x64.zip

Step 4 - # Chmod to make Executable
         chmod +x cv4pve-diag

# Running Diagnostic Command Tool within its Directory
# NOTE: Use ./ in front of the the Command cv4pve-diag if you are in the same Directory as cv4pve-diag.
./cv4pve-diag --output=Text --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@debian:/cv4pvediag# ./cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool from the Root Directory
# NOTE: If you are at the Root Directory, use the Directory Path to Run cv4pve-diag
/cv4pvediag/cv4pve-diag --output=Text --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@debian:/# /cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
                        
```
## Usage
```
  cv4pve-diag [options] [command]

Options:
  --api-token <api-token>                            Api token format 'USER@REALM!TOKENID=UUID'. Require Proxmox VE 6.2 or later
  --username <username>                              User name <username>@<realm>
  --password <password>                              The password. Specify 'file:path_file' to store password in file.
  --host <host> (REQUIRED)                           The host name host[:port],host1[:port],host2[:port]
  --settings-file <settings-file>                    File settings (generated from create-settings)
  --ignored-issues-file <ignored-issues-file>        File ignored issues (generated from create-ignored-issues)
  --ignored-issues-show                              Show second table with ignored issue
  -o, --output <Html|Json|JsonPretty|Markdown|Text>  Type output [default: Text]
  --version                                          Show version information
  -?, -h, --help                                     Show help and usage information

Commands:
  create-settings        Create file settings (settings.json)
  create-ignored-issues  Create File ignored issues (ignored-issues.json)
  export-collect         Export collect data collect to data.json
  execute                Execute diagnostic and print result to console
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
Step 1 - # Check available releases and get the specific version number
         https://github.com/Corsinvest/cv4pve-diag/releases

Step 2 - # Download specific version (replace VERSION with actual version like v1.9.0)
         wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-x64.zip

         # Alternative: Get latest release URL programmatically
         LATEST_URL=$(curl -s https://api.github.com/repos/Corsinvest/cv4pve-diag/releases/latest | grep browser_download_url | grep 
         linux-x64 | cut -d '"' -f 4) wget "$LATEST_URL"

Step 3 - # Unzip the Download
         unzip cv4pve-diag-linux-x64.zip

Step 4 - # Chmod to make Executable
         chmod +x cv4pve-diag

# Optional: Move to the System Path to make cv4pve-diag Global
mv cv4pve-diag /usr/local/bin/

# Running Diagnostic Command Tool Globally
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@debian:/# cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool within its Directory
# NOTE: Use ./ in front of the the Command cv4pve-diag if you are in the same Directory as cv4pve-diag.
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@debian:/cv4pvediag# ./cv4pvediag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool from the Root Directory
# NOTE: If you are at the Root Directory, use the Directory Path to Run cv4pve-diag
/cv4pvediag/cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@debian:/# /cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
```

### Windows Installation

**Option 1: WinGet (Recommended)**
```powershell
Step 1 - # Install using Windows Package Manager
         # PreInstalled in Windows 10(1809 or Later), Windows 11 and Windows 2025 Server
         winget install Corsinvest.cv4pve.diag

# Optional: Add cv4pve-diag to System PATH to make cv4pve-diag Global
1. Start Run Type: sysdm.cpl
2. Select the Advanced Tab
3. Select the Environment Variables Button
4. In the System Variables Windows Scroll for: Path
5. Edit the Path: Insert the Path to the cv4pve-diag Directory in the Variable Value Window
C:\cv4pve-diag.exe-win-x(ARCHITECTURE)\

NOTE: You will need a SemiColon (;) after the Last Path Referenced when Adding New Paths.
EXAMPLE: C:\Program Files\Common Files\Oracle\Java\java8path;C:\cv4pve-diag.exe-win-x(ARCHITECTURE)\

# Running Diagnostic Command Tool Globally in CMD(DOS Terminal)
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

# Example
C:\>cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute 
                              
Attention: There is a Slight Delay when using the Tool due to Processing the Information.
           Please wait for Data to Display.
           It is Recommened to Run the Tool in PowerShell when Outputing in Text Mode for
           proper Displaying of the Data.
           You can use CMD(DOS Terminal) however there is a Limatation of Columns Displaying.

# Running Diagnostic Command Tool within its Directory in CMD(DOS Terminal)
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

# Example
C:\cv4pve-diag.exe-win-x(ARCHITECTURE)> cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute 
```

**Option 2: Manual Installation**

**PowerShell**
```powershell
Step 1 - # Check available releases
         https://github.com/Corsinvest/cv4pve-diag/releases

Step 2 - # PowerShell 3.0 and Later
         # Download Specific Version (replace VERSION with actual version)
         Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag.exe-win-x64.zip" - 
         OutFile "cv4pve-diag.zip"

Step 3 - # Unzip the Download
         Expand-Archive cv4pve-diag.zip -DestinationPath "C:\Tools\cv4pve-diag"

# Optional: Add cv4pve-diag to System PATH to make cv4pve-diag Global
1. Start Run Type: sysdm.cpl
2. Select the Advanced Tab
3. Select the Environment Variables Button
4. In the System Variables Windows Scroll for: Path
5. Edit the Path: Insert the Path to the cv4pve-diag Directory in the Variable Value Window
C:\cv4pve-diag.exe-win-x(ARCHITECTURE)\

NOTE: You will need a SemiColon (;) after the Last Path Referenced when Adding New Paths.
EXAMPLE: C:\Program Files\Common Files\Oracle\Java\java8path;C:\cv4pve-diag.exe-win-x(ARCHITECTURE)\

# Running Diagnostic Command Tool Globally in PowerShell
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

# Example        
PS C:\cv4pve-diag.exe-win-x(ARCHITECTURE)>.\cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
NOTE: The .\ in front of the Command cv4pve-diag is needed.

# Running Diagnostic Command Tool within its Directory in PowerShell
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

# Example
PS C:\cv4pve-diag.exe-win-x(ARCHITECTURE)> .\cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
```
**Direct Download on Windows x86 and x64**
```
Step 1 - # Download the Lastest Zip File cv4pve-diag.exe-win-x(ARCHITECTURE).zip to a Directory of your Choice

x86 Version:
         Option A - Click On File to Download: https://github.com/Corsinvest/cv4pve-diag/releases
                    NOTE: Select under Assets and Choose Lastest Version
         Option B - Direct Downlod: https://github.com/Corsinvest/cv4pve-diag/releases/download/v(x.x.x)/cv4pve-diag.exe-win-x86.zip
                    NOTE: v(x.x.x) is the Version Number

                    Example Option B for v1.10.0:      
		    https://github.com/Corsinvest/cv4pve-diag/releases/download/v1.10.0/cv4pve-diag.exe-win-x86.zip

x64 Version:
         Option A - Click On File to Download: https://github.com/Corsinvest/cv4pve-diag/releases
                    NOTE: Select under Assets and Choose Lastest Version
         Option B - Direct Downlod: https://github.com/Corsinvest/cv4pve-diag/releases/download/v(x.x.x)/cv4pve-diag.exe-win-x64.zip
                    NOTE: v(x.x.x) is the Version Number

                    Example Option B for v1.10.0:      
		    https://github.com/Corsinvest/cv4pve-diag/releases/download/v1.10.0/cv4pve-diag.exe-win-x64.zip

# Optional: Add cv4pve-diag to System PATH to make cv4pve-diag Global
1. Start Run Type: sysdm.cpl
2. Select the Advanced Tab
3. Select the Environment Variables Button
4. In the System Variables Windows Scroll for: Path
5. Edit the Path: Insert the Path to the cv4pve-diag Directory in the Variable Value Window
C:\cv4pve-diag.exe-win-x(ARCHITECTURE)\

NOTE: You will need a SemiColon (;) after the Last Path Referenced when Adding New Paths.
EXAMPLE: C:\Program Files\Common Files\Oracle\Java\java8path;C:\cv4pve-diag.exe-win-x(ARCHITECTURE)\

# Running Diagnostic Command Tool Globally in CMD(DOS Terminal)
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

# Example
C:\>cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute 
                              
Attention: There is a Slight Delay when using the Tool due to Processing the Information.
           Please wait for Data to Display.
           It is Recommened to Run the Tool in PowerShell when Outputing in Text Mode for
           proper Displaying of the Data.
           You can use CMD(DOS Terminal) however there is a Limatation of Displaying correctly.

# Running Diagnostic Command Tool within its Directory in CMD(DOS Terminal)
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

# Example
C:\cv4pve-diag.exe-win-x(ARCHITECTURE)> cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
```

### MacOS Installation

```bash
Step 1 - # Check available releases
         https://github.com/Corsinvest/cv4pve-diag/releases
Step 2 - # Download specific version (replace VERSION with actual version)
         wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-osx-x64.zip

Step 3 - # Unzip the Download
         unzip cv4pve-diag-osx-x64.zip

Step 4 - # Chmod to make Executable
         chmod +x cv4pve-diag

Step 5 - # Move to applications
         mv cv4pve-diag /usr/local/bin/

# Running Diagnostic Command Tool Globally
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@MacOS:/$ cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool within its Directory
# NOTE: Use ./ in front of the the Command cv4pve-diag if you are in the same Directory as cv4pve-diag.
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@MacOS:/cv4pvediag$ ./cv4pvediag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool from the Root Directory
# NOTE: If you are at the Root Directory, use the Directory Path to Run cv4pve-diag
/cv4pvediag/cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@MacOS:/$ /cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
```

### MacOS ARM Installation

```bash
Step 1 - # Check available releases
         https://github.com/Corsinvest/cv4pve-diag/releases
Step 2 - # Download specific version (replace VERSION with actual version)
         wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-osx-arm64.zip

Step 3 - # Unzip the Download
         unzip cv4pve-diag-osx-arm64.zip

Step 4 - # Chmod to make Executable
         chmod +x cv4pve-diag

Step 5 - # Move to applications
         mv cv4pve-diag /usr/local/bin/

# Running Diagnostic Command Tool Globally
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@MacOS-ARM:/$ cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool within its Directory
# NOTE: Use ./ in front of the the Command cv4pve-diag if you are in the same Directory as cv4pve-diag.
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@MacOS-ARM:/cv4pvediag$ ./cv4pvediag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool from the Root Directory
# NOTE: If you are at the Root Directory, use the Directory Path to Run cv4pve-diag
/cv4pvediag/cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@MacOS-ARM:/$ /cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute
```

### ARM Installation

```
Step 1 - # Check available releases
         https://github.com/Corsinvest/cv4pve-diag/releases
Step 2 - # Download specific version (replace VERSION with actual version)
         wget https://github.com/Corsinvest/cv4pve-diag/releases/download/VERSION/cv4pve-diag-linux-arm(ARCHITECTURE).zip

Step 3 - # Unzip the Download
         unzip cv4pve-diag-linux-arm(ARCHITECTURE).zip

Step 4 - # Chmod to make Executable
         chmod +x cv4pve-diag

Step 5 - # Move to applications
         mv cv4pve-diag /usr/local/bin/

# Running Diagnostic Command Tool Globally
cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@ARM:/# cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool within its Directory
# NOTE: Use ./ in front of the the Command cv4pve-diag if you are in the same Directory as cv4pve-diag.
./cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@ARM:/cv4pvediag# ./cv4pvediag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

# Running Diagnostic Command Tool from the Root Directory
# NOTE: If you are at the Root Directory, use the Directory Path to Run cv4pve-diag
/cv4pvediag/cv4pve-diag --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD execute

#Example
root@ARM:/# /cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

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

## Advanced Features

### Custom Settings

<details>
<summary><strong>Customize Diagnostic Rules</strong></summary>

The settings file allows you to customize thresholds and rules for diagnostics:

```json
{
  "Node": {
    "Cpu": { "Warning": 70, "Critical": 80 },
    "Memory": { "Warning": 70, "Critical": 80 }
  },
  "Storage": {
    "Threshold": { "Warning": 70, "Critical": 80 }
  }
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
    "Id": "nodes/pve01/qemu/105",
    "Context": "Qemu",
    "SubContext": "Protection"
  }
]
```

**Pattern matching (regex):**
- Exact match: `"Id": "nodes/pve01/qemu/105"` - ignora solo VM 105 su pve01
- Node match: `"Id": "nodes/pve01/.*"` - ignora tutti i problemi su pve01
- Partial match: `"Description": ".*test.*"` - ignora se descrizione contiene "test"
- All match: `"Id": ".*"` - ignora tutti gli ID (usa con Context/SubContext specifici)

</details>

---

## Diagnostic Checks

### Check Categories

#### **Node Checks**
- Node online status
- Update availability
- Replication status
- ZFS health
- CPU/Memory usage
- Storage capacity

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

#### **Storage Checks**
- Storage capacity
- Orphaned images
- Disk allocation
- Replication errors
- Backup file validation

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

[![cv4pve-admin](https://raw.githubusercontent.com/Corsinvest/cv4pve-admin/main/src/Corsinvest.ProxmoxVE.Admin/wwwroot/doc/images/screenshot/modules/diagnostic/results.png)](https://github.com/Corsinvest/cv4pve-admin)### Documentation Links
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
