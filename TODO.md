# TODO

## Checks to Implement

### Node
- [ ] **Large time difference between nodes** — extend NTP check to compare node times against each other, not just client; requires parallel node data fetch (current sequential fetch introduces artificial time skew between nodes)

### VM (QEMU)
- [ ] **VM with outdated machine type** — `machine=` value is older than the latest available on the node (e.g. `pc-i440fx-6.2` when `pc-i440fx-8.2` is available); check via `nodes/{node}/capabilities/qemu/machines`
- [ ] **QEMU guest agent version** — read agent version via `agent/info` on running VMs; idea: flag VMs with significantly older agent versions compared to others (requires knowing latest version per OS/distro — complex)
- [ ] **VM CPU flags** — check if CPU flags relevant to security (e.g. `+spec-ctrl`, `+ssbd`) are explicitly set or missing for VMs exposed to untrusted workloads
- [ ] **VM consolidation needed** — identify nodes with low overall CPU/RAM utilization where VMs could be consolidated to free up nodes

### Network
- [ ] **Bridge with no VLAN awareness** — `vlan-aware=0` on a bridge used by VMs with VLAN tags

### Security
- [ ] **Firewall rules with source `0.0.0.0/0`** — overly permissive inbound rules

---

## Ideas / Improvements

### CLI profiles (--fast / --full)
- [ ] Add `Enabled` flag to `SettingsRrd` and agent check settings
- [ ] Add static `Settings.Fast()`, `Settings.Standard()`, `Settings.Full()` methods (same pattern as cv4pve-report)
- [ ] Add `--fast` / `--full` options to `execute` and `create-settings` commands in `Program.cs`
- Fast = no RRD, no SMART, no agent check; Full = everything enabled, RRD on week timeframe

---

## Ideas / Future Checks

### Node (API)
- [ ] **Node memory overcommit** — sum of all VM/CT allocated RAM exceeds physical node RAM (`nodes/{node}/status` + VM configs)
- [ ] **Corosync ring with packet loss** — corosync stats show retransmits/errors on a ring (`cluster/log`)

### Node (API)
- [ ] **Multipath degraded / lost communication** — check multipath storage paths via node hardware/storage info; flag nodes with degraded or lost multipath paths

### Node (SSH — future)
- [ ] **OOM killer activity** — `dmesg` or `/var/log/kern.log` contains OOM kill events
- [ ] **Corosync totem retransmit count** — `corosync-cfgtool -s` shows retransmits > 0
- [ ] **ZFS ARC hit rate too low** — `arc_summary` or `/proc/spl/kstat/zfs/arcstats` shows hit rate below threshold
- [ ] **Ceph OSD down** — `ceph osd stat` shows OSDs not up/in
- [ ] **Ceph health not OK** — `ceph health` returns WARN or ERR

### Cluster (API)
- [ ] **Datacenter backup jobs overlap** — multiple jobs scheduled at same time targeting same storage
- [ ] **Backup history anomaly** — read vzdump task logs for the last N days (configurable), compute per-VM average duration and size, warn when latest backup deviates significantly (duration too long, size drop too large). Requires reading task logs via `nodes/{node}/tasks?typefilter=vzdump` + log content per task.




