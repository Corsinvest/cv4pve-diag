/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
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
    public void Json_unknown_name_throws()
        => Assert.Throws<JsonException>(() => Deserialize("""{ "Gravity": "Fatal" }"""));

    [Fact]
    public void Json_explicit_null_means_any()
    {
        var rule = Deserialize("""{ "Context": null, "Gravity": null }""");
        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
    }

    [Theory]
    [InlineData("""{ "Gravity": true }""")]
    [InlineData("""{ "Context": 1.5 }""")]
    [InlineData("""{ "Context": [] }""")]
    public void Json_value_that_is_not_a_name_or_a_number_throws(string json)
        => Assert.Throws<JsonException>(() => Deserialize(json));

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
