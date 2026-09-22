/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// Privilege detection behind the backup-permission pre-check. The interesting case is the one
/// from issue #52: PVEAuditor grants every *.Audit privilege, which is enough to list a storage's
/// content but NOT to see the backup volumes inside it — PVE filters those out and returns an
/// empty list rather than an error, so the backup checks would report healthy guests as unprotected.
/// </summary>
public class DiagnosticEnginePermissionsTests
{
    // Exactly what `pveum user permissions` reports for an account holding only PVEAuditor.
    private static readonly IReadOnlyList<string> PveAuditorPrivileges =
    [
        "Datastore.Audit",
        "Mapping.Audit",
        "Pool.Audit",
        "SDN.Audit",
        "Sys.Audit",
        "VM.Audit",
        "VM.GuestAgent.Audit",
    ];

    private static Dictionary<string, IReadOnlyList<string>> Perms(params (string Path, string[] Privileges)[] entries)
        => entries.ToDictionary(e => e.Path, e => (IReadOnlyList<string>)e.Privileges);

    [Fact]
    public void PveAuditor_lacks_the_privileges_needed_to_list_backup_volumes()
    {
        var permissions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["/storage/truenas-backups"] = PveAuditorPrivileges,
        };

        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.AllocateSpace"));
        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Backup"));
    }

    [Fact]
    public void PveAuditor_does_grant_the_audit_privileges_it_is_expected_to()
    {
        var permissions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["/storage/truenas-backups"] = PveAuditorPrivileges,
        };

        // Guards against a detector that simply returns false for everything.
        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.Audit"));
        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Audit"));
    }

    [Fact]
    public void Privilege_granted_on_any_path_is_found()
    {
        var permissions = Perms(
            ("/storage/local", ["Datastore.Audit"]),
            ("/storage/truenas-backups", ["Datastore.Audit", "Datastore.AllocateSpace"]));

        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.AllocateSpace"));
    }

    [Fact]
    public void Privilege_match_is_case_insensitive()
    {
        var permissions = Perms(("/vms", ["vm.backup"]));

        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Backup"));
    }

    [Fact]
    public void Empty_permission_set_reports_nothing_granted()
    {
        var permissions = new Dictionary<string, IReadOnlyList<string>>();

        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Backup"));
    }

    [Fact]
    public void Path_with_no_privileges_does_not_report_a_grant()
    {
        var permissions = Perms(("/storage/truenas-backups", []));

        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.AllocateSpace"));
    }

    [Fact]
    public void Full_backup_privileges_are_detected()
    {
        // The configuration the README tells users to create.
        var permissions = Perms(
            ("/storage", ["Datastore.Audit", "Datastore.AllocateSpace"]),
            ("/vms", ["VM.Audit", "VM.Backup"]));

        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.AllocateSpace"));
        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Backup"));
    }

    [Fact]
    public void PveAuditor_holds_every_foundational_audit_privilege()
    {
        // The privileges that /cluster/resources filters on are all part of PVEAuditor, so an
        // auditor account sees the full inventory — it is only the backup volumes it cannot see.
        // This is what makes the bug so easy to hit: the report looks complete.
        var permissions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["/"] = PveAuditorPrivileges,
        };

        foreach (var privilege in new[] { "VM.Audit", "Datastore.Audit", "Sys.Audit", "Pool.Audit" })
        {
            Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, privilege), privilege);
        }
    }

    [Fact]
    public void An_account_with_no_audit_privileges_is_detected_as_missing_them()
    {
        // Guests would silently vanish from /cluster/resources for this account.
        var permissions = Perms(("/storage/backups", ["Datastore.Audit"]));

        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Audit"));
        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Sys.Audit"));
        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.Audit"));
    }

    [Fact]
    public void Privilege_on_a_single_guest_does_not_count_as_covering_all_guests()
    {
        // The dangerous case: an ACL on one VM satisfies "granted somewhere", but every other guest
        // is silently filtered out of /cluster/resources. Root coverage is what actually matters.
        var permissions = Perms(("/vms/100", ["VM.Audit"]));

        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "VM.Audit"));
        Assert.False(DiagnosticEngine.CoversRoot(permissions, "VM.Audit", "/vms"));
    }

    [Fact]
    public void Privilege_on_the_root_covers_everything_below_it()
    {
        var permissions = Perms(("/vms", ["VM.Audit"]));

        Assert.True(DiagnosticEngine.CoversRoot(permissions, "VM.Audit", "/vms"));
    }

    [Fact]
    public void Privilege_on_slash_covers_every_root()
    {
        // A propagating grant at "/" is reported on "/" alone, not expanded to child paths.
        var permissions = Perms(("/", ["VM.Audit", "Datastore.Audit"]));

        Assert.True(DiagnosticEngine.CoversRoot(permissions, "VM.Audit", "/vms"));
        Assert.True(DiagnosticEngine.CoversRoot(permissions, "Datastore.Audit", "/storage"));
    }

    [Fact]
    public void Privilege_on_a_different_root_does_not_cover_this_one()
    {
        var permissions = Perms(("/storage", ["Datastore.Audit", "VM.Audit"]));

        Assert.False(DiagnosticEngine.CoversRoot(permissions, "VM.Audit", "/vms"));
    }

    [Fact]
    public void Root_listed_without_the_privilege_does_not_count_as_coverage()
    {
        // get_effective_permissions always includes the standard roots, so the key being present
        // says nothing on its own — only the privilege list under it does.
        var permissions = Perms(("/vms", ["VM.Audit"]), ("/storage", []));

        Assert.False(DiagnosticEngine.CoversRoot(permissions, "Datastore.Audit", "/storage"));
    }

    [Fact]
    public void An_account_scoped_to_one_storage_is_a_valid_configuration()
    {
        // Not a fault: an account deliberately restricted to one storage should analyze that
        // storage. The visibility privileges are absent at root, which is reported as reduced
        // scope (Info) — only the backup privileges escalate to Warning, because their absence
        // makes other checks report the opposite of the truth instead of simply reporting less.
        var permissions = Perms(("/storage/backups", ["Datastore.Audit"]));

        var visibilityOnly = new[]
        {
            ("VM.Audit", "/vms"),
            ("Datastore.Audit", "/storage"),
            ("Sys.Audit", "/nodes"),
            ("Pool.Audit", "/pool"),
        };

        foreach (var (privilege, root) in visibilityOnly)
        {
            Assert.False(DiagnosticEngine.CoversRoot(permissions, privilege, root), $"{privilege} on {root}");
        }
    }

    [Fact]
    public void Backup_privilege_on_only_some_guests_still_counts_as_missing()
    {
        // A partial VM.Backup grant is worse than none for the backup checks: the listing returns
        // backups for the covered guests and nothing for the rest, so the uncovered ones would be
        // reported as unprotected. It must be treated as missing so the checks get skipped.
        var permissions = Perms(
            ("/storage", ["Datastore.Audit", "Datastore.AllocateSpace"]),
            ("/vms/100", ["VM.Backup"]));

        Assert.False(DiagnosticEngine.CoversRoot(permissions, "VM.Backup", "/vms"));
    }

    [Fact]
    public void Datastore_Allocate_is_recognised_as_a_distinct_privilege()
    {
        // Datastore.Allocate short-circuits check_volume_access, so the engine accepts it in place
        // of Datastore.AllocateSpace. It must not be confused with the AllocateSpace privilege.
        var permissions = Perms(("/storage", ["Datastore.Allocate"]));

        Assert.True(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.Allocate"));
        Assert.False(DiagnosticEngine.HasPrivilegeAnywhere(permissions, "Datastore.AllocateSpace"));
    }
}
