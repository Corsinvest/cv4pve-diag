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

> **Health checks and diagnostics for Proxmox VE** — reads your whole cluster through the API and tells you what is wrong.
>
> 📖 **Documentation: [corsinvest.github.io/cv4pve-diag](https://corsinvest.github.io/cv4pve-diag/)**

---

## Why

A Proxmox VE cluster rarely breaks all at once: it drifts. A disk gets excluded from backup, a snapshot is forgotten for months, a node keeps running the old kernel after an update, `root@pam` has no second factor, an HA guest has no replica to fail over to. Nothing in the web UI shows these problems across the cluster — you find out during a restore, a failover or an audit.

cv4pve-diag reads the whole cluster and lists them in one report: **170+ checks** on cluster, nodes, storages, VMs and containers, each finding with a stable code, a severity and the resource it concerns. **100+ findings are mapped to 18 compliance frameworks** (ISO 27001, NIS2, DORA, PCI DSS, ACN NIS2 Italy, …).

It **runs outside the nodes and uses only the Proxmox VE API**: nothing to install on the cluster, no SSH, no root shell.

---

## Quick start

```bash
# Windows
winget install Corsinvest.cv4pve.diag

# Linux (other platforms and packages: see the documentation)
wget https://github.com/Corsinvest/cv4pve-diag/releases/latest/download/cv4pve-diag-linux-x64.zip
unzip cv4pve-diag-linux-x64.zip && chmod +x cv4pve-diag

# Run against any node of the cluster, with an API token
./cv4pve-diag --host=pve1.local --api-token='diag@pve!audit=UUID' --output-file=report.html execute
```

The API token needs the privileges listed in [Permissions](https://corsinvest.github.io/cv4pve-diag/permissions/) — note that `PVEAuditor` alone cannot see backups.

---

## Documentation

| | |
|---|---|
| [Getting started](https://corsinvest.github.io/cv4pve-diag/getting-started/) | Install, connect, run, read the report |
| [Permissions](https://corsinvest.github.io/cv4pve-diag/permissions/) | Creating the user and API token, required privileges |
| [Checks](https://corsinvest.github.io/cv4pve-diag/checks/) | Every check with code, severity and meaning |
| [Settings](https://corsinvest.github.io/cv4pve-diag/settings/) | Thresholds, profiles, performance, CVE lookup |
| [Ignore rules](https://corsinvest.github.io/cv4pve-diag/ignored-issues/) | Silence findings you have accepted |
| [Compliance](https://corsinvest.github.io/cv4pve-diag/compliance/) | Framework mapping and auditor reports |

Video tutorial: [youtube.com/watch?v=hn1nw9KXlsg](https://www.youtube.com/watch?v=hn1nw9KXlsg)

---

## Part of the cv4pve suite

Use `cv4pve-diag` to know *what is wrong*, [cv4pve-report](https://github.com/Corsinvest/cv4pve-report) to know *what you have*. [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin) runs the same diagnostics from a web interface. The whole suite: [corsinvest.it/cv4pve](https://www.corsinvest.it/en/cv4pve/).

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/en/cv4pve/).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
