/*
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Diagnostic;
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

var optOutput = app.AddOption<OutputType>("--output|-o", "Type output");
optOutput.DefaultValueFactory = (_) => OutputType.Text;

var optOutputFile = app.AddOption<string>("--output-file", "Output file name");

app.AddCommand("create-settings", $"Create file settings ({settingsFileName})")
   .SetAction((action) =>
   {
       File.WriteAllText(settingsFileName, JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions { WriteIndented = true }));
       Console.Out.WriteLine(OutputEngine.PrintEnum("TimeFrame", typeof(RrdDataTimeFrame)));
       Console.Out.WriteLine(OutputEngine.PrintEnum("Consolidation", typeof(RrdDataConsolidation)));
       Console.Out.WriteLine($"Create file: {settingsFileName}");
   });

app.AddCommand("create-ignored-issues", $"Create File ignored issues ({ignoredIssuesFileName})")
   .SetAction((action) =>
   {
       File.WriteAllText(ignoredIssuesFileName, JsonSerializer.Serialize(new[] { new DiagnosticResult() }, new JsonSerializerOptions { WriteIndented = true }));
       Console.Out.WriteLine(OutputEngine.PrintEnum("Context", typeof(DiagnosticResultContext)));
       Console.Out.WriteLine(OutputEngine.PrintEnum("Gravity", typeof(DiagnosticResultGravity)));
       Console.Out.WriteLine($"Create file: {ignoredIssuesFileName}");
   });

app.AddCommand("execute", "Execute diagnostic and print result to console")
   .SetAction(async (action)
      => await OutputEngine.CreateAsync(await app.ClientTryLoginAsync(loggerFactory),
                                        action.GetValue(optSettingsFile),
                                        action.GetValue(optIgnoredIssuesFile),
                                        action.GetValue(optOutput),
                                        action.GetValue(optShowIgnoredIssues),
                                        action.GetValue(optOutputFile)));

return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger(typeof(Program)));