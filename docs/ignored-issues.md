# Ignore Rules

`cv4pve-diag` lets you suppress diagnostic findings you have already reviewed and accepted, so the report focuses on what is actually new or actionable.

A common scenario: the tool reports that a specific VM does not have `Protection = enabled` (`IG0011`), but you have decided on purpose to leave it off for that guest. Instead of seeing that finding every run, you add a rule to the ignore file and the next runs skip it.

The matched issues disappear from the report. With `--ignored-issues-show` they stay in the report, marked with an `X` in an extra `IgnoredIssue` column, so you can check what the rules hide.

---

## How to use it

```bash
# Generate ignored issues template (writes ignored-issues.json with one example rule — edit it before use)
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid create-ignored-issues

# Run with ignored issues
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --ignored-issues-file=ignored-issues.json execute

# Show ignored issues in a separate table (useful to audit what is being hidden)
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --ignored-issues-file=ignored-issues.json --ignored-issues-show execute
```

---

## File format

A JSON array of rule objects. A finding is suppressed when **every** field declared on a rule matches the finding (logical AND within the rule). Multiple rules are evaluated independently (logical OR across rules).

All string fields support **regex** patterns — use `.*` to match anything. A pattern matches if it is found **anywhere** in the value: `"Id": "nodes/pve01/qemu/105"` also matches `nodes/pve01/qemu/1050`. To match one guest only, anchor it: `"^nodes/pve01/qemu/105$"`. An invalid pattern stops the run with an error before the cluster is analyzed.

The file may contain `//` comments and trailing commas. `Context` and `Gravity` accept names (`"Qemu"`, `"Warning"`) or their numbers.

```json
[
  {
    "ErrorCode": "IG0011"
  },
  {
    "Id": "^nodes/pve01/qemu/105$",
    "SubContext": "Protection"
  },
  {
    "Id": "nodes/pve01/.*",
    "Context": "Qemu"
  }
]
```

In the example above:
- the first rule hides every `IG0011` finding cluster-wide
- the second rule hides the `Protection` sub-context only on VM 105 of node `pve01`
- the third rule hides every Qemu finding on node `pve01` (regardless of code)

---

## Available fields

| Field         | Matches                                     | Example                  |
| ------------- | ------------------------------------------- | ------------------------ |
| `ErrorCode`   | The check code (see [checks.md](checks.md)) | `"IG0011"`               |
| `Id`          | The resource URL/path of the finding        | `"nodes/pve01/.*"`       |
| `SubContext`  | The finding's SubContext                    | `"Protection"`           |
| `Description` | The free-text description                   | `".*test.*"`             |
| `Context`     | The finding context type                    | `"Qemu"` / `"Node"` / …  |
| `Gravity`     | The severity                                | `"Info"` / `"Warning"` / `"Critical"` |

All fields are optional — only specified fields are matched. An empty object `{}` matches every finding and is almost never what you want.

> **`Node` and `Info` mean "any".** `Context: "Node"` and `Gravity: "Info"` are the default values, so a rule setting them does not filter on them: `{ "Gravity": "Info" }` matches every finding. Filter on `ErrorCode` instead (codes starting with `I` are Info, the second letter `N` is Node — see [checks.md](checks.md#code-nomenclature)).

---

## Tips

- **Be as specific as you can.** A rule that only sets `ErrorCode` hides the check everywhere; pair it with `Id` (regex) to scope to a node, pool or guest.
- **Use `--ignored-issues-show`** during the first runs to verify the rule does what you expect before letting it silently hide findings.
- **Keep the file under version control** alongside your runbook. Each rule should be paired with a comment in the surrounding documentation that explains *why* the finding was accepted.
- **Avoid `Description` regexes** when an equivalent `ErrorCode` rule exists — descriptions can change between releases, codes do not.
