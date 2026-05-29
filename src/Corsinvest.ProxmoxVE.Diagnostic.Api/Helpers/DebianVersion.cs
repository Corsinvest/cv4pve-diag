/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;

/// <summary>
/// Debian-style version comparison, equivalent to <c>dpkg --compare-versions</c>.
/// Reference: <c>verrevcmp</c> in dpkg's <c>lib/dpkg/version.c</c>
/// and <c>deb-version(7)</c>.
///
/// Format: <c>[epoch:]upstream-version[-debian-revision]</c>.
/// Both upstream and revision are compared with the same algorithm:
/// alternate non-digit and digit runs. In a non-digit run the character order is:
/// <c>~</c> (sorts before everything, even end-of-string) &lt; end-of-string &lt; letters &lt; the rest.
/// An absent debian-revision sorts before <c>"0"</c>.
/// </summary>
internal static class DebianVersion
{
    /// <summary>
    /// Compares two Debian version strings. Returns a negative number if
    /// <paramref name="a"/> &lt; <paramref name="b"/>, zero if equal, positive if greater.
    /// Null or whitespace is treated as the lowest possible version.
    /// </summary>
    public static int Compare(string? a, string? b)
    {
        a = (a ?? "").Trim();
        b = (b ?? "").Trim();
        if (a.Length == 0 && b.Length == 0) { return 0; }
        if (a.Length == 0) { return -1; }
        if (b.Length == 0) { return 1; }

        var (epochA, upstreamA, revisionA) = Split(a);
        var (epochB, upstreamB, revisionB) = Split(b);

        var cmp = epochA.CompareTo(epochB);
        if (cmp != 0) { return cmp; }

        cmp = VerRevCmp(upstreamA, upstreamB);
        if (cmp != 0) { return cmp; }

        return VerRevCmp(revisionA, revisionB);
    }

    // Splits "[epoch:]upstream[-revision]" into its three parts. Missing epoch → 0, missing revision → "".
    private static (int Epoch, string Upstream, string Revision) Split(string v)
    {
        var epoch = 0;
        var colon = v.IndexOf(':');
        if (colon > 0 && int.TryParse(v.AsSpan(0, colon), out var e))
        {
            epoch = e;
            v = v[(colon + 1)..];
        }

        // The Debian revision is everything after the LAST '-'.
        // If there is no '-', the whole thing is upstream and the revision is empty.
        var dash = v.LastIndexOf('-');
        return dash >= 0
            ? (epoch, v[..dash], v[(dash + 1)..])
            : (epoch, v, "");
    }

    // The core dpkg comparison: alternate non-digit and digit segments.
    private static int VerRevCmp(string a, string b)
    {
        var i = 0;
        var j = 0;
        while (i < a.Length || j < b.Length)
        {
            // 1) Non-digit run, with the Debian character ordering.
            var diff = 0;
            while ((i < a.Length && !char.IsDigit(a[i])) || (j < b.Length && !char.IsDigit(b[j])))
            {
                var ac = i < a.Length ? Order(a[i]) : 0;
                var bc = j < b.Length ? Order(b[j]) : 0;
                if (ac != bc) { return ac - bc; }
                i++;
                j++;
            }

            // 2) Skip leading zeros so the longer "number" wins only by value.
            while (i < a.Length && a[i] == '0') { i++; }
            while (j < b.Length && b[j] == '0') { j++; }

            // 3) Digit run: the longer digit run wins; if same length, compare digit by digit.
            while (i < a.Length && j < b.Length && char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                if (diff == 0) { diff = a[i] - b[j]; }
                i++;
                j++;
            }
            if (i < a.Length && char.IsDigit(a[i])) { return 1; }
            if (j < b.Length && char.IsDigit(b[j])) { return -1; }
            if (diff != 0) { return diff; }
        }
        return 0;
    }

    // Debian sorting order for non-digit characters inside a version segment.
    //   '~' sorts before everything, including the empty string (we return a NEGATIVE value).
    //   Letters sort according to their code point.
    //   Other characters (punctuation: +, -, .) sort AFTER letters: we offset by 256.
    private static int Order(char c)
    {
        if (c == '~') { return -1; }
        if (char.IsLetter(c)) { return c; }
        return c + 256;
    }
}
