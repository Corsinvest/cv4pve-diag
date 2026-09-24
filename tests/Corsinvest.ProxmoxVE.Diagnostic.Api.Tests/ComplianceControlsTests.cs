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

    // Every ComplianceMapping field declared in a standard's class (ComplianceControls.Nis2, ...).
    private static IEnumerable<(string Class, ComplianceMapping Mapping)> DeclaredFields()
        => typeof(ComplianceControls).GetNestedTypes()
                                     .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                                                       .Where(f => f.FieldType == typeof(ComplianceMapping))
                                                       .Select(f => (t.Name, (ComplianceMapping)f.GetValue(null)!)));

    [Fact]
    public void Every_control_belongs_to_the_standard_of_its_class()
    {
        // A copy-pasted entry (e.g. a C5 control created with ComplianceStandard.Ens) would be
        // reported under the wrong standard.
        var wrong = DeclaredFields().Where(f => f.Mapping.Standard.ToString() != f.Class)
                                    .Select(f => $"{f.Class}: {f.Mapping.Standard}/{f.Mapping.ControlId}")
                                    .ToList();
        Assert.Empty(wrong);
    }

    [Fact]
    public void Every_declared_control_is_in_the_catalog()
    {
        // A control missing from its class' All list is used by the checks but unknown to
        // Find / GetTitle, so reports show it without a title.
        var catalog = ComplianceControls.All.ToHashSet();
        var missing = DeclaredFields().Where(f => !catalog.Contains(f.Mapping))
                                      .Select(f => $"{f.Class}/{f.Mapping.ControlId}")
                                      .ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Find_returns_null_for_unknown_control()
    {
        Assert.Null(ComplianceControls.Find(ComplianceStandard.Iso27001, "A.0.0.0-does-not-exist"));
    }
}
