/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

public class DiagnosticIgnoreRuleTests
{
    // -------- IsMatch: fields left null --------

    [Fact]
    public void IsMatch_empty_rule_matches_every_finding()
        => Assert.True(new DiagnosticIgnoreRule().IsMatch(MakeFinding()));

    [Fact]
    public void IsMatch_null_Context_and_Gravity_match_any()
    {
        var rule = new DiagnosticIgnoreRule { ErrorCode = "WG0017" };
        Assert.True(rule.IsMatch(MakeFinding(context: DiagnosticResultContext.Lxc, gravity: DiagnosticResultGravity.Critical)));
        Assert.True(rule.IsMatch(MakeFinding(context: DiagnosticResultContext.Node, gravity: DiagnosticResultGravity.Info)));
    }

    // -------- IsMatch: Node and Info are values like the others --------

    [Fact]
    public void IsMatch_Context_Node_matches_only_Node()
    {
        var rule = new DiagnosticIgnoreRule { Context = DiagnosticResultContext.Node };
        Assert.True(rule.IsMatch(MakeFinding(context: DiagnosticResultContext.Node)));
        Assert.False(rule.IsMatch(MakeFinding(context: DiagnosticResultContext.Qemu)));
        Assert.False(rule.IsMatch(MakeFinding(context: DiagnosticResultContext.Storage)));
    }

    [Fact]
    public void IsMatch_Gravity_Info_matches_only_Info()
    {
        var rule = new DiagnosticIgnoreRule { Gravity = DiagnosticResultGravity.Info };
        Assert.True(rule.IsMatch(MakeFinding(gravity: DiagnosticResultGravity.Info)));
        Assert.False(rule.IsMatch(MakeFinding(gravity: DiagnosticResultGravity.Warning)));
        Assert.False(rule.IsMatch(MakeFinding(gravity: DiagnosticResultGravity.Critical)));
    }

    // -------- IsMatch: patterns --------

    [Fact]
    public void IsMatch_unanchored_Id_matches_a_longer_id()
    {
        var rule = new DiagnosticIgnoreRule { Id = "nodes/pve01/qemu/10" };
        Assert.True(rule.IsMatch(MakeFinding(id: "nodes/pve01/qemu/100")));
    }

    [Fact]
    public void IsMatch_anchored_Id_matches_one_resource()
    {
        var rule = new DiagnosticIgnoreRule { Id = "^nodes/pve01/qemu/10$" };
        Assert.True(rule.IsMatch(MakeFinding(id: "nodes/pve01/qemu/10")));
        Assert.False(rule.IsMatch(MakeFinding(id: "nodes/pve01/qemu/100")));
    }

    [Fact]
    public void IsMatch_different_ErrorCode_returns_false()
    {
        var rule = new DiagnosticIgnoreRule { ErrorCode = "^WG0019$" };
        Assert.False(rule.IsMatch(MakeFinding(errorCode: "WG0017")));
    }

    [Fact]
    public void IsMatch_pattern_on_a_null_field_of_the_finding_does_not_throw()
    {
        var rule = new DiagnosticIgnoreRule { SubContext = "Backup" };
        Assert.False(rule.IsMatch(new DiagnosticResult { Id = "nodes/pve01" }));
    }

    // -------- JSON: names are exact, numbers are the legacy format --------

    [Fact]
    public void Json_names_are_read_as_exact_values()
    {
        var rule = Deserialize("""{ "Context": "Node", "Gravity": "info" }""");
        Assert.Equal(DiagnosticResultContext.Node, rule.Context);
        Assert.Equal(DiagnosticResultGravity.Info, rule.Gravity);
    }

    [Fact]
    public void Json_missing_fields_are_null()
    {
        var rule = Deserialize("""{ "ErrorCode": "IG0011" }""");
        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
        Assert.Null(rule.Id);
    }

    [Fact]
    public void Json_legacy_number_zero_means_any()
    {
        var rule = Deserialize("""{ "Context": 0, "Gravity": 0 }""");
        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
    }

    [Fact]
    public void Json_legacy_number_not_zero_is_the_value()
    {
        var rule = Deserialize("""{ "Context": 3, "Gravity": 2 }""");
        Assert.Equal(DiagnosticResultContext.Qemu, rule.Context);
        Assert.Equal(DiagnosticResultGravity.Critical, rule.Gravity);
    }

