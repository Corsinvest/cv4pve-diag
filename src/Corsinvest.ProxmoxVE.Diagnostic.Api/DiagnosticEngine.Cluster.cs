/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task<bool> CheckClusterAsync(int pveMajorVersion)
    {
        var clusterConfigNodesTask = client.Cluster.Config.Nodes.GetAsync()
                                            .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster config nodes");
        var clusterBackupTask = client.Cluster.Backup.GetAsync()
                                      .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster backup jobs");
        await Task.WhenAll(clusterConfigNodesTask, clusterBackupTask);
        var clusterConfigNodes = clusterConfigNodesTask.Result;
        var hasCluster = clusterConfigNodes.Any();
        _clusterBackups = clusterBackupTask.Result;

        await CheckClusterBackupAsync();

        if (hasCluster)
        {
            var clusterStatus = await client.Cluster.Status.GetAsync()
                                      .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster status");
            await CheckClusterQuorumAndHaAsync(clusterStatus, pveMajorVersion);
            await CheckClusterHaAndReplicationAsync();
        }

        await CheckClusterPoolsAsync();
        await CheckClusterFirewallAsync();
        await CheckClusterAccessAsync();
        await CheckClusterLogAsync();

        // Single-node "cluster": HA, quorum and replication are not effective.
        // Counted independently of hasCluster so even a non-clustered single host gets the hint.
        var nodeCount = _resources.Count(a => a.ResourceType == ClusterResourceType.Node);
        if (nodeCount == 1)
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = "IC0017",
                Description = "Cluster has a single node — HA, quorum and replication provide no real protection",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Topology",
                Gravity = DiagnosticResultGravity.Info,
            });
        }

        return hasCluster;
    }

    private async Task CheckClusterLogAsync()
    {
        // 200 recent entries — enough to catch a burst of errors without dragging the whole journal.
        var entries = await client.Cluster.Log.GetAsync(max: 200)
                            .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster log");

        // syslog priorities 0..3 are emerg/alert/crit/err — anything above is warning/info/debug.
        var errors = entries.Count(e => e.Severity >= 0 && e.Severity <= 3);
        if (errors >= 10)
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster",
                ErrorCode = "IC0015",
                Description = $"Cluster log has {errors} error-level entries in the last 200 — review the journal",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Log",
                Gravity = DiagnosticResultGravity.Info,
            });
        }
    }

    private async Task CheckClusterBackupAsync()
    {
        var backupList = _clusterBackups.ToList();

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

        // Backup jobs without compression waste storage space (zstd recommended).
        // PBS targets are skipped: Proxmox Backup Server compresses chunks server-side
        // (always zstd) and exposes no 'compress' option on the job.
        _result.AddRange(backupList.Where(a => a.Enabled
                                               && string.IsNullOrWhiteSpace(a.Compress)
                                               && !IsPbsStorage(a.Storage))
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
                                               && a.ExtensionData?.ContainsKey("maxfiles") is not true
                                               && a.ExtensionData?.ContainsKey("prune-backups") is not true)
                                   .Select(a => new DiagnosticResult
                                   {
                                       Id = $"cluster/backup/{a.Id}",
                                       ErrorCode = "WC0002",
                                       Description = $"Backup job '{a.Id}' has no retention policy (maxfiles/prune) — storage will fill up",
                                       Context = DiagnosticResultContext.Cluster,
                                       SubContext = "Backup",
                                       Gravity = DiagnosticResultGravity.Warning,
                                   }));

        // Enabled backup job with no schedule: it will never run automatically.
        _result.AddRange(backupList.Where(a => a.Enabled && string.IsNullOrWhiteSpace(a.Schedule))
                                   .Select(a => new DiagnosticResult
                                   {
                                       Id = $"cluster/backup/{a.Id}",
                                       ErrorCode = "WC0017",
                                       Description = $"Backup job '{a.Id}' is enabled but has no schedule — it will never run automatically",
                                       Context = DiagnosticResultContext.Cluster,
                                       SubContext = "Backup",
                                       Gravity = DiagnosticResultGravity.Warning,
                                   }));

        // Disabled backup jobs: informational, often leftover configuration worth reviewing.
        _result.AddRange(backupList.Where(a => !a.Enabled)
                                   .Select(a => new DiagnosticResult
                                   {
                                       Id = $"cluster/backup/{a.Id}",
                                       ErrorCode = "IC0012",
                                       Description = $"Backup job '{a.Id}' is currently disabled",
                                       Context = DiagnosticResultContext.Cluster,
                                       SubContext = "Backup",
                                       Gravity = DiagnosticResultGravity.Info,
                                   }));

        // Cluster-wide task feed — used here for recent backup failures and below for task error rate.
        var clusterTasks = (await client.Cluster.Tasks.GetAsync()
                                  .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "cluster task feed"))
                                  .ToList();

        // Recent vzdump task that did not complete successfully — backup likely failed.
        _result.AddRange(clusterTasks.Where(t => (t.Type?.StartsWith("vzdump", StringComparison.OrdinalIgnoreCase) ?? false)
                                                 && t.EndTime > 0
                                                 && !string.Equals(t.Status, "OK", StringComparison.OrdinalIgnoreCase))
                                     .Select(t => new DiagnosticResult
                                     {
                                         Id = $"nodes/{t.Node}",
                                         ErrorCode = "WC0018",
                                         Description = $"Backup task on node '{t.Node}' by '{t.User}' ended with status '{t.Status}'",
                                         Context = DiagnosticResultContext.Cluster,
                                         SubContext = "Backup",
                                         Gravity = DiagnosticResultGravity.Warning,
                                     }));

        // Overall task failure rate — sustained failures across the cluster usually indicate a systemic issue.
        var finishedTasks = clusterTasks.Where(t => t.EndTime > 0).ToList();
        if (finishedTasks.Count >= 10)
        {
            var failed = finishedTasks.Count(t => !string.Equals(t.Status, "OK", StringComparison.OrdinalIgnoreCase));
            var ratio = (double)failed / finishedTasks.Count;
            if (ratio >= 0.10)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = "cluster",
                    ErrorCode = "IC0016",
                    Description = $"Cluster task failure rate is {ratio:P0} ({failed}/{finishedTasks.Count}) — investigate recurring errors",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "Tasks",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }
        }
    }

    private async Task CheckClusterHaAndReplicationAsync()
    {
        // Cluster without any HA resource configured — no automatic failover on node failure
        var haResourcesTask = client.Cluster.Ha.Resources.GetAsync()
                                    .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "HA resources");
        var haStatusTask = client.Cluster.Ha.Status.Current.GetAsync()
                                 .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "HA status");
        var replJobsTask = client.Cluster.Replication.GetAsync()
                                 .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "replication jobs");
        await Task.WhenAll(haResourcesTask, haStatusTask, replJobsTask);

        // Cache the guest ids referenced by HA / enabled replication so per-guest checks don't re-walk them.
        foreach (var h in haResourcesTask.Result)
        {
            // Sid format is "<type>:<vmid>" — e.g. "vm:100", "ct:200".
            var parts = (h.Sid ?? "").Split(':');
            if (parts.Length == 2 && long.TryParse(parts[1], out var vmid)) { _haVmIds.Add(vmid); }
        }
        foreach (var r in replJobsTask.Result.Where(r => !r.Disable && !string.IsNullOrWhiteSpace(r.Guest)))
        {
            if (long.TryParse(r.Guest, out var vmid)) { _replicatedVmIds.Add(vmid); }
        }

        if (!haResourcesTask.Result.Any())
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

        // HA service in error state — the resource is not running and will not be recovered automatically
        _result.AddRange(haStatusTask.Result
                            .Where(a => a.Type == "service"
                                        && !string.IsNullOrWhiteSpace(a.State)
                                        && a.State.Equals("error", StringComparison.OrdinalIgnoreCase))
                            .Select(a => new DiagnosticResult
                            {
                                Id = $"cluster/ha/{a.Sid}",
                                ErrorCode = "CC0005",
                                Description = $"HA resource '{a.Sid}' is in error state on node '{a.Node}' — manual recovery required",
                                Context = DiagnosticResultContext.Cluster,
                                SubContext = "HA",
                                Gravity = DiagnosticResultGravity.Critical,
                            }));

        // Cluster without any replication job — no storage redundancy between nodes
        if (!replJobsTask.Result.Any())
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

        // Disabled replication job — the guest's data is no longer kept in sync on the target node
        _result.AddRange(replJobsTask.Result.Where(a => a.Disable)
                                            .Select(a => new DiagnosticResult
                                            {
                                                Id = $"cluster/replication/{a.Id}",
                                                ErrorCode = "WC0009",
                                                Description = $"Replication job '{a.Id}' (guest {a.Guest} → {a.Target}) is disabled — data is no longer replicated",
                                                Context = DiagnosticResultContext.Cluster,
                                                SubContext = "Replication",
                                                Gravity = DiagnosticResultGravity.Warning,
                                            }));

        // Enabled replication job without a schedule — it will never run automatically
        _result.AddRange(replJobsTask.Result.Where(a => !a.Disable && string.IsNullOrWhiteSpace(a.Schedule))
                                            .Select(a => new DiagnosticResult
                                            {
                                                Id = $"cluster/replication/{a.Id}",
                                                ErrorCode = "WC0010",
                                                Description = $"Replication job '{a.Id}' (guest {a.Guest} → {a.Target}) has no schedule — it will never run automatically",
                                                Context = DiagnosticResultContext.Cluster,
                                                SubContext = "Replication",
                                                Gravity = DiagnosticResultGravity.Warning,
                                            }));
    }

    private async Task CheckClusterQuorumAndHaAsync(IEnumerable<ClusterStatus> clusterStatus,
                                                    int pveMajorVersion)
    {
        // Quorum lost means the cluster cannot make decisions — VMs may not start or migrate
        var clusterInfo = clusterStatus.FirstOrDefault(a => a.Type == "cluster");
        if (clusterInfo?.Quorate == 0)
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
            var onlineCount = _resources.Count(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
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

        if (pveMajorVersion < 9)
        {
            // HA groups referencing nodes that are currently offline — failover may not work
            var onlineNodeNames = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                            .Select(a => a.Node)
                                            .ToHashSet();

            foreach (var haGroup in await client.Cluster.Ha.Groups.GetAsync()
                                          .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "HA groups"))
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
    }

    // Pools with no VMs and no storage assigned serve no purpose
    private async Task CheckClusterPoolsAsync()
    => _result.AddRange((await client.Pools.GetAsync()
                                .ToSafeEnum(_result, "cluster", DiagnosticResultContext.Cluster, "pools"))
                            .Where(a => !_resources.Any(r => r.Pool == a.Id))
                            .Select(a => new DiagnosticResult
                            {
                                Id = $"cluster/pool/{a.Id}",
                                ErrorCode = "IC0004",
                                Description = $"Pool '{a.Id}' is empty (no VMs or storage assigned)",
                                Context = DiagnosticResultContext.Cluster,
                                SubContext = "Pool",
                                Gravity = DiagnosticResultGravity.Info,
                            }));

    private async Task CheckClusterFirewallAsync()
    {
        var clusterFwOptions = await client.Cluster.Firewall.Options.GetAsync()
                                     .ToSafeSingle(_result, "cluster", DiagnosticResultContext.Cluster, "cluster firewall options");
        // If the fetch failed we already recorded a finding — nothing else this method can do.
        if (clusterFwOptions == null) { return; }

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

        // Firewall enabled at cluster level but disabled on individual nodes — inconsistent protection.
        // The per-node fetch is wrapped: a single faulty node degrades to null and is silently skipped
        // (the failure was already recorded as a finding by ToSafeSingle).
        var onlineNodes = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline).ToList();
        var nodeFwResults = await RunParallelAsync(onlineNodes,
            node => client.Nodes[node.Node].Firewall.Options.GetAsync()
                          .ToSafeSingle(_result, node.GetWebUrl(), DiagnosticResultContext.Node, $"firewall options on node '{node.Node}'"));
        foreach (var (node, nodeFwOptions) in onlineNodes.Zip(nodeFwResults))
        {
            if (nodeFwOptions != null && !nodeFwOptions.Enable)
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

        // Cluster firewall rules with source or dest 0.0.0.0/0 — overly permissive
        var clusterRules = (await client.Cluster.Firewall.Rules.GetAsync()
                                  .ToSafeEnum(_result, "cluster/firewall/rules", DiagnosticResultContext.Cluster, "cluster firewall rules")).ToList();
        _result.AddRange(clusterRules.Where(r => r.Enable
                                                 && (r.Source == "0.0.0.0/0" || r.Dest == "0.0.0.0/0"))
                                     .Select(r => new DiagnosticResult
                                     {
                                         Id = "cluster/firewall/rules",
                                         ErrorCode = "WC0008",
                                         Description = $"Firewall rule #{r.Positon} allows traffic from/to 0.0.0.0/0 — overly permissive",
                                         Context = DiagnosticResultContext.Cluster,
                                         SubContext = "Firewall",
                                         Gravity = DiagnosticResultGravity.Warning,
                                     }));

        // Cluster firewall is enabled but no enabled rule has logging configured — no audit trail.
        // "nolog" or empty disables logging; anything else (warning, info, debug, …) is considered logging.
        var enabledRules = clusterRules.Where(r => r.Enable).ToList();
        if (enabledRules.Count > 0
            && !enabledRules.Any(r => !string.IsNullOrWhiteSpace(r.Log)
                                      && !string.Equals(r.Log, "nolog", StringComparison.OrdinalIgnoreCase)))
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster/firewall/rules",
                ErrorCode = "IC0013",
                Description = $"Cluster firewall has {enabledRules.Count} enabled rules but none have logging configured — no audit trail",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Firewall",
                Gravity = DiagnosticResultGravity.Info,
            });
        }

        // Many disabled rules cluster-wide — stale configuration accumulating noise.
        var disabledCount = clusterRules.Count(r => !r.Enable);
        if (disabledCount >= 10)
        {
            _result.Add(new DiagnosticResult
            {
                Id = "cluster/firewall/rules",
                ErrorCode = "IC0014",
                Description = $"Cluster firewall has {disabledCount} disabled rules — consider cleaning up stale configuration",
                Context = DiagnosticResultContext.Cluster,
                SubContext = "Firewall",
                Gravity = DiagnosticResultGravity.Info,
            });
        }
    }

    private async Task CheckClusterAccessAsync()
    {
        // Local users (pam/pve realm) without expiration and tokens without expiration are a security risk
        var accessUsersTask = client.Access.Users.GetAsync().ToSafeEnum(_result, "access/users", DiagnosticResultContext.Cluster, "access users");
        var tfaEntriesTask = client.Access.Tfa.GetAsync().ToSafeEnum(_result, "access/tfa", DiagnosticResultContext.Cluster, "TFA entries");
        var aclsTask = client.Access.Acl.GetAsync().ToSafeEnum(_result, "access/acl", DiagnosticResultContext.Cluster, "ACL entries");
        var groupsTask = client.Access.Groups.GetAsync().ToSafeEnum(_result, "access/groups", DiagnosticResultContext.Cluster, "access groups");
        var rolesTask = client.Access.Roles.GetAsync().ToSafeEnum(_result, "access/roles", DiagnosticResultContext.Cluster, "access roles");
        var domainsTask = client.Access.Domains.GetAsync().ToSafeEnum(_result, "access/domains", DiagnosticResultContext.Cluster, "access domains");
        await Task.WhenAll(accessUsersTask, tfaEntriesTask, aclsTask, groupsTask, rolesTask, domainsTask);
        var accessUsers = accessUsersTask.Result;
        var tfaEntries = tfaEntriesTask.Result;
        var acls = aclsTask.Result;
        var groups = groupsTask.Result;
        var roles = rolesTask.Result;
        var domains = domainsTask.Result;

        var usersWithTfa = tfaEntries.Where(t => t.Entries?.Any() is true)
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
        var adminUserIds = acls.Where(a => a.Roleid == "Administrator" && a.Type == "user")
                               .Select(a => a.UsersGroupid)
                               .ToHashSet();

        // ACL Administrator role assigned at root path '/' — too permissive, prefer scoped permissions
        _result.AddRange(acls.Where(a => a.Roleid == "Administrator" && a.Path == "/" && a.Type == "user")
                             .Select(a => new DiagnosticResult
                             {
                                 Id = "access/acl",
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

        // Enabled users without an email — notifications (backup failures, fencing, etc.) cannot reach them
        _result.AddRange(accessUsers.Where(a => a.Enable && string.IsNullOrWhiteSpace(a.Email))
                                    .Select(a => new DiagnosticResult
                                    {
                                        Id = $"access/users/{a.Id}",
                                        ErrorCode = "IC0007",
                                        Description = $"User '{a.Id}' has no email configured — will not receive notifications",
                                        Context = DiagnosticResultContext.Cluster,
                                        SubContext = "Access",
                                        Gravity = DiagnosticResultGravity.Info,
                                    }));

        // Empty groups — no users assigned, usually leftover configuration
        _result.AddRange(groups.Where(a => string.IsNullOrWhiteSpace(a.Users))
                               .Select(a => new DiagnosticResult
                               {
                                   Id = $"access/groups/{a.Id}",
                                   ErrorCode = "IC0008",
                                   Description = $"Group '{a.Id}' has no members",
                                   Context = DiagnosticResultContext.Cluster,
                                   SubContext = "Access",
                                   Gravity = DiagnosticResultGravity.Info,
                               }));

        // Custom roles not referenced by any ACL — dead configuration
        var rolesInUse = acls.Where(a => !string.IsNullOrWhiteSpace(a.Roleid))
                             .Select(a => a.Roleid)
                             .ToHashSet();
        _result.AddRange(roles.Where(a => a.Special == 0 && !rolesInUse.Contains(a.Id))
                              .Select(a => new DiagnosticResult
                              {
                                  Id = $"access/roles/{a.Id}",
                                  ErrorCode = "IC0009",
                                  Description = $"Custom role '{a.Id}' is not assigned in any ACL — unused",
                                  Context = DiagnosticResultContext.Cluster,
                                  SubContext = "Access",
                                  Gravity = DiagnosticResultGravity.Info,
                              }));

        // Groups holding Administrator role on '/': any enabled member without TFA is a security risk.
        // WC0007 covers users with direct ACL; this covers users that get admin transitively via group.
        var privilegedGroupIds = acls.Where(a => a.Path == "/" && a.Type == "group" && a.Roleid == "Administrator")
                                     .Select(a => a.UsersGroupid)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups.Where(g => privilegedGroupIds.Contains(g.Id) && !string.IsNullOrWhiteSpace(g.Users)))
        {
            foreach (var member in g.Users.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (usersWithTfa.Contains(member)) { continue; }
                var u = accessUsers.FirstOrDefault(x => string.Equals(x.Id, member, StringComparison.OrdinalIgnoreCase));
                if (u != null && !u.Enable) { continue; }
                _result.Add(new DiagnosticResult
                {
                    Id = $"access/users/{member}",
                    ErrorCode = "WC0013",
                    Description = $"User '{member}' has Administrator role via group '{g.Id}' but no TFA configured",
                    Context = DiagnosticResultContext.Cluster,
                    SubContext = "Access",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }

        // Disabled user that still has Administrator ACL on '/': leftover privilege from before deactivation.
        var disabledUserIds = accessUsers.Where(u => !u.Enable)
                                         .Select(u => u.Id)
                                         .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _result.AddRange(acls.Where(a => a.Path == "/" && a.Type == "user" && a.Roleid == "Administrator"
                                         && disabledUserIds.Contains(a.UsersGroupid))
                             .Select(a => new DiagnosticResult
                             {
                                 Id = $"access/users/{a.UsersGroupid}",
                                 ErrorCode = "WC0014",
                                 Description = $"Disabled user '{a.UsersGroupid}' still has Administrator role on '/' — revoke the ACL entry",
                                 Context = DiagnosticResultContext.Cluster,
                                 SubContext = "Access",
                                 Gravity = DiagnosticResultGravity.Warning,
                             }));

        // Administrator ACL on '/' with Propagate disabled: unusual, often a misconfiguration.
        _result.AddRange(acls.Where(a => a.Path == "/" && a.Roleid == "Administrator" && a.Propagate == 0)
                             .Select(a => new DiagnosticResult
                             {
                                 Id = "access/acl",
                                 ErrorCode = "IC0010",
                                 Description = $"{a.Type} '{a.UsersGroupid}' has Administrator role on '/' but Propagate is disabled — children resources do not inherit it",
                                 Context = DiagnosticResultContext.Cluster,
                                 SubContext = "Access",
                                 Gravity = DiagnosticResultGravity.Info,
                             }));

        // External realm (LDAP/AD/OpenID) without realm-level TFA: weaker baseline than pve/pam where per-user TFA can be enforced.
        _result.AddRange(domains.Where(d => string.IsNullOrWhiteSpace(d.Tfa)
                                            && (string.Equals(d.Type, "ldap", StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(d.Type, "ad", StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(d.Type, "openid", StringComparison.OrdinalIgnoreCase)))
                                .Select(d => new DiagnosticResult
                                {
                                    Id = $"access/domains/{d.Realm}",
                                    ErrorCode = "IC0011",
                                    Description = $"External realm '{d.Realm}' ({d.Type}) does not enforce TFA at realm level",
                                    Context = DiagnosticResultContext.Cluster,
                                    SubContext = "Access",
                                    Gravity = DiagnosticResultGravity.Info,
                                }));

        // root@pam API tokens without privilege separation inherit full root rights — they should always be priv-separated.
        if (root != null)
        {
            _result.AddRange(root.Tokens.Where(t => t.Privsep == 0)
                                        .Select(t => new DiagnosticResult
                                        {
                                            Id = $"access/users/root@pam",
                                            ErrorCode = "WC0015",
                                            Description = $"root@pam token '{t.Id}' has no privilege separation — it has full root rights",
                                            Context = DiagnosticResultContext.Cluster,
                                            SubContext = "Access",
                                            Gravity = DiagnosticResultGravity.Warning,
                                        }));
        }

        // Enabled user whose expiration has already passed: account should have been deactivated.
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _result.AddRange(accessUsers.Where(u => u.Enable && u.Expire > 0 && u.Expire < nowUnix)
                                    .Select(u => new DiagnosticResult
                                    {
                                        Id = $"access/users/{u.Id}",
                                        ErrorCode = "WC0016",
                                        Description = $"User '{u.Id}' is enabled but expired on {DateTimeOffset.FromUnixTimeSeconds(u.Expire):yyyy-MM-dd} — account should be deactivated",
                                        Context = DiagnosticResultContext.Cluster,
                                        SubContext = "Access",
                                        Gravity = DiagnosticResultGravity.Warning,
                                    }));
    }
}
