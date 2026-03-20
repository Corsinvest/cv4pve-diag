/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Diagnostic.Api;
using Microsoft.Extensions.Logging;

var settingsFileName = "settings.json";
var ignoredIssuesFileName = "ignored-issues.json";

var app = ConsoleHelper.CreateApp("cv4pve-diag", "Diagnostic for Proxmox VE");
var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

var optSettingsFile = app.AddOption<string>("--settings-file", "File settings (generated from create-settings)")
                         .AddValidatorExistFile();

var optIgnoredIssuesFile = app.AddOption<string>("--ignored-issues-file", "File ignored issues (generated from create-ignored-issues)")
                              .AddValidatorExistFile();

var optShowIgnoredIssues = app.AddOption<bool>("--ignored-issues-show", "Show second table with ignored issue");

var optOutput = app.TableOutputOption();

app.AddCommand("create-settings", $"Create file settings ({settingsFileName})")
   .SetAction((action) =>
   {
       File.WriteAllText(settingsFileName, JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions { WriteIndented = true }));
       Console.Out.WriteLine(PrintEnum("SeriesType", typeof(SettingsTimeSeriesType)));
       Console.Out.WriteLine($"Create file: {settingsFileName}");
   });

app.AddCommand("create-ignored-issues", $"Create File ignored issues ({ignoredIssuesFileName})")
   .SetAction((action) =>
   {
       File.WriteAllText(ignoredIssuesFileName, JsonSerializer.Serialize(new[] { new DiagnosticResult() }, new JsonSerializerOptions { WriteIndented = true }));
       Console.Out.WriteLine(PrintEnum("Context", typeof(DiagnosticResultContext)));
       Console.Out.WriteLine(PrintEnum("Gravity", typeof(DiagnosticResultGravity)));
       Console.Out.WriteLine($"Create file: {ignoredIssuesFileName}");
   });

app.AddCommand("execute", "Execute diagnostic and print result to console")
   .SetAction(async (action) =>
   {
       var client = await app.ClientTryLoginAsync(loggerFactory);

       var settings = !string.IsNullOrWhiteSpace(action.GetValue(optSettingsFile))
                           ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(action.GetValue(optSettingsFile)!))
                           : new Settings();

       var ignoredIssues = !string.IsNullOrWhiteSpace(action.GetValue(optIgnoredIssuesFile))
                               ? JsonSerializer.Deserialize<List<DiagnosticResult>>(File.ReadAllText(action.GetValue(optIgnoredIssuesFile)!))
                               : [];

       var result = await new DiagnosticEngine(client, settings!).AnalyzeAsync(ignoredIssues!);

       PrintResult([.. result.Where(a => !a.IsIgnoredIssue)], action.GetValue(optOutput));
       if (action.GetValue(optShowIgnoredIssues)) { PrintResult([.. result.Where(a => a.IsIgnoredIssue)], action.GetValue(optOutput)); }
   });

return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger(typeof(Program)));

void PrintResult(ICollection<DiagnosticResult> data, TableGenerator.Output output)
{
    var rows = data.OrderByDescending(a => a.Gravity)
                   .ThenBy(a => a.Context)
                   .ThenBy(a => a.SubContext)
                   .Select(a => new object[] { a.Id, a.Description, a.Context, a.SubContext, a.Gravity });

    Console.Out.Write(TableGenerator.To(["Id", "Description", "Context", "SubContext", "Gravity"], rows, output));
}

string PrintEnum(string title, Type typeEnum) => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";
