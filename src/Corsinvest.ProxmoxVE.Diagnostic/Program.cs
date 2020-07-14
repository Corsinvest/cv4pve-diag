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

            var app = ShellHelper.CreateConsoleApp("cv4pve-diag", "Diagnostic for Proxmox VE");

            var optSettings = app.Option("--settings-file", "File settings (generated from settings-create)", CommandOptionType.SingleValue);
            optSettings.Accepts().ExistingFile();

            var optOutput = app.OutputTypeArgument();

            app.Command("settings-create", cmd =>
            {
                cmd.Description = $"Create file settings ({settingsFileName})";
                cmd.AddFullNameLogo();

                cmd.OnExecute(() =>
                {
                    File.WriteAllText(settingsFileName, JsonConvert.SerializeObject(new Settings(), Formatting.Indented));
                    app.Out.WriteLine("Values form TimeSeries are: 0 = Day, 1 Week");
                    app.Out.WriteLine($"Create file: {settingsFileName}");
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
                    Print(ci);
                });
            });

            app.OnExecute(() =>
            {
                var ci = new ClusterInfo();
                ci.Collect(app.ClientTryLogin());
                ci = JsonConvert.DeserializeObject<ClusterInfo>(JsonConvert.SerializeObject(ci));

                Print(ci);
            });

            void Print(ClusterInfo ci)
            {
                var settings = new Settings();
                if (optSettings.HasValue())
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(optSettings.Value()));
                }

                var rowsBase = Application.Analyze(ci, settings)
                                            .OrderByDescending(a => a.Gravity)
                                            .ThenBy(a => a.Context)
                                            .ThenBy(a => a.SubContext);

                var columns = new string[] { /*"Error Code", */"Id", "Description", "Context", "SubContext", "Gravity" };

                var rows = rowsBase.Select(a => new object[]
                                            {
                                                    // a.ErrorCode,
                                                a.Id,
                                                a.Description,
                                                a.Context,
                                                a.SubContext,
                                                a.Gravity
                                            });

                app.Out.Write(TableHelper.Create(columns, rows, optOutput.GetEnumValue<TableOutputType>(), false));
            }

            app.ExecuteConsoleApp(args);
        }
    }
}