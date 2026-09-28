---
title: Diagnostic checks
description: How cv4pve-diag checks are coded and organised, and where to find each of them.
---

cv4pve-diag runs 170+ checks, grouped by the kind of object they look at. Each area has its own page:

| Area | What is checked |
|---|---|
| [Cluster](/cv4pve-diag/checks/cluster/) | The cluster as a whole: quorum, HA, replication, backup jobs, firewall, users, tokens, ACLs |
| [Node](/cv4pve-diag/checks/node/) | Each node: versions and updates, consistency across nodes, services, certificates, network, disks, resource usage |
| [Storage](/cv4pve-diag/checks/storage/) | Each storage: availability, capacity and thin provisioning, orphaned images and backups |
| [VM (QEMU)](/cv4pve-diag/checks/vm/) | Each virtual machine: configuration, guest agent, hardware, CPU, backup and snapshots, HA, network, usage |
| [Container (LXC)](/cv4pve-diag/checks/container/) | Each container: configuration, privileges and AppArmor, backup and snapshots, HA, network, usage |

## Reading the tables

- **Code** — short alphanumeric identifier, structured as `<Severity><Area><NNNN>` (see [Code nomenclature](#code-nomenclature) below). Use this code with [ignore rules](/cv4pve-diag/ignored-issues/) to suppress specific findings.
- **SubContext** — the area of the check, the same value as the SubContext column of the report.
- **Gravity** — the severity emitted when the check fails. Some threshold checks (CPU, memory, PSI, disk usage, …) can emit either Warning or Critical depending on the value.

> Checks tagged with compliance controls (ISO 27001 / NIS2 / DORA / PCI DSS, …) attach the mapping to the finding. See [Compliance Mapping](/cv4pve-diag/compliance/).

> With `IncludeOkResult: true` in [Settings](/cv4pve-diag/settings/#general), every check also emits an Ok result when it passes — useful for full audit reports.

### Code nomenclature

Every check has a code shaped as **`<Severity><Area><NNNN>`** — two letters followed by a four-digit sequence number.

**First letter — Severity:**

| Letter | Meaning  | When it's used |
|---|---|---|
| **`C`** | Critical | The condition blocks normal operation or risks data loss (e.g. quorum lost, disk failing, certificate expired). |
| **`W`** | Warning  | The condition is incorrect or risky but the cluster is still functioning (e.g. firewall off, no backup retention, NIC down). |
| **`I`** | Info     | The condition is suboptimal, informative, or a best-practice violation (e.g. no metric server, pool without ACL, disk not on VirtIO). |

**Second letter — Area:**

| Letter | Meaning  | Scope |
|---|---|---|
| **`C`** | Cluster  | Cluster-wide concerns: HA, quorum, backup jobs, ACL, TFA, firewall options, cluster log. |
| **`N`** | Node     | Per-node: subscription, NTP, services, disks (SMART/ZFS/LVM-thin), replication state, kernel/PVE version. |
| **`S`** | Storage  | Per-storage: reachability, usage threshold, orphans, thin overcommit. |
| **`G`** | Guest    | Per-VM/CT: agent, config hygiene, hardware, snapshots, backup coverage, OS support. |

**Sequence number** — Four digits assigned in the order a check was introduced inside its `<Severity><Area>` family. Numbers are stable: once a code is published it never changes meaning, even if the check itself is removed (codes are not reused).

**Examples:**

| Code | Severity | Area | Reading |
|---|---|---|---|
| `CC0001` | Critical | Cluster | First Critical-Cluster check (quorum lost). |
| `CN0010` | Critical | Node | Tenth Critical-Node check (ZFS pool not ONLINE). |
| `WG0017` | Warning  | Guest   | 17th Warning-Guest check (no vzdump backup configured). |
| `WS0002` | Warning  | Storage | Second Warning-Storage check (orphaned disk image). |
| `IC0017` | Info     | Cluster | 17th Info-Cluster check (single-node topology). |
| `IG0016` | Info     | Guest   | 16th Info-Guest check (machine type outdated). |
| `WN0045` | Warning  | Node    | 45th Warning-Node check (cross-node clock drift). |

The first letter is always the severity of the finding. Checks with a warning and a critical threshold — CPU, memory, PSI, storage usage, health score, SSD wearout and others — have two codes with the same number: a `W` code above the warning threshold and a `C` code above the critical one, e.g. `WG0025`/`CG0025` for VM CPU usage. The tables list them together.

Exceptions to the scheme:

- **`CU0001`** — `U` is not an area: it is emitted by the engine when a cluster resource cannot be classified (see [Meta codes](#meta-codes)).
- **`WG0042`** — reported on any object, not only guests, whenever an API call fails (see [Meta codes](#meta-codes)).
- **`WN0044`**/**`CN0044`** (ZFS pool usage) are reported with Context `Storage`, but their Id is the node with the pool name, e.g. `nodes/pve1 (rpool)`: an ignore rule must match that Id.
- **`WC0020`** is reported as Info or Warning depending on which privilege is missing.

---

## Checks that depend on privileges

:::note
**Backup checks and privileges.** `WG0019`, `WG0020` and `WS0003` read the backup files listed by
each storage. Proxmox requires both `Datastore.AllocateSpace` (on the storage) and `VM.Backup` (on
the guest) to include a backup volume in that listing — `Datastore.Audit` alone is enough to call
the endpoint but not to see the volumes, and PVE filters them out silently rather than returning
an error. The `PVEAuditor` role does not grant either privilege.

When they are missing, `WC0020` is reported as a **Warning**. If they are missing everywhere the
three backup checks are skipped instead of flagging every guest as having no backups; if they are
granted only on part of `/storage` or `/vms`, the checks still run, and their results for the
rest of the cluster cannot be trusted.

In the same way, `WS0002` and `WS0003` are skipped when `VM.Audit` does not cover `/vms`: the
disks and backups of the guests the account cannot see would all look orphaned.

The other privileges (`VM.Audit`, `Datastore.Audit`, `Sys.Audit`, `Pool.Audit`) are reported as
**Info**: an account restricted to part of the cluster is a legitimate configuration, so the
finding states what the analysis covers rather than reporting a fault. See [Permissions](/cv4pve-diag/permissions/).
:::

## Meta codes

These codes are not regular checks — they are emitted by the engine itself when something goes wrong outside the diagnostic logic.

| Code   | SubContext | Gravity | Description                                                                                                  |
| ------ | ---------- | ------- | ------------------------------------------------------------------------------------------------------------ |
| CU0001 | Status / ApiError | Critical | A cluster resource has an unknown type, or `/cluster/resources` could not be read at all — in that case nothing else can be analysed. |
| WG0042 | ApiError   | Warning | A Proxmox VE API call failed during analysis (network error, permission denied, timeout, …); an endpoint that does not exist on the installed Proxmox VE version (HTTP 501) is skipped silently. The affected check was skipped; the underlying call/endpoint is reported in the description. |

---

> All checks can be suppressed via [ignore rules](/cv4pve-diag/ignored-issues/). Use the `ErrorCode` field to target specific checks precisely.
