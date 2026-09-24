/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    /// <summary>
    /// A privilege the analysis depends on, and what silently degrades without it.
    /// </summary>
    /// <param name="Privilege">PVE privilege name.</param>
    /// <param name="Root">ACL root the privilege must cover for the whole inventory to be visible.</param>
    /// <param name="Alternative">Privilege that substitutes for it, if any.</param>
    /// <param name="Impact">What the user loses — phrased as the visible consequence.</param>
    /// <param name="DisablesBackupChecks">Whether the backup checks must be skipped without it.</param>
    /// <param name="DisablesOrphanChecks">Whether the orphaned image/backup checks must be skipped
    /// without it, also when it is granted on part of the root only.</param>
    private sealed record RequiredPrivilege(string Privilege,
                                            string Root,
                                            string? Alternative,
                                            string Impact,
                                            bool DisablesBackupChecks = false,
                                            bool DisablesOrphanChecks = false);

    // Privileges whose absence does NOT surface as an API error. PVE filters the caller's view
    // instead of returning 403, so the analysis silently sees less than it should:
    //
    //  - /cluster/resources drops entries the caller cannot audit (PVE/API2/Cluster.pm), so guests,
    //    storages and pools simply vanish from the report rather than being reported as unreadable.
    //  - a storage content listing drops individual backup volumes (check_volume_access in
    //    PVE/Storage.pm), which makes healthy guests look like they have no backups at all.
    //
    // Privileges that DO fail loudly (Sys.Modify on APT, for example) are deliberately absent: the
    // ToSafe* wrappers already report those as WG0042, which is accurate and needs no pre-check.
    private static readonly RequiredPrivilege[] RequiredPrivileges =
    [
        new("VM.Audit",
            "/vms",
            null,
            "VMs and containers are missing from the analysis entirely — they are filtered out of /cluster/resources",
            DisablesOrphanChecks: true),

        new("Datastore.Audit",
            "/storage",
            null,
            "storages are missing from the analysis — storage capacity and orphaned-image checks cannot run"),

        new("Sys.Audit",
            "/nodes",
            null,
            "node details are unavailable — services, disks, certificates and version checks cannot run"),

        new("Pool.Audit",
            "/pool",
            null,
            "pools are missing — pool-based backup jobs cannot be resolved to their guests"),

        // check_volume_access takes the Datastore.Allocate short-circuit before reaching the
        // per-content-type branches, so it substitutes for Datastore.AllocateSpace.
        new("Datastore.AllocateSpace",
            "/storage",
            "Datastore.Allocate",
            "backup files are invisible: PVE returns an empty list instead of an error, so every guest looks unprotected",
            DisablesBackupChecks: true),

        new("VM.Backup",
            "/vms",
            null,
            "backup files are invisible: PVE returns an empty list instead of an error, so every guest looks unprotected",
            DisablesBackupChecks: true),
    ];

    /// <summary>
    /// Reports up-front which parts of the cluster the account can actually see. PVE answers a
    /// request the caller is only partly entitled to by filtering the response rather than failing
    /// it, so a narrower ACL silently produces a narrower report with no indication of what was left
    /// out. Restricting an account is a legitimate choice — the point is to state the resulting
    /// scope (Info), not to treat it as a fault; Warning is reserved for the backup privileges,
    /// whose absence makes other checks report the opposite of the truth.
    /// </summary>
    private async Task CheckPermissionsAsync()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> permissions;
        try
        {
            permissions = await client.Access.Permissions.GetPermissionsAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Can't read our own privileges. Leave every check enabled rather than suppressing
            // findings on a guess, but say the verification did not happen.
            _result.Add(new DiagnosticResult
            {
                Id = "access/permissions",
                ErrorCode = DiagnosticSafeExtensions.ApiErrorCode,
                Description = $"Unable to verify privileges: {(ex is PveResultException pex ? DiagnosticSafeExtensions.BuildApiErrorMessage(pex.Result) : ex.Message)}. "
                              + "Findings may be incomplete if the account is missing read privileges.",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "ApiError",
                Gravity = DiagnosticResultGravity.Warning,
            });
            return;
        }

        // Privileges that only matter to the backup checks are irrelevant when those checks are
        // switched off — reporting them would be noise about a feature the user opted out of.
        var relevant = RequiredPrivileges.Where(a => settings.Backup.Enabled || !a.DisablesBackupChecks);

        var missing = relevant.Where(a => !CoversRoot(permissions, a.Privilege, a.Root)
                                          && (a.Alternative == null || !CoversRoot(permissions, a.Alternative, a.Root)))
                              .ToList();

        // Granted somewhere, but not on the root: the analysis sees only the subset the ACL covers,
        // and PVE gives no indication that anything was left out. Worth its own message — the fix
        // is different (widen the existing grant) and so is the consequence (partial, not absent).
        var partial = missing.Where(a => HasPrivilegeAnywhere(permissions, a.Privilege)
                                         || (a.Alternative != null && HasPrivilegeAnywhere(permissions, a.Alternative)))
                             .ToHashSet();

        if (missing.Count == 0) { return; }

        // Without these the backup listing comes back empty for every guest, so WG0019/WG0020/WS0003
        // would each report the opposite of the truth. One accurate finding beats one wrong finding
        // per guest, so skip them and explain why. Only when the privilege is missing everywhere:
        // granted on part of the root (typically the backup storage itself) the checks stay on, and
        // the WC0020 message says the rest is not covered.
        var backupChecksOff = missing.Any(a => a.DisablesBackupChecks && !partial.Contains(a));
        if (backupChecksOff) { _backupChecksEnabled = false; }

        // A guest the account cannot see still owns its disks and backups: WS0002/WS0003 would list
        // them as orphaned (delete candidates). Wrong either way, whether part or all is hidden.
        if (missing.Any(a => a.DisablesOrphanChecks)) { _orphanChecksEnabled = false; }

        foreach (var item in missing)
        {
            var name = $"'{item.Privilege}'{(item.Alternative == null ? "" : $" (or '{item.Alternative}')")}";

            // A restricted ACL is a deliberate administrative decision, not a fault: an account
            // scoped to one storage is meant to analyze that storage. Report the reduced scope as
            // Info so the report says what it covers, and reserve Warning for the case where the
            // missing privilege makes other checks produce WRONG findings rather than fewer ones.
            _result.Add(new DiagnosticResult
            {
                Id = "access/permissions",
                ErrorCode = "WC0020",
                Description = partial.Contains(item)
                    ? $"Privilege {name} is granted on part of '{item.Root}' — the analysis covers only that subset. "
                      + $"Outside it, {item.Impact}. This is expected if the account is scoped on purpose; "
                      + (item.DisablesOrphanChecks ? "the orphaned image and backup checks (WS0002, WS0003) are skipped; " : "")
                      + $"grant {name} on '{item.Root}' to cover everything."
                    : $"Privilege {name} is not granted on '{item.Root}' — {item.Impact}. "
                      + "Proxmox omits these from its response without reporting an error, so the analysis cannot see what is missing. "
                      + (item.DisablesBackupChecks ? "The backup checks (WG0019, WG0020, WS0003) are skipped. " : "")
                      + (item.DisablesOrphanChecks ? "The orphaned image and backup checks (WS0002, WS0003) are skipped. " : "")
                      + "See the permissions section in the README.",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Permissions",
                Gravity = item.DisablesBackupChecks
                    ? DiagnosticResultGravity.Warning
                    : DiagnosticResultGravity.Info,
            });
        }
    }

    // True when the privilege covers the whole root — granted on "/" or on the root itself, both of
    // which propagate to every path below. GetPermissionsAsync reports only the paths carrying an
    // explicit ACL (plus the standard roots), so a grant on a single guest or storage appears solely
    // on that path: it would satisfy HasPrivilegeAnywhere while leaving the rest of the inventory
    // invisible, which is precisely the silent gap this check exists to catch.
    internal static bool CoversRoot(IReadOnlyDictionary<string, IReadOnlyList<string>> permissions, string privilege, string root)
        => HasPrivilegeOnPath(permissions, privilege, "/")
           || HasPrivilegeOnPath(permissions, privilege, root);

    private static bool HasPrivilegeOnPath(IReadOnlyDictionary<string, IReadOnlyList<string>> permissions, string privilege, string path)
        => permissions.TryGetValue(path, out var privileges)
           && privileges.Contains(privilege, StringComparer.OrdinalIgnoreCase);

    // True when the privilege is granted on any ACL path at all — used to tell "granted on a subset"
    // apart from "not granted anywhere", which need different advice.
    internal static bool HasPrivilegeAnywhere(IReadOnlyDictionary<string, IReadOnlyList<string>> permissions, string privilege)
        => permissions.Any(a => a.Value.Contains(privilege, StringComparer.OrdinalIgnoreCase));
}
