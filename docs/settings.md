# Settings Reference

`cv4pve-diag` runs with a default settings profile that produces a useful baseline on most clusters. For more control you can pass a JSON settings file via `--settings-file=/path/settings.json`.

This document describes every field, its default, and what changes when you tune it.

---

## Including Ok results

The `IncludeOkResult` top-level flag controls whether passing checks also produce a result entry:

- `false` (default) — only failures are emitted. Output and exit code are unchanged from the legacy mode.
- `true` — every diagnostic check (compliance-mapped or operational) also emits an `Ok` result with `Gravity = Ok` on success. Useful for full audit reports where you need to prove that controls were verified, not just violated.

See [compliance.md](compliance.md) for the catalog of compliance mappings attached to each check.

---

## Generate a template with all defaults

```bash
cv4pve-diag create-settings --output=settings.json
```

---

## File format

Plain JSON. Unknown fields are ignored. Omitted fields fall back to defaults — you only need to include what you want to override.

```json
{
  "MaxParallelRequests": 5,
  "ApiTimeout": 0,
  "IncludeOkResult": false,
  "Backup": { "Enabled": true, "MaxAgeDays": 60, "RecentDays": 7 },
  "Snapshot": { "Enabled": true, "MaxCount": 10, "MaxAgeDays": 30 },
  "Cve": { "NvdEnabled": false, "MinCvssScore": 7.0 }
}
```

---

## Full settings.json with all defaults

```jsonc
{
  "Storage": {
    "Rrd": {
      "TimeFrame": "Day",         // RRD window for storage usage averaging: Hour, Day, Week, Month, Year
      "Consolidation": "Average", // RRD function: Average (smooth) or Maximum (peak detection)
    },
    "Threshold": {
      "Warning": 70,              // storage usage % above which WS0001 warns
      "Critical": 85,             // storage usage % above which WS0001 becomes critical
    },
  },
  "Node": {
    "Smart": {                                              // one extra API call per disk per node — off by default
      "Enabled": false,                                     // enable per-attribute SMART parsing (temperature, reallocated, pending, CRC, …)
      "Temperature": { "Warning": 55, "Critical": 65 },     // disk temperature °C (WN0019 / CN0007); set Warning to 0 to skip
      "SsdWearout":  { "Warning": 70, "Critical": 85 },     // SSD life consumed % (WN0018)
    },
    "NodeStorage": {
      "ZfsDetail": false,         // per-pool vdev state and I/O error checks (CN0012, WN0024, WN0025); 1 API call per pool
      "LvmThinMetadata": true,    // LVM-thin metadata usage check (WN0026 / CN0013); 1 API call per node
    },
    "MaxVCpuRatio": 4.0,                  // vCPU overcommit ratio (sum vCPU / physical CPU) above which WG0036 fires
    "ConsolidationCpuThreshold": 10.0,    // node CPU % below which the node is flagged as consolidation candidate (IN0003)
    "ConsolidationMemThreshold": 20.0,    // node RAM % below which the node is flagged as consolidation candidate
    "Rrd": {                                                  // node-specific PSI defaults — tighter than VM/CT (PVE 9.0+ only)
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu":        { "Warning": 40, "Critical": 70 },      // PSI CPU some — % time at least one task stalled (WN0031)
        "IoFull":     { "Warning": 10, "Critical": 30 },      // PSI I/O full (WN0032)
        "MemoryFull": { "Warning": 5,  "Critical": 15 },      // PSI memory full (WN0033)
      },
    },
    "Cpu":         { "Warning": 70, "Critical": 85 },         // node CPU % over RRD window (WN0027)
    "Memory":      { "Warning": 70, "Critical": 85 },         // node memory % (WN0038)
    "Network":     { "Warning": 0,  "Critical": 0  },         // network throughput threshold in bytes/sec; 0 disables
    "HealthScore": { "Warning": 70, "Critical": 50 },         // composite score (CPU 40% + RAM 40% + disk 20%); lower = worse (WG0032)
  },
  "Qemu": {                                                   // VM thresholds — same structure as Node, different defaults
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu":        { "Warning": 50, "Critical": 80 },      // PSI CPU some on the VM (WG0029)
        "IoFull":     { "Warning": 20, "Critical": 50 },      // PSI I/O full on the VM (WG0030)
        "MemoryFull": { "Warning": 10, "Critical": 30 },      // PSI memory full on the VM (WG0031)
      },
    },
    "Cpu":         { "Warning": 70, "Critical": 85 },         // VM CPU % over RRD window (WG0025)
    "Memory":      { "Warning": 70, "Critical": 85 },         // VM memory % (WG0026)
    "Network":     { "Warning": 0,  "Critical": 0  },         // VM network bytes/sec; 0 disables
    "HealthScore": { "Warning": 60, "Critical": 40 },         // VM composite score (CPU 50% + RAM 50%) (WG0032)
  },
  "Lxc": {                                                    // container thresholds — same structure and defaults as Qemu
    "Rrd": {
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "Pressure": {
        "Cpu":        { "Warning": 50, "Critical": 80 },
        "IoFull":     { "Warning": 20, "Critical": 50 },
        "MemoryFull": { "Warning": 10, "Critical": 30 },
      },
    },
    "Cpu":         { "Warning": 70, "Critical": 85 },
    "Memory":      { "Warning": 70, "Critical": 85 },
    "Network":     { "Warning": 0,  "Critical": 0  },
    "HealthScore": { "Warning": 60, "Critical": 40 },
  },
  "Snapshot": {
    "Enabled": true,                  // master switch; false skips the snapshot fetch (one API call per VM/CT)
    "MaxCount": 10,                   // warn (WG0023) when a guest has more than N snapshots; 0 disables
    "MaxAgeDays": 30,                 // warn (WG0024) when a snapshot is older than N days; 0 disables
  },
  "Backup": {
    "Enabled": true,                  // master switch; false skips the per-storage backup content fetch (no WG0019/WG0020/WS0003)
    "MaxAgeDays": 60,                 // warn (WG0019) when backup files are older than N days; 0 disables
    "RecentDays": 7,                  // warn (WG0020) when no backup exists within N days for a guest (RPO violation); 0 disables
  },
  "MaxParallelRequests": 5,           // max parallel API requests; set to 1 for sequential mode (slower, easier to debug)
  "ApiTimeout": 0,                    // per-request HTTP timeout in seconds; 0 uses the SDK default (~100s)
  "IncludeOkResult": false,           // when true every check also emits an Ok result on success — useful for full audit-style reports (see compliance.md)
  "Cve": {
    "NvdEnabled": false,              // check for CVEs specific to Proxmox VE (NVD API 2.0); requires internet access from the host running cv4pve-diag
    "MinCvssScore": 7.0,              // ignore CVEs below this CVSS score; 0 reports everything (very noisy). CN0015 ≥ 9.0, WN0042 7.0–8.9
  },
}
```

### Health Score Formula

```
Node  score = 100 - (cpu% × 0.4 + ram% × 0.4 + disk% × 0.2)
VM/CT score = 100 - (cpu% × 0.5 + ram% × 0.5)
```

Set `Warning` and `Critical` to `0` to disable health score checks entirely.

> **PSI Pressure** (PVE 9.0+): Linux Pressure Stall Information metrics. Checks are automatically skipped on older PVE versions where the values are always zero.

---

## Performance Tuning

By default the diagnostic runs up to **5 parallel API requests** (`MaxParallelRequests = 5`). This works well for most clusters, but you can tune it to match your environment.

### Speed up the diagnostic

Increase `MaxParallelRequests` to fetch more data at the same time:

```jsonc
"MaxParallelRequests": 10
```

> **Don't go too high.** Each parallel request is a real HTTP call to Proxmox. Too many at once can slow down the API, increase memory usage on both sides, and make the diagnostic less stable. Values between 5 and 15 are a reasonable range.

### Handle slow or high-latency clusters

Parallelism means more simultaneous requests — if your cluster is slow or the network has high latency, some calls may time out. Increase `ApiTimeout` to give them more time:

```jsonc
"ApiTimeout": 300   // seconds (0 = 100s default)
```

### Summary

| Setting | Effect | Default |
|---------|--------|---------|
| `MaxParallelRequests` ↑ | Faster, but more load on Proxmox and higher memory usage | 5 |
| `ApiTimeout` ↑ | Avoids timeouts on slow/high-latency clusters | 100s |

---

## CVE Scanning

cv4pve-diag can optionally check your cluster for Proxmox VE specific CVEs via the NVD (National Vulnerability Database) API. **Disabled by default**, requires internet access at runtime, no API key needed.

```jsonc
"Cve": {
  "NvdEnabled": true,
  "MinCvssScore": 7.0   // ignore CVEs below this score
}
```

- CVSS score ≥ 9.0 → **Critical** (`CN0015`)
- CVSS score ≥ 7.0 → **Warning** (`WN0042`)

Only CVEs that apply to your installed `pve-manager` version are reported (matched against the NVD version range).

> **Scope.** This check covers Proxmox VE itself (`cpe:2.3:a:proxmox:virtual_environment`). For a Debian-wide system package audit run [`debsecan`](https://manpages.debian.org/bookworm/debsecan/debsecan.1.en.html) directly on each node — PVE's REST API does not expose the full installed package set, so it is not something diag can do remotely.

---

## Recommended overrides by scenario

**Production cluster, conservative**

```json
{
  "Backup": { "RecentDays": 1, "MaxAgeDays": 30 },
  "Cve": { "NvdEnabled": true, "MinCvssScore": 7.0 },
  "Node": { "Smart": { "Enabled": true } }
}
```

**Audit / compliance run**

```json
{
  "IncludeOkResult": true,
  "Cve": { "NvdEnabled": true, "MinCvssScore": 4.0 }
}
```

**Lab / dev — silence noise**

```json
{
  "Snapshot": { "MaxCount": 0, "MaxAgeDays": 0 },
  "Backup": { "Enabled": false }
}
```

**Slow / metered API connection**

```json
{
  "MaxParallelRequests": 2,
  "ApiTimeout": 30,
  "Node": { "Smart": { "Enabled": false }, "NodeStorage": { "ZfsDetail": false } }
}
```
