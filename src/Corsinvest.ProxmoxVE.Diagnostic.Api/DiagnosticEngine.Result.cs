/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    /// <summary>
    /// Single entry point for every diagnostic finding. Evaluates the condition and adds a
    /// <see cref="DiagnosticResult"/> to <c>_result</c>.
    /// <list type="bullet">
    /// <item>If <paramref name="isOk"/> is false → failure with <paramref name="gravityKo"/> + <paramref name="descriptionKo"/>.</item>
    /// <item>If <paramref name="isOk"/> is true AND <c>settings.IncludeOkResult</c> → pass with <see cref="DiagnosticResultGravity.Ok"/> + <paramref name="descriptionOk"/>.</item>
    /// <item>If <paramref name="isOk"/> is true AND the setting is off → nothing is added (legacy behaviour).</item>
    /// </list>
    /// Checks with no compliance mapping should pass <c>compliance: []</c>; the resulting JSON
    /// always exposes a (possibly empty) <see cref="DiagnosticResult.Compliance"/> list so consumers
    /// never have to special-case null vs missing.
    /// </summary>
    private void CreateResult(
        bool isOk,
        string id,
        string errorCode,
        string subContext,
        DiagnosticResultContext context,
        DiagnosticResultGravity gravityKo,
        string descriptionKo,
        string descriptionOk,
        IReadOnlyList<ComplianceMapping> compliance)
    {
        // Pass + user does not want OK entries → skip silently (matches legacy output)
        if (isOk && !settings.IncludeOkResult) { return; }

        _result.Add(new()
        {
            Id = id,
            ErrorCode = errorCode,
            SubContext = subContext,
            Context = context,
            Gravity = isOk ? DiagnosticResultGravity.Ok : gravityKo,
            Description = isOk ? descriptionOk : descriptionKo,
            Compliance = compliance,
        });
    }

    /// <summary>
    /// Per-item check helper: walks <paramref name="items"/>, emits one KO finding per failing item,
    /// or a single aggregated OK finding when all items pass (subject to <c>settings.IncludeOkResult</c>).
    /// </summary>
    private void CreateResultPerItem<T>(
        IReadOnlyList<T> items,
        Func<T, bool> isItemOk,
        Func<T, string> itemId,
        Func<T, string> itemDescriptionKo,
        string aggregatedIdOk,
        Func<IReadOnlyList<T>, string> aggregatedDescriptionOk,
        string errorCode,
        string subContext,
        DiagnosticResultContext context,
        DiagnosticResultGravity gravityKo,
        IReadOnlyList<ComplianceMapping> compliance)
    {
        var failing = items.Where(i => !isItemOk(i)).ToList();

        if (failing.Count == 0)
        {
            CreateResult(
                isOk: true,
                id: aggregatedIdOk,
                errorCode: errorCode,
                subContext: subContext,
                context: context,
                gravityKo: gravityKo,
                descriptionKo: "",
                descriptionOk: aggregatedDescriptionOk(items),
                compliance: compliance);
        }
        else
        {
            foreach (var item in failing)
            {
                CreateResult(
                    isOk: false,
                    id: itemId(item),
                    errorCode: errorCode,
                    subContext: subContext,
                    context: context,
                    gravityKo: gravityKo,
                    descriptionKo: itemDescriptionKo(item),
                    descriptionOk: "",
                    compliance: compliance);
            }
        }
    }
}
