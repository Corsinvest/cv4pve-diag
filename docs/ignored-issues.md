# Ignore Rules

`cv4pve-diag` lets you suppress diagnostic findings you have already reviewed and accepted, so the report focuses on what is actually new or actionable.

A common scenario: the tool reports that a specific VM does not have `Protection = enabled` (`IG0011`), but you have decided on purpose to leave it off for that guest. Instead of seeing that finding every run, you add a rule to the ignore file and the next runs skip it.

The matched issues either disappear from the report entirely, or — if you pass `--ignored-issues-show` — are displayed in a separate table so you can still see what was suppressed without polluting the main output.

---

## How to use it

```bash
# Generate ignored issues template (writes ignored-issues.json with placeholder rules)
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

All string fields support **regex** patterns — use `.*` to match anything.

```json
[
  {
    "ErrorCode": "IG0011"
  },
  {
    "Id": "nodes/pve01/qemu/105",
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

---

## Tips

- **Be as specific as you can.** A rule that only sets `ErrorCode` hides the check everywhere; pair it with `Id` (regex) to scope to a node, pool or guest.
- **Use `--ignored-issues-show`** during the first runs to verify the rule does what you expect before letting it silently hide findings.
- **Keep the file under version control** alongside your runbook. Each rule should be paired with a comment in the surrounding documentation that explains *why* the finding was accepted.
- **Avoid `Description` regexes** when an equivalent `ErrorCode` rule exists — descriptions can change between releases, codes do not.
