/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
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
    /// Code of the finding reported for a rule that cannot be applied
    /// </summary>
    public const string InvalidRuleErrorCode = "CU0002";

    // Context and Gravity read from JSON with a value out of their list: the rule is kept, so
    // that it is reported as a finding, and matches nothing
    private readonly List<string> _readErrors = [];

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
    [JsonIgnore]
    public DiagnosticResultContext? Context { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Context))]
    internal JsonElement? ContextJson
    {
        get => WriteEnum(Context);
        set => Context = ReadEnum<DiagnosticResultContext>(value);
    }

    /// <summary>
    /// Gravity of the finding. Null matches any gravity.
    /// </summary>
    [JsonIgnore]
    public DiagnosticResultGravity? Gravity { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Gravity))]
    internal JsonElement? GravityJson
    {
        get => WriteEnum(Gravity);
        set => Gravity = ReadEnum<DiagnosticResultGravity>(value);
    }

    /// <summary>
    /// Check if the rule matches the finding. A rule that is not valid matches nothing.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool IsMatch(DiagnosticResult result)
        => _readErrors.Count == 0
            && IsMatch(result.ErrorCode, ErrorCode)
            && IsMatch(result.Id, Id)
            && IsMatch(result.SubContext, SubContext)
            && IsMatch(result.Description, Description)
            && (Context == null || result.Context == Context)
            && (Gravity == null || result.Gravity == Gravity);

    private static bool IsMatch(string? text, string? pattern)
        => pattern == null || Regex.IsMatch(text ?? string.Empty, pattern);

    /// <summary>
    /// Check that the rule can be applied: every pattern is a regular expression, Context and
    /// Gravity are values of their list. A rule that matches no finding is valid.
    /// </summary>
    /// <returns>One message for each problem, empty when the rule is valid</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>(_readErrors);

        foreach (var (field, pattern) in new[] { (nameof(ErrorCode), ErrorCode),
                                                 (nameof(Id), Id),
                                                 (nameof(SubContext), SubContext),
                                                 (nameof(Description), Description) })
        {
            if (pattern == null) { continue; }
            try { _ = new Regex(pattern); }
            catch (ArgumentException ex) { errors.Add($"invalid regular expression in {field} '{pattern}': {ex.Message}"); }
        }

        if (Context.HasValue && !Enum.IsDefined(Context.Value)) { errors.Add(NotValid<DiagnosticResultContext>(Context.Value.ToString())); }
        if (Gravity.HasValue && !Enum.IsDefined(Gravity.Value)) { errors.Add(NotValid<DiagnosticResultGravity>(Gravity.Value.ToString())); }

        return errors;
    }

    private static string NotValid<TEnum>(string text) where TEnum : struct, Enum
        => $"'{text}' is not a valid {typeof(TEnum).Name}. Values: {string.Join(", ", Enum.GetNames<TEnum>())}";

    private static JsonElement? WriteEnum<TEnum>(TEnum? value) where TEnum : struct, Enum
        => value == null
            ? null
            : JsonSerializer.SerializeToElement(value.Value.ToString());

    // A name ("Node", "Info") is that value only. A number is the format of the files written by
    // older versions, where 0 was the default value and meant "any": it is read as null.
    private TEnum? ReadEnum<TEnum>(JsonElement? json) where TEnum : struct, Enum
    {
        if (json == null || json.Value.ValueKind == JsonValueKind.Null) { return null; }

        if (json.Value.ValueKind == JsonValueKind.String)
        {
            var text = json.Value.GetString();
            if (Enum.TryParse<TEnum>(text, true, out var named) && Enum.IsDefined(named)) { return named; }
            _readErrors.Add(NotValid<TEnum>(text!));
            return null;
        }

        if (json.Value.ValueKind == JsonValueKind.Number && json.Value.TryGetInt32(out var number))
        {
            if (number == 0) { return null; }

            var value = (TEnum)Enum.ToObject(typeof(TEnum), number);
            if (Enum.IsDefined(value)) { return value; }
        }

        _readErrors.Add(NotValid<TEnum>(json.Value.GetRawText()));
        return null;
    }

    // A rule that cannot be applied is left out and reported as a finding: a wrong pattern must
    // not lose the analysis. The number in the description is the position of the rule, from 1.
    internal static List<DiagnosticResult> RemoveInvalid(IEnumerable<DiagnosticIgnoreRule>? rules, out List<DiagnosticIgnoreRule> valid)
    {
        valid = [];
        var invalid = new List<DiagnosticResult>();
        var number = 0;

        foreach (var rule in rules ?? [])
        {
            number++;
            var errors = rule.Validate();
            if (errors.Count == 0)
            {
                valid.Add(rule);
                continue;
            }

            invalid.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = InvalidRuleErrorCode,
                Description = $"Ignore rule #{number} is not applied: {string.Join("; ", errors)}",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "IgnoreRule",
                Gravity = DiagnosticResultGravity.Critical,
            });
        }

        return invalid;
    }

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
