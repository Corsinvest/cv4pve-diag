# Changelog

---

## [2.0.1] — 2026-03-21

### Improvements

- Health score thresholds are now configurable separately for nodes (`Node.HealthScore`) and virtual machines/containers (`Qemu.HealthScore`, `Lxc.HealthScore`). Default thresholds: nodes `warning=70, critical=80`; VMs and containers `warning=60, critical=40`.

### Fixed

- Removed unused `HealthScore` top-level setting from `settings.json` (replaced by per-host-type settings above).

---

## [2.0.0] — 2026-03-20

### What's new

**New diagnostic checks:**

- **Thin Provisioning Overcommit** — detects when the total disk space allocated to VMs/CTs exceeds the actual physical capacity of thin-provisioned storages (LVM-thin, ZFS, Ceph). Helps prevent unexpected out-of-space failures.
- **Disk Cache Unsafe** — warns when a VM disk is configured with `cache=unsafe`. This setting improves performance but risks data loss on power failure.
- **Snapshot Count** — warns when a VM/CT has too many snapshots. Long snapshot chains degrade I/O performance.
- **Snapshot Age** — warns when snapshots are older than a configured number of days. Old snapshots are often forgotten and waste storage space.
- **Health Score** — a composite score (0–100) based on CPU, memory and disk usage. Raises a warning or critical when the score drops below configured thresholds.
- **LXC Nesting without Keyctl** — warns when a container has `nesting=1` (required for Docker inside LXC) but is missing `keyctl=1`, which is needed for full compatibility.
- **Unused disks on containers** — detached disks (`unused0`, `unused1`, …) are now detected and reported on LXC containers too, not only on VMs.
- **VM/CT locked** — warns when a VM or container is locked (e.g. during a backup or migration that did not complete).
- **CPU x86-64 compatibility** — detects mismatched CPU feature levels across cluster nodes that could prevent safe live migration.

### Settings

Two new sections in `settings.json`:

- `Snapshot` — configure max snapshot count and max snapshot age in days
- `HealthScore` — configure warning and critical score thresholds

### Breaking change for library users

The `Application` class has been replaced by `DiagnosticEngine`. If you use the NuGet package directly, update your code:

```csharp
// Before (v1.x)
var result = await Application.AnalyzeAsync(client, settings, ignoredIssues);

// After (v2.0)
var result = await new DiagnosticEngine(client, settings).AnalyzeAsync(ignoredIssues);
```

### Other changes

- License changed from GPL-3.0 to MIT
- Unused/detached disks (`unused0`, `unused1`, …) are now reported for both VMs and containers
- Fixed: unused/detached disks were incorrectly reported as "disk excluded from backup"
- Fixed: thin overcommit calculation was inflated by LXC bind mounts

---

## [1.10.0]

- Internal refactor for better performance and maintainability
- Improved async API calls throughout

---

## [1.9.1]

- Updated dependencies

---

## [1.9.0]

- Added .NET 10 support
- Improved error handling and null checks
- Added GitHub Actions publish workflows

---

## [1.7.0]

- Fixed ZFS and memory usage calculation errors

---

## [1.6.0]

- Improved memory and storage size display

---

## [1.5.3]

- Proxmox VE 8.2 compatibility fixes

---

## [1.5.0]

- Fixed snapshot checks

---

## [1.4.0]

- Added ignore rules with regex pattern matching
- Added JSON and HTML output formats
- Added API token support
- Added Ceph and subscription checks
- Added VM locked check
- Fixed OsType detection

---

## [1.0.0]

- Initial release
