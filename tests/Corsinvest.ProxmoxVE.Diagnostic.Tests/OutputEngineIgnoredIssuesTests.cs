/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Diagnostic.Api;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Tests;

public class OutputEngineIgnoredIssuesTests
{
    // -------- LoadIgnoredIssues: the file --------

    [Fact]
    public void LoadIgnoredIssues_reads_names_comments_and_trailing_commas()
    {
        var rules = OutputEngine.LoadIgnoredIssues("""
            [
              // accepted on the lab node
              { "ErrorCode": "IG0011", "Id": "^nodes/pve01/qemu/100$", "Context": "Qemu", "Gravity": "Info", },
            ]
            """);

        var rule = Assert.Single(rules);
        Assert.Equal("IG0011", rule.ErrorCode);
        Assert.Equal("^nodes/pve01/qemu/100$", rule.Id);
        Assert.Equal(DiagnosticResultContext.Qemu, rule.Context);
        Assert.Equal(DiagnosticResultGravity.Info, rule.Gravity);
    }

    [Fact]
    public void LoadIgnoredIssues_reads_the_file_of_older_versions()
    {
        var rules = OutputEngine.LoadIgnoredIssues("""
            [ { "Id": null, "ErrorCode": "WG0017", "Context": 0, "SubContext": null, "Description": null, "Gravity": 0, "Compliance": [], "IsIgnoredIssue": false } ]
            """);

        var rule = Assert.Single(rules);
        Assert.Null(rule.Context);
        Assert.Null(rule.Gravity);
        Assert.Empty(rule.Validate());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    public void LoadIgnoredIssues_no_rules_is_an_empty_list(string json)
        => Assert.Empty(OutputEngine.LoadIgnoredIssues(json));

    [Theory]
    [InlineData("""[ { "Id": "nodes/(" } ]""")]
    [InlineData("""[ { "Gravity": "Fatal" } ]""")]
    [InlineData("""[ { "Context": 99 } ]""")]
    public void LoadIgnoredIssues_rule_that_cannot_be_applied_does_not_throw(string json)
        => Assert.NotEmpty(Assert.Single(OutputEngine.LoadIgnoredIssues(json)).Validate());

    [Theory]
    [InlineData("[ { \"Id\": ")]
    [InlineData("not json")]
    [InlineData("""{ "Id": "nodes/pve01" }""")]
    public void LoadIgnoredIssues_file_that_is_not_a_list_of_rules_throws(string json)
        => Assert.Throws<JsonException>(() => OutputEngine.LoadIgnoredIssues(json));

    // -------- WarnInvalidIgnoredIssues: said before the analysis --------

    [Fact]
    public void WarnInvalidIgnoredIssues_valid_rules_write_nothing()
    {
        var writer = new StringWriter();

        OutputEngine.WarnInvalidIgnoredIssues(OutputEngine.LoadIgnoredIssues("""
            [ { "Gravity": "Info" }, { "Id": "^nodes/no-such-node$" }, { } ]
            """), writer);

        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void WarnInvalidIgnoredIssues_no_rules_write_nothing()
    {
        var writer = new StringWriter();
        OutputEngine.WarnInvalidIgnoredIssues([], writer);
        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void WarnInvalidIgnoredIssues_writes_one_line_for_each_broken_rule_with_its_position()
    {
        var writer = new StringWriter();

        OutputEngine.WarnInvalidIgnoredIssues(OutputEngine.LoadIgnoredIssues("""
            [ { "Gravity": "Info" }, { "Id": "nodes/(" }, { "Gravity": "Fatal" }, { "Context": 99 }, { } ]
            """), writer);

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("WARNING: Ignore rule #2 is not applied: invalid regular expression in Id 'nodes/('", lines[0]);
        Assert.Equal("WARNING: Ignore rule #3 is not applied: 'Fatal' is not a valid DiagnosticResultGravity. Values: Info, Warning, Critical, Ok", lines[1]);
        Assert.Equal("WARNING: Ignore rule #4 is not applied: '99' is not a valid DiagnosticResultContext. Values: Node, Cluster, Storage, Qemu, Lxc", lines[2]);
    }

    [Fact]
    public void WarnInvalidIgnoredIssues_two_problems_in_one_rule_are_on_one_line()
    {
        var writer = new StringWriter();

        OutputEngine.WarnInvalidIgnoredIssues(OutputEngine.LoadIgnoredIssues("""
            [ { "Id": "nodes/(", "Gravity": "Fatal" } ]
            """), writer);

        var line = Assert.Single(writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("WARNING: Ignore rule #1 is not applied: ", line);
        Assert.Contains("'Fatal' is not a valid DiagnosticResultGravity", line);
        Assert.Contains("invalid regular expression in Id 'nodes/('", line);
    }
}
