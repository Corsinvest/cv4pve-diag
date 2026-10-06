---
title: "Read the cv4pve-diag report: columns and output formats"
description: The output formats of cv4pve-diag and what each column of the report means.
sidebar:
  label: Reading the report
---

## Columns

```
| Id                           | Code   | Description                                           | Context | SubContext | Gravity  |
| access/users/root@pam        | CC0004 | root@pam has no TFA configured: …                     | Cluster | Access     | Critical |
| nodes/pve02/qemu/203         | CG0002 | Disk 'scsi0' disabled for backup                      | Qemu    | Backup     | Critical |
| nodes/pve01                  | WN0013 | Node requires reboot: running kernel '6.8.12-20-pve'… | Node    | Reboot     | Warning  |
| nodes/pve01/storage/datapool | WS0002 | Image Orphaned 51.54 GB file vm-106-disk-1            | Storage | Image      | Warning  |
```

| Column | Meaning |
|---|---|
| **Id** | The object the finding is about, as a Proxmox VE API path: `nodes/pve02/qemu/203` is VM 203 on node pve02. |
| **Code** | Stable identifier of the check: look it up in the [checks catalog](../checks/), use it in [ignore rules](../ignored-issues/). |
| **Description** | What is wrong, with the values found. |
| **Context** | Kind of object: `Cluster`, `Node`, `Storage`, `Qemu` (VM), `Lxc` (container). |
| **SubContext** | Area of the check: Backup, HA, Firewall, Usage, … |
| **Gravity** | `Critical`: fix now, it risks data or availability. `Warning`: wrong or risky, not yet failing. `Info`: best-practice advice. `Ok`: the check passed (only with [`IncludeOkResult`](../settings/#general)). |

Two more columns appear only when asked for:

| Column | When | Meaning |
|---|---|---|
| **ControlId** | `--compliance=<standard>` | The controls of that standard the finding relates to: see [Compliance](../compliance/). |
| **IgnoredIssue** | `--ignored-issues-show` | `X` when an [ignore rule](../ignored-issues/) matches the finding. Without the option, those findings are left out of the report. |

The **Excel** report puts the same data in one `Summary` sheet, under a header with the date,
the run duration, the cv4pve-diag version, the analysed nodes and, with `--compliance`, the selected
standard. Its table has filters on every column, and always includes the `Ignored Issue` column.

Rows are sorted by gravity, then context and subcontext. On a single Proxmox VE host, `IC0017` (single-node topology) is reported on every run by design: see [Single-node setups](/cv4pve-diag/compliance/#single-node-setups-and-compliance); on a lab host, hide it with an [ignore rule](/cv4pve-diag/ignored-issues/). If the report contains `WC0020`, `WG0042` or `CU0001`, part of the
cluster could not be read: the rest of the report is incomplete for that part. See [Permissions](../permissions/).

## Output formats

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
cv4pve-diag --host=pve01 --api-token='diag@pve!audit=<uuid>' --output-file=report.html execute
```

Each JSON entry has the fields `Id`, `Code`, `Description`, `Context`, `SubContext` and `Gravity`, plus `ControlId` with `--compliance` and `IgnoredIssue` with `--ignored-issues-show`. An existing output file is overwritten. Excel without `--output-file` is written to `cv4pve-diagnostic-<timestamp>.xlsx` in the current folder.
