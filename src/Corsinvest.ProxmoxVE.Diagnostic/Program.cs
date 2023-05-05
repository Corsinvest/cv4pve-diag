/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Diagnostic.Api;

namespace Corsinvest.ProxmoxVE.Diagnostic;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var settingsFileName = "settings.json";
        var ignoredIssuesFileName = "ignored-issues.json";

        var app = ConsoleHelper.CreateApp("cv4pve-diag", "Diagnostic for Proxmox VE");

        var optSettingsFile = app.AddOption<string>("--settings-file", "File settings (generated from create-settings)");
        optSettingsFile.AddValidatorExistFile();

        var optIgnoredIssuesFile = app.AddOption<string>("--ignored-issues-file", "File ignored issues (generated from create-ignored-issues)");
        optSettingsFile.AddValidatorExistFile();

        var optShowIgnoredIssues = app.AddOption<bool>("--ignored-issues-show", "Show second table with ignored issue");

        var optOutput = app.TableOutputOption();

        var cmdCreateSettings = app.AddCommand("create-settings", $"Create file settings ({settingsFileName})");
        cmdCreateSettings.SetHandler(() =>
        {
            File.WriteAllText(settingsFileName, JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions { WriteIndented = true }));
            Console.Out.WriteLine(PrintEnum("SeriesType", typeof(SettingsTimeSeriesType)));
            Console.Out.WriteLine($"Create file: {settingsFileName}");
        });

        var cmdCreateIgnoredIssues = app.AddCommand("create-ignored-issues", $"Create File ignored issues ({ignoredIssuesFileName})");
        cmdCreateIgnoredIssues.SetHandler(() =>
        {
            File.WriteAllText(ignoredIssuesFileName, JsonSerializer.Serialize(new[] { new DiagnosticResult() }, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(ignoredIssuesFileName, JsonSerializer.Serialize(new[] { new DiagnosticResult() }, new JsonSerializerOptions { WriteIndented = true }));
            Console.Out.WriteLine(PrintEnum("Context", typeof(DiagnosticResultContext)));
            Console.Out.WriteLine(PrintEnum("Gravity", typeof(DiagnosticResultGravity)));
            Console.Out.WriteLine($"Create file: {ignoredIssuesFileName}");
        });

        async Task<InfoHelper.Info> GetInfo()
        {
            var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());
            var client = await app.ClientTryLogin(loggerFactory);
            return await InfoHelper.Collect(client, true, 2, false, false);
        }

        var fileExport = "data.json";
        var cmdExportCollect = app.AddCommand("export-collect", $"Export collect data collect to {fileExport}");
        cmdExportCollect.SetHandler(async () =>
        {
            File.WriteAllText(fileExport, JsonSerializer.Serialize(await GetInfo(), new JsonSerializerOptions { WriteIndented = true }));
            Console.Out.WriteLine($"Exported {fileExport}!");
        });

        var cmdExamineCollect = app.AddCommand("examine-collect", $"Examine collect data collect from {fileExport}");
        cmdExamineCollect.IsHidden = true;
        cmdExamineCollect.SetHandler((string settingsFile,
                                      string ignoredIssuesFile,
                                      bool showIgnoredIssues,
                                      TableGenerator.Output output) =>
        {
            var info = JsonSerializer.Deserialize<InfoHelper.Info>(File.ReadAllText(fileExport));
            Print(info, settingsFile, ignoredIssuesFile, showIgnoredIssues, output);
        }, optSettingsFile, optIgnoredIssuesFile, optShowIgnoredIssues, optOutput);

        var cmdExecute = app.AddCommand("execute", $"Execute diagnostic and print result to console");
        cmdExecute.SetHandler(async (string settingsFile,
                                            string ignoredIssuesFile,
                                            bool showIgnoredIssues,
                                            TableGenerator.Output output) =>
        {
            Print(await GetInfo(), settingsFile, ignoredIssuesFile, showIgnoredIssues, output);
        }, optSettingsFile, optIgnoredIssuesFile, optShowIgnoredIssues, optOutput);

        return await app.ExecuteApp(args);
    }

    private static void Print(InfoHelper.Info info,
                              string settingsFile,
                              string ignoredIssuesFile,
                              bool showIgnoredIssues,
                              TableGenerator.Output output)
    {
        var settings = !string.IsNullOrWhiteSpace(settingsFile)
                            ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile))
                            : new Settings();

        var ignoredIssues = !string.IsNullOrWhiteSpace(ignoredIssuesFile)
                                ? JsonSerializer.Deserialize<List<DiagnosticResult>>(File.ReadAllText(ignoredIssuesFile))
                                : new List<DiagnosticResult>();

        var result = Application.Analyze(info, settings, ignoredIssues);

        PrintResult(result.Where(a => !a.IsIgnoredIssue).ToList(), output);
        if (showIgnoredIssues) { PrintResult(result.Where(a => a.IsIgnoredIssue).ToList(), output); }
    }

    private static void PrintResult(ICollection<DiagnosticResult> data, TableGenerator.Output output)
    {
        var rows = data.OrderByDescending(a => a.Gravity)
                       .ThenBy(a => a.Context)
                       .ThenBy(a => a.SubContext)
                       .Select(a => new object[] { a.Id, a.Description, a.Context, a.SubContext, a.Gravity });

        Console.Out.Write(TableGenerator.To(new string[] { "Id", "Description", "Context", "SubContext", "Gravity" },
                                            rows,
                                            output));
    }

    private static string PrintEnum(string title, Type typeEnum)
        => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";
}
