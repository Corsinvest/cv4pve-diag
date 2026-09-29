# <img src="icon.png" alt="" height="36" align="top"> cv4pve-diag

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

> **Health checks and diagnostics for Proxmox VE** — reads your whole cluster through the API and tells you what is wrong.
>
> **[Documentation](https://corsinvest.github.io/cv4pve-diag/)**
>
> Prefer a web interface with scheduled runs? cv4pve-diag also runs inside [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin), as its [Diagnostics](https://corsinvest.github.io/cv4pve-admin/modules/diagnostics/) module.

---

## Why

A Proxmox VE cluster rarely breaks all at once: it drifts. A disk gets excluded from backup, a snapshot is forgotten for months, a node keeps running the old kernel after an update, `root@pam` has no second factor, an HA guest has no replica to fail over to. Nothing in the web UI shows these problems across the cluster — you find out during a restore, a failover or an audit.

cv4pve-diag reads the whole cluster and lists them in one report: **170+ checks** on cluster, nodes, storages, VMs and containers, each finding with a stable code, a severity and the resource it concerns.

It **runs outside the nodes and uses only the Proxmox VE API**: nothing to install on the cluster, no SSH, no root shell.

---

## What the report looks like

```
| Id                           | Code   | Description                                                                                        | Context | SubContext | Gravity  |
|------------------------------|--------|----------------------------------------------------------------------------------------------------|---------|------------|----------|
| access/users/root@pam        | CC0004 | root@pam has no TFA configured — full access protected only by password                            | Cluster | Access     | Critical |
| nodes/pve02/qemu/203         | CG0002 | Disk 'scsi0' disabled for backup                                                                   | Qemu    | Backup     | Critical |
| nodes/pve01                  | WN0013 | Node requires reboot: running kernel '6.8.12-20-pve' but newer kernel '6.8.12-43-pve' is installed | Node    | Reboot     | Warning  |
| nodes/pve01/storage/pbs01    | WS0009 | Storage usage 80% - 2.58 TB of 3.22 TB                                                             | Storage | Usage      | Warning  |
| nodes/pve01/storage/datapool | WS0002 | Image Orphaned 51.54 GB file vm-106-disk-1                                                         | Storage | Image      | Warning  |
| cluster                      | IC0002 | No HA resources configured — VMs will not automatically restart on node failure                    | Cluster | HA         | Info     |
```

Rows are sorted by severity. Every code is explained in the [checks catalog](https://corsinvest.github.io/cv4pve-diag/checks/).

---

## Compliance evidence

100+ findings are tagged with the controls they relate to. Run it with `--compliance=Nis2` (or any of 18 frameworks) and the report keeps only the findings that matter for that standard, each with its control in a `ControlId` column — a report you can hand to an auditor, produced from the cluster itself.

```
| Id                    | Code   | Description                                                                                        | Gravity  | ControlId |
|-----------------------|--------|----------------------------------------------------------------------------------------------------|----------|-----------|
| access/users/root@pam | CC0004 | root@pam has no TFA configured — full access protected only by password                            | Critical | Art.21(j) |
| nodes/pve02/qemu/203  | CG0002 | Disk 'scsi0' disabled for backup                                                                   | Critical | Art.21(c) |
| nodes/pve01           | WN0013 | Node requires reboot: running kernel '6.8.12-20-pve' but newer kernel '6.8.12-43-pve' is installed | Warning  | Art.21(e) |
| cluster               | IC0002 | No HA resources configured — VMs will not automatically restart on node failure                    | Info     | Art.21(c) |
```

(Context and SubContext columns left out here for width.)

ISO 27001 · **NIS2** · NIS2 Implementing Regulation · **ACN NIS2 Italy** · DORA · PCI DSS · GDPR · AgID · ENS · BSI C5 · BSI IT-Grundschutz · ISO 22301 · SOC 2 · NIST 800-53 · ISO 27017 · ISO 27018 · CIS · NIST CSF — see the [compliance mapping](https://corsinvest.github.io/cv4pve-diag/compliance/).

---

## Features

- **Every format** — Text, HTML, Excel, JSON, Markdown, from one self-contained binary.
- **Profiles** — `--fast` for a quick scan of a large cluster, `--full` for audits: every optional check, passing checks too.
- **Ignore rules** — hide the findings you have accepted, and still show them for review when you want.
- **CVE lookup** — optional NVD check of the installed Proxmox VE version.
- **Honest about what it cannot see** — checks the token's privileges first and says in the report what the analysis does not cover.
- **Keeps running with a node down** — give it more than one host and it uses the first that answers.

---

## Quick start

```bash
# Windows
winget install Corsinvest.cv4pve.diag

# Linux (other platforms and packages: see the documentation)
wget https://github.com/Corsinvest/cv4pve-diag/releases/latest/download/cv4pve-diag-linux-x64.zip
unzip cv4pve-diag-linux-x64.zip && chmod +x cv4pve-diag

# Run against any node of the cluster, with an API token
./cv4pve-diag --host=pve1.local --api-token='diag@pve!audit=<uuid>' --output-file=report.html execute
```

The API token needs the privileges listed in [Permissions](https://corsinvest.github.io/cv4pve-diag/permissions/) — note that `PVEAuditor` alone cannot see backups.

---

## Documentation

| | |
|---|---|
| [Getting started](https://corsinvest.github.io/cv4pve-diag/getting-started/) | Install, connect, run |
| [Permissions](https://corsinvest.github.io/cv4pve-diag/permissions/) | Creating the user and API token, required privileges |
| [Reading the report](https://corsinvest.github.io/cv4pve-diag/reading-the-report/) | Columns, severities, output formats, Excel |
| [Checks](https://corsinvest.github.io/cv4pve-diag/checks/) | Every check with code, severity and meaning |
| [Settings](https://corsinvest.github.io/cv4pve-diag/settings/) | Thresholds, profiles, performance, CVE lookup |
| [Ignore rules](https://corsinvest.github.io/cv4pve-diag/ignored-issues/) | Silence findings you have accepted |
| [Compliance](https://corsinvest.github.io/cv4pve-diag/compliance/) | Framework mapping and auditor reports |
| [Troubleshooting](https://corsinvest.github.io/cv4pve-diag/troubleshooting/) | See what the tool is doing when something goes wrong |

---

## Related tools

Use `cv4pve-diag` to know *what is wrong*, [cv4pve-report](https://corsinvest.github.io/cv4pve-report/) to know *what you have*. The whole suite: [corsinvest.it/cv4pve](https://www.corsinvest.it/en/cv4pve/).

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/en/cv4pve/).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
