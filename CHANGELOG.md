# Changelog

---

## [2.1.0] — 2026-04-08

### New checks

**Node:**
- **Memory overcommit** (`WN0036`) — the total RAM allocated to VMs and containers on a node exceeds the node's physical memory. This can cause system instability or unexpected VM crashes.
- **Bridge not VLAN-aware** (`WN0037`) — a VM or container uses a VLAN tag on a network bridge that does not support VLANs. The tag is silently ignored and network traffic may not be isolated as expected.
- **VM consolidation candidate** (`IN0003`) — a node has very low CPU and RAM usage. Consider migrating its VMs to other nodes to free up hardware.
- **Firewall rule allows all traffic** (`WC0008`) — a cluster firewall rule uses `0.0.0.0/0` as source or destination, allowing traffic from or to any address. This is overly permissive and increases the attack surface.

**LXC containers (new checks):**
- **Nesting without keyctl** (`WG0038`) — the container has Docker nesting enabled but is missing the `keyctl` option. Without it, nested containers may leak cryptographic keys between each other.
- **Privileged container** (`WG0039`) — the container runs as privileged, meaning the root user inside has the same permissions as root on the host. Use unprivileged containers where possible.
- **Privileged without AppArmor** (`CG0006`) — a privileged container also has AppArmor protection disabled. There is no kernel-level confinement: a compromised container can affect the entire host.
- **No memory limit** (`WG0040`) — the container has no RAM limit configured. It can consume all available host memory and starve other VMs and containers.
- **Swap disabled** (`IG0013`) — the container has no swap space. Under heavy memory pressure, the OS will kill processes instead of using swap.
- **No hostname** (`IG0014`) — the container has no hostname set, making it harder to identify in logs and monitoring tools.
- **Raw LXC config entries** (`WG0041`) — the container has low-level LXC configuration entries that bypass Proxmox VE management. These can cause unexpected behavior after upgrades.

### Error code unification

VM and container checks previously used separate code prefixes (`WQ*`/`IQ*`/`CQ*` for QEMU, `WL*`/`IL*`/`CL*` for LXC). All guest codes are now unified under `WG*`, `IG*`, `CG*`. Checks that apply to both VMs and containers share the same code.

> If you use ignore rules based on error codes, update any `WQ*`, `IQ*`, `CQ*`, `WL*`, `IL*`, or `CL*` codes to their `*G*` equivalents.

### Performance

Analysis is significantly faster on large clusters. Backup content, VM configs, and storage lists are now fetched once and reused across all checks — instead of being fetched repeatedly for each VM or container. On a typical cluster this reduces the number of API calls by ~18% and total analysis time by ~32%.

### Fixes

- Memory, network-in, and network-out threshold breaches on nodes now report distinct error codes (`WN0038`, `WN0039`, `WN0040`) instead of all sharing the CPU code `WN0027`.
- Minor code quality improvements with no user-visible impact.

---

## [2.0.3] — 2026-04-03

### License change

- License changed from **MIT** to **GPL-3.0-only**.

### New checks

**VM (QEMU):**
- **CPU type 'host' with HA enabled** (`CQ0004`) — critical when a VM uses CPU type `host` and is managed by HA. HA requires live migration, which is impossible with `host` CPU type.
- **Disk on local storage with HA enabled** (`CQ0005`) — critical when a VM disk is on non-shared storage but the VM is managed by HA. Live migration will fail.
- **vCPU overcommit** (`WQ0036`) — warns when the total vCPU count on a node exceeds the configured ratio vs physical CPUs (default 4.0x, configurable via `Node.MaxVCpuRatio`).
- **Machine type not set** (`IQ0012`) — info when a VM has no machine type configured. QEMU will use the default, which may change across PVE upgrades and cause unexpected guest behavior.
- **No network interface** (`WQ0034`) — warns when a VM has no network interface configured (completely isolated from the network).
- **Duplicate MAC address** (`WQ0033`) — warns when two or more VMs share the same MAC address, which causes network conflicts.
- **Snapshot with RAM state** (`WQ0035`) — warns when a snapshot includes the full guest RAM state, wasting disk space and blocking storage migration.

**LXC:**
- **No memory limit** (`WL0022`) — warns when a container has `Memory=0` (unbounded), which allows it to consume all host RAM and starve other guests.

**Storage:**
- **No backup storage configured** (`WS0006`) — warns when no storage in the cluster has the `backup` content type, meaning vzdump has nowhere to save backups.
- **Backup storage unreachable from node** (`WS0007`) — warns when a backup job targets a storage that is not available on a node where VMs reside. Those VMs will not be backed up.

### Improvements

- **Excel export** (`--output=Excel`) — new output format that generates an `.xlsx` report with a summary header (generated date, duration, nodes, version) and a filtered table with all diagnostic results. File name defaults to `cv4pve-diagnostic-<timestamp>.xlsx` if `--output-file` is not specified.
- **`--output-file`** — new option to save any output format to a file instead of stdout.
- **`Node.MaxVCpuRatio`** — new configurable setting (default `4.0`) for the vCPU overcommit check.
- **`SettingsPressure`** — PSI pressure thresholds extracted into a dedicated class (`Pressure.Cpu`, `Pressure.IoFull`, `Pressure.MemoryFull`) for cleaner settings structure.

---

## [2.0.2] — 2026-03-25

### New checks

**Node:**
- **Disk temperature** — warns when a disk temperature exceeds the configured threshold (requires SMART data).
- **Disk SMART errors** — detects reallocated sectors, pending sectors, offline uncorrectable sectors, UDMA CRC errors and reported uncorrectable errors. Each is a separate check with its own threshold.
- **ZFS vdev state** — warns when a ZFS pool vdev is in a degraded or faulted state.
- **ZFS vdev I/O errors** — warns when a ZFS pool vdev has accumulated read, write or checksum errors.
- **ZFS pool errors** — warns when a ZFS pool reports errors.
- **LVM-thin metadata usage** — warns when LVM-thin metadata usage is high. A full metadata volume causes data corruption.
- **IOWait** — warns when node IOWait (from RRD data) exceeds the configured threshold.
- **Root filesystem usage** — warns when the root filesystem usage exceeds the configured threshold.
- **SWAP usage** — warns when SWAP usage exceeds the configured threshold.
- **PSI CPU / IO / Memory pressure** — warns when Linux Pressure Stall Information (PSI) metrics exceed thresholds (PVE 9.0+).

**VM/CT:**
- **Pending config changes** — warns when a VM or container has configuration changes that require a reboot to take effect.
- **VM state in snapshot** — warns when a snapshot includes the RAM state, which significantly increases snapshot size and restore time.

**LXC:**
- **Privileged container** — warns when a container runs as privileged (root inside = root on host).
- **Privileged container without AppArmor** — critical when a privileged container also has AppArmor disabled (no kernel confinement).
- **Nesting without keyctl** — warns when nesting is enabled but `keyctl` is not (required for Docker-in-LXC).
- **Raw LXC config** — warns when a container has raw LXC config entries that bypass PVE abstractions.
- **Swap = 0** — warns when a container has swap disabled (OOM killer risk under memory pressure).
- **No hostname** — info when a container has no hostname configured.

**Cluster:**
- **No backup job** — warns when no backup job is configured for any VM/CT.
- **Backup job without compression** — info when a backup job has no compression configured.
- **Backup job without retention** — warns when a backup job has no maxfiles/prune policy (storage will fill up).
- **No HA resources** — info when no HA resources are configured (VMs won't restart on node failure).
- **No storage replication** — info when no storage replication jobs exist.
- **Cluster firewall disabled** — warns when the cluster-level firewall is disabled.
- **Cluster firewall policy** — warns when inbound or outbound firewall policy is not DROP.
- **root@pam without TFA** — critical when the root user has no two-factor authentication configured.
- **Admin users without TFA** — warns when admin users have no TFA configured.
- **Overly broad permissions** — warns when a user has the Administrator role at root path `/` instead of pool- or node-scoped permissions.
- **Disabled user with active API token** — warns when a disabled user still has valid API tokens that should be revoked.

### Improvements

- **Unique error code per check** — every check now has a distinct code in the format `[gravity][context][0001-9999]` (e.g. `WN0014`, `CQ0001`). This makes it possible to ignore individual checks precisely via ignore rules.
- **macOS `.pkg` packages** — releases now include `.pkg` installers for `osx-x64` and `osx-arm64`.
- **Packages for Linux** — releases now include `.deb` and `.rpm` packages for `amd64`, `arm64` and `armhf`/`armv7hl`. AUR package updated automatically on release.

### Breaking changes

- **Error codes changed** — all codes have been reassigned. Existing ignore rules must be updated. Run `cv4pve-diag diag` and check the `Code` column for the new values.
- **Settings JSON structure changed** — new top-level sections `Rrd`, `SmartDisk`, `Backup`, `NodeStorage` have been added. Regenerate your `settings.json` with `cv4pve-diag create-settings`.

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
