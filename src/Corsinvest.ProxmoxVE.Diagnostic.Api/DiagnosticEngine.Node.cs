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

public partial class DiagnosticEngine
{
    private record NodeCompareData(NodeVersion Version,
                                   string[] Hosts,
                                   NodeDns Dns,
                                   string Timezone,
                                   IEnumerable<NodeAptVersion> AptVersions,
                                   NodeStatus Status,
                                   long UtcTime,
                                   NodeAptRepositories? AptRepositories,
                                   IEnumerable<NodeNetwork> Networks);

    private async Task CheckNodesAsync(List<ClusterResource> resources, bool hasCluster)
    {
        var endOfLife = new Dictionary<int, DateTime>
        {
            { 7, new DateTime(2024, 07, 01) },
            { 6, new DateTime(2022, 09, 01) },
            { 5, new DateTime(2020, 07, 01) },
            { 4, new DateTime(2018, 06, 01) },
        };

        var onlineNodes = resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline).ToList();

        // Pre-fetch lightweight per-node data once to avoid redundant API calls during cross-node comparisons
        var nodeCompareData = new Dictionary<string, NodeCompareData>();
        foreach (var item in onlineNodes)
        {
            var api = client.Nodes[item.Node];
            var timeRaw = (await api.Time.Time()).ToData();
            var timezone = timeRaw.timezone as string ?? string.Empty;
            var nodeUtcTime = timeRaw.time is long t ? t : 0L;
            nodeCompareData[item.Node] = new NodeCompareData(await api.Version.GetAsync(),
                                                             ((string)(await api.Hosts.GetEtcHosts()).ToData().data).Split('\n'),
                                                             await api.Dns.GetAsync(),
                                                             timezone,
                                                             await api.Apt.Versions.GetAsync(),
                                                             await api.Status.GetAsync(),
                                                             nodeUtcTime,
                                                             await api.Apt.Repositories.GetAsync(),
                                                             await api.Network.GetAsync());
        }

        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Node))
        {
            var id = item.GetWebUrl();

            if (!item.IsOnline)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WN0001",
                    Description = "Node not online",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Status",
                    Gravity = DiagnosticResultGravity.Warning,
                });
                continue;
            }

            var nodeApi = client.Nodes[item.Node];
            var (version, hosts, dns, timezone, aptVersions, nodeStatus, nodeUtcTime, aptRepositories, networks) = nodeCompareData[item.Node];
            if (!int.TryParse(version.Version?.Split(".")[0], out var nodeVersion)) { continue; }

            #region End Of Life
            // PVE versions with a known EOL date that has already passed
            if (endOfLife.TryGetValue(nodeVersion, out var eolDate) && _now.Date >= eolDate)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WN0001",
                    Description = $"Version {version.Version} end of life {eolDate}",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "EOL",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Subscription
            // Without an active subscription the node uses the community repo and has no enterprise support
            var subscription = await nodeApi.Subscription.GetAsync();
            if (!subscription.Status.Equals("active", StringComparison.CurrentCultureIgnoreCase))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WN0001",
                    Description = "Node not have subscription active",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Subscription",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region RrdData
            // Historical resource usage (CPU, RAM, network, disk) via RRD — period configurable (day/week)
            var rrdData = settings.Node.TimeSeries switch
            {
                SettingsTimeSeriesType.Day => await nodeApi.Rrddata.GetAsync(RrdDataTimeFrame.Day, RrdDataConsolidation.Average),
                SettingsTimeSeriesType.Week => await nodeApi.Rrddata.GetAsync(RrdDataTimeFrame.Week, RrdDataConsolidation.Average),
                _ => throw new ArgumentOutOfRangeException("settings.Node.TimeSeries"),
            };

            CheckNodeRrd(_result, settings, id, rrdData);
            #endregion

            #region Cross-node comparisons
            // All nodes in a cluster must share the same PVE version, /etc/hosts, DNS and timezone
            // to avoid subtle live migration and corosync issues.
            // checkInNodes flags prevent duplicate issues when comparing node A vs B and B vs A.
            var checkInNodes = new bool[] { true, true, true, true, true };
            foreach (var other in onlineNodes.Where(a => a.Node != item.Node))
            {
                if (!nodeCompareData.TryGetValue(other.Node, out var otherData)) { continue; }
                var (otherVersion, otherHosts, otherDns, otherTimezone, otherAptVersions, _, _, otherAptRepositories, otherNetworks) = otherData;

                if (checkInNodes[0] && !version.IsEqual(otherVersion))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WN0001",
                        Description = "Nodes version not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Version",
                        Gravity = DiagnosticResultGravity.Critical,
                    });
                    checkInNodes[0] = false;
                }

                if (checkInNodes[1] && string.Join("", hosts) != string.Join("", otherHosts))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WN0001",
                        Description = "Nodes hosts configuration not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Hosts",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    checkInNodes[1] = false;
                }

                if (checkInNodes[2] && !dns.IsEqual(otherDns))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WN0001",
                        Description = "Nodes DNS not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "DNS",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    checkInNodes[2] = false;
                }

                if (checkInNodes[3] && timezone != otherTimezone)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WN0001",
                        Description = "Nodes Timezone not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Timezone",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    checkInNodes[3] = false;
                }

                // APT repository sources must be identical across nodes to ensure consistent upgrades.
                // Compare the enabled URIs from all repository files — order-insensitive.
                if (checkInNodes[4])
                {
                    var getUris = (NodeAptRepositories? repos) =>
                        (repos?.Files ?? [])
                             .SelectMany(f => f.Repositories ?? [])
                             .Where(r => r.Enabled)
                             .SelectMany(r => r.URIs ?? [])
                             .OrderBy(u => u)
                             .ToList();

                    var uris = getUris(aptRepositories);
                    var otherUris = getUris(otherAptRepositories);

                    if (!uris.SequenceEqual(otherUris))
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WN0001",
                            Description = "Nodes APT repositories not equal — inconsistent package sources may cause upgrade problems",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "AptRepositories",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                        checkInNodes[4] = false;
                    }
                }

                // MTU mismatch on physical NICs between nodes can cause packet fragmentation,
                // corosync instability and live migration failures.
                // Compare MTU of eth interfaces by name — only flag if the same interface exists on both nodes.
                if (checkInNodes[4])
                {
                    var myMtus = networks.Where(a => a.Type == "eth" && a.Mtu.HasValue)
                                         .ToDictionary(a => a.Interface, a => a.Mtu!.Value);

                    var otherMtus = otherNetworks.Where(a => a.Type == "eth" && a.Mtu.HasValue)
                                                 .ToDictionary(a => a.Interface, a => a.Mtu!.Value);

                    var mtuMismatches = myMtus.Where(kv => otherMtus.TryGetValue(kv.Key, out var otherMtu)
                                                           && otherMtu != kv.Value)
                                              .Select(kv => $"{kv.Key}: {kv.Value} vs {otherMtus[kv.Key]}")
                                              .ToList();

                    if (mtuMismatches.Count > 0)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WN0001",
                            Description = $"NIC MTU mismatch with other nodes: {string.Join(", ", mtuMismatches)}",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "Network",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                        checkInNodes[4] = false;
                    }
                }
            }
            #endregion

            #region Network Card
            // Physical NICs (type=eth) that are down — could mean a cable/switch problem
            _result.AddRange(networks.Where(a => a.Type == "eth" && !a.Active)
                                     .Select(a => new DiagnosticResult
                                     {
                                         Id = id,
                                         ErrorCode = "WN0002",
                                         Description = $"Network card '{a.Interface}' not active",
                                         Context = DiagnosticResultContext.Node,
                                         SubContext = "Network",
                                         Gravity = DiagnosticResultGravity.Warning,
                                     }));
            #endregion

            #region Package Versions
            // Mismatched package versions across nodes can cause subtle incompatibilities after partial upgrades
            var addErrPackageVersions = false;
            foreach (var other in onlineNodes.Where(a => a.Node != item.Node))
            {
                if (addErrPackageVersions) { break; }
                if (!nodeCompareData.TryGetValue(other.Node, out var otherPkgData)) { continue; }
                var (_, _, _, _, otherAptVersions, _, _, _, _) = otherPkgData;
                foreach (var pkg in aptVersions)
                {
                    if (!otherAptVersions.Any(a => a.Version == pkg.Version && a.Title == pkg.Title && a.Package == pkg.Package))
                    {
                        addErrPackageVersions = true;
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WN0001",
                            Description = "Nodes package version not equal",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "PackageVersions",
                            Gravity = DiagnosticResultGravity.Critical,
                        });
                        break;
                    }
                }
            }
            #endregion

            #region Services
            // corosync is only expected on clustered setups; time sync service name changed in PVE 7+
            var serviceExcluded = new List<string>();
            if (!hasCluster) { serviceExcluded.Add("corosync"); }
            serviceExcluded.Add(nodeVersion >= 7
                                    ? "systemd-timesyncd"
                                    : "chrony");

            _result.AddRange((await nodeApi.Services.GetAsync())
                            .Where(a => !a.IsRunning && !serviceExcluded.Contains(a.Name))
                            .Select(a => new DiagnosticResult
                            {
                                Id = id,
                                ErrorCode = "WN0002",
                                Description = $"Service '{a.Description}' not running",
                                Context = DiagnosticResultContext.Node,
                                SubContext = "Service",
                                Gravity = DiagnosticResultGravity.Warning,
                            }));
            #endregion

            #region Certificates
            // Expired TLS certificates break the web UI and API access
            _result.AddRange((await nodeApi.Certificates.Info.GetAsync())
                              .Where(a => DateTimeOffset.FromUnixTimeSeconds(a.NotAfter) < _now)
                              .Select(a => new DiagnosticResult
                              {
                                  Id = id,
                                  ErrorCode = "WN0002",
                                  Description = $"Certificate '{a.FileName}' expired",
                                  Context = DiagnosticResultContext.Node,
                                  SubContext = "Certificates",
                                  Gravity = DiagnosticResultGravity.Critical,
                              }));
            #endregion

            #region Replication
            // Replication jobs with errors mean the secondary copy is out of date
            var replCount = (await nodeApi.Replication.GetAsync())
                                .Count(a => a.ExtensionData != null && a.ExtensionData.ContainsKey("errors"));
            if (replCount > 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IN0001",
                    Description = $"{replCount} Replication has errors",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Replication",
                    Gravity = DiagnosticResultGravity.Critical,
                });
            }
            #endregion

            await CheckNodeDiskAsync(nodeApi, settings, id);

            #region APT Updates
            // Any pending update is informational; "important" priority updates (security) are Warning
            var aptUpdate = await nodeApi.Apt.Update.GetAsync();
            var updateCount = aptUpdate.Count();
            if (updateCount > 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IN0001",
                    Description = $"{updateCount} Update available",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Update",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }

            var updateImportantCount = aptUpdate.Count(a => a.Priority == "important");
            if (updateImportantCount > 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IN0001",
                    Description = $"{updateImportantCount} Update Important available",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Update",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Reboot required
            // If the running kernel release differs from the installed package version, a reboot is needed
            if (nodeStatus?.CurrentKernel != null && !string.IsNullOrWhiteSpace(nodeStatus.Kversion))
            {
                // Kversion contains the full uname string; CurrentKernel.Release is the running kernel
                // Compare the running kernel release against the installed kversion string
                var runningKernel = nodeStatus.CurrentKernel?.Release ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(runningKernel) && !nodeStatus.Kversion.Contains(runningKernel))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WN0001",
                        Description = $"Node requires reboot: running kernel '{runningKernel}' differs from installed '{nodeStatus.Kversion}'",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Reboot",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            #region NTP
            // Compare node UTC time against the client machine time — offset > 60s indicates NTP issue
            if (nodeUtcTime > 0)
            {
                var ntpOffset = Math.Abs(nodeUtcTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                if (ntpOffset > 60)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WN0001",
                        Description = $"Node time offset is {ntpOffset}s — NTP may not be synchronized",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "NTP",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            #region IOMMU
            // IOMMU is required for PCI passthrough (GPU, NIC, etc.).
            // If all detected PCI devices report IommuGroup == -1 the kernel/firmware has IOMMU disabled.
            // Note: a node with no PCI devices at all is not flagged.
            var pciDevices = await nodeApi.Hardware.Pci.GetAsync();
            if (pciDevices.Any() && pciDevices.All(a => a.IommuGroup == -1))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IN0001",
                    Description = "IOMMU is not enabled — PCI passthrough will not work (enable intel_iommu=on or amd_iommu=on in kernel cmdline)",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "IOMMU",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }
            #endregion

            #region Task history
            // Failed tasks in the last 48 hours (errors=true filters server-side for efficiency)
            var dayTask = new DateTimeOffset(_now.AddDays(-2)).ToUnixTimeSeconds();
            var tasks = (await nodeApi.Tasks.GetAsync(errors: true, limit: 1000)).Where(a => a.StartTime >= dayTask);
            CheckTaskHistory(_result, tasks, DiagnosticResultContext.Node, id);
            #endregion
        }

        #region CPU Compatibility Mode
        // Calculate the minimum common x86-64 feature level across all online nodes.
        // The level determines the safest CPU type to assign to VMs for live migration.
        // If nodes have different levels, VMs using a higher level cannot migrate to lower-level nodes.
        // Levels: v2-AES (2008+), v3 (Haswell 2013+), v4 (Skylake-X 2017+)
        if (hasCluster && onlineNodes.Count > 1)
        {
            var nodeLevels = nodeCompareData.Where(kv => kv.Value.Status?.CpuInfo?.Flags != null)
                                            .ToDictionary(kv => kv.Key, kv => NodeHelper.GetCpuX86Level(kv.Value.Status.CpuInfo.Flags));

            if (nodeLevels.Count > 1)
            {
                var minLevel = nodeLevels.Values.Min();
                var maxLevel = nodeLevels.Values.Max();

                if (minLevel != null && maxLevel != null && minLevel.Level != maxLevel.Level)
                {
                    var lowerNodes = nodeLevels.Where(kv => kv.Value.Level == minLevel.Level)
                                               .Select(kv => kv.Key);
                    _result.Add(new DiagnosticResult
                    {
                        Id = "cluster",
                        ErrorCode = "WN0001",
                        Description = $"CPU level mismatch: minimum is {minLevel.Name}, maximum is {maxLevel.Name}. " +
                                      $"Nodes at minimum level: {string.Join(", ", lowerNodes)}. " +
                                      $"Use cpu type '{minLevel.Name}' for safe live migration.",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "CPUCompatibility",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
        }
        #endregion
    }

    private async Task CheckNodeDiskAsync(PveClient.PveNodes.PveNodeItem nodeApi,
                                          Settings settings,
                                          string id)
    {
        #region Disks
        // S.M.A.R.T. status: anything other than PASSED/OK indicates a failing or failed disk
        var disksAll = await nodeApi.Disks.List.GetAsync(include_partitions: false);

        _result.AddRange(disksAll.Where(a => a.Health != "PASSED" && a.Health != "OK")
                                 .Select(a => new DiagnosticResult
                                 {
                                     Id = id,
                                     ErrorCode = "CN0003",
                                     Description = $"Disk '{a.DevPath}' S.M.A.R.T. status problem",
                                     Context = DiagnosticResultContext.Node,
                                     SubContext = "S.M.A.R.T.",
                                     Gravity = DiagnosticResultGravity.Warning,
                                 }));

        // SSD wearout reported as N/A means the drive doesn't expose wear data — worth investigating
        _result.AddRange(disksAll.Where(a => a.IsSsd && a.Wearout == "N/A")
                                 .Select(a => new DiagnosticResult
                                 {
                                     Id = id,
                                     ErrorCode = "CN0003",
                                     Description = $"Disk ssd '{a.DevPath}' wearout not valid.",
                                     Context = DiagnosticResultContext.Node,
                                     SubContext = "SSD Wearout",
                                     Gravity = DiagnosticResultGravity.Warning,
                                 }));

        // SSD wearout percentage above threshold (100 - wearout = wear consumed)
        CheckThreshold(_result,
                       settings.SsdWearoutThreshold,
                       "CN0003",
                       DiagnosticResultContext.Node,
                       "SSD Wearout",
                       disksAll.Where(a => a.IsSsd && a.Wearout != "N/A")
                               .Select(a => new ThresholdDataPoint(100.0 - Convert.ToDouble(a.Wearout), 0d, id, $"SSD '{a.DevPath}'")),
                       true,
                       false);
        #endregion

        #region Zfs
        // ZFS pool health: anything other than ONLINE means degraded/faulted pool
        var zfsList = await nodeApi.Disks.Zfs.GetAsync() ?? [];

        _result.AddRange(zfsList.Where(a => a.Health != "ONLINE")
                                .Select(a => new DiagnosticResult
                                {
                                    Id = id,
                                    ErrorCode = "CN0003",
                                    Description = $"Zfs '{a.Name}' health problem {a.Health}",
                                    Context = DiagnosticResultContext.Node,
                                    SubContext = "Zfs",
                                    Gravity = DiagnosticResultGravity.Critical,
                                }));

        // ZFS pool usage above storage threshold
        CheckThreshold(_result,
                       settings.Storage.Threshold,
                       "CS0001",
                       DiagnosticResultContext.Storage,
                       "Zfs",
                       zfsList.Select(a => new ThresholdDataPoint(Convert.ToDouble(a.Alloc),
                                                                  Convert.ToDouble(a.Size),
                                                                  $"{id} ({a.Name})",
                                                                  $"Zfs '{a.Name}'")),
                       false,
                       true);
        #endregion
    }
}
