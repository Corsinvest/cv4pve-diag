/*
 * This file is part of the cv4pve-diag https://github.com/Corsinvest/cv4pve-diag,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension.Info;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Diagnostic.Api;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Corsinvest.ProxmoxVE.Diagnostic
{
    class Program
    {
        static void Main(string[] args)
        {
            var settingsFileName = "settings.json";
            var ignoredIssuesFileName = "ignored-issues.json";

            var app = ShellHelper.CreateConsoleApp("cv4pve-diag", "Diagnostic for Proxmox VE");

            var optSettingsFile = app.Option("--settings-file", "File settings (generated from create-settings)", CommandOptionType.SingleValue);
            optSettingsFile.Accepts().ExistingFile();

            var optIgnoredIssuesFile = app.Option("--ignored-issues-file", "File ignored issues (generated from create-ignored-issues)", CommandOptionType.SingleValue);
            optIgnoredIssuesFile.Accepts().ExistingFile();

            var optShowIgnoredIssues = app.Option("--ignored-issues-show", "Show second table with ignored issue", CommandOptionType.NoValue);

            var optOutput = app.OutputTypeArgument();

            app.Command("create-settings", cmd =>
            {
                cmd.Description = $"Create file settings ({settingsFileName})";
                cmd.AddFullNameLogo();

                cmd.OnExecute(() =>
                {
                    File.WriteAllText(settingsFileName, JsonConvert.SerializeObject(new Settings(), Formatting.Indented));
                    app.Out.WriteLine(PrintEnum("SeriesType", typeof(SettingsTimeSeriesType)));
                    app.Out.WriteLine($"Create file: {settingsFileName}");
                });
            });

            app.Command("create-ignored-issues", cmd =>
            {
                cmd.Description = $"Create File ignored issues ({ignoredIssuesFileName})";
                cmd.AddFullNameLogo();

                cmd.OnExecute(() =>
                {
                    File.WriteAllText(ignoredIssuesFileName, JsonConvert.SerializeObject(new[] { new DiagnosticResult() }, Formatting.Indented));
                    app.Out.WriteLine(PrintEnum("Context", typeof(DiagnosticResultContext)));
                    app.Out.WriteLine(PrintEnum("Gravity", typeof(DiagnosticResultGravity)));
                    app.Out.WriteLine($"Create file: {ignoredIssuesFileName}");
                });
            });

            var fileExport = "data.json";
            app.Command("export-collect", cmd =>
            {
                cmd.ShowInHelpText = false;
                cmd.Description = $"Export collect data collect to {fileExport}";
                cmd.AddFullNameLogo();

                cmd.OnExecute(() =>
                {
                    var ci = new ClusterInfo();
                    ci.Collect(app.ClientTryLogin());

                    File.WriteAllText(fileExport, JsonConvert.SerializeObject(ci, Formatting.Indented));
                    app.Out.WriteLine($"Exported {fileExport}!");
                });
            });

            app.Command("examine-collect", cmd =>
            {
                cmd.ShowInHelpText = false;
                cmd.Description = $"Examine collect data collect from {fileExport}";
                cmd.AddFullNameLogo();
                cmd.OnExecute(() =>
                {
                    var ci = JsonConvert.DeserializeObject<ClusterInfo>(File.ReadAllText(fileExport));
                    Print(app, ci, optSettingsFile, optIgnoredIssuesFile, optShowIgnoredIssues, optOutput);
                });
            });

            app.OnExecute(() =>
            {
                var ci = new ClusterInfo();
                ci.Collect(app.ClientTryLogin());
                ci = JsonConvert.DeserializeObject<ClusterInfo>(JsonConvert.SerializeObject(ci));

                Print(app, ci, optSettingsFile, optIgnoredIssuesFile, optShowIgnoredIssues, optOutput);
            });

            app.ExecuteConsoleApp(args);
        }

        private static void Print(CommandLineApplication app,
                                  ClusterInfo ci,
                                  CommandOption optSettingsFile,
                                  CommandOption optIgnoredIssuesFile,
                                  CommandOption optShowIgnoredIssues,
                                  CommandOption<TableOutputType> optOutput)
        {
            var settings = optSettingsFile.HasValue() ?
                           JsonConvert.DeserializeObject<Settings>(File.ReadAllText(optSettingsFile.Value())) :
                           new Settings();

            var ignoredIssues = optIgnoredIssuesFile.HasValue() ?
                                JsonConvert.DeserializeObject<List<DiagnosticResult>>(File.ReadAllText(optIgnoredIssuesFile.Value())) :
                                new List<DiagnosticResult>();

            var ret = Application.Analyze(ci, settings, ignoredIssues);

            var outputType = optOutput.GetEnumValue<TableOutputType>();

            PrintResult(app, ret.Result, outputType);
            if (optShowIgnoredIssues.HasValue()) { PrintResult(app, ret.ResultIgnoredIssues, outputType); }
        }

        private static void PrintResult(CommandLineApplication app,
                                        ICollection<DiagnosticResult> data,
                                        TableOutputType outputType)
        {
            var rows = data.OrderByDescending(a => a.Gravity)
                           .ThenBy(a => a.Context)
                           .ThenBy(a => a.SubContext)
                           .Select(a => new object[] { a.Id, a.Description, a.Context, a.SubContext, a.Gravity });

            app.Out.Write(TableHelper.Create(new string[] { "Id", "Description", "Context", "SubContext", "Gravity" },
                                             rows,
                                             outputType, false));
        }

        private static string PrintEnum(string title, Type typeEnum)
            => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";
    }
}