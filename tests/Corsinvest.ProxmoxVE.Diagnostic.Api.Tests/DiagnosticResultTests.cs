/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

public class DiagnosticResultTests
{
    // -------- DecodeContext --------

    [Theory]
    [InlineData("node", DiagnosticResultContext.Node)]
    [InlineData("Node", DiagnosticResultContext.Node)]
    [InlineData("NODE", DiagnosticResultContext.Node)]
    [InlineData("storage", DiagnosticResultContext.Storage)]
    [InlineData("qemu", DiagnosticResultContext.Qemu)]
    [InlineData("lxc", DiagnosticResultContext.Lxc)]
    [InlineData("cluster", DiagnosticResultContext.Cluster)]
    public void DecodeContext_recognises_known_values_case_insensitive(string text, DiagnosticResultContext expected)
        => Assert.Equal(expected, DiagnosticResult.DecodeContext(text));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    [InlineData("vm")]              // PVE sometimes uses "vm" not "qemu" — falls through
    public void DecodeContext_unknown_falls_back_to_Cluster(string text)
        => Assert.Equal(DiagnosticResultContext.Cluster, DiagnosticResult.DecodeContext(text));

    // -------- CheckIgnoreIssue: positive matches --------

    [Fact]
    public void CheckIgnoreIssue_exact_match_on_all_fields_returns_true()
    {
        var rule = new DiagnosticResult
        {
            Id = "nodes/pve01/qemu/100",
            ErrorCode = "WG0017",
            SubContext = "Backup",
            Description = "vzdump backup not configured",
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        };
        var finding = new DiagnosticResult
        {
            Id = "nodes/pve01/qemu/100",
            ErrorCode = "WG0017",
            SubContext = "Backup",
            Description = "vzdump backup not configured",
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        };
        Assert.True(rule.CheckIgnoreIssue(finding));
    }

    [Fact]
    public void CheckIgnoreIssue_regex_pattern_matches_id_substring()
    {
        var rule = new DiagnosticResult
        {
            Id = @"nodes/pve01/qemu/\d+",       // regex matches any vmid on pve01
            ErrorCode = "WG0017",
            SubContext = "Backup",
            Description = ".*",
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        };
        var finding = new DiagnosticResult
        {
            Id = "nodes/pve01/qemu/100",
            ErrorCode = "WG0017",
            SubContext = "Backup",
            Description = "vzdump backup not configured",
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        };
        Assert.True(rule.CheckIgnoreIssue(finding));
    }

    [Fact]
    public void CheckIgnoreIssue_pattern_on_description_matches_substring()
    {
        var rule = new DiagnosticResult
        {
            Id = ".*",
            ErrorCode = "WG0019",
            SubContext = "Backup",
            Description = "older than 60 days",
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        };
        var finding = new DiagnosticResult
        {
            Id = "nodes/pve01/qemu/100",
            ErrorCode = "WG0019",
            SubContext = "Backup",
            Description = "1 backup older than 60 days (88.05 GB)",
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        };
        Assert.True(rule.CheckIgnoreIssue(finding));
    }

    // -------- CheckIgnoreIssue: negative matches --------

    [Fact]
    public void CheckIgnoreIssue_different_ErrorCode_returns_false()
    {
        var rule = MakeRule(errorCode: "WG0017");
        var finding = MakeFinding(errorCode: "WG0019");
        Assert.False(rule.CheckIgnoreIssue(finding));
    }

    [Fact]
    public void CheckIgnoreIssue_different_Id_returns_false()
    {
        var rule = MakeRule(id: "nodes/pve01/qemu/100");
        var finding = MakeFinding(id: "nodes/pve02/qemu/200");
        Assert.False(rule.CheckIgnoreIssue(finding));
    }

    [Fact]
    public void CheckIgnoreIssue_different_Context_returns_false()
    {
        var rule = MakeRule(context: DiagnosticResultContext.Qemu);
        var finding = MakeFinding(context: DiagnosticResultContext.Lxc);
        Assert.False(rule.CheckIgnoreIssue(finding));
    }

    // -------- CheckIgnoreIssue: documented quirks --------
    // Context/Gravity defaults are treated as "any" so a rule can ignore across all contexts/gravities.

    [Fact]
    public void CheckIgnoreIssue_default_Context_matches_any()
    {
        var rule = new DiagnosticResult
        {
            Id = ".*",
            ErrorCode = ".*",
            SubContext = ".*",
            Description = ".*",
            // Context left at default → should match any finding context
            Gravity = DiagnosticResultGravity.Warning,
        };
        Assert.True(rule.CheckIgnoreIssue(MakeFinding(context: DiagnosticResultContext.Lxc)));
        Assert.True(rule.CheckIgnoreIssue(MakeFinding(context: DiagnosticResultContext.Storage)));
    }

    // Gravity.Info == default(0), so an Info rule matches findings of any gravity by design.
    // Documented here so a future change of enum order doesn't silently break ignore rules.
    [Fact]
    public void CheckIgnoreIssue_default_Gravity_Info_matches_any_gravity()
    {
        var rule = new DiagnosticResult
        {
            Id = ".*",
            ErrorCode = ".*",
            SubContext = ".*",
            Description = ".*",
            Context = DiagnosticResultContext.Qemu,
            // Gravity left at default (Info=0) → matches any
        };
        Assert.True(rule.CheckIgnoreIssue(MakeFinding(gravity: DiagnosticResultGravity.Critical)));
        Assert.True(rule.CheckIgnoreIssue(MakeFinding(gravity: DiagnosticResultGravity.Warning)));
    }

    [Fact]
    public void CheckIgnoreIssue_non_default_Gravity_must_match_exactly()
    {
        var rule = MakeRule(gravity: DiagnosticResultGravity.Warning);
        Assert.False(rule.CheckIgnoreIssue(MakeFinding(gravity: DiagnosticResultGravity.Critical)));
        Assert.True(rule.CheckIgnoreIssue(MakeFinding(gravity: DiagnosticResultGravity.Warning)));
    }

    // -------- helpers --------

    private static DiagnosticResult MakeRule(
        string id = ".*",
        string errorCode = ".*",
        string subContext = ".*",
        string description = ".*",
        DiagnosticResultContext context = DiagnosticResultContext.Qemu,
        DiagnosticResultGravity gravity = DiagnosticResultGravity.Warning)
        => new()
        {
            Id = id,
            ErrorCode = errorCode,
            SubContext = subContext,
            Description = description,
            Context = context,
            Gravity = gravity,
        };

    private static DiagnosticResult MakeFinding(
        string id = "nodes/pve01/qemu/100",
        string errorCode = "WG0017",
        string subContext = "Backup",
        string description = "vzdump backup not configured",
        DiagnosticResultContext context = DiagnosticResultContext.Qemu,
        DiagnosticResultGravity gravity = DiagnosticResultGravity.Warning)
        => new()
        {
            Id = id,
            ErrorCode = errorCode,
            SubContext = subContext,
            Description = description,
            Context = context,
            Gravity = gravity,
        };
}