    [Fact]
    public void Json_legacy_number_of_Ok_is_minus_one()
        => Assert.Equal(DiagnosticResultGravity.Ok, Deserialize("""{ "Gravity": -1 }""").Gravity);

    // A value out of its list does not throw: the rule is read, is not valid and matches nothing
    [Theory]
    [InlineData("""{ "Gravity": 99 }""", "'99' is not a valid DiagnosticResultGravity. Values: Info, Warning, Critical, Ok")]
    [InlineData("""{ "Context": -1 }""", "'-1' is not a valid DiagnosticResultContext. Values: Node, Cluster, Storage, Qemu, Lxc")]
    [InlineData("""{ "Gravity": "Fatal" }""", "'Fatal' is not a valid DiagnosticResultGravity. Values: Info, Warning, Critical, Ok")]
    [InlineData("""{ "Gravity": true }""", "'true' is not a valid DiagnosticResultGravity. Values: Info, Warning, Critical, Ok")]
    [InlineData("""{ "Context": 1.5 }""", "'1.5' is not a valid DiagnosticResultContext. Values: Node, Cluster, Storage, Qemu, Lxc")]
    [InlineData("""{ "Context": [] }""", "'[]' is not a valid DiagnosticResultContext. Values: Node, Cluster, Storage, Qemu, Lxc")]
    public void Json_value_out_of_its_list_makes_the_rule_not_valid(string json, string error)
    {
        var rule = Deserialize(json);
        Assert.Equal([error], rule.Validate());
        Assert.False(rule.IsMatch(MakeFinding()));
    }

    [Fact]
    public void Json_explicit_null_means_any()
    {
        var rule = Deserialize("""{ "Context": null, "Gravity": null }""");
        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
    }

    // The template of the older versions was a whole DiagnosticResult, with numbers
    [Fact]
    public void Json_old_template_is_read_and_matches_as_before()
    {
        var rule = Deserialize("""
            {
              "Id": "nodes/pve01/qemu/100",
              "ErrorCode": "WG0017",
              "Context": 0,
              "SubContext": null,
              "Description": null,
              "Gravity": 0,
              "Compliance": [],
              "IsIgnoredIssue": false
            }
            """);

        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
        Assert.True(rule.IsMatch(MakeFinding(context: DiagnosticResultContext.Qemu, gravity: DiagnosticResultGravity.Warning)));
        Assert.False(rule.IsMatch(MakeFinding(errorCode: "IG0011")));
    }

    [Fact]
    public void Json_roundtrip_reads_back_the_same_rule()
    {
        var rule = new DiagnosticIgnoreRule
        {
            ErrorCode = "IG0011",
            Id = "^nodes/pve01/qemu/100$",
            Context = DiagnosticResultContext.Node,
            Gravity = DiagnosticResultGravity.Info,
        };

        var copy = Deserialize(JsonSerializer.Serialize(rule));

        Assert.Equal(rule.ErrorCode, copy.ErrorCode);
        Assert.Equal(rule.Id, copy.Id);
        Assert.Equal(DiagnosticResultContext.Node, copy.Context);
        Assert.Equal(DiagnosticResultGravity.Info, copy.Gravity);
    }

    [Fact]
    public void Json_roundtrip_writes_names()
    {
        var json = JsonSerializer.Serialize(new DiagnosticIgnoreRule { Context = DiagnosticResultContext.Node, Gravity = DiagnosticResultGravity.Info });
        Assert.Contains("\"Context\":\"Node\"", json);
        Assert.Contains("\"Gravity\":\"Info\"", json);
    }

    // -------- FromLegacy: rules held in a DiagnosticResult --------

    [Fact]
    public void FromLegacy_default_Context_and_Gravity_become_any()
    {
        var rule = DiagnosticIgnoreRule.FromLegacy(new DiagnosticResult { ErrorCode = "WG0017" });
        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
        Assert.True(rule.IsMatch(MakeFinding(gravity: DiagnosticResultGravity.Critical)));
    }

    [Fact]
    public void FromLegacy_other_values_are_kept()
    {
        var rule = DiagnosticIgnoreRule.FromLegacy(new DiagnosticResult
        {
            Context = DiagnosticResultContext.Qemu,
            Gravity = DiagnosticResultGravity.Warning,
        });
        Assert.Equal(DiagnosticResultContext.Qemu, rule.Context);
        Assert.Equal(DiagnosticResultGravity.Warning, rule.Gravity);
    }

    // -------- Validate: a rule that cannot be applied --------

    [Fact]
    public void Validate_rule_that_matches_nothing_is_valid()
        => Assert.Empty(new DiagnosticIgnoreRule { Id = "^nodes/no-such-node/qemu/999999$", Gravity = DiagnosticResultGravity.Ok }.Validate());

    [Fact]
    public void Validate_reports_each_broken_pattern_with_its_field()
    {
        var errors = new DiagnosticIgnoreRule { Id = "nodes/(", Description = "[a-" }.Validate();
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, a => a.StartsWith("invalid regular expression in Id 'nodes/('"));
        Assert.Contains(errors, a => a.StartsWith("invalid regular expression in Description '[a-'"));
    }

    [Fact]
    public void Validate_reports_Context_and_Gravity_out_of_their_values()
    {
        var errors = new DiagnosticIgnoreRule
        {
            Context = (DiagnosticResultContext)99,
            Gravity = (DiagnosticResultGravity)7,
        }.Validate();

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, a => a.StartsWith("'99' is not a valid DiagnosticResultContext"));
        Assert.Contains(errors, a => a.StartsWith("'7' is not a valid DiagnosticResultGravity"));
    }

    // -------- RemoveInvalid: reported as findings, not thrown --------

    [Fact]
    public void RemoveInvalid_keeps_the_valid_rules_and_reports_the_others()
    {
        var first = new DiagnosticIgnoreRule { ErrorCode = "WG0017" };
        var third = new DiagnosticIgnoreRule { Gravity = DiagnosticResultGravity.Info };

        var findings = DiagnosticIgnoreRule.RemoveInvalid([first, new DiagnosticIgnoreRule { Id = "nodes/(" }, third], out var valid);

        Assert.Equal([first, third], valid);
        var finding = Assert.Single(findings);
        Assert.Equal(DiagnosticIgnoreRule.InvalidRuleErrorCode, finding.ErrorCode);
        Assert.Equal(DiagnosticResultGravity.Critical, finding.Gravity);
        Assert.Equal(DiagnosticResultContext.Cluster, finding.Context);
        Assert.StartsWith("Ignore rule #2 is not applied: invalid regular expression in Id 'nodes/('", finding.Description);
        Assert.False(finding.IsIgnoredIssue);
    }

    [Fact]
    public void RemoveInvalid_no_rules_reports_nothing()
    {
        Assert.Empty(DiagnosticIgnoreRule.RemoveInvalid(null, out var valid));
        Assert.Empty(valid);
    }

    [Fact]
    public void InvalidRuleErrorCode_is_stable()
        => Assert.Equal("CU0002", DiagnosticIgnoreRule.InvalidRuleErrorCode);

    // -------- AnalyzeAsync: a broken rule does not stop the analysis --------

    [Fact]
    public async Task AnalyzeAsync_broken_rule_is_a_finding_that_no_rule_hides()
    {
        // Nothing listens on port 1: every call fails at once and the analysis stops early.
        using var httpClient = new HttpClient();
        var engine = new DiagnosticEngine(new PveClient("127.0.0.1", 1) { Timeout = TimeSpan.FromSeconds(5) },
                                          new Settings(),
                                          httpClient);

        var result = await engine.AnalyzeAsync([new DiagnosticIgnoreRule { Id = "nodes/(" }, new DiagnosticIgnoreRule()]);

        var finding = Assert.Single(result, a => a.ErrorCode == DiagnosticIgnoreRule.InvalidRuleErrorCode);
        Assert.StartsWith("Ignore rule #1 is not applied", finding.Description);
        Assert.False(finding.IsIgnoredIssue);
    }

    // -------- helpers --------

    private static DiagnosticIgnoreRule Deserialize(string json)
        => JsonSerializer.Deserialize<DiagnosticIgnoreRule>(json)!;

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
