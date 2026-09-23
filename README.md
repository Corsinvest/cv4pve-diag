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
> Runs **170+ built-in diagnostic checks on every scan** (cluster, nodes, storages, VMs, LXCs — see [docs/checks.md](docs/checks.md)) and tags **100+ findings against 14 compliance frameworks** (ISO 27001, NIS2, DORA, PCI DSS, GDPR, AgID, ENS, BSI C5, SOC 2, NIST 800-53, ISO 27017, ISO 27018, CIS Controls, NIST CSF — see [docs/compliance.md](docs/compliance.md)).
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
- **Compliance mapping** — 100+ findings tagged across 14 frameworks (ISO 27001, NIS2, DORA, PCI DSS, GDPR, AgID, ENS, BSI C5, SOC 2, NIST 800-53, ISO 27017, ISO 27018, CIS Controls, NIST CSF); filter and add control ids to the report with `--compliance=<standard>` (see [docs/compliance.md](docs/compliance.md))
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
| **Datastore.Audit** | Check storage capacity, list disk images | Storage systems |
| **Pool.Audit** | Access pool information | Resource pools |
| **Sys.Audit** | Node system information, services, disks | Cluster nodes |
| **Sys.Modify** | APT repositories, available updates and installed package versions | Cluster nodes |
| **Datastore.AllocateSpace** | List backup files — see the note below | Storage systems |
| **VM.Backup** | List backup files — see the note below | Virtual machines |

cv4pve-diag verifies these at startup and reports **`WC0020`** for each one the account does not
hold, saying what the analysis will not cover without it. Restricting an account to part of the
cluster is perfectly valid — that case is reported as **Info**, simply stating the scope. Only the
backup privileges raise a **Warning**, because without them other checks report the opposite of the
truth rather than just less (see below).

