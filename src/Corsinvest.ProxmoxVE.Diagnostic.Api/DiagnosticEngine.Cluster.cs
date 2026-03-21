/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task<(bool HasCluster, IEnumerable<ClusterBackup> ClusterBackups)> CheckClusterAsync(List<ClusterResource> resources)
    {
        var clusterConfigNodes = await client.Cluster.Config.Nodes.GetAsync();
        var hasCluster = clusterConfigNodes.Any();
        var clusterBackups = await client.Cluster.Backup.GetAsync();

        CheckClusterBackupCompression(clusterBackups);

        if (hasCluster)
        {
            var clusterStatus = await client.Cluster.Status.GetAsync();
            await CheckClusterQuorumAndHaAsync(resources, clusterStatus);
        }

        await CheckClusterPoolsAsync(resources);
        await CheckClusterFirewallAsync(resources);
        await CheckClusterAccessAsync();

        return (hasCluster, clusterBackups);
    }

    private void CheckClusterBackupCompression(IEnumerable<ClusterBackup> clusterBackups)
    {
        // Backup jobs without compression waste storage space (zstd recommended)
        _result.AddRange(clusterBackups.Where(a => a.Enabled && string.IsNullOrWhiteSpace(a.Compress))
                                       .Select(a => new DiagnosticResult
                                       {
                                           Id = $"cluster/backup/{a.Id}",
                                           ErrorCode = "CC0001",
                                           Description = $"Backup job '{a.Id}' has no compression configured",
                                           Context = DiagnosticResultContext.Cluster,
                                           SubContext = "Backup",
                                           Gravity = DiagnosticResultGravity.Info,
                                       }));
    }

    private async Task CheckClusterQuorumAndHaAsync(List<ClusterResource> resources,
                                                    IEnumerable<ClusterStatus> clusterStatus)
    {
        // Quorum lost means the cluster cannot make decisions — VMs may not start or migrate
        var clusterInfo = clusterStatus.FirstOrDefault(a => a.Type == "cluster");
        if (clusterInfo != null && clusterInfo.Quorate == 0)
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = "CC0001",
                Description = "Cluster has lost quorum — VM operations may be blocked",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Quorum",
                Gravity = DiagnosticResultGravity.Critical,
            });
        }

        // Corosync expected_votes must match the number of online nodes.
        // A mismatch means Corosync still expects votes from nodes that are gone,
        // which can prevent quorum even when all remaining nodes are online.
        if (clusterInfo != null)
        {
            var onlineCount = resources.Count(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
            if (clusterInfo.ExpectedVotes.HasValue
                && clusterInfo.ExpectedVotes.Value != onlineCount)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = "cluster",
                    ErrorCode = "CC0002",
                    Description = $"Corosync expected_votes ({clusterInfo.ExpectedVotes.Value}) does not match online node count ({onlineCount}) — quorum may be unstable",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "Quorum",
                    Gravity = DiagnosticResultGravity.Critical,
                });
            }
        }

        // HA groups referencing nodes that are currently offline — failover may not work
        var onlineNodeNames = resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                       .Select(a => a.Node)
                                       .ToHashSet();
        foreach (var haGroup in await client.Cluster.Ha.Groups.GetAsync())
        {
            if (string.IsNullOrWhiteSpace(haGroup.Nodes)) { continue; }
            var groupNodes = haGroup.Nodes.Split(',').Select(n => n.Split(':')[0].Trim());
            var offlineMembers = groupNodes.Where(n => !onlineNodeNames.Contains(n)).ToList();
            if (offlineMembers.Count > 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = $"cluster/ha/groups/{haGroup.Group}",
                    ErrorCode = "CC0001",
                    Description = $"HA group '{haGroup.Group}' has offline node(s): {string.Join(", ", offlineMembers)}",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "HA",
                    Gravity = DiagnosticResultGravity.Critical,
                });
            }
        }
    }

    // Pools with no VMs and no storage assigned serve no purpose
    private async Task CheckClusterPoolsAsync(List<ClusterResource> resources)
    => _result.AddRange((await client.Pools.GetAsync())
                            .Where(a => !resources.Any(r => r.Pool == a.Id))
                            .Select(a => new DiagnosticResult
                            {
                                Id = $"cluster/pool/{a.Id}",
                                ErrorCode = "CC0001",
                                Description = $"Pool '{a.Id}' is empty (no VMs or storage assigned)",
                                Context = DiagnosticResultContext.Cluster,
                                SubContext = "Pool",
                                Gravity = DiagnosticResultGravity.Info,
                            }));

    private async Task CheckClusterFirewallAsync(List<ClusterResource> resources)
    {
        // Firewall enabled at cluster level but disabled on individual nodes — inconsistent protection
        var clusterFwOptions = await client.Cluster.Firewall.Options.GetAsync();
        if (!clusterFwOptions.Enable) { return; }

        foreach (var node in resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline))
        {
            var nodeFwOptions = await client.Nodes[node.Node].Firewall.Options.GetAsync();
            if (!nodeFwOptions.Enable)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = node.GetWebUrl(),
                    ErrorCode = "WN0001",
                    Description = $"Cluster firewall is enabled but node '{node.Node}' has firewall disabled",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Firewall",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }
    }

    private async Task CheckClusterAccessAsync()
    {
        // Local users (pam/pve realm) without expiration and tokens without expiration are a security risk
        var accessUsers = await client.Access.Users.GetAsync();
        foreach (var user in accessUsers.Where(a => a.Enable && (a.RealmType == "pam" || a.RealmType == "pve")))
        {
            // Expire=0 means no expiration set
            if (user.Expire == 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = $"access/users/{user.Id}",
                    ErrorCode = "CC0001",
                    Description = $"Local user '{user.Id}' has no expiration date configured",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "Access",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }

            // API tokens without expiration remain valid indefinitely — security risk
            _result.AddRange(user.Tokens.Where(a => a.Expire == 0)
                                        .Select(token => new DiagnosticResult
                                        {
                                            Id = $"access/users/{user.Id}",
                                            ErrorCode = "CC0001",
                                            Description = $"API token '{user.Id}!{token.Id}' has no expiration date configured",
                                            Context = DiagnosticResultContext.Cluster,
                                            SubContext = "Access",
                                            Gravity = DiagnosticResultGravity.Info,
                                        }));
        }
    }
}
