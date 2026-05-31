# TODO

## Checks to Implement

### Node
- [ ] **Large time difference between nodes** — extend NTP check to compare node times against each other, not just client; requires parallel node data fetch (current sequential fetch introduces artificial time skew between nodes)

### VM (QEMU)
- [ ] **VM with outdated machine type** — `machine=` value is older than the latest available on the node (e.g. `pc-i440fx-6.2` when `pc-i440fx-8.2` is available); check via `nodes/{node}/capabilities/qemu/machines`

### Network

---

## Ideas / Optimizations

### Backup content memory usage
- [ ] **Limit `_backupContentByStorage` retention** — on large clusters with long backup history the in-memory content can grow to 10k-50k entries. Filter entries older than `Max(MaxAgeDays, RecentDays)` days when loading, since orphaned backup check only needs recent entries and old ones are irrelevant for all checks.

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

---

## Ceph (deferred)

Ceph checks are currently out of scope. When prioritised, the following are good candidates:

- [ ] **Ceph health not OK** — `GET /cluster/ceph/status` returns `HEALTH_WARN` / `HEALTH_ERR`. Critical findings include OSDs down, PGs inactive/degraded, mon quorum lost.
- [ ] **Ceph OSD down** — list OSDs via API, flag those not `up` and `in`.
- [ ] **Ceph PG degraded / inactive / stale** — count PGs not in `active+clean` state; warn above threshold.
- [ ] **Ceph storage near full** — pool usage approaching `mon_osd_full_ratio` or `nearfull_ratio`.
- [ ] **Ceph MGR / MON quorum incomplete** — fewer running than configured.
- [ ] **Ceph version skew across nodes** — different ceph versions installed on different nodes (similar to existing PVE version mismatch check).
- [ ] **Ceph slow ops** — `ceph status` reports slow ops on OSDs.
- [ ] **Ceph scrub stuck / never completed** — last scrub timestamp older than threshold per pool.

Compliance mapping for Ceph checks should reuse the existing resilience / data-protection controls (A.5.30, NIS2 Art.21(c), DORA Art.12, GDPR 32(1)(b/c), CIS 11, NIST PR.IR-04 / PR.DS-11 / RC.RP-01).

---

## Compliance — future work

### Additional standards

The 9 current frameworks cover most EU + US-private scenarios. Sector-specific or country-specific standards to consider when there is concrete demand:

- [ ] **HIPAA Security Rule** (US healthcare) — large overlap with NIST CSF; mostly procedural. Map to data-at-rest / data-in-transit / access control checks.
- [ ] **HDS** (Hébergeur de Données de Santé, FR) — French healthcare hosting baseline.
- [ ] **C5** (BSI Germany, cloud) — German legal equivalent of ISO 27017; technical subset overlaps.
- [ ] **ENS** (Esquema Nacional de Seguridad, ES) — Spanish PA baseline, equivalent of AgID.
- [ ] **TISAX** — automotive industry baseline (VDA ISA).
- [ ] **HITRUST CSF** — health-IT US.
- [ ] **FedRAMP** — US federal cloud authorisation baseline.
- [ ] **BSI IT-Grundschutz** (Germany) — fine-grained module catalogue; only a small subset is automatable.

### Tests for compliance

Today the xUnit suite (66 tests) does not cover the compliance layer. Worth adding:

- [ ] **Catalog integrity** — no duplicate `(Standard, ControlId)`; every declared standard has ≥1 control; no empty `ControlTitle`; lookup by `(standard, id)` round-trips for every entry.
- [ ] **Mapping reachability** — every `ComplianceMapping` referenced in any `DiagnosticEngine.*.cs` exists in the catalog (caught at compile-time today, but a runtime test guards against reflection-driven dynamic lookups in the future).
- [ ] **`--compliance` filter** — running with `--compliance=Iso27001` on a snapshot fixture produces only findings whose `Compliance` list contains an `Iso27001` mapping; the report includes a `ControlId` column.
- [ ] **`IncludeOkResult` behaviour** — with the flag off the output has zero `Gravity = Ok` entries; with it on every check that ran emits exactly one `Ok` when the condition holds.
- [ ] **Excel compliance sheet** — when `--compliance` is passed, the Summary sheet has the `Control Id` column and the header shows the standard name.
- [ ] **No-op `CreateResult(isOk: true, compliance: [])`** — does not add anything to `_result` when `IncludeOkResult` is off, regardless of whether compliance is empty.

