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
            await CheckClusterHaAndReplicationAsync();
        }

        await CheckClusterPoolsAsync(resources);
        await CheckClusterFirewallAsync(resources);
        await CheckClusterAccessAsync();

        return (hasCluster, clusterBackups);
    }

    private void CheckClusterBackupCompression(IEnumerable<ClusterBackup> clusterBackups)
    {
        var backupList = clusterBackups.ToList();

        // No backup jobs defined at all — entire cluster has no automated backup
        if (backupList.Count == 0)
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster/backup",
                ErrorCode = "WC0001",
                Description = "No backup job configured — no automated backup for any VM/CT",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Backup",
                Gravity = DiagnosticResultGravity.Warning,
            });
            return;
        }

        // Backup jobs without compression waste storage space (zstd recommended)
        _result.AddRange(backupList.Where(a => a.Enabled && string.IsNullOrWhiteSpace(a.Compress))
                                   .Select(a => new DiagnosticResult
                                   {
                                       Id = $"cluster/backup/{a.Id}",
                                       ErrorCode = "IC0001",
                                       Description = $"Backup job '{a.Id}' has no compression configured",
                                       Context = DiagnosticResultContext.Cluster,
                                       SubContext = "Backup",
                                       Gravity = DiagnosticResultGravity.Info,
                                   }));

        // Backup jobs without retention policy — storage will fill up indefinitely
        _result.AddRange(backupList.Where(a => a.Enabled
                                               && !(a.ExtensionData?.ContainsKey("maxfiles") == true)
                                               && !(a.ExtensionData?.ContainsKey("prune-backups") == true))
                                   .Select(a => new DiagnosticResult
                                   {
                                       Id = $"cluster/backup/{a.Id}",
                                       ErrorCode = "WC0002",
                                       Description = $"Backup job '{a.Id}' has no retention policy (maxfiles/prune) — storage will fill up",
                                       Context = DiagnosticResultContext.Cluster,
                                       SubContext = "Backup",
                                       Gravity = DiagnosticResultGravity.Warning,
                                   }));
    }

    private async Task CheckClusterHaAndReplicationAsync()
    {
        // Cluster without any HA resource configured — no automatic failover on node failure
        var haResources = await client.Cluster.Ha.Resources.GetAsync();
        if (!haResources.Any())
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = "IC0002",
                Description = "No HA resources configured — VMs will not automatically restart on node failure",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "HA",
                Gravity = DiagnosticResultGravity.Info,
            });
        }

        // Cluster without any replication job — no storage redundancy between nodes
        var replJobs = await client.Cluster.Replication.GetAsync();
        if (!replJobs.Any())
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = "IC0003",
                Description = "No storage replication jobs configured — no redundant copy of VM data across nodes",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Replication",
                Gravity = DiagnosticResultGravity.Info,
            });
        }
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
                    ErrorCode = "CC0003",
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
                                ErrorCode = "IC0004",
                                Description = $"Pool '{a.Id}' is empty (no VMs or storage assigned)",
                                Context = DiagnosticResultContext.Cluster,
                                SubContext = "Pool",
                                Gravity = DiagnosticResultGravity.Info,
                            }));

    private async Task CheckClusterFirewallAsync(List<ClusterResource> resources)
    {
        var clusterFwOptions = await client.Cluster.Firewall.Options.GetAsync();

        // Cluster firewall completely disabled — no traffic filtering at all
        if (!clusterFwOptions.Enable)
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = "WC0003",
                Description = "Cluster firewall is disabled — no traffic filtering is active",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Firewall",
                Gravity = DiagnosticResultGravity.Warning,
            });
            return;
        }

        // Inbound and outbound policies should be DROP — ACCEPT allows unmatched traffic through
        foreach (var (policy, direction) in new[] {
            (clusterFwOptions.PolicyIn, "inbound"),
            (clusterFwOptions.PolicyOut, "outbound") })
        {
            if (!string.IsNullOrWhiteSpace(policy)
                && !policy.Equals("DROP", StringComparison.OrdinalIgnoreCase))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = "cluster",
                    ErrorCode = "WC0004",
                    Description = $"Cluster firewall {direction} policy is '{policy}' — recommended value is DROP",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "Firewall",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }

        // Firewall enabled at cluster level but disabled on individual nodes — inconsistent protection
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

        // TFA check — fetch once and reuse for all user checks
        var tfaEntries = await client.Access.Tfa.GetAsync();
        var usersWithTfa = tfaEntries.Where(t => t.Entries?.Any() == true)
                                     .Select(t => t.UserId)
                                     .ToHashSet();

        // root@pam without TFA is a critical security risk — full access with a single password
        var root = accessUsers.FirstOrDefault(u => u.Id == "root@pam" && u.Enable);
        if (root != null && !usersWithTfa.Contains("root@pam"))
        {
            _result.Add(new DiagnosticResult
            {
                Id = "access/users/root@pam",
                ErrorCode = "CC0004",
                Description = "root@pam has no TFA configured — full access protected only by password",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Access",
                Gravity = DiagnosticResultGravity.Critical,
            });
        }

        // Admin users without TFA — fetch ACLs once to find users with Administrator role
        var acls = await client.Access.Acl.GetAsync();
        var adminUserIds = acls.Where(a => a.Roleid == "Administrator" && a.Type == "user")
                               .Select(a => a.UsersGroupid)
                               .ToHashSet();

        // ACL Administrator role assigned at root path '/' — too permissive, prefer scoped permissions
        _result.AddRange(acls.Where(a => a.Roleid == "Administrator" && a.Path == "/" && a.Type == "user")
                             .Select(a => new DiagnosticResult
                             {
                                 Id = $"access/acl",
                                 ErrorCode = "WC0005",
                                 Description = $"User '{a.UsersGroupid}' has Administrator role at root path '/' — prefer pool/node-scoped permissions",
                                 Context = DiagnosticResultContext.Cluster,
                                 SubContext = "Access",
                                 Gravity = DiagnosticResultGravity.Warning,
                             }));

        // Disabled users with active API tokens — tokens remain valid even when user is disabled
        _result.AddRange(accessUsers.Where(u => !u.Enable && u.Tokens.Any())
                                    .SelectMany(u => u.Tokens.Select(token => new DiagnosticResult
                                    {
                                        Id = $"access/users/{u.Id}",
                                        ErrorCode = "WC0006",
                                        Description = $"Disabled user '{u.Id}' has active API token '{u.Id}!{token.Id}' — token remains valid and should be revoked",
                                        Context = DiagnosticResultContext.Cluster,
                                        SubContext = "Access",
                                        Gravity = DiagnosticResultGravity.Warning,
                                    })));

        foreach (var user in accessUsers.Where(u => u.Enable && adminUserIds.Contains(u.Id) && u.Id != "root@pam"))
        {
            if (!usersWithTfa.Contains(user.Id))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = $"access/users/{user.Id}",
                    ErrorCode = "WC0007",
                    Description = $"Admin user '{user.Id}' has no TFA configured",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "Access",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }
        foreach (var user in accessUsers.Where(a => a.Enable && (a.RealmType == "pam" || a.RealmType == "pve")))
        {
            // Expire=0 means no expiration set
            if (user.Expire == 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = $"access/users/{user.Id}",
                    ErrorCode = "IC0005",
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
                                            ErrorCode = "IC0006",
                                            Description = $"API token '{user.Id}!{token.Id}' has no expiration date configured",
                                            Context = DiagnosticResultContext.Cluster,
                                            SubContext = "Access",
                                            Gravity = DiagnosticResultGravity.Info,
                                        }));
        }
    }
}
