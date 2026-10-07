/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Rule that marks the matching findings as ignored. A finding matches when every field set on
/// the rule matches it; a field left null matches any value.
/// </summary>
public class DiagnosticIgnoreRule
{
    /// <summary>
    /// Regular expression on the resource id of the finding
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Regular expression on the error code of the finding
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Regular expression on the sub context of the finding
    /// </summary>
    public string? SubContext { get; set; }

    /// <summary>
    /// Regular expression on the description of the finding
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Context of the finding. Null matches any context.
    /// </summary>
    [JsonConverter(typeof(DiagnosticIgnoreRuleEnumConverter<DiagnosticResultContext>))]
    public DiagnosticResultContext? Context { get; set; }

    /// <summary>
    /// Gravity of the finding. Null matches any gravity.
    /// </summary>
    [JsonConverter(typeof(DiagnosticIgnoreRuleEnumConverter<DiagnosticResultGravity>))]
    public DiagnosticResultGravity? Gravity { get; set; }

    /// <summary>
    /// Check if the rule matches the finding
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool IsMatch(DiagnosticResult result)
        => IsMatch(result.ErrorCode, ErrorCode)
            && IsMatch(result.Id, Id)
            && IsMatch(result.SubContext, SubContext)
            && IsMatch(result.Description, Description)
            && (Context == null || result.Context == Context)
            && (Gravity == null || result.Gravity == Gravity);

    private static bool IsMatch(string? text, string? pattern)
        => pattern == null || Regex.IsMatch(text ?? string.Empty, pattern);

    // Rules held in a DiagnosticResult could not tell "Node" and "Info" from "not set":
    // both are the default value of their enum, and meant "any"
    internal static DiagnosticIgnoreRule FromLegacy(DiagnosticResult rule)
        => new()
        {
            Id = rule.Id,
            ErrorCode = rule.ErrorCode,
            SubContext = rule.SubContext,
            Description = rule.Description,
            Context = rule.Context == default ? null : rule.Context,
            Gravity = rule.Gravity == default ? null : rule.Gravity,
        };
}
