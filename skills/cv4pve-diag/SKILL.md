---
name: cv4pve-diag
description: Run a health check of a Proxmox VE cluster with cv4pve-diag and explain its findings — quorum, HA, backups, snapshots, certificates, updates, disks, storage usage, guest configuration, firewall — with the code, severity and resource of each problem. Use it when the user asks what is wrong with the cluster, whether it follows best practices, or what a cv4pve-diag code means. It only reads.
---

# cv4pve-diag

`cv4pve-diag` reads a Proxmox VE cluster through its API and reports the problems it finds, one row per
finding. It sends only read requests. The connection options (host and API token) are in a file the user
names: if you do not know its path, ask.

## Rules

- Connect only with that file, passed with `@`. Do not print it or copy the token anywhere.
- `--output`, `--output-file`, `--settings-file`, `--ignored-issues-file` and `--compliance` go before
  `execute`; `--fast` and `--full` after it.
- Use the standard profile. `--full` adds S.M.A.R.T., ZFS detail and Ok results, and looks up CVEs on
  `services.nvd.nist.gov`: use it only when the user asks.
- The exit code is 0 even when the report has Critical findings: read the findings, not the exit code. Any
  other code is a failure, with a line starting `ERROR:`.
- An account that cannot see part of the cluster gets fewer findings, not an error: look for `WC0020` and
  `WG0042` before saying that an area is clean.
- Do not write `ignored-issues.json` or a settings file unless the user asks: propose the rule and let the
  user decide.
- If an option is refused, check `cv4pve-diag --help`: this skill can be newer than the tool.

## Run

```bash
cv4pve-diag @<options-file> --output Json execute                            # standard profile
cv4pve-diag @<options-file> --output Json execute --fast                     # large cluster, fewer checks
cv4pve-diag @<options-file> --output Json --output-file diag.json execute    # to a file
cv4pve-diag @<options-file> --output Json --compliance Nis2 execute          # findings mapped to a standard
```

## Read

The JSON is an array of findings, sorted by `Gravity` (Critical first), then `Context` and `SubContext`.
Every value is a string:

| Key | Content |
|---|---|
| `Id` | The resource: `cluster`, `nodes/<node>`, `nodes/<node>/qemu/<vmid>`, `nodes/<node>/lxc/<vmid>`, `nodes/<node>/storage/<name>`, … |
| `Code` | The check, e.g. `WG0017` |
| `Description` | What was found |
| `Context` | `Cluster`, `Node`, `Storage`, `Qemu` (VM) or `Lxc` (container) |
| `SubContext` | The area of the check: `Backup`, `SnapshotOld`, `Agent`, `Firewall`, `Update`, … |
| `Gravity` | `Critical`, `Warning`, `Info`; also `Ok` with `--full` |
| `ControlId` | Only with `--compliance`: the controls of that standard the finding maps to |

## Explain a finding

A code is `<Severity><Area><NNNN>`: severity `C` Critical, `W` Warning, `I` Info; area `C` Cluster,
`N` Node, `S` Storage, `G` Guest. The meaning of each code is in the table of its page:

- `?C…` → https://corsinvest.github.io/cv4pve-diag/checks/cluster/
- `?N…` → https://corsinvest.github.io/cv4pve-diag/checks/node/
- `?S…` → https://corsinvest.github.io/cv4pve-diag/checks/storage/
- `?G…` → https://corsinvest.github.io/cv4pve-diag/checks/vm/ (`Context` `Qemu`) or
  https://corsinvest.github.io/cv4pve-diag/checks/container/ (`Lxc`)
- `CU0001`, `WG0042` (an API call failed) and `WC0020` (missing privileges) →
  https://corsinvest.github.io/cv4pve-diag/checks/

Give the user the resource, what is wrong, why it matters and how to fix it in Proxmox VE. Do not fix it
yourself: cv4pve-diag only reads, and a change is the user's decision.

Documentation: https://corsinvest.github.io/cv4pve-diag/
