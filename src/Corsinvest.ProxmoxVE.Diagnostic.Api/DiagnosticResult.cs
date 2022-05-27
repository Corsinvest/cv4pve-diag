/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Diagnostic result
/// </summary>
public class DiagnosticResult
{
    /// <summary>
    /// Id
    /// </summary>
    /// <value></value>
    public string Id { get; set; }

    /// <summary>
    /// Tag
    /// </summary>
    /// <value></value>
    [JsonIgnore]
    public object Tag { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    /// <value></value>
    [JsonIgnore]
    public string ErrorCode { get; set; }

    /// <summary>
    /// Context
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public DiagnosticResultContext Context { get; set; }

    /// <summary>
    /// Subcontext
    /// </summary>
    /// <value></value>
    public string SubContext { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    /// <value></value>
    public string Description { get; set; }

    /// <summary>
    /// Gravity
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public DiagnosticResultGravity Gravity { get; set; }

    /// <summary>
    /// Decode context
    /// </summary>
    public static DiagnosticResultContext DecodeContext(string text)
        => Enum.TryParse<DiagnosticResultContext>(text, true, out var ret) ?
                            (DiagnosticResultContext)ret :
                            DiagnosticResultContext.Cluster;

    /// <summary>
    /// Check ignore issue
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool CheckIgnoreIssue(DiagnosticResult result)
        => CheckString(result.Id, Id) &&
           CheckString(result.SubContext, SubContext) &&
           CheckString(result.Description, Description) &&
           result.Context == Context &&
           result.Gravity == Gravity;

    private static bool CheckString(string text, string pattern) => pattern == null || Regex.IsMatch(text, pattern);

    /// <summary>
    /// IsIgnoredIssue
    /// </summary>
    /// <value></value>
    public bool IsIgnoredIssue { get; set; }
}
