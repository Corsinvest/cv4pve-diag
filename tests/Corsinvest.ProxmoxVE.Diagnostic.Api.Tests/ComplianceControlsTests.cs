/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// Structural tests on the compliance control catalog. These do not exercise the diagnostic
/// engine — they guard against typos and refactor accidents in the catalog itself
/// (duplicate keys, empty titles, standards declared in the enum but missing from the catalog).
/// </summary>
public class ComplianceControlsTests
{
    [Fact]
    public void Catalog_has_no_duplicate_standard_and_id_pairs()
    {
        var duplicates = ComplianceControls.All
                                           .GroupBy(m => (m.Standard, m.ControlId))
                                           .Where(g => g.Count() > 1)
                                           .Select(g => $"{g.Key.Standard}/{g.Key.ControlId} (x{g.Count()})")
                                           .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Every_declared_standard_has_at_least_one_control()
    {
        var standardsInCatalog = ComplianceControls.All.Select(m => m.Standard).ToHashSet();

        var missing = Enum.GetValues<ComplianceStandard>()
                          .Where(s => !standardsInCatalog.Contains(s))
                          .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void No_control_has_an_empty_title()
    {
        var empties = ComplianceControls.All
                                        .Where(m => string.IsNullOrWhiteSpace(m.ControlTitle))
                                        .Select(m => $"{m.Standard}/{m.ControlId}")
                                        .ToList();

        Assert.Empty(empties);
    }

    [Fact]
    public void No_control_has_an_empty_id()
    {
        var empties = ComplianceControls.All
                                        .Where(m => string.IsNullOrWhiteSpace(m.ControlId))
                                        .Select(m => $"{m.Standard}/<empty>")
                                        .ToList();

        Assert.Empty(empties);
    }

    [Fact]
    public void Find_round_trips_every_catalog_entry()
    {
        foreach (var entry in ComplianceControls.All)
        {
            var found = ComplianceControls.Find(entry.Standard, entry.ControlId);
            Assert.NotNull(found);
            Assert.Equal(entry, found);
        }
    }

    [Fact]
    public void Find_returns_null_for_unknown_control()
    {
        Assert.Null(ComplianceControls.Find(ComplianceStandard.Iso27001, "A.0.0.0-does-not-exist"));
    }
}
