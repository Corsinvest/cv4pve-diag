/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests.Helpers;

public class DebianVersionTests
{
    // ----- equality -----
    [Theory]
    [InlineData("1.0", "1.0")]
    [InlineData("1.0-1", "1.0-1")]
    [InlineData("0:1.0", "1.0")]               // explicit zero epoch == implicit
    [InlineData("1.0-0", "1.0-0")]
    [InlineData("8.2.4", "8.2.4")]
    public void Compare_equal_versions_returns_zero(string a, string b)
        => Assert.Equal(0, DebianVersion.Compare(a, b));

    // ----- ordering: a < b -----
    [Theory]
    [InlineData("1.0", "1.1")]
    [InlineData("1.0", "2.0")]
    [InlineData("1.9", "1.10")]                // numeric, not lexical
    [InlineData("8.2.4~rc1", "8.2.4")]         // ~ precedes everything, even nothing
    [InlineData("8.2.4~rc1", "8.2.4~rc2")]
    [InlineData("8.2.4", "8.2.4-1")]           // missing revision sorts before any revision
    [InlineData("3.0.11-1~deb12u2", "3.0.11-1~deb12u3")]   // the real-world canary
    [InlineData("1:9.9", "2:1.0")]             // epoch dominates
    [InlineData("", "1.0")]                     // empty < any non-empty
    public void Compare_less_than(string a, string b)
        => Assert.True(DebianVersion.Compare(a, b) < 0,
                       $"expected '{a}' < '{b}', got {DebianVersion.Compare(a, b)}");

    // ----- ordering: a > b (mirror of above) -----
    [Theory]
    [InlineData("1.1", "1.0")]
    [InlineData("8.2.4", "8.2.4~rc1")]
    [InlineData("8.2.4-1", "8.2.4")]
    [InlineData("3.0.11-1~deb12u3", "3.0.11-1~deb12u2")]
    [InlineData("2:1.0", "1:9.9")]
    [InlineData("1.0", "")]
    public void Compare_greater_than(string a, string b)
        => Assert.True(DebianVersion.Compare(a, b) > 0,
                       $"expected '{a}' > '{b}', got {DebianVersion.Compare(a, b)}");

    // ----- null / whitespace handling -----
    [Fact]
    public void Compare_both_null_returns_zero()
        => Assert.Equal(0, DebianVersion.Compare(null, null));

    [Fact]
    public void Compare_null_lhs_returns_negative()
        => Assert.True(DebianVersion.Compare(null, "1.0") < 0);

    [Fact]
    public void Compare_null_rhs_returns_positive()
        => Assert.True(DebianVersion.Compare("1.0", null) > 0);

    [Theory]
    [InlineData("  1.0  ", "1.0")]
    [InlineData("\t1.0\n", "1.0")]
    public void Compare_trims_whitespace(string a, string b)
        => Assert.Equal(0, DebianVersion.Compare(a, b));

    // ----- tilde precedes everything (the tricky rule) -----
    // ~ sorts BEFORE end-of-string, but a letter sorts AFTER end-of-string,
    // so "1~~a" > "1~~" (same prefix, then 'a' vs nothing → 'a' wins).
    [Theory]
    [InlineData("1~", "1")]                    // ~ alone < nothing
    [InlineData("1~~", "1~")]                  // earlier in the ~ chain
    [InlineData("1~a", "1")]                   // ~a < nothing because the ~ at position 1 wins
    public void Compare_tilde_sorts_before(string a, string b)
        => Assert.True(DebianVersion.Compare(a, b) < 0,
                       $"expected '{a}' < '{b}' (tilde rule)");

    // ----- letters before non-letter symbols within a non-digit run -----
    // Per Debian: in a non-digit run, letters sort before non-letter non-tilde chars.
    // Note: '-' splits upstream/revision so it can't be tested directly inside upstream.
    [Theory]
    [InlineData("1.0a", "1.0+")]               // 'a' (letter) < '+' (symbol)
    public void Compare_letters_before_symbols(string a, string b)
        => Assert.True(DebianVersion.Compare(a, b) < 0,
                       $"expected '{a}' < '{b}' (letter-before-symbol rule)");

    // ----- subtle ~ + letter interactions (regression guard) -----
    // After equal prefix "1~~", left has "a" and right has end-of-string.
    // Letters sort AFTER end-of-string, so "1~~a" > "1~~".
    [Fact]
    public void Compare_letter_after_tilde_chain_is_greater_than_chain_alone()
        => Assert.True(DebianVersion.Compare("1~~a", "1~~") > 0);

    // ----- the CVE use case: skip when installed >= fixed -----
    [Theory]
    [InlineData("3.0.11-1~deb12u3", "3.0.11-1~deb12u2", true)]   // installed newer → patched
    [InlineData("3.0.11-1~deb12u2", "3.0.11-1~deb12u2", true)]   // installed equal → patched
    [InlineData("3.0.11-1~deb12u1", "3.0.11-1~deb12u2", false)]  // installed older → still vulnerable
    public void Compare_models_the_fixed_version_check(string installed, string fixedInRelease, bool patched)
    {
        var cmp = DebianVersion.Compare(installed, fixedInRelease);
        Assert.Equal(patched, cmp >= 0);
    }
}