> [!IMPORTANT]
> **`PVEAuditor` alone is not enough to see backups.**
>
> The built-in `PVEAuditor` role grants only the `*.Audit` privileges, so the backup checks
> (`WG0019`, `WG0020`, `WS0003`) will report **"No recent backups found!"** for every guest even when
> backups exist and are perfectly healthy.
>
> This is not a permission *error* you would notice: listing a storage's contents succeeds with
> `200 OK`, but Proxmox filters out every backup volume the user may not access, returning an empty
> list. From the outside it is indistinguishable from a guest that genuinely has no backups.
>
> Proxmox requires **both** `Datastore.AllocateSpace` (on the storage) and `VM.Backup` (on the guest)
> to list a backup volume — see [`check_volume_access`](https://github.com/proxmox/pve-storage/blob/master/src/PVE/Storage.pm).
> Note that these are more than read-only: `VM.Backup` also permits *starting* backups, and
> `Datastore.AllocateSpace` permits *allocating* space.
>
> To grant them on top of `PVEAuditor`:
>
> ```bash
> # a custom role with the two extra privileges
> pveum role add CV4PVEDiagBackup --privs "Datastore.AllocateSpace,VM.Backup"
> pveum acl modify /storage --users cv4pve@pam --roles CV4PVEDiagBackup
> pveum acl modify /vms     --users cv4pve@pam --roles CV4PVEDiagBackup
> ```
>
> If you prefer to keep the account strictly read-only, that is a valid choice — just be aware the
> backup checks cannot work, and treat their findings as unreliable.

> [!NOTE]
> **Why missing privileges are hard to notice.** Proxmox answers a request the caller is only partly
> entitled to by *filtering the response*, not by failing it. `/cluster/resources` drops the guests,
> storages and pools you cannot audit; a storage listing drops the backup volumes you cannot access.
> Both return `200 OK`, so from the outside a filtered result is indistinguishable from a genuinely
> empty one — a guest missing `VM.Audit` simply does not appear in the report at all.
>
> That is why cv4pve-diag checks privileges up front rather than relying on API errors. Privileges
> that *do* fail loudly, such as `Sys.Modify` for the APT checks, are reported as `WG0042` when the
> call fails and need no pre-check.
>
> **To analyze the whole cluster, grant the privileges on the roots** (`/vms`, `/storage`, `/nodes`,
> `/pool`) or on `/`. An ACL on individual guests or storages is equally valid — it simply scopes the
> analysis to those objects. cv4pve-diag reports that scope as `WC0020` (Info) so the report says
> what it covers, since PVE itself gives no indication that anything was left out.

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
+------------------------------+--------+-------------------------------------------------------------------------------------------------------------------------------------+---------+-----------------+----------+
| Id                           | Code   | Description                                                                                                                         | Context | SubContext      | Gravity  |
+------------------------------+--------+-------------------------------------------------------------------------------------------------------------------------------------+---------+-----------------+----------+
| access/users/root@pam        | CC0004 | root@pam has no TFA configured — full access protected only by password                                                             | Cluster | Access          | Critical |
| nodes/pve02/lxc/101          | CG0006 | Privileged container has AppArmor disabled — no kernel confinement, root inside has unrestricted host access                        | Lxc     | Security        | Critical |
| nodes/pve02/qemu/203         | CG0002 | Disk 'scsi0' disabled for backup                                                                                                    | Qemu    | Backup          | Critical |
| nodes/pve02/lxc/100          | CG0002 | Disk 'mp0' disabled for backup                                                                                                      | Lxc     | Backup          | Critical |
| nodes/pve01/qemu/1104        | WG0026 | Memory (rrd Day Average) usage 93.3% - 10.02 GB of 10.74 GB                                                                         | Qemu    | Usage           | Critical |
| nodes/pve01                  | WN0013 | Node requires reboot: running kernel '6.8.12-20-pve' but newer kernel '6.8.12-43-pve' is installed                                  | Node    | Reboot          | Warning  |
| nodes/pve01/storage/datapool | WS0002 | Image Orphaned 51.54 GB file vm-106-disk-1                                                                                          | Storage | Image           | Warning  |
| nodes/pve01/storage/pbs01    | WS0001 | Storage usage 80% - 2.58 TB of 3.22 TB                                                                                              | Storage | Usage           | Warning  |
| nodes/pve02/qemu/106         | WG0003 | Qemu Agent not enabled                                                                                                              | Qemu    | Agent           | Warning  |
| nodes/pve02/qemu/999         | WG0017 | vzdump backup not configured                                                                                                        | Qemu    | Backup          | Warning  |
| nodes/pve01/qemu/1010        | WG0037 | CPU type 'kvm64' is missing security flags: +spec-ctrl, +ssbd, +pcid, +md-clear — add to cpu flags to mitigate Spectre/Meltdown/MDS | Qemu    | CPU             | Warning  |
| nodes/pve02/qemu/203         | WG0005 | Cdrom mounted on 'ide2' (local:iso/debian-12.6.0-amd64-netinst.iso)                                                                 | Qemu    | Hardware        | Warning  |
| nodes/pve01/qemu/1013        | WG0002 | OS 'Microsoft Windows 8.x/2012/2012r2' not maintained from vendor!                                                                  | Qemu    | OSNotMaintained | Warning  |
| nodes/pve02                  | IN0001 | 6 Update available                                                                                                                  | Node    | Update          | Info     |
| nodes/pve01/qemu/1000        | IG0011 | For production environment is better VM Protection = enabled                                                                        | Qemu    | Protection      | Info     |
+------------------------------+--------+-------------------------------------------------------------------------------------------------------------------------------------+---------+-----------------+----------+
```

---

## Settings Reference

Full field-by-field reference, defaults, recommended scenarios and the complete `settings.json` template are in [docs/settings.md](docs/settings.md).

```bash
# Built-in profiles: --fast (quick scan), default Standard, --full (every optional check, for audits)
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid execute --full

# Generate a settings file (optionally from a profile) and run with it
cv4pve-diag create-settings --full
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
