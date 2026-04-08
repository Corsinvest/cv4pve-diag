/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Diagnostic.Api;
using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Extension;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.Diagnostic;

internal class OutputEngine
{
    public static string PrintEnum(string title, Type typeEnum)
        => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";

    public static async Task CreateAsync(PveClient client,
                                         string? settingsFile,
                                         string? ignoredIssuesFile,
                                         OutputType output,
                                         bool showIgnoredIssues,
                                         string? outputFile)
    {

        var settings = !string.IsNullOrWhiteSpace(settingsFile)
                          ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile!))
                          : new Settings();

        var ignoredIssues = !string.IsNullOrWhiteSpace(ignoredIssuesFile)
                                ? JsonSerializer.Deserialize<List<DiagnosticResult>>(File.ReadAllText(ignoredIssuesFile!))
                                : [];

        var duration = Stopwatch.StartNew();
        var result = await new DiagnosticEngine(client, settings!).AnalyzeAsync(ignoredIssues!);
        duration.Stop();

        if (!string.IsNullOrWhiteSpace(outputFile)) { File.Delete(outputFile!); }

        if (output == OutputType.Excel)
        {
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                outputFile = $"cv4pve-diagnostic-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                Console.WriteLine($"Output file name not provided, using default: {outputFile}");
            }

            CreateExcel(result,
                        string.Join(",", (await client.GetResourcesAsync(ClusterResourceType.Node)).Select(a => a.Node)),
                        duration.Elapsed,
                        outputFile);
        }
        else
        {
            CreateTable(result, output, showIgnoredIssues, outputFile);
        }
    }

    private static void CreateTable(IEnumerable<DiagnosticResult> result,
                                    OutputType output,
                                    bool showIgnoredIssues,
                                    string? outputFile)
    {
        var tabOutput = output switch
        {
            OutputType.Text => TableGenerator.Output.Text,
            OutputType.Markdown => TableGenerator.Output.Markdown,
            OutputType.Html => TableGenerator.Output.Html,
            OutputType.JsonPretty => TableGenerator.Output.JsonPretty,
            OutputType.Json => TableGenerator.Output.Json,
            _ => throw new ArgumentException("Invalid output type"),
        };

        var rows = result.OrderByDescending(a => a.Gravity)
                         .ThenBy(a => a.Context)
                         .ThenBy(a => a.SubContext)
                         .Select(a => showIgnoredIssues
                            ? new object[]
                            {
                                a.Id,
                                a.ErrorCode,
                                a.Description,
                                a.Context,
                                a.SubContext,
                                a.Gravity,
                                a.IsIgnoredIssue ? "X" : ""
                            }
                            :
                            [
                                a.Id,
                                a.ErrorCode,
                                a.Description,
                                a.Context,
                                a.SubContext,
                                a.Gravity
                            ]);

        var columns = showIgnoredIssues
                        ? new[] { "Id", "Code", "Description", "Context", "SubContext", "Gravity", "IgnoredIssue" }
                        : ["Id", "Code", "Description", "Context", "SubContext", "Gravity"];

        var data = TableGenerator.To(columns, rows, tabOutput);

        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            File.WriteAllText(outputFile!, data);
        }
        else
        {
            Console.Out.Write(data);
        }
    }

    public static void CreateExcel(IEnumerable<DiagnosticResult> data, string nodes, TimeSpan elapsed, string fileName)
    {
        using var workbook = new XLWorkbook();

        var ws = workbook.Worksheets.Add("Summary");
        var row = 1;

        void Add(string value)
        {
            ws.Cell(row, 1).Value = value;
            ws.Cell(row, 1).Style.Font.SetBold(true);
            ws.Cell(row, 1).Style.Font.SetFontSize(14);
            row++;
        }

        void AddKV(string key, string value)
        {
            ws.Cell(row, 1).Value = key;
            ws.Cell(row, 2).Value = value;
            row++;
        }

        ws.Cell(row, 1).Value = "DIAGNOSTIC REPORT";
        ws.Cell(row, 1).Style.Font.SetBold(true);
        ws.Cell(row, 1).Style.Font.SetFontSize(20);
        ws.Range(row, 1, row, 3).Merge();
        row += 2;

        Add("Report Information");

        AddKV("Generated:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AddKV("Duration:", $"{elapsed.TotalSeconds:F1}s");
        AddKV("Application:", $"cv4pve-diag v{typeof(OutputEngine).Assembly.GetName().Version?.ToString(3)}");
        AddKV("Nodes:", nodes);
        row++;

        ws.Cell(row, 1).Value = "Generated by";
        ws.Cell(row, 1).Style.Font.SetItalic(true);
        ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.Gray);
        ws.Cell(row, 2).Value = "cv4pve-diag";
        ws.Cell(row, 2).SetHyperlink(new XLHyperlink("https://github.com/Corsinvest/cv4pve-diag"));
        ws.Cell(row, 2).Style.Font.SetFontColor(XLColor.Blue);
        ws.Cell(row, 2).Style.Font.SetUnderline(XLFontUnderlineValues.Single);
        ws.Cell(row, 2).Style.Font.SetItalic(true);
        row += 2;

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 60;

        var table = ws.Cell(row, 1)
                      .InsertTable(data.Select(a => new
                      {
                          a.Id,
                          a.ErrorCode,
                          a.Context,
                          a.SubContext,
                          a.Description,
                          a.Gravity,
                          IsIgnoredIssue = a.IsIgnoredIssue ? "X" : ""
                      }), true);

        table.AutoFilter.IsEnabled = true;
        var fileds = table.Fields.ToList();
        fileds[1].HeaderCell.Value = "Error Code";
        fileds[3].HeaderCell.Value = "Sub Context";
        fileds[6].HeaderCell.Value = "Ignored Issue";

        ws.Columns().AdjustToContents();

        workbook.SaveAs(fileName);
    }

}
