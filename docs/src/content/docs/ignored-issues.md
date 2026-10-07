---
title: Ignore accepted findings in cv4pve-diag
description: Suppress the findings you have reviewed and accepted with ignore rules, so the cv4pve-diag report shows only what is new or needs action.
sidebar:
  label: Ignore Rules
---

`cv4pve-diag` lets you suppress diagnostic findings you have already reviewed and accepted, so the report focuses on what is actually new or actionable.

A common scenario: the tool reports that a specific VM does not have `Protection = enabled` (`IG0011`), but you have decided on purpose to leave it off for that guest. Instead of seeing that finding every run, you add a rule to the ignore file and the next runs skip it.

The matched issues disappear from the report. With `--ignored-issues-show` they stay in the report, marked with an `X` in an extra `IgnoredIssue` column, so you can check what the rules hide.

---

## How to use it

```bash
# Generate a template (writes ignored-issues.json with one example rule: edit it before use).
# No connection is needed; it also prints the accepted Context and Gravity values.
cv4pve-diag create-ignored-issues

# Run with ignored issues
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --ignored-issues-file=ignored-issues.json execute

# Keep ignored issues in the report, marked in an extra IgnoredIssue column (to audit what is hidden)
cv4pve-diag --host=pve.local --api-token=user@realm!token=uuid \
  --ignored-issues-file=ignored-issues.json --ignored-issues-show execute
```

---

## File format

A JSON array of rule objects. A finding is suppressed when **every** field declared on a rule matches the finding (logical AND within the rule). Multiple rules are evaluated independently (logical OR across rules).

All string fields support **regex** patterns, **case-sensitive**: use `.*` to match anything. A pattern matches if it is found **anywhere** in the value: `"Id": "nodes/pve01/qemu/105"` also matches `nodes/pve01/qemu/1050`. To match one guest only, anchor it: `"^nodes/pve01/qemu/105$"`.
The file may contain `//` comments and trailing commas. `Context` and `Gravity` take names (`"Qemu"`, `"Warning"`) and match that value only: `create-ignored-issues` prints the accepted values. Files written by older versions, with numbers, are still read: there `0` keeps meaning "any", as it did.

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
| `ErrorCode`   | The check code (see [Diagnostic Checks](../checks/)) | `"IG0011"`               |
| `Id`          | The resource URL/path of the finding        | `"nodes/pve01/.*"`       |
| `SubContext`  | The finding's SubContext                    | `"Protection"`           |
| `Description` | The free-text description                   | `".*test.*"`             |
| `Context`     | The finding context type                    | `"Qemu"` / `"Node"` / …  |
| `Gravity`     | The severity                                | `"Info"` / `"Warning"` / `"Critical"` / `"Ok"` |

All fields are optional: only specified fields are matched. To match any context or any gravity, leave the field out. An empty object `{}` matches every finding and is almost never what you want.

> **Changed in 2.8.0.** `Context: "Node"` and `Gravity: "Info"` used to mean "any", so `{ "Gravity": "Info" }` matched every finding. They now match Node and Info only. A rule that carried them without meaning it (the old template wrote `"Gravity": "Info"` in its example) hides fewer findings than before: remove the field to get the old behaviour.

---

## Rules that cannot be applied

A rule cannot be applied when one of its patterns is not a regular expression, or when its `Context` or `Gravity` is not one of the accepted values. The run does not stop:

- the rule is left out, so the findings it was meant to hide are in the report
- the report contains one `CU0002` finding, Critical, for each such rule, with its position in the file and the reason
- one `WARNING` line for each such rule is written on the error stream before the analysis starts

```text
WARNING: Ignore rule #2 is not applied: invalid regular expression in Id 'nodes/(': Invalid pattern 'nodes/(' at offset 7. Not enough )'s.
```

No rule can hide a `CU0002` finding, an empty `{}` included, and it is kept when the report is filtered with `--compliance`. The exit code stays 0: a script has to look for `CU0002` in the report, not at the exit code.

A rule that matches no finding is valid, for example a rule for a guest that has been removed: nothing is reported for it. A file that is not valid JSON does stop the run, with an error.

> **Changed in 2.8.0.** A broken pattern used to stop the run with an error before the cluster was analyzed.

---

## Tips

- **Be as specific as you can.** A rule that only sets `ErrorCode` hides the check everywhere; pair it with `Id` (regex) to scope to a node, pool or guest.
- **Use `--ignored-issues-show`** during the first runs to verify the rule does what you expect before letting it silently hide findings.
- **Keep the file under version control** alongside your runbook. Each rule should be paired with a comment in the surrounding documentation that explains *why* the finding was accepted.
- **Avoid `Description` regexes** when an equivalent `ErrorCode` rule exists: descriptions can change between releases, codes do not.
