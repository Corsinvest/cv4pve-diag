---
title: Getting started
description: Install cv4pve-diag, connect it to a Proxmox VE cluster, run it and read the report.
---

cv4pve-diag runs **outside the cluster** — on your workstation, a management VM or a scheduled job —
and talks only to the Proxmox VE REST API on port 8006. Nothing is installed on the nodes.

## Installation

| Platform           | Command |
| ------------------ | ------- |
| **Linux**          | `wget https://github.com/Corsinvest/cv4pve-diag/releases/latest/download/cv4pve-diag-linux-x64.zip && unzip cv4pve-diag-linux-x64.zip && chmod +x cv4pve-diag` |
| **Windows WinGet** | `winget install Corsinvest.cv4pve.diag` |
| **Windows manual** | Download `cv4pve-diag.exe-win-x64.zip` from [Releases](https://github.com/Corsinvest/cv4pve-diag/releases/latest) |
| **Arch Linux**     | `yay -S cv4pve-diag` |
| **Debian/Ubuntu**  | `sudo dpkg -i cv4pve-diag-VERSION-ARCH.deb` |
| **RHEL/Fedora**    | `sudo rpm -i cv4pve-diag-VERSION-ARCH.rpm` |
| **macOS**          | `brew tap Corsinvest/homebrew-tap && brew install cv4pve-diag` |

Binaries are self-contained: no .NET runtime to install. ARM builds (`linux-arm`, `linux-arm64`, `osx-arm64`, `win-arm64`) are on the [Releases page](https://github.com/Corsinvest/cv4pve-diag/releases/latest).

## Connect to the cluster

Create a dedicated user and API token first — see [Permissions](../permissions/). Then:

```bash
cv4pve-diag --host=pve1.local --api-token='diag@pve!audit=UUID' execute
```

Quote the token: the `!` in it is special to bash.

| Option | What it does |
|---|---|
| `--host` | One or more nodes, comma-separated: `pve1,pve2:8006,[fe80::1]`. Port defaults to 8006. At startup the first node that answers is used, so the run works while a node is down. Any node gives the view of the whole cluster. |
| `--api-token` | `USER@REALM!TOKENID=UUID`. Recommended. |
| `--username` / `--password` | Alternative to the token, e.g. `--username=diag@pve`. `--password=file:/path/secret` stores the password encrypted in that file, asking for it the first time. |
| `--validate-certificate` | Verify the node TLS certificate. **Off by default**, so the default self-signed Proxmox certificate is accepted — turn it on if the nodes have a trusted certificate. |

Long command lines can go in a parameter file, one option per line, passed with `@`:

```bash
cv4pve-diag @/etc/cv4pve/production.conf execute
```

## Run

```bash
cv4pve-diag --host=pve1 --api-token='diag@pve!audit=UUID' execute           # standard profile
cv4pve-diag --host=pve1 --api-token='diag@pve!audit=UUID' execute --fast    # quick scan of a large cluster
cv4pve-diag --host=pve1 --api-token='diag@pve!audit=UUID' execute --full    # everything, for audits
```

`--fast` and `--full` go **after** `execute`. What they change, and every threshold, is in [Settings](../settings/). Findings you have reviewed and accepted can be hidden with [ignore rules](../ignored-issues/).

## Output

`--output` (or `-o`) selects the format: `Text` (default), `Html`, `Markdown`, `Json`, `JsonPretty`, `Excel`.
With `--output-file`, the format follows the file extension unless you pass a format other than `Text`:

| Extension | Format |
|---|---|
| `.xlsx` | Excel |
| `.html` / `.htm` | Html |
| `.json` | Json |
| `.md` | Markdown |
| anything else | Text |

```bash
cv4pve-diag --host=pve1 --api-token='diag@pve!audit=UUID' --output-file=report.html execute
```

An existing output file is overwritten. Excel without `--output-file` is written to `cv4pve-diagnostic-<timestamp>.xlsx` in the current folder.

## Reading the report

```
| Id                           | Code   | Description                                           | Context | SubContext | Gravity  |
| access/users/root@pam        | CC0004 | root@pam has no TFA configured — …                    | Cluster | Access     | Critical |
| nodes/pve02/qemu/203         | CG0002 | Disk 'scsi0' disabled for backup                      | Qemu    | Backup     | Critical |
| nodes/pve01                  | WN0013 | Node requires reboot: running kernel '6.8.12-20-pve'… | Node    | Reboot     | Warning  |
| nodes/pve01/storage/datapool | WS0002 | Image Orphaned 51.54 GB file vm-106-disk-1            | Storage | Image      | Warning  |
```

| Column | Meaning |
|---|---|
| **Id** | The object the finding is about, as a Proxmox VE API path: `nodes/pve02/qemu/203` is VM 203 on node pve02. |
| **Code** | Stable identifier of the check — look it up in the [checks catalog](../checks/), use it in [ignore rules](../ignored-issues/). |
| **Description** | What is wrong, with the values found. |
| **Context** | Kind of object: `Cluster`, `Node`, `Storage`, `Qemu` (VM), `Lxc` (container). |
| **SubContext** | Area of the check: Backup, HA, Firewall, Usage, … |
| **Gravity** | `Critical` — fix now, it risks data or availability. `Warning` — wrong or risky, not yet failing. `Info` — best-practice advice. `Ok` — the check passed (only with [`IncludeOkResult`](../settings/#including-ok-results)). |

Two more columns appear only when asked for:

| Column | When | Meaning |
|---|---|---|
| **ControlId** | `--compliance=<standard>` | The controls of that standard the finding relates to — see [Compliance](../compliance/). |
| **IgnoredIssue** | `--ignored-issues-show` | `X` when an [ignore rule](../ignored-issues/) matches the finding. Without the option, those findings are left out of the report. |

The **Excel** report puts the same data in one `Summary` sheet, under a header with the date,
the run duration, the cv4pve-diag version, the analysed nodes and, with `--compliance`, the selected
standard. Its table has filters on every column, and always includes the `Ignored Issue` column.

Rows are sorted by gravity, then context. If the report contains `WC0020`, `WG0042` or `CU0001`, part of the
cluster could not be read — the rest of the report is incomplete for that part. See [Permissions](../permissions/).

## Automation

The exit code is `0` whenever the analysis completes, **whatever the findings**; it is `1` only if the run
itself fails (connection, authentication, invalid options). To fail a scheduled job or a pipeline on
critical findings, read the JSON output:

```bash
cv4pve-diag --host=pve1 --api-token="$PVE_TOKEN" --output-file=report.json execute
if jq -e 'any(.[]; .Gravity == "Critical")' report.json > /dev/null; then
  echo "Critical findings in the Proxmox VE cluster"; exit 1
fi
```

Each JSON entry has the fields `Id`, `Code`, `Description`, `Context`, `SubContext` and `Gravity`.

:::tip[Prefer a web interface?]
[cv4pve-admin](https://corsinvest.github.io/cv4pve-admin/modules/diagnostics/) runs the same diagnostics
on a cron schedule from its web UI, with thresholds edited in the browser and PDF / Excel reports —
no scripts to maintain.
:::

## Network access

Besides the Proxmox VE API, cv4pve-diag connects to:

- `api.github.com`, at most once every 24 hours, to tell you when a newer release exists. The result is cached in `~/.cv4pve/update-check.json`; if GitHub is unreachable the run is not affected.
- `services.nvd.nist.gov`, only when the CVE lookup is enabled (`Cve.NvdEnabled`, or `--full`). See [CVE scanning](../settings/#cve-scanning).

:::note[Single-node hosts]
Single-node setups get resilience findings (no HA, no replication, single-node topology) on every run — by design, since a single node does not meet the business-continuity controls those checks map to. On lab and dev hosts, silence them with [ignore rules](../ignored-issues/). See [Single-node setups and compliance](../compliance/#single-node-setups-and-compliance).
:::
