/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;
using Xunit;
using Range = Corsinvest.ProxmoxVE.Diagnostic.Api.DiagnosticEngine.CveVersionRange;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// NVD version ranges behind CN0015 / WN0042. The old matching took the first range only,
/// ignored its start and treated versionEndExcluding (the fixed version) as vulnerable.
/// </summary>
public class DiagnosticEngineCveTests
{
    // A cve element shaped like the NVD 2.0 API, with the given cpeMatch entries.
    private static JsonElement Cve(string cpeMatches)
        => JsonDocument.Parse($$"""{ "configurations": [ { "nodes": [ { "cpeMatch": [ {{cpeMatches}} ] } ] } ] }""").RootElement;

    [Fact]
    public void Excluded_end_is_the_fixed_version()
    {
        var range = new Range(EndExcluding: "8.2.5");

        Assert.True(range.Contains("8.2.4"));
        Assert.False(range.Contains("8.2.5"));
    }

    [Fact]
    public void Start_of_the_range_is_honoured()
    {
        var range = new Range(StartIncluding: "8.0", EndExcluding: "8.2.5");

        Assert.False(range.Contains("7.4.17"));
        Assert.True(range.Contains("8.1.4"));
    }

    [Fact]
    public void Every_range_of_the_cve_is_read_not_only_the_first()
    {
        var ranges = DiagnosticEngine.ExtractProxmoxVersionRanges(Cve("""
            { "criteria": "cpe:2.3:a:proxmox:virtual_environment:*:*:*:*:*:*:*:*", "versionStartIncluding": "7.0", "versionEndExcluding": "7.4.18" },
            { "criteria": "cpe:2.3:a:proxmox:virtual_environment:*:*:*:*:*:*:*:*", "versionStartIncluding": "8.0", "versionEndExcluding": "8.2.8" },
            { "criteria": "cpe:2.3:a:other:product:*:*:*:*:*:*:*:*", "versionEndExcluding": "99" }
            """));

        Assert.Equal(2, ranges.Count);
        Assert.True(DiagnosticEngine.CveApplies(ranges, "8.2.7"));
        Assert.False(DiagnosticEngine.CveApplies(ranges, "8.4.21"));
    }

    [Fact]
    public void Concrete_version_in_the_cpe_is_an_exact_match()
    {
        var ranges = DiagnosticEngine.ExtractProxmoxVersionRanges(Cve("""
            { "criteria": "cpe:2.3:a:proxmox:virtual_environment:7.2:*:*:*:*:*:*:*" }
            """));

        Assert.True(DiagnosticEngine.CveApplies(ranges, "7.2"));
        Assert.False(DiagnosticEngine.CveApplies(ranges, "7.3"));
    }

    [Fact]
    public void Range_without_any_bound_is_ignored()
        => Assert.Empty(DiagnosticEngine.ExtractProxmoxVersionRanges(Cve("""
            { "criteria": "cpe:2.3:a:proxmox:virtual_environment:*:*:*:*:*:*:*:*" }
            """)));

    [Fact]
    public void Unknown_installed_version_matches_nothing()
        => Assert.False(DiagnosticEngine.CveApplies([new Range(EndExcluding: "99")], ""));

    // ----- DebianVersion: a digit against a non-digit counts as 0, as in dpkg -----
    [Theory]
    [InlineData("1.2", "1.a")]
    [InlineData("1.2", "1.+x")]
    public void Digit_sorts_before_letters_and_punctuation(string lower, string higher)
        => Assert.True(DebianVersion.Compare(lower, higher) < 0);
}
