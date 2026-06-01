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
>
> Runs **170+ built-in diagnostic checks on every scan** (cluster, nodes, storages, VMs, LXCs — see [docs/checks.md](docs/checks.md)) and tags **40+ findings against 14 compliance frameworks** (ISO 27001, NIS2, DORA, PCI DSS, GDPR, AgID, ENS, BSI C5, SOC 2, NIST 800-53, ISO 27017, ISO 27018, CIS Controls, NIST CSF — see [docs/compliance.md](docs/compliance.md)).
>
> **Single-node hosts** will see resilience findings (no HA / no replication / single-node topology) flagged on every run — by design, since a single node is **not compliant** with the business-continuity controls those checks map to. On lab / dev setups, use [ignore rules](docs/ignored-issues.md) to silence them. See [Single-node setups and compliance](docs/compliance.md#single-node-setups-and-compliance).

---

## Where cv4pve-diag fits

The cv4pve suite follows the Unix philosophy — each tool does one thing and does it well. `cv4pve-diag` is focused on **finding problems**: it runs a fixed set of health checks and reports what is wrong.

| Tool | Purpose |
|---|---|
| [**cv4pve-diag**](https://github.com/Corsinvest/cv4pve-diag) | **Health checks** — detects misconfigurations, risks and best-practice violations |
| [cv4pve-report](https://github.com/Corsinvest/cv4pve-report) | Cluster inventory and reporting |

> Use `cv4pve-diag` when you want to know *what is wrong*, `cv4pve-report` when you want to know *what you have*.

---

## Quick Start

```bash
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
- **Diagnostic checks catalog** — 170+ checks with codes, descriptions and severity (see [docs/checks.md](docs/checks.md))
- **Configurable thresholds** — CPU, memory, disk, network, health score, SMART, PSI pressure
- **Settings** — JSON-driven configuration for thresholds, backup, snapshot, CVE, compliance (see [docs/settings.md](docs/settings.md))
- **Performance tuning** — `MaxParallelRequests` and `ApiTimeout` for slow / high-latency clusters (see [docs/settings.md#performance-tuning](docs/settings.md#performance-tuning))
- **Ignore rules** — suppress known/accepted issues by ErrorCode, Id, SubContext or Description (see [docs/ignored-issues.md](docs/ignored-issues.md))
- **API token** support (Proxmox VE 6.2+)
- **Compliance mapping** — 40+ findings tagged across 14 frameworks (ISO 27001, NIS2, DORA, PCI DSS, GDPR, AgID, ENS, BSI C5, SOC 2, NIST 800-53, ISO 27017, ISO 27018, CIS Controls, NIST CSF); filter and add control ids to the report with `--compliance=<standard>` (see [docs/compliance.md](docs/compliance.md))
- **CVE scanning (Proxmox VE only)** — optional NVD lookup for known vulnerabilities affecting the installed Proxmox VE version (see [docs/settings.md#cve-scanning](docs/settings.md#cve-scanning))

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

<details>
<summary><strong>Security &amp; Permissions</strong></summary>

### Required Permissions

| Permission | Purpose | Scope |
|------------|---------|-------|
| **VM.Audit** | Read VM/CT configuration and status | Virtual machines |
| **Datastore.Audit** | Check storage capacity and content | Storage systems |
| **Pool.Audit** | Access pool information | Resource pools |
| **Sys.Audit** | Node system information, services, disks | Cluster nodes |
| **Sys.Modify** | APT repositories, available updates and installed package versions | Cluster nodes |

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

# --output is inferred from --output-file extension when not specified:
# .xlsx → Excel, .html/.htm → Html, .json → Json, .md → Markdown
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid --output-file=report.xlsx execute

# With settings and ignore rules
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --settings-file=settings.json \
  --ignored-issues-file=ignored-issues.json \
  execute

# Audit report filtered by compliance standard (adds a ControlId column)
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --compliance=Iso27001 execute

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
| nodes/pve02                 | WN0005 | Nodes hosts configuration not equal                                | Node    | Hosts           | Warning  |
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

## Settings Reference

Full field-by-field reference, defaults, recommended scenarios and the complete `settings.json` template are in [docs/settings.md](docs/settings.md).

```bash
# Generate default settings file
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid create-settings

# Run with custom settings
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid --settings-file=settings.json execute
```

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
