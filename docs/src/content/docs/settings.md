---
title: Settings
description: Every field of the cv4pve-diag settings file — default, effect and the checks it drives.
---

cv4pve-diag works with sensible defaults. To change thresholds, turn optional checks on or off, or tune
performance, pass a JSON settings file with `--settings-file`.

## Profiles

Three built-in profiles cover the common cases. Use them on `execute` — they go **after** the command —
or as the starting point of a settings file:

| Profile | Option | What it does | For |
|---|---|---|---|
| **Fast** | `--fast` | Skips backup content, snapshots and LVM-thin metadata | Quick scan of large clusters |
| **Standard** | *(default)* | The defaults below | Daily checks |
| **Full** | `--full` | Also S.M.A.R.T. details, ZFS pool details, NVD CVE lookup (needs internet access) and Ok results | Audits, full verification |

A `--settings-file` always wins over `--fast` / `--full`.

## Settings file

`create-settings` writes `settings.json` in the current folder, with the defaults or with a profile, and
prints the accepted values of `TimeFrame` and `Consolidation`:

```bash
cv4pve-diag create-settings          # Standard
cv4pve-diag create-settings --fast   # Fast
cv4pve-diag create-settings --full   # Full
```

The file is plain JSON. Unknown fields are ignored and omitted fields keep their default, so a file can
hold only what you change:

```json
{
  "Backup": { "RecentDays": 1 },
  "Snapshot": { "MaxAgeDays": 14 },
  "Cve": { "NvdEnabled": true }
}
```

Fields are listed below as `Group.Field`. Thresholds are pairs `{ "Warning": …, "Critical": … }`: the check
warns when the value reaches `Warning` and becomes critical when it reaches `Critical` (for the health
score, when it falls *below* them). A level set to `0` is off; `0` / `0` turns the check off.

## General

| Field | Default | What it does |
|---|---|---|
| `MaxParallelRequests` | `5` | API requests run in parallel. Higher is faster but loads Proxmox VE and uses more memory — 5 to 15 is a reasonable range; `1` runs sequentially. |
| `ApiTimeout` | `0` | Per-request timeout in seconds; `0` keeps the default of about 100 s. Raise it on slow or high-latency clusters. |
| `IncludeOkResult` | `false` | Also report passing checks, with gravity `Ok` — for audit reports that must show what was verified. A few checks never report Ok: the S.M.A.R.T. attribute checks, LVM-thin metadata, `WC0020`, and any threshold set to `0/0`. |

## Backup

| Field | Default | What it does | Check |
|---|---|---|---|
| `Backup.Enabled` | `true` | Reads the backup files on every storage. `false` skips that read and the checks below. | — |
| `Backup.MaxAgeDays` | `60` | Warns about backup files older than N days (protected backups excluded). `0` turns it off. | `WG0019` |
| `Backup.RecentDays` | `7` | Warns when a guest has no backup in the last N days. `0` turns it off. | `WG0020` |

Orphaned backup files (`WS0003`) are also found only when `Backup.Enabled` is on.

## Snapshot

| Field | Default | What it does | Check |
|---|---|---|---|
| `Snapshot.Enabled` | `true` | Reads the snapshots of every guest (one API call per VM/CT). `false` skips all snapshot checks. | `WG0021`, `WG0022`, `WG0023`, `WG0024`, `WG0035` |
| `Snapshot.MaxAgeDays` | `30` | Warns about snapshots older than N days. `0` turns it off. | `WG0023` |
| `Snapshot.MaxCount` | `10` | Warns when a guest has more than N snapshots. `0` turns it off. | `WG0024` |

## Storage

| Field | Default | What it does | Check |
|---|---|---|---|
| `Storage.Threshold` | `70` / `85` | Usage % of storages, of the node root filesystem, of node swap and of ZFS pools. | `WS0009`/`CS0009`, `WN0029`/`CN0029`, `WN0030`/`CN0030`, `WN0044`/`CN0044` |
| `Storage.Rrd` | — | Written by `create-settings` but not used: storage checks read the current usage. | — |

## Node

| Field | Default | What it does | Check |
|---|---|---|---|
| `Node.Cpu` | `70` / `85` | CPU usage % over the RRD window. | `WN0027`/`CN0027` |
| `Node.Memory` | `70` / `85` | Memory usage %. | `WN0038`/`CN0038` |
| `Node.Network` | `0` / `0` (off) | Network throughput in bytes/s, in and out. | `WN0039`/`CN0039`, `WN0040`/`CN0040` |
| `Node.IoWait` | `10` / `25` | Average CPU I/O wait % — a sign of a storage bottleneck. | `WN0028`/`CN0028` |
| `Node.HealthScore` | `70` / `50` | Composite score, see [health score](#health-score). Lower is worse. | `WN0048`/`CN0048` |
| `Node.Rrd.Pressure.Cpu` | `40` / `70` | PSI CPU: % of time at least one task was stalled (PVE 9.0+). | `WN0031`/`CN0031` |
| `Node.Rrd.Pressure.IoFull` | `10` / `30` | PSI I/O full (PVE 9.0+). | `WN0032`/`CN0032` |
| `Node.Rrd.Pressure.MemoryFull` | `5` / `15` | PSI memory full (PVE 9.0+). | `WN0033`/`CN0033` |
| `Node.Rrd.TimeFrame` | `Day` | RRD window for the averages: `Hour`, `Day`, `Week`, `Month`, `Year`. | — |
| `Node.Rrd.Consolidation` | `Average` | `Average` smooths peaks, `Maximum` catches them. | — |
| `Node.MaxVCpuRatio` | `4.0` | vCPUs of the VMs on the node (running or stopped, templates and containers excluded) divided by its physical CPUs, above which the node is overcommitted. | `WG0036` |
| `Node.ConsolidationCpuThreshold` | `10.0` | Current node CPU %… | `IN0003` |
| `Node.ConsolidationMemThreshold` | `20.0` | …and current node RAM % both below these: the node could be consolidated. | `IN0003` |
| `Node.Smart.Enabled` | `false` | Per-attribute S.M.A.R.T. checks — reallocated, pending, uncorrectable sectors, CRC errors, temperature. One extra API call per disk. | `WN0020`–`WN0022`, `CN0008`, `CN0009`, `WN0019`/`CN0019` |
| `Node.Smart.Temperature` | `55` / `65` | Disk temperature °C. | `WN0019`/`CN0019` |
| `Node.Smart.SsdWearout` | `70` / `85` | SSD life consumed %. Runs even when `Smart.Enabled` is off. | `WN0018`/`CN0018` |
| `Node.NodeStorage.ZfsDetail` | `false` | Per-pool vdev state and I/O errors. One API call per pool. | `CN0012`, `WN0024`, `WN0025` |
| `Node.NodeStorage.LvmThinMetadata` | `true` | LVM-thin metadata usage, fixed limits 90% / 95%. One API call per node. | `WN0026` / `CN0026` |

## VM and container

`Qemu` (VMs) and `Lxc` (containers) have the same fields, with the same defaults.

| Field | Default | What it does | Check |
|---|---|---|---|
| `Qemu.Cpu`, `Lxc.Cpu` | `70` / `85` | CPU usage % over the RRD window. | `WG0025`/`CG0025` |
| `Qemu.Memory`, `Lxc.Memory` | `70` / `85` | Memory usage %. | `WG0026`/`CG0026` |
| `Qemu.Network`, `Lxc.Network` | `0` / `0` (off) | Network throughput in bytes/s, in and out. | `WG0027`/`CG0027`, `WG0028`/`CG0028` |
| `Qemu.HealthScore`, `Lxc.HealthScore` | `60` / `40` | Composite score, see [health score](#health-score). Lower is worse. | `WG0032`/`CG0032` |
| `….Rrd.Pressure.Cpu` | `50` / `80` | PSI CPU inside the guest (PVE 9.0+). | `WG0029`/`CG0029` |
| `….Rrd.Pressure.IoFull` | `20` / `50` | PSI I/O full (PVE 9.0+). | `WG0030`/`CG0030` |
| `….Rrd.Pressure.MemoryFull` | `10` / `30` | PSI memory full (PVE 9.0+). | `WG0031`/`CG0031` |
| `….Rrd.TimeFrame`, `….Rrd.Consolidation` | `Day`, `Average` | As for nodes. | — |

## CVE

| Field | Default | What it does | Check |
|---|---|---|---|
| `Cve.NvdEnabled` | `false` | Looks up the CVEs that affect the installed `pve-manager` version in the NVD (National Vulnerability Database). Needs internet access; no API key. | `CN0042`, `WN0042` |
| `Cve.MinCvssScore` | `7.0` | Ignores CVEs below this CVSS score; `0` reports everything (very noisy). Score ≥ 9.0 is Critical (`CN0042`), anything lower Warning (`WN0042`). | — |

The lookup covers Proxmox VE itself (`cpe:2.3:a:proxmox:virtual_environment`). For the Debian packages of
the nodes run [`debsecan`](https://manpages.debian.org/bookworm/debsecan/debsecan.1.en.html) on each node:
the Proxmox VE API does not expose the full list of installed packages.

## Health score

```
Node  score = 100 - (cpu% × 0.4 + ram% × 0.4 + disk% × 0.2)
VM/CT score = 100 - (cpu% × 0.5 + ram% × 0.5)
```

PSI checks are skipped on Proxmox VE before 9.0, where those values are always zero.

## Recommended overrides

**Production cluster, conservative**

```json
{
  "Backup": { "RecentDays": 1, "MaxAgeDays": 30 },
  "Cve": { "NvdEnabled": true, "MinCvssScore": 7.0 },
  "Node": { "Smart": { "Enabled": true } }
}
```

**Audit / compliance run**

```json
{
  "IncludeOkResult": true,
  "Cve": { "NvdEnabled": true, "MinCvssScore": 4.0 }
}
```

**Lab / dev — silence noise**

```json
{
  "Snapshot": { "MaxCount": 0, "MaxAgeDays": 0 },
  "Backup": { "Enabled": false }
}
```

**Slow or high-latency connection**

```json
{
  "MaxParallelRequests": 2,
  "ApiTimeout": 30,
  "Node": { "Smart": { "Enabled": false }, "NodeStorage": { "ZfsDetail": false } }
}
```
