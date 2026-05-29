/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

public class EnumerableExtensionsTests
{
    // -------- Sum --------

    [Fact]
    public void Sum_empty_returns_zero()
        => Assert.Equal(0UL, Array.Empty<int>().Sum(_ => 0UL));

    [Fact]
    public void Sum_adds_selected_values()
    {
        var data = new[] { 1, 2, 3, 4 };
        Assert.Equal(10UL, data.Sum(x => (ulong)x));
    }

    [Fact]
    public void Sum_supports_large_totals_within_ulong_range()
    {
        // Realistic cluster scenario: summing per-node memory in bytes.
        // 8 nodes × 1 TiB ≈ 8 * 2^40 = 8.8 * 10^12, well below ulong.MaxValue (1.8 * 10^19).
        var data = Enumerable.Repeat(1UL << 40, 8);
        Assert.Equal(8UL << 40, data.Sum(x => x));
    }

    // Documents the current behaviour: Sum does not protect against ulong overflow.
    // It wraps around silently. The whole helper is internal and we control all callers,
    // so the policy is "callers must ensure the running total fits"; this test pins the
    // contract so a future change to a checked() arithmetic is a deliberate, visible decision.
    [Fact]
    public void Sum_wraps_silently_on_overflow()
    {
        var data = new[] { ulong.MaxValue, 1UL };
        Assert.Equal(0UL, data.Sum(x => x));
    }

    // -------- Average --------

    [Fact]
    public void Average_computes_integer_mean()
    {
        var data = new[] { 10, 20, 30 };
        Assert.Equal(20UL, data.Average(x => (ulong)x));
    }

    [Fact]
    public void Average_truncates_toward_zero()
    {
        // Integer division: (1+2+2) / 3 = 5/3 = 1, not 1.67.
        var data = new[] { 1, 2, 2 };
        Assert.Equal(1UL, data.Average(x => (ulong)x));
    }

    // Documents the current behaviour: Average over an empty source throws DivideByZeroException.
    // No caller in the engine passes an empty source today; if that ever changes, the caller
    // must guard. This test pins the contract.
    [Fact]
    public void Average_empty_source_throws()
        => Assert.Throws<DivideByZeroException>(() => Array.Empty<int>().Average(_ => 0UL));

    // Average enumerates the source twice (once via Sum, once via Count). Pinning this so a
    // future refactor to single-pass doesn't break callers that rely on stable two-pass behaviour
    // (none today, but worth flagging if it changes).
    [Fact]
    public void Average_enumerates_source_twice()
    {
        var enumerated = 0;
        IEnumerable<int> Source()
        {
            foreach (var i in new[] { 1, 2, 3 })
            {
                enumerated++;
                yield return i;
            }
        }

        Source().Average(x => (ulong)x);
        Assert.Equal(6, enumerated);   // 3 items × 2 passes
    }
}
