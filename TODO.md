# TODO

## Checks to Implement

### Node
- [ ] **Large time difference between nodes** — extend NTP check to compare node times against each other, not just client

### VM (QEMU)
- [ ] **VM with CPU type 'host' and HA enabled** — host CPU + HA is a conflict (HA requires live migration)
- [ ] **VM with very high vCPU count vs physical cores** — vCPU overcommit ratio above threshold (use node CPU count vs sum of VM sockets×cores)

### Storage
- [ ] **Backup storage not reachable from all nodes** — backup job targets a storage not mounted on all nodes
- [ ] **Storage with no backup content type** — no storage configured to hold backups

### Network
- [ ] **Bridge with no VLAN awareness** — `vlan-aware=0` on a bridge used by VMs with VLAN tags

### Security
- [ ] **Firewall rules with source `0.0.0.0/0`** — overly permissive inbound rules

---

## Ideas / Future Checks

### Node (API)
- [ ] **Node memory overcommit** — sum of all VM/CT allocated RAM exceeds physical node RAM (`nodes/{node}/status` + VM configs)
- [ ] **Corosync ring with packet loss** — corosync stats show retransmits/errors on a ring (`cluster/log`)

### Node (SSH — future)
- [ ] **OOM killer activity** — `dmesg` or `/var/log/kern.log` contains OOM kill events
- [ ] **Corosync totem retransmit count** — `corosync-cfgtool -s` shows retransmits > 0
- [ ] **ZFS ARC hit rate too low** — `arc_summary` or `/proc/spl/kstat/zfs/arcstats` shows hit rate below threshold
- [ ] **Ceph OSD down** — `ceph osd stat` shows OSDs not up/in
- [ ] **Ceph health not OK** — `ceph health` returns WARN or ERR

### VM / LXC (API)
- [ ] **VM with no network interface** — VM has no `net*` configured (isolated, no connectivity)
- [ ] **VM with duplicate MAC address** — two VMs share the same MAC (causes network issues); check across all VMs
- [ ] **LXC with no memory limit** — `Memory=0` means unbounded, can starve the node
- [ ] **VM/LXC snapshot with RAM state** — snapshot includes RAM (`vmstate=1`), wastes disk and blocks certain operations
- [ ] **VM disk on local storage with HA enabled** — HA cannot migrate VM if disk is on non-shared storage

### Cluster (API)
- [ ] **Datacenter backup jobs overlap** — multiple jobs scheduled at same time targeting same storage
