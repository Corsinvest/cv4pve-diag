---
title: VM (QEMU) checks
description: "Checks on every QEMU virtual machine: agent, hardware, CPU, disks, backup, snapshots, HA, firewall, usage."
---

Checks run on every QEMU virtual machine. Several codes are shared with containers and mean the same thing there. How to read the codes: [Overview](/cv4pve-diag/checks/).

## State and configuration

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CG0001` | VM State | Critical | Hibernated VM state left in pending — VM was suspended and never resumed (not when hibernated on purpose) |
| `IG0010` | Status | Info | Config changes pending reboot to take effect |
| `WG0015` | Status | Warning | VM is locked and cannot be managed |
| `WG0001` | OS | Warning | VM OS type is not configured (or left as Other) |
| `WG0002` | OSNotMaintained | Warning | Guest OS has reached end of life |
| `WG0016` | StartOnBoot | Warning | VM will not start automatically after host reboot |
| `IG0011` | Protection | Info | VM protection flag not set |

## Guest agent

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0003` | Agent | Warning | Guest agent not configured |
| `WG0004` | Agent | Warning | Agent enabled but not responding inside guest |
| `WG0014` | Agent | Warning | Template has QEMU agent enabled — unused on templates |

## Hardware and drivers

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `IG0001` | VirtIO | Info | SCSI controller is not VirtIO — lower performance |
| `IG0002` | VirtIO | Info | Disk on IDE/SATA, or on SCSI with a non-VirtIO controller |
| `IG0003` | VirtIO | Info | Network interface not using VirtIO driver |
| `WG0005` | Hardware | Warning | CD-ROM drive has an image mounted |
| `WG0008` | Hardware | Warning | Disk uses cache=unsafe — data loss risk on host crash |
| `WG0009` | Hardware | Warning | Disk uses writeback cache but backup is disabled |
| `IG0007` | Hardware | Info | VM has virtio-rng device — verify this is intentional |
| `IG0008` | Hardware | Info | VM has serial console configured — verify this is intentional |
| `IG0012` | Hardware | Info | Machine type not set or without a version (e.g. q35) — may change across PVE upgrades |
| `IG0016` | Hardware | Info | Machine type pinned to an old version — newer version available on the node |
| `WG0012` | Hardware | Warning | Host USB/PCI passthrough configured (SPICE USB excluded) — no live migration or HA |
| `WG0018` | Hardware | Warning | Disk detached from VM but still in storage |
| `IG0005` | Balloon | Info | RAM is statically allocated — no memory ballooning |
| `IG0006` | Balloon | Info | Balloon has no room to reclaim memory |
| `WG0010` | SecureBoot | Warning | Windows 11 requires UEFI (bios=ovmf) |
| `WG0011` | SecureBoot | Warning | Windows 11 requires TPM 2.0 |

## CPU

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0006` | CPU | Warning | CPU type 'host' or 'max' prevents live migration |
| `IG0004` | CPU | Info | CPU type is outdated (kvm64, also when no CPU type is set) |
| `WG0037` | CPU | Warning | Spectre/Meltdown flags missing for the node's CPU vendor (Intel: spec-ctrl, ssbd, pcid, md-clear; AMD: ibpb, virt-ssbd) |
| `WG0007` | CPU | Warning | CPU hotplug enabled on Windows guest — not supported |
| `CG0004` | CPU | Critical | CPU type 'host' or 'max' is incompatible with HA — live migration required by HA is impossible |
| `WG0036` | CPU | Warning | Node vCPU overcommit ratio exceeds configured threshold |

## Backup and snapshots

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0017` | Backup | Warning | VM not included in any backup job |
| `CG0002` | Backup | Critical | A disk has backup disabled |
| `WG0019` | Backup | Warning | Backup files older than configured days found (protected backups excluded) |
| `WG0020` | Backup | Warning | No backup found in the last configured days |
| `WG0021` | AutoSnapshot | Warning | cv4pve-autosnap not configured |
| `WG0022` | AutoSnapshot | Warning | Old AutoSnap snapshots present — update required |
| `WG0023` | SnapshotOld | Warning | Snapshots older than configured age |
| `WG0035` | Snapshot | Warning | Snapshot includes RAM state — wastes disk space and blocks storage migration |
| `WG0024` | SnapshotCount | Warning | Snapshot count exceeds configured limit |

## High availability

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CG0005` | HA | Critical | Disk on non-shared storage, VM managed by HA and not replicated — migration will fail |
| `IG0015` | HA | Info | Running VM not managed by HA while other guests are — will not be restarted on node failure |
| `WG0043` | Replication | Warning | HA VM with disks on local storage has no enabled replication job — failover target will have no recent data |

## Network and firewall

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WG0034` | Network | Warning | VM has no network interface — completely isolated from network |
| `WG0033` | Network | Warning | MAC address shared with another VM/CT or interface — causes network conflicts |
| `WG0013` | Firewall | Warning | VM firewall is disabled — exposed to all bridge traffic |
| `IG0009` | Firewall | Info | VM can spoof source IP addresses |

## Tasks and resource usage

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CG0003` | Tasks | Critical | Failed tasks found in the last 48 hours |
| `WG0025` | Usage | Warning/Critical | CPU usage above configured threshold |
| `WG0026` | Usage | Warning/Critical | Memory usage above configured threshold |
| `WG0027/WG0028` | Usage | Warning/Critical | Network throughput above configured threshold |
| `WG0029` | Pressure | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+) |
| `WG0030` | Pressure | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+) |
| `WG0031` | Pressure | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+) |
| `WG0032` | HealthScore | Warning/Critical | Composite health score below threshold |

Any of these can be hidden with an [ignore rule](/cv4pve-diag/ignored-issues/) on its code.
