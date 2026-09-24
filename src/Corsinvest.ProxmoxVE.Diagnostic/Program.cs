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
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;
using Microsoft.Extensions.Logging;

var settingsFileName = "settings.json";
var ignoredIssuesFileName = "ignored-issues.json";

var app = ConsoleHelper.CreateApp("Diagnostic for Proxmox VE");
var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

var optSettingsFile = app.AddOption<string>("--settings-file", "File settings (generated from create-settings)")
                         .AddValidatorExistFile();

var optIgnoredIssuesFile = app.AddOption<string>("--ignored-issues-file", "File ignored issues (generated from create-ignored-issues)")
                              .AddValidatorExistFile();

var optShowIgnoredIssues = app.AddOption<bool>("--ignored-issues-show", "Show second table with ignored issue");

var optOutput = app.AddOption<OutputType>("--output|-o", "Type output");
optOutput.DefaultValueFactory = (_) => OutputType.Text;

var optOutputFile = app.AddOption<string>("--output-file", "Output file name");

var optCompliance = app.AddOption<ComplianceStandard?>("--compliance",
    "Add the Compliance column to the output, showing mappings for the selected standard only (Iso27001, Nis2, Dora, PciDss, …). Omit the flag to hide the column.");


static Settings Profile(bool fast, bool full)
    => fast && full
        ? throw new ArgumentException("--fast and --full cannot be used together")
        : fast
            ? Settings.Fast()
            : full
                ? Settings.Full()
                : Settings.Standard();

var cmdCreateSettings = app.AddCommand("create-settings", $"Create file settings ({settingsFileName})");
var optCreateFast = cmdCreateSettings.AddOption<bool>("--fast", "Use fast profile (skips backup content, snapshots and LVM-thin metadata)");
var optCreateFull = cmdCreateSettings.AddOption<bool>("--full", "Use full profile (every optional check on: S.M.A.R.T., ZFS detail, NVD CVE lookup, Ok results)");
cmdCreateSettings.SetAction((action) =>
   {
       var settings = Profile(action.GetValue(optCreateFast), action.GetValue(optCreateFull));
       File.WriteAllText(settingsFileName, JsonSerializer.Serialize(settings, Settings.JsonOptions));
       Console.Out.WriteLine(OutputEngine.PrintEnum("TimeFrame", typeof(RrdDataTimeFrame)));
       Console.Out.WriteLine(OutputEngine.PrintEnum("Consolidation", typeof(RrdDataConsolidation)));
       Console.Out.WriteLine($"Create file: {settingsFileName}");
   });

app.AddCommand("create-ignored-issues", $"Create File ignored issues ({ignoredIssuesFileName})")
   .SetAction((_) =>
   {
       // An empty DiagnosticResult matched every finding (default Context/Gravity mean "any"):
       // the template is a real, narrow example instead — edit it before use.
       var example = new DiagnosticResult
       {
           ErrorCode = "IG0011",
           Id = "nodes/pve01/qemu/100",
           Context = DiagnosticResultContext.Qemu,
           Gravity = DiagnosticResultGravity.Info,
       };
       File.WriteAllText(ignoredIssuesFileName,
                         JsonSerializer.Serialize(new[] { example },
                                                  new JsonSerializerOptions
                                                  {
                                                      WriteIndented = true,
                                                      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                                                  }));
       Console.Out.WriteLine(OutputEngine.PrintEnum("Context", typeof(DiagnosticResultContext)));
       Console.Out.WriteLine(OutputEngine.PrintEnum("Gravity", typeof(DiagnosticResultGravity)));
       Console.Out.WriteLine($"Create file: {ignoredIssuesFileName}");
   });

var cmdExecute = app.AddCommand("execute", "Execute diagnostic and print result to console");
var optExecuteFast = cmdExecute.AddOption<bool>("--fast", "Use fast profile (skips backup content, snapshots and LVM-thin metadata)");
var optExecuteFull = cmdExecute.AddOption<bool>("--full", "Use full profile (every optional check on: S.M.A.R.T., ZFS detail, NVD CVE lookup, Ok results)");
cmdExecute.SetAction(async (action)
      => await OutputEngine.CreateAsync(await app.ClientTryLoginAsync(loggerFactory),
                                        action.GetValue(optSettingsFile),
                                        Profile(action.GetValue(optExecuteFast), action.GetValue(optExecuteFull)),
                                        action.GetValue(optIgnoredIssuesFile),
                                        action.GetValue(optOutput),
                                        action.GetValue(optShowIgnoredIssues),
                                        action.GetValue(optOutputFile),
                                        action.GetValue(optCompliance)));

return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger<Program>());