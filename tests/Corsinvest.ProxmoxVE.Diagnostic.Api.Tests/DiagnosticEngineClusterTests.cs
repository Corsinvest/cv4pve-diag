/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Newtonsoft.Json;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// Cluster-level rules behind WG0017 (backup job coverage), CC0002 (quorum) and WN0001 (node firewall).
/// </summary>
public class DiagnosticEngineClusterTests
{
    // Jobs are deserialized like the API returns them: 'exclude' is not a typed property.
    private static ClusterBackup Job(string json) => JsonConvert.DeserializeObject<ClusterBackup>(json)!;

    // ----- WG0017 -----
    [Fact]
    public void Job_listing_the_vmid_covers_the_guest()
        => Assert.True(DiagnosticEngine.IsCoveredByBackupJob([Job("""{"enabled":1,"vmid":"100,1000,105"}""")], 1000, "pve01", null));

    [Fact]
    public void Vmid_list_matches_whole_ids_only()
        => Assert.False(DiagnosticEngine.IsCoveredByBackupJob([Job("""{"enabled":1,"vmid":"1000,1050"}""")], 100, "pve01", null));

    [Fact]
    public void All_job_skips_the_excluded_guests()
    {
        var jobs = new[] { Job("""{"enabled":1,"all":1,"exclude":"104,105"}""") };

        Assert.True(DiagnosticEngine.IsCoveredByBackupJob(jobs, 100, "pve01", null));
        Assert.False(DiagnosticEngine.IsCoveredByBackupJob(jobs, 105, "pve01", null));
    }

    [Fact]
    public void Job_pinned_to_a_node_covers_only_guests_on_that_node()
    {
        var jobs = new[] { Job("""{"enabled":1,"all":1,"node":"pve01"}""") };

        Assert.True(DiagnosticEngine.IsCoveredByBackupJob(jobs, 100, "pve01", null));
        Assert.False(DiagnosticEngine.IsCoveredByBackupJob(jobs, 100, "pve02", null));
    }

    [Fact]
    public void Pool_job_covers_the_guests_of_that_pool()
    {
        var jobs = new[] { Job("""{"enabled":1,"pool":"prod"}""") };

        Assert.True(DiagnosticEngine.IsCoveredByBackupJob(jobs, 100, "pve01", "prod"));
        Assert.False(DiagnosticEngine.IsCoveredByBackupJob(jobs, 100, "pve01", "test"));
        Assert.False(DiagnosticEngine.IsCoveredByBackupJob(jobs, 100, "pve01", null));
    }

    [Fact]
    public void Disabled_job_covers_nothing()
        => Assert.False(DiagnosticEngine.IsCoveredByBackupJob([Job("""{"enabled":0,"all":1}""")], 100, "pve01", null));

    // ----- CC0002 -----
    [Fact]
    public void Node_with_two_of_three_votes_breaks_quorum_when_lost()
    {
        // The real cluster: cc01 quorum_votes=2, cc02 quorum_votes=1.
        var breaking = DiagnosticEngine.NodesBreakingQuorum([("cc01", 2), ("cc02", 1)]);

        Assert.Equal([("cc01", 2)], breaking);
    }

    [Fact]
    public void Two_nodes_with_one_vote_each_both_break_quorum()
        => Assert.Equal(2, DiagnosticEngine.NodesBreakingQuorum([("pve01", 1), ("pve02", 1)]).Count);

    [Fact]
    public void Three_equal_nodes_survive_any_single_loss()
        => Assert.Empty(DiagnosticEngine.NodesBreakingQuorum([("a", 1), ("b", 1), ("c", 1)]));

    // ----- WN0001 -----
    [Fact]
    public void Node_firewall_without_enable_is_enabled()
        => Assert.True(DiagnosticEngine.IsNodeFirewallEnabled(new Dictionary<string, object> { ["log_level_in"] = "nolog" }));

    [Fact]
    public void Node_firewall_is_disabled_only_by_enable_zero()
    {
        Assert.False(DiagnosticEngine.IsNodeFirewallEnabled(new Dictionary<string, object> { ["enable"] = 0L }));
        Assert.True(DiagnosticEngine.IsNodeFirewallEnabled(new Dictionary<string, object> { ["enable"] = 1L }));
    }

    // ----- WC0002: retention of a job (JSON object) or a storage (key=value list) -----
    [Theory]
    [InlineData("""{"keep-daily":"30"}""", null, true)]
    [InlineData("keep-daily=7,keep-last=3", null, true)]
    [InlineData("keep-all=1", null, false)]
    [InlineData("""{"keep-all": 1}""", null, false)]
    [InlineData("keep-last=0", null, false)]
    [InlineData(null, "3", true)]
    [InlineData(null, "0", false)]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    public void Retention_is_read_from_prune_backups_or_maxfiles(string? pruneBackups, string? maxFiles, bool? expected)
        => Assert.Equal(expected, DiagnosticEngine.BackupRetention(pruneBackups, maxFiles));
}
