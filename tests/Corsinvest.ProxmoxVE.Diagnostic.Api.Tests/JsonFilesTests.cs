/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// The JSON files users write by hand: the ignore file and settings.json, read the way the CLI
/// reads them (System.Text.Json). The enum attributes used to be Newtonsoft-only, so the
/// documented examples threw, and a partial settings file silently reset nested defaults.
/// </summary>
public class JsonFilesTests
{
    // ----- ignore file -----
    [Fact]
    public void Ignore_rule_with_enum_names_as_in_the_docs_is_read()
    {
        var rules = JsonSerializer.Deserialize<List<DiagnosticResult>>(
            """[{ "Id": "nodes/pve01/.*", "Context": "Qemu", "Gravity": "Warning" }]""")!;

        var rule = Assert.Single(rules);
        Assert.Equal(DiagnosticResultContext.Qemu, rule.Context);
        Assert.Equal(DiagnosticResultGravity.Warning, rule.Gravity);
    }

    [Fact]
    public void Ignore_rule_with_enum_numbers_still_reads()
    {
        // Files written before the fix have numbers (what create-ignored-issues used to write).
        var json = $$"""[{ "Context": {{(int)DiagnosticResultContext.Qemu}}, "Gravity": {{(int)DiagnosticResultGravity.Warning}} }]""";
        var rule = Assert.Single(JsonSerializer.Deserialize<List<DiagnosticResult>>(json)!);

        Assert.Equal(DiagnosticResultContext.Qemu, rule.Context);
        Assert.Equal(DiagnosticResultGravity.Warning, rule.Gravity);
    }

    [Fact]
    public void Findings_serialize_enums_as_names_and_leave_out_tag()
    {
        var finding = new DiagnosticResult { Context = DiagnosticResultContext.Lxc, Gravity = DiagnosticResultGravity.Critical, Tag = new object() };
        var json = JsonSerializer.Serialize(finding);

        Assert.Contains("\"Lxc\"", json);
        Assert.Contains("\"Critical\"", json);
        Assert.DoesNotContain("Tag", json);
    }

    // ----- settings.json -----
    [Fact]
    public void Partial_settings_file_keeps_the_defaults_it_does_not_mention()
    {
        // Only Qemu.Cpu is set: Qemu.HealthScore must stay at its own default (60/40), not the
        // class default of SettingsThresholdHost (70/50) that a replaced object would carry.
        var settings = JsonSerializer.Deserialize<Settings>("""{ "Qemu": { "Cpu": { "Warning": 80, "Critical": 90 } } }""",
                                                            Settings.JsonOptions)!;

        Assert.Equal(80, settings.Qemu.Cpu.Warning);
        Assert.Equal(60, settings.Qemu.HealthScore.Warning);
        Assert.Equal(40, settings.Qemu.HealthScore.Critical);
        Assert.Equal(50, settings.Qemu.Rrd.Pressure.Cpu.Warning);
    }

    [Fact]
    public void Settings_accept_enum_names_comments_and_trailing_commas()
    {
        var settings = JsonSerializer.Deserialize<Settings>("""
            {
              // weekly view for nodes
              "Node": { "Rrd": { "TimeFrame": "Week", "Consolidation": "Maximum", }, },
            }
            """, Settings.JsonOptions)!;

        Assert.Equal(RrdDataTimeFrame.Week, settings.Node.Rrd.TimeFrame);
        Assert.Equal(RrdDataConsolidation.Maximum, settings.Node.Rrd.Consolidation);
    }

    [Fact]
    public void Node_rrd_is_a_single_setting_with_node_pressure_defaults()
    {
        // Node used to hide the inherited Rrd with a 'new' property: the fetch read one, the
        // threshold labels read the other, so a Week timeframe was reported as "rrd Day".
        var settings = JsonSerializer.Deserialize<Settings>("""{ "Node": { "Rrd": { "TimeFrame": "Week" } } }""",
                                                            Settings.JsonOptions)!;
        SettingsThresholdHost asHost = settings.Node;

        Assert.Same(settings.Node.Rrd, asHost.Rrd);
        Assert.Equal(RrdDataTimeFrame.Week, asHost.Rrd.TimeFrame);
        Assert.Equal(40, settings.Node.Rrd.Pressure.Cpu.Warning);
    }

    [Fact]
    public void Settings_written_by_create_settings_read_back_unchanged()
    {
        var written = JsonSerializer.Serialize(Settings.Full(), Settings.JsonOptions);
        var read = JsonSerializer.Deserialize<Settings>(written, Settings.JsonOptions)!;

        Assert.Contains("\"Day\"", written);
        Assert.Equal(written, JsonSerializer.Serialize(read, Settings.JsonOptions));
    }
}
