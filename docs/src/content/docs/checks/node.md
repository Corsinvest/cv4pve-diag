---
title: Node checks
description: "Checks run on every node: versions, subscriptions, services, certificates, updates, disks, ZFS, network, resource usage."
---

Checks run on every node of the cluster. Cross-node checks compare each node with its peers and are skipped on a single node. How to read the codes: [Overview](/cv4pve-diag/checks/).

## Versions, updates and vulnerabilities

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WN0003` | EOL | Warning | Installed PVE version has reached end of life |
| `WN0004` | Subscription | Warning | Node has no active Proxmox VE subscription |
| `CN0001` | Version | Critical | Nodes in cluster have different PVE versions |
| `CN0002` | PackageVersions | Critical | A package installed on both nodes has a different version (old kernels ignored) |
| `IN0001` | Update | Info | Packages available for update |
| `WN0012` | Update | Warning | Security/important packages available for update |
| `WN0013` | Reboot | Warning | A newer kernel is installed than the one running — reboot needed |
| `WN0042/CN0015` | CVE | Warning/Critical | A known CVE affects the installed Proxmox VE version |

## Consistency across nodes

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WN0005` | Hosts | Warning | `/etc/hosts` entries differ between nodes (comments and spacing ignored) |
| `WN0006` | DNS | Warning | DNS configuration differs between nodes |
| `WN0007` | Timezone | Warning | Timezone differs between nodes |
| `WN0008` | AptRepositories | Warning | APT repository sources differ between nodes |
| `WN0014` | NTP | Warning | Node clock differs by more than 60 s from the clock of the machine running cv4pve-diag |
| `WN0045` | NTP | Warning | Node clock drifts > 5s from another cluster node (corosync / HA / log correlation risk) |
| `WN0015` | CPUCompatibility | Warning | Nodes have different x86-64 feature levels — live migration may fail |

## Services, certificates and firewall

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WN0002` | Status | Warning | Node is not reachable |
| `WN0001` | Firewall | Warning | Cluster firewall enabled but individual node has it disabled |
| `WN0011` | Service | Warning | A required system service is not running |
| `CN0003` | Certificates | Critical | TLS certificate has expired |
| `WN0023` | Certificates | Warning | TLS certificate expires within 30 days |
| `IN0004` | Certificates | Info | Web UI (port 8006) uses the default Proxmox certificate or a self-signed one — no custom `pveproxy-ssl.pem` |
| `IN0002` | IOMMU | Info | IOMMU disabled — PCI passthrough will not work |

## Network

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WN0009` | Network | Warning | Physical NIC MTU differs between nodes |
| `WN0010` | Network | Warning | Physical NIC in use (bridge, bond, VLAN or own IP) is down |
| `WN0034` | Network | Warning | Bond has fewer than two slaves — no link redundancy |
| `WN0046` | Network | Warning | VM/CT uses a VLAN (tag or trunk) not in the `bridge-vids` of its VLAN-aware bridge |
| `WN0047` | Network | Warning | Bridge used by a VM/CT is missing on another node — migration / HA recovery fails |

## Disks: S.M.A.R.T., ZFS, LVM-thin

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `WN0016` | S.M.A.R.T. | Warning | Disk reports a SMART health problem |
| `WN0019/CN0007` | S.M.A.R.T. | Warning/Critical | Disk temperature exceeds configured threshold |
| `WN0020` | S.M.A.R.T. | Warning | Disk has reallocated sectors — disk may be failing |
| `CN0008` | S.M.A.R.T. | Critical | Disk has pending sectors — imminent data loss risk |
| `CN0009` | S.M.A.R.T. | Critical | Disk has offline uncorrectable sectors |
| `WN0021` | S.M.A.R.T. | Warning | Disk has UDMA CRC errors — check cable/controller |
| `WN0022` | S.M.A.R.T. | Warning | Disk has reported uncorrectable errors |
| `WN0017` | SSD Wearout | Warning | SSD does not expose wear data |
| `WN0018` | SSD Wearout | Warning/Critical | SSD wearout consumed above threshold |
| `CN0010` | Zfs | Critical | ZFS pool is not in ONLINE state |
| `CN0012` | Zfs | Critical | ZFS pool vdev is in a degraded or faulted state |
| `WN0025` | Zfs | Warning | ZFS pool vdev has accumulated read/write/checksum errors |
| `WN0024` | Zfs | Warning | ZFS pool reports errors |
| `WN0026/CN0013` | LvmThin | Warning/Critical | LVM-thin metadata usage is high — full metadata causes data corruption |

## Replication and tasks

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `CN0004` | Replication | Critical | Replication job has errors |
| `CN0005` | Tasks | Critical | Failed tasks found in the last 48 hours |

## Resource usage

| Code | SubContext | Gravity | Description |
|---|---|---|---|
| `IN0003` | Consolidation | Info | Node CPU and RAM utilization both below threshold — consider consolidating VMs |
| `WN0036` | Memory | Warning | Sum of RAM allocated to VMs and containers exceeds physical node RAM |
| `WN0027` | Usage | Warning/Critical | CPU usage above configured threshold |
| `WN0038` | Usage | Warning/Critical | Memory usage above configured threshold |
| `WN0039/WN0040` | Usage | Warning/Critical | Network throughput above configured threshold |
| `WN0028` | Usage | Warning/Critical | CPU IOWait above configured threshold — indicates storage bottleneck |
| `WN0029` | Usage | Warning/Critical | Node root filesystem usage above configured threshold |
| `WN0030` | Usage | Warning/Critical | Node SWAP usage above configured threshold — indicates RAM pressure |
| `WN0031` | Pressure | Warning/Critical | Linux PSI CPU pressure above threshold (PVE 9.0+) |
| `WN0032` | Pressure | Warning/Critical | Linux PSI I/O full pressure above threshold (PVE 9.0+) |
| `WN0033` | Pressure | Warning/Critical | Linux PSI memory full pressure above threshold (PVE 9.0+) |
| `WG0032` | HealthScore | Warning/Critical | Composite health score below threshold |

Any of these can be hidden with an [ignore rule](/cv4pve-diag/ignored-issues/) on its code.
