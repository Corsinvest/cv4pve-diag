---
title: Storage checks
description: "Checks on storages: availability, usage, orphaned images and backups, thin provisioning, backup targets."
---

Checks on every storage, on each node that can use it. How to read the codes: [Overview](/cv4pve-diag/checks/).

## Availability

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CS0001` | Status | Critical | Storage is not accessible on a node (shared storages are checked on every node) |
| `WS0008` | Status | Warning | Storage is disabled but an enabled backup job or a guest still uses it |
| `WS0005` | Shared | Warning | Shared storage only mounted on one node (not when restricted to one node by 'nodes') |

## Capacity

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WS0001` | Usage | Warning/Critical | Storage usage above configured threshold |
| `WS0004` | ThinOvercommit | Warning | Allocated disk space exceeds physical capacity on a node (thin provisioning) |
| `WN0044` | Zfs | Warning/Critical | ZFS pool disk usage above configured threshold |

## Backups and orphaned data

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WS0003` | Backup | Warning | Backup files whose VMID no longer exists (one finding per VMID and storage) |
| `WS0006` | Backup | Warning | No storage has 'backup' content type — backups cannot be stored |
| `WS0007` | Backup | Warning | Enabled backup job storage not available on a node it runs on — VMs there not backed up |
| `WS0002` | Image | Warning | Disk image or container volume not attached to any VM/CT on that node |

Any of these can be hidden with an [ignore rule](/cv4pve-diag/ignored-issues/) on its code.
