/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Diagnostic helper
/// </summary>
public partial class DiagnosticEngine(PveClient client, Settings settings)
{
    private readonly List<DiagnosticResult> _result = new();
    private readonly DateTime _now = DateTime.Now;


    /// <summary>
    /// Analyze cluster by querying PVE API directly
    /// </summary>
    public async Task<ICollection<DiagnosticResult>> AnalyzeAsync(List<DiagnosticResult> ignoredIssues)
    {
        var allResources = await client.Cluster.Resources.GetAsync();
        var resources = allResources.Where(a => !a.IsUnknown).ToList();

        // Resources with unknown type are always a problem — report them all as Critical
        _result.AddRange(allResources.Where(a => a.IsUnknown)
                                     .Select(a => new DiagnosticResult
                                     {
                                         Id = a.GetWebUrl(),
                                         ErrorCode = "CU0001",
                                         Description = $"Unknown resource {a.Type}",
                                         Context = DiagnosticResult.DecodeContext(a.Type),
                                         SubContext = "Status",
                                         Gravity = DiagnosticResultGravity.Critical,
                                     }));

        var (hasCluster, clusterBackups) = await CheckClusterAsync(resources);

        await CheckStorageAsync(resources);
        await CheckNodesAsync(resources, hasCluster);
        await CheckQemuAsync(resources, clusterBackups, hasCluster);
        await CheckLxcAsync(resources, clusterBackups);

        foreach (var ignoredIssue in ignoredIssues)
        {
            foreach (var item in _result.Where(a => ignoredIssue.CheckIgnoreIssue(a)))
            {
                item.IsIgnoredIssue = true;
            }
        }

        return _result;
    }
}
