/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// The analysis runs with the invariant culture (descriptions must not depend on the machine's
/// regional settings) and gives the caller its own culture back.
/// </summary>
public class DiagnosticEngineCultureTests
{
    [Fact]
    public async Task Caller_culture_is_restored_after_the_analysis()
    {
        var italian = CultureInfo.GetCultureInfo("it-IT");
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = italian;
        try
        {
            // Nothing listens on port 1: every call fails at once and the analysis stops early.
            using var httpClient = new HttpClient();
            var engine = new DiagnosticEngine(new PveClient("127.0.0.1", 1) { Timeout = TimeSpan.FromSeconds(5) },
                                              new Settings(),
                                              httpClient);
            await engine.AnalyzeAsync([]);

            Assert.Equal(italian, CultureInfo.CurrentCulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
