---
title: Container (LXC) checks
description: "Checks on every LXC container: privileges, AppArmor, features, memory, backup, snapshots, HA, firewall, usage."
---

Checks run on every LXC container. Several codes are shared with VMs and mean the same thing there. How to read the codes: [Overview](/cv4pve-diag/checks/).

## State and configuration

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `IG0010` | Status | Info | Config changes pending reboot to take effect |
| `WG0015` | Status | Warning | Container is locked and cannot be managed |
| `WG0041` | Config | Warning | Container has raw LXC config entries that bypass PVE abstractions |
| `IG0014` | Config | Info | Container has no hostname configured |
| `WG0016` | StartOnBoot | Warning | CT will not start automatically after host reboot |
| `IG0011` | Protection | Info | CT protection flag not set |
| `WG0018` | Hardware | Warning | Disk detached from CT but still in storage |

## Security and features

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0039` | Security | Warning | Container runs as privileged — root inside has host-level access |
| `CG0006` | Security | Critical | Privileged container has AppArmor disabled — no kernel confinement |
| `IG0017` | Features | Info | Unprivileged container with `nesting=1` but no `keyctl=1` — Docker/systemd may not work (replaces `WG0038`) |

## Backup and snapshots

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0017` | Backup | Warning | CT not included in any backup job |
| `CG0002` | Backup | Critical | A disk has backup disabled |
| `WG0019` | Backup | Warning | Backup files older than configured days found (protected excluded) |
| `WG0020` | Backup | Warning | No backup found in the last configured days |
| `WG0021` | AutoSnapshot | Warning | cv4pve-autosnap not configured |
| `WG0022` | AutoSnapshot | Warning | Old AutoSnap snapshots present — update required |
| `WG0023` | SnapshotOld | Warning | Snapshots older than configured age |
| `WG0035` | Snapshot | Warning | Snapshot includes RAM state — wastes disk space and blocks storage migration |
| `WG0024` | SnapshotCount | Warning | Snapshot count exceeds configured limit |

## High availability

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CG0005` | HA | Critical | Disk on non-shared storage, CT managed by HA and not replicated — migration will fail |
| `IG0015` | HA | Info | Running container not managed by HA while other guests are — will not be restarted on node failure |
| `WG0043` | Replication | Warning | HA container with disks on local storage has no enabled replication job — failover target will have no recent data |

## Network and firewall

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0033` | Network | Warning | MAC address shared with another VM/CT or interface — causes network conflicts |
| `WG0013` | Firewall | Warning | Container firewall is disabled — exposed to all bridge traffic |
| `IG0009` | Firewall | Info | Container can spoof source IP addresses |

## Tasks and resource usage

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0040` | Memory | Warning | Container has no memory limit (Memory=0) — can consume all host RAM |
| `IG0013` | Memory | Info | Container has swap disabled — OOM killer risk under memory pressure |
| `CG0003` | Tasks | Critical | Failed tasks found in the last 48 hours |
| `WG0025` | Usage | Warning/Critical | CPU usage above configured threshold |
| `WG0026` | Usage | Warning/Critical | Memory usage above configured threshold |
| `WG0027/WG0028` | Usage | Warning/Critical | Network throughput above configured threshold |
| `WG0029` | Pressure | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+) |
| `WG0030` | Pressure | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+) |
| `WG0031` | Pressure | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+) |
| `WG0032` | HealthScore | Warning/Critical | Composite health score below threshold |

Any of these can be hidden with an [ignore rule](/cv4pve-diag/ignored-issues/) on its code.
