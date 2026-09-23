/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;

/// <summary>
/// VLAN id lists as Proxmox VE writes them: <c>bridge-vids</c> on a host bridge
/// (<c>"2-4094"</c>, <c>"10 20 100-200"</c>) and <c>trunks</c> on a guest NIC (<c>"10;20;30-40"</c>).
/// </summary>
internal static class VlanIds
{
    /// <summary>
    /// Parses a VLAN id list into inclusive ranges. Space, comma and semicolon all separate items;
    /// malformed items and single ids outside 1–4094 are skipped. A range reaching past the valid
    /// ids is clamped rather than dropped: PVE accepts <c>trunks=1-4095</c>, which means "all VLANs".
    /// </summary>
    public static IReadOnlyList<(int From, int To)> Parse(string? value)
    {
        var ranges = new List<(int From, int To)>();
        foreach (var item in (value ?? "").Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('-');
            if (parts.Length == 1 && int.TryParse(parts[0], out var id) && IsValid(id))
            {
                ranges.Add((id, id));
            }
            else if (parts.Length == 2
                     && int.TryParse(parts[0], out var from)
                     && int.TryParse(parts[1], out var to)
                     && from <= to)
            {
                from = Math.Max(from, 1);
                to = Math.Min(to, 4094);
                if (from <= to) { ranges.Add((from, to)); }
            }
        }
        return ranges;
    }

    /// <summary>Every VLAN id covered by <paramref name="ranges"/>, in ascending order.</summary>
    public static IEnumerable<int> Expand(IReadOnlyList<(int From, int To)> ranges)
        => ranges.SelectMany(r => Enumerable.Range(r.From, r.To - r.From + 1)).Distinct().Order();

    /// <summary>
    /// Compacts VLAN ids into a readable list of ranges: <c>[2,3,4,7,9,10]</c> → <c>"2-4, 7, 9-10"</c>.
    /// A trunk carrying every VLAN would otherwise print thousands of numbers.
    /// </summary>
    public static string Format(IEnumerable<int> ids)
    {
        var parts = new List<string>();
        int? start = null, prev = null;
        foreach (var id in ids.Distinct().Order())
        {
            if (prev.HasValue && id == prev + 1) { prev = id; continue; }
            if (start.HasValue) { parts.Add(start == prev ? $"{start}" : $"{start}-{prev}"); }
            start = prev = id;
        }
        if (start.HasValue) { parts.Add(start == prev ? $"{start}" : $"{start}-{prev}"); }
        return string.Join(", ", parts);
    }

    public static bool Contains(IReadOnlyList<(int From, int To)> ranges, int id)
        => ranges.Any(r => id >= r.From && id <= r.To);

    private static bool IsValid(int id) => id is >= 1 and <= 4094;
}
