/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Diagnostic.Api;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;
using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Extension;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Diagnostic;

internal class OutputEngine
{
    public static string PrintEnum(string title, Type typeEnum)
        => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";

    // Findings that say the analysis is incomplete: kept even when --compliance filters the rest.
    // WG0042 = API call failed, CU0001 = cluster resources unreadable, WC0020 = missing privileges.
    private static readonly HashSet<string> _analysisScopeCodes = ["WG0042", "CU0001", "WC0020"];

    private static readonly JsonSerializerOptions _ignoredIssuesJsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Reads the ignore file. Context and Gravity may be names ("Qemu") or numbers. Every pattern
    /// is checked here, so a broken regex stops the run before the API calls, not after them.
    /// </summary>
    internal static List<DiagnosticResult> LoadIgnoredIssues(string json)
    {
        var rules = JsonSerializer.Deserialize<List<DiagnosticResult>>(json, _ignoredIssuesJsonOptions) ?? [];
        for (var i = 0; i < rules.Count; i++)
        {
            foreach (var (field, pattern) in new[] { ("ErrorCode", rules[i].ErrorCode),
                                                     ("Id", rules[i].Id),
                                                     ("SubContext", rules[i].SubContext),
                                                     ("Description", rules[i].Description) })
            {
                if (pattern == null) { continue; }
                try { _ = new Regex(pattern); }
                catch (ArgumentException ex)
                {
                    throw new FormatException($"Ignore rule #{i + 1}: invalid regular expression in {field} '{pattern}': {ex.Message}", ex);
                }
            }
        }
        return rules;
    }

    public static async Task CreateAsync(PveClient client,
                                         string? settingsFile,
                                         Settings profile,
                                         string? ignoredIssuesFile,
                                         OutputType output,
                                         bool showIgnoredIssues,
                                         string? outputFile,
                                         ComplianceStandard? compliance)
    {
        // A settings file wins over the --fast / --full profile, as in cv4pve-report.
        var settings = !string.IsNullOrWhiteSpace(settingsFile)
                          ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile!), Settings.JsonOptions)
                          : profile;

        var ignoredIssues = !string.IsNullOrWhiteSpace(ignoredIssuesFile)
                                ? LoadIgnoredIssues(File.ReadAllText(ignoredIssuesFile!))
                                : [];

        // Infer the output format from the file extension when the user passed --output-file
        // but left --output on its default (Text). Keeps the CLI feeling natural:
        // '--output-file=report.xlsx' alone produces a real Excel file instead of an ASCII table
        // saved with the wrong extension.
        if (output == OutputType.Text && !string.IsNullOrWhiteSpace(outputFile))
        {
            output = Path.GetExtension(outputFile).ToLowerInvariant() switch
            {
                ".xlsx" => OutputType.Excel,
                ".html" or ".htm" => OutputType.Html,
                ".json" => OutputType.Json,
                ".md" => OutputType.Markdown,
                _ => output,
            };
        }

        var duration = Stopwatch.StartNew();
        using var httpClient = new HttpClient();
        var result = await new DiagnosticEngine(client, settings!, httpClient).AnalyzeAsync(ignoredIssues!);
        duration.Stop();

        // When --compliance is passed, keep only findings mapped to that standard — plus the ones
        // saying the analysis is incomplete (API errors, unreadable resources, missing privileges):
        // without them an audit report looks clean when the data was never read.
        if (compliance.HasValue)
        {
            result = [.. result.Where(r => r.Compliance.Any(m => m.Standard == compliance.Value)
                                           || _analysisScopeCodes.Contains(r.ErrorCode))];
        }

        // Ignored findings leave the report; --ignored-issues-show keeps them, marked in a column.
        if (!showIgnoredIssues) { result = [.. result.Where(r => !r.IsIgnoredIssue)]; }

        if (!string.IsNullOrWhiteSpace(outputFile)) { File.Delete(outputFile); }

        if (output == OutputType.Excel)
        {
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                outputFile = $"cv4pve-diagnostic-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                Console.WriteLine($"Output file name not provided, using default: {outputFile}");
            }

            // Node list for the summary sheet only: a failure here must not lose the report.
            string nodes;
            try { nodes = string.Join(",", (await client.GetResourcesAsync(ClusterResourceType.Node)).Select(a => a.Node)); }
            catch (Exception ex) when (ex is not OperationCanceledException) { nodes = "n/a"; }

            CreateExcel(result,
                        nodes,
                        duration.Elapsed,
                        outputFile,
                        compliance);
        }
        else
        {
            CreateTable(result, output, showIgnoredIssues, outputFile, compliance);
        }
    }

    /// <summary>
    /// Comma-separated list of ControlId values matching the selected standard.
    /// </summary>
    private static string FormatControlIds(DiagnosticResult result, ComplianceStandard standard)
        => string.Join(", ", result.Compliance
                                   .Where(m => m.Standard == standard)
                                   .Select(m => m.ControlId));

    private static void CreateTable(IEnumerable<DiagnosticResult> result,
                                    OutputType output,
                                    bool showIgnoredIssues,
                                    string? outputFile,
                                    ComplianceStandard? compliance)
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

        // Enums as names (the JSON output wrote them as numbers); text escaped for the formats
        // that interpret it (NVD descriptions and PVE error bodies can contain markup or '|').
        string Cell(object? value)
        {
            var text = value?.ToString() ?? "";
            return output switch
            {
                OutputType.Html => WebUtility.HtmlEncode(text),
                OutputType.Markdown => text.Replace("|", "\\|").Replace("\r", "").Replace("\n", " "),
                _ => text,
            };
        }

        var rows = result.OrderByDescending(a => a.Gravity)
                         .ThenBy(a => a.Context)
                         .ThenBy(a => a.SubContext)
                         .Select(a =>
                         {
                             var baseRow = new List<object>
                             {
                                 Cell(a.Id),
                                 Cell(a.ErrorCode),
                                 Cell(a.Description),
                                 Cell(a.Context),
                                 Cell(a.SubContext),
                                 Cell(a.Gravity),
                             };
                             if (compliance.HasValue) { baseRow.Add(FormatControlIds(a, compliance.Value)); }
                             if (showIgnoredIssues) { baseRow.Add(a.IsIgnoredIssue ? "X" : ""); }
                             return baseRow.ToArray();
                         });

        var columnList = new List<string> { "Id", "Code", "Description", "Context", "SubContext", "Gravity" };
        if (compliance.HasValue) { columnList.Add("ControlId"); }
        if (showIgnoredIssues) { columnList.Add("IgnoredIssue"); }
        var columns = columnList.ToArray();

        var data = TableGenerator.To(columns, rows, tabOutput);

        // The HTML table has no <head>: declare UTF-8 so "—" and "°C" read correctly when opened locally.
        if (output == OutputType.Html) { data = "<meta charset=\"utf-8\">" + data; }

        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            File.WriteAllText(outputFile, data);
        }
        else
        {
            Console.Out.Write(data);
        }
    }

    public static void CreateExcel(IEnumerable<DiagnosticResult> data,
                                   string nodes,
                                   TimeSpan elapsed,
                                   string fileName,
                                   ComplianceStandard? compliance)
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
        if (compliance.HasValue) { AddKV("Compliance:", compliance.Value.ToString()); }
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

        IXLTable table;
        if (compliance.HasValue)
        {
            var std = compliance.Value;
            table = ws.Cell(row, 1)
                      .InsertTable(data.Select(a => new
                      {
                          a.Id,
                          a.ErrorCode,
                          a.Context,
                          a.SubContext,
                          a.Description,
                          a.Gravity,
                          ControlId = FormatControlIds(a, std),
                          IsIgnoredIssue = a.IsIgnoredIssue ? "X" : "",
                      }), true);
        }
        else
        {
            table = ws.Cell(row, 1)
                      .InsertTable(data.Select(a => new
                      {
                          a.Id,
                          a.ErrorCode,
                          a.Context,
                          a.SubContext,
                          a.Description,
                          a.Gravity,
                          IsIgnoredIssue = a.IsIgnoredIssue ? "X" : "",
                      }), true);
        }

        table.AutoFilter.IsEnabled = true;
        var fields = table.Fields.ToList();
        fields[1].HeaderCell.Value = "Error Code";
        fields[3].HeaderCell.Value = "Sub Context";
        // Last column is always IsIgnoredIssue; rename for display.
        fields[^1].HeaderCell.Value = "Ignored Issue";
        if (compliance.HasValue)
        {
            // Zero-indexed: 6 = ControlId.
            fields[6].HeaderCell.Value = "Control Id";
        }

        ws.Columns().AdjustToContents();

        workbook.SaveAs(fileName);
    }
}
