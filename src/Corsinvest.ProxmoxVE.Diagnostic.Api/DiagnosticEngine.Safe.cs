/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Net;
using Corsinvest.ProxmoxVE.Api;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Safe wrappers around SDK calls. Mirror cv4pve-report's <c>ToSafe*</c>: a failing PVE API
/// call never aborts the analysis — it degrades to an empty list / default and records a
/// Warning DiagnosticResult so the user knows the picture is incomplete. A 501 (endpoint not
/// implemented on this PVE version) is silent — it is not a problem.
/// </summary>
internal static class DiagnosticSafeExtensions
{
    // ErrorCode used whenever a PVE API call fails during analysis.
    public const string ApiErrorCode = "WG0042";

    /// <summary>
    /// Awaits an SDK call returning <c>IEnumerable&lt;T&gt;</c>. Returns an empty list on failure.
    /// </summary>
    public static async Task<IReadOnlyList<T>> ToSafeEnum<T>(this Task<IEnumerable<T>> task,
                                                             List<DiagnosticResult> result,
                                                             string id,
                                                             DiagnosticResultContext context,
                                                             string what)
    {
        try { return (await task)?.ToList() ?? []; }
        catch (Exception ex) when (Record(ex, result, id, context, what)) { return []; }
    }

    /// <summary>
    /// Single-object variant of <see cref="ToSafeEnum{T}"/>. Returns <c>default(T)</c> on failure.
    /// </summary>
    public static async Task<T?> ToSafeSingle<T>(this Task<T> task,
                                                 List<DiagnosticResult> result,
                                                 string id,
                                                 DiagnosticResultContext context,
                                                 string what)
    {
        try { return await task; }
        catch (Exception ex) when (Record(ex, result, id, context, what)) { return default; }
    }

    // Exception filter: returns true (catch fires) for every failure we swallow, after recording
    // a Warning. Returns false only for cancellation (let it bubble). 501 is swallowed silently.
    private static bool Record(Exception ex, List<DiagnosticResult> result, string id, DiagnosticResultContext context, string what)
    {
        if (ex is OperationCanceledException) { return false; }

        // Endpoint not implemented on this PVE version — expected, not a problem.
        if (ex is PveResultException { Result.StatusCode: HttpStatusCode.NotImplemented }) { return true; }

        var detail = ex is PveResultException pex ? BuildApiErrorMessage(pex.Result) : ex.Message;

        result.Add(new DiagnosticResult
        {
            Id = id,
            ErrorCode = ApiErrorCode,
            Description = $"Unable to read {what}: {detail}",
            Context = context,
            SubContext = "ApiError",
            Gravity = DiagnosticResultGravity.Warning,
        });
        return true;
    }

    // "<code> <reason> — <api error> — <METHOD> <path>" so a finding is self-contained.
    public static string BuildApiErrorMessage(Result r)
    {
        var parts = new List<string> { $"{(int)r.StatusCode} {r.ReasonPhrase}" };
        var apiError = r.GetError();
        if (!string.IsNullOrWhiteSpace(apiError)) { parts.Add(apiError); }
        if (!string.IsNullOrWhiteSpace(r.RequestResource)) { parts.Add($"{r.MethodType} {r.RequestResource}"); }
        return string.Join(" — ", parts);
    }
}
