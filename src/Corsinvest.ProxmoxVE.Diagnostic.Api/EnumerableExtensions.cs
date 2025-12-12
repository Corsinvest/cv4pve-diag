/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

internal static class EnumerableExtensions
{
    public static ulong Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, ulong> summer)
    {
        ulong total = 0;
        foreach (var item in source) { total += summer(item); }
        return total;
    }

    public static ulong Average<TSource>(this IEnumerable<TSource> source, Func<TSource, ulong> summer)
        => source.Sum(summer) / Convert.ToUInt64(source.Count());
}