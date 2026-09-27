---
title: Cluster checks
description: "Checks on the cluster as a whole: quorum, HA, replication, backup jobs, firewall, users, tokens and ACLs."
---

Checks on the cluster as a whole — they run once per analysis, whatever node you connect to. How to read the codes: [Overview](/cv4pve-diag/checks/).

## Backup jobs

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WC0001` | Backup | Warning | No automated backup job for any VM/CT |
| `IC0001` | Backup | Info | Backup job has no compression configured |
| `WC0002` | Backup | Warning | Neither the backup job nor its storage has a retention (prune-backups) — storage will fill up |
| `WC0017` | Backup | Warning | Enabled backup job has no schedule — will never run |
| `IC0012` | Backup | Info | Backup job is disabled |
| `WC0018` | Backup | Warning | Recent vzdump task ended with non-OK status |
| `WC0019` | Backup | Warning | Two or more enabled backup jobs run on the same storage at the same schedule (I/O contention) |

## Quorum, HA and replication

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CC0001` | Quorum | Critical | Cluster has lost quorum — VM operations may be blocked |
| `CC0002` | Quorum | Critical | Losing this single node would leave the cluster without quorum (skipped when a QDevice is configured) |
| `CC0003` | HA | Critical | HA group references nodes that are currently offline (PVE 8 and earlier — HA groups became rules in PVE 9) |
| `CC0005` | HA | Critical | HA service is in error state — manual recovery required |
| `IC0002` | HA | Info | No VMs/CTs protected by HA — no automatic failover on node failure |
| `IC0003` | Replication | Info | No storage replication configured — no redundant copy across nodes |
| `WC0009` | Replication | Warning | Replication job is disabled — guest data is no longer replicated |
| `WC0010` | Replication | Warning | Enabled replication job has no schedule — it will never run |
| `IC0017` | Topology | Info | Cluster has a single node — HA / quorum / replication ineffective |

## Users, tokens and permissions

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `IC0004` | Pool | Info | Resource pool exists but has no VMs or storage assigned |
| `IC0020` | Pool | Info | Pool has members but no ACL entry at `/pool/<id>` — not used as privilege boundary |
| `CC0004` | Access | Critical | root@pam has no two-factor authentication configured |
| `WC0007` | Access | Warning | User with Administrator role has no two-factor authentication |
| `WC0005` | Access | Warning | User has Administrator role at root path `/` — prefer scoped permissions |
| `WC0006` | Access | Warning | Disabled user still has valid API tokens that should be revoked |
| `IC0005` | Access | Info | Local user has no expiration date configured |
| `IC0006` | Access | Info | API token of an enabled local (pam/pve) user has no expiration date |
| `IC0007` | Access | Info | Enabled user has no email — will not receive notifications |
| `IC0008` | Access | Info | Group has no members |
| `IC0009` | Access | Info | Custom role is not assigned in any ACL |
| `WC0013` | Access | Warning | User has Administrator role via a group but no TFA configured |
| `WC0014` | Access | Warning | Disabled user still has Administrator role on `/` |
| `IC0010` | Access | Info | Administrator role on `/` with Propagate disabled |
| `IC0011` | Access | Info | LDAP/AD/OpenID realm does not enforce TFA at realm level |
| `WC0015` | Access | Warning | root@pam API token has no privilege separation |
| `WC0016` | Access | Warning | User has expiration date in the past but is still enabled |
| `IC0021` | Access | Info | API token has no comment — purpose / owner cannot be attributed at audit time |
| `WC0020` | Permissions | Info/Warning | Account cannot see part of the cluster — Info for a reduced analysis scope, Warning when missing backup privileges would make other checks report the opposite of the truth |

## Firewall

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WC0003` | Firewall | Warning | Cluster-level firewall is completely disabled |
| `WC0004` | Firewall | Warning | Inbound policy is ACCEPT — unmatched incoming traffic is let through (outbound is not judged) |
| `WC0008` | Firewall | Warning | Cluster firewall rule with overly permissive source or destination |
| `IC0013` | Firewall | Info | Cluster firewall has enabled rules but none configure logging |
| `IC0014` | Firewall | Info | Cluster firewall has 10+ disabled rules — stale configuration |

## Logs, tasks and metrics

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `IC0015` | Log | Info | 10+ error-level entries in the cluster journal |
| `IC0016` | Tasks | Info | 10%+ of recent cluster tasks failed |
| `IC0018` | Metrics | Info | No external metric server configured — only volatile RRD data |
| `IC0019` | Metrics | Info | Metric servers exist but every one of them is disabled |

## Versions

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WC0011` | Version | Warning | Online nodes run different Proxmox VE versions |
| `WC0012` | Version | Warning | Online nodes run different kernel versions |

Any of these can be hidden with an [ignore rule](/cv4pve-diag/ignored-issues/) on its code.
