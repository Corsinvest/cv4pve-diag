/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Humanizer.Bytes;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Diagnostic helper
/// </summary>
public class Application
{
    /// <summary>
    /// Analyze
    /// </summary>
    /// <param name="info"></param>
    /// <param name="settings"></param>
    /// <param name="ignoredIssues"></param>
    /// <returns></returns>
    public static ICollection<DiagnosticResult> Analyze(InfoHelper.Info info,
                                                        Settings settings,
                                                        List<DiagnosticResult> ignoredIssues)
    {
        var result = new List<DiagnosticResult>();
        if (info == null) { return result; }

        CheckUnknown(result, info);

        var resources = info.Cluster.Resources.Where(a => !a.IsUnknown);

        CheckStorage(info, result, resources, settings);
        CheckNode(info, result, resources, settings);
        CheckQemu(info, result, resources, settings);
        CheckLxc(info, result, resources, settings);

        //filter with ignore
        foreach (var ignoredIssue in ignoredIssues)
        {
            foreach (var item in result)
            {
                if (ignoredIssue.CheckIgnoreIssue(item)) { item.IsIgnoredIssue = true; }
            }
        }

        return result;
    }

    private static void CheckUnknown(List<DiagnosticResult> result, InfoHelper.Info info)
    {
        result.AddRange(info.Cluster.Resources
                                   .Where(a => a.IsUnknown)
                                   .Select(a => new DiagnosticResult
                                   {
                                       Id = a.Id,
                                       ErrorCode = "CU0001",
                                       Description = $"Unknown resource {a.Type}",
                                       Context = DiagnosticResult.DecodeContext(a.Type),
                                       SubContext = "Status",
                                       Gravity = DiagnosticResultGravity.Critical,
                                   }));
    }

    class NodeStorageContentComparer : IEqualityComparer<NodeStorageContent>
    {
        public bool Equals(NodeStorageContent x, NodeStorageContent y) => x.Volume == y.Volume;

        public int GetHashCode([DisallowNull] NodeStorageContent obj)
        {
            if (obj == null) { return 0; }
            var NameHashCode = obj.Volume == null ? 0 : obj.Volume.GetHashCode();
            return obj.Volume.GetHashCode() ^ NameHashCode;
        }
    }

    private static void CheckStorage(InfoHelper.Info info,
                                     List<DiagnosticResult> result,
                                     IEnumerable<ClusterResource> resources,
                                     Settings settings)
    {
        //storage
        result.AddRange(resources
                        .Where(a => a.ResourceType == ClusterResourceType.Storage && !a.IsAvailable)
                        .Select(a => new DiagnosticResult
                        {
                            Id = a.Id,
                            ErrorCode = "CS0001",
                            Description = "Storage not available",
                            Context = DiagnosticResultContext.Storage,
                            SubContext = "Status",
                            Gravity = DiagnosticResultGravity.Critical,
                        }));

        //storage usage
        CheckThreshold(result,
                       settings.Storage.Threshold,
                       "CS0001",
                       DiagnosticResultContext.Storage,
                       "Usage",
                       resources.Where(a => a.ResourceType == ClusterResourceType.Storage && a.IsAvailable)
                                .Select(a =>
                                (
                                    (double)a.DiskUsage,
                                    (double)a.DiskSize,
                                    a.Storage,
                                    "Storage"
                                )),
                       false,
                       true);

        #region Orphaned Images
        //images in storage
        var storagesImages = new List<NodeStorageContent>();
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Storage
                                                  && a.Content.Split(",").Contains("images")))
        {
            var node = GetNode(info, item.Node);
            var content = node.Storages.Where(a => a.Detail.Storage == item.Storage
                                                   && a.Detail.Content.Split(",").Contains("images"))
                                       .SelectMany(a => a.Content)
                                       .Where(a => a.Content == "images");
            storagesImages.AddRange(content);
        };

        storagesImages = storagesImages.Distinct(new NodeStorageContentComparer()).ToList();

        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
        {
            var node = GetNode(info, item.Node);
            var config = item.VmType switch
            {
                VmType.Qemu => node.Qemu.First(a => a.Detail.VmId == item.VmId).Config,
                VmType.Lxc => (VmConfig)node.Lxc.First(a => a.Detail.VmId == item.VmId).Config,
                _ => null,
            };

            //check disk exists
            foreach (var disk in config.Disks)
            {
                var images = storagesImages.Where(a => a.VmId == item.VmId
                                                       && a.Storage == disk.Storage
                                                       && a.FileName == disk.FileName)
                                           .ToArray();

                foreach (var image in images) { storagesImages.Remove(image); }
            }
        }

        result.AddRange(storagesImages.Select(a => new DiagnosticResult
        {
            Id = a.Storage,
            ErrorCode = "WN0001",
            Description = $"Image Orphaned {ByteSize.FromBytes(a.Size)} file {a.FileName}",
            Context = DiagnosticResultContext.Storage,
            SubContext = "Image",
            Gravity = DiagnosticResultGravity.Warning,
        }));
        #endregion
    }

    private static InfoHelper.Info.NodeInfo GetNode(InfoHelper.Info info, string node)
        => info.Nodes.FirstOrDefault(a => a.Detail.Node == node);

    private static void CheckNode(InfoHelper.Info info,
                                  List<DiagnosticResult> result,
                                  IEnumerable<ClusterResource> resources,
                                  Settings settings)
    {
        var nodes = resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
        var hasCluster = info.Cluster.Config.Nodes.Any();

        //node
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Node))
        {
            var errorId = item.Node;
            var node = GetNode(info, item.Node);

            if (!item.IsOnline)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WN0001",
                    Description = "Node not online",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Status",
                    Gravity = DiagnosticResultGravity.Warning,
                });
                continue;
            }

            #region End Of Life
            var endOfLife = new Dictionary<int, DateOnly>()
            {
                {6 , new DateOnly(2022,07,01)},
                {5 , new DateOnly(2020,07,01)},
                {4 , new DateOnly(2018,06,01)},
            };

            var nodeVersion = int.Parse(node.Version.Version.Split('.')[0]);

            if (endOfLife.TryGetValue(nodeVersion, out var eolDate))
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WN0001",
                    Description = $"Version {node.Version.Version} end of life {eolDate} ",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "EOL",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Subscription
            if (node.Subscription.Status != "Active")
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WN0001",
                    Description = "Node not have subscription active",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Subscription",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region RrdData
            var rrdData = settings.Node.TimeSeries switch
            {
                SettingsTimeSeriesType.Day => node.RrdData.Day,
                SettingsTimeSeriesType.Week => node.RrdData.Week,
                _ => null,
            };

            CheckNodeRrd(result, settings, errorId, rrdData);
            #endregion

            #region Check nodes difference
            var checkInNodes = new bool[] { true, true, true, true };
            foreach (var itemOtherNode in nodes)
            {
                var otherNode = GetNode(info, itemOtherNode.Node);

                //version
                if (checkInNodes[0] && !node.Version.IsEqual(otherNode.Version))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WN0001",
                        Description = "Nodes version not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Version",
                        Gravity = DiagnosticResultGravity.Critical,
                    });
                    checkInNodes[0] = false;
                }

                //hosts files
                if (checkInNodes[1] && string.Join("", node.Hosts) != string.Join("", otherNode.Hosts))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WN0001",
                        Description = "Nodes hosts configuration not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Hosts",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    checkInNodes[1] = false;
                }

                //DNS
                if (checkInNodes[2] && !node.Dns.IsEqual(otherNode.Dns))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WN0001",
                        Description = "Nodes DNS not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "DNS",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    checkInNodes[2] = false;
                }

                //timezone
                if (checkInNodes[3] && node.Timezone != otherNode.Timezone)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WN0001",
                        Description = "Nodes Timezone not equal",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Timezone",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    checkInNodes[3] = false;
                }
            }
            #endregion

            #region Network Card
            result.AddRange(node.Network.Where(a => a.Type == "eth" && !a.Active)
                          .Select(a => new DiagnosticResult
                          {
                              Id = errorId,
                              ErrorCode = "WN0002",
                              Description = $"Network card '{a.Interface}' not active",
                              Context = DiagnosticResultContext.Node,
                              SubContext = "Network",
                              Gravity = DiagnosticResultGravity.Warning,
                          }));
            #endregion

            #region Package Versions
            var addErrPackageVersions = false;
            foreach (var itemOtherNode in nodes)
            {
                if (addErrPackageVersions) { break; }

                var otherNode = GetNode(info, itemOtherNode.Node);

                foreach (var pkg in node.Apt.Version)
                {
                    if (!otherNode.Apt.Version.Any(a => a.Version == pkg.Version
                                                        && a.Title == pkg.Title
                                                        && a.Package == pkg.Package))
                    {
                        addErrPackageVersions = true;
                        result.Add(new DiagnosticResult
                        {
                            Id = errorId,
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
            var serviceExcluded = new List<string>();
            if (!hasCluster) { serviceExcluded.Add("corosync"); }

            //see https://pve.proxmox.com/wiki/Time_Synchronization
            serviceExcluded.Add(nodeVersion >= 7 ? "systemd-timesyncd" : "chrony");

            result.AddRange(node.Services.Where(a => !a.IsRunning && !serviceExcluded.Contains(a.Name))
                                         .Select(a => new DiagnosticResult
                                         {
                                             Id = errorId,
                                             ErrorCode = "WN0002",
                                             Description = $"Service '{a.Description}' not running",
                                             Context = DiagnosticResultContext.Node,
                                             SubContext = "Service",
                                             Gravity = DiagnosticResultGravity.Warning,
                                         }));
            #endregion

            #region Certificates
            result.AddRange(node.Certificates
                                .Where(a => DateTimeOffset.FromUnixTimeSeconds(a.Notafter) < info.Date)
                                .Select(a => new DiagnosticResult
                                {
                                    Id = errorId,
                                    ErrorCode = "WN0002",
                                    Description = $"Certificate '{a.FileName}' expired",
                                    Context = DiagnosticResultContext.Node,
                                    SubContext = "Certificates",
                                    Gravity = DiagnosticResultGravity.Critical,
                                }));
            #endregion

            // //Ceph
            // if (item.Detail.Ceph.Config != null)
            // {
            //     //Cluster
            //     if (item.Detail.Ceph.Status.health.status.Value != "HEALTH_OK")
            //     {
            //         result.Add(new DiagnosticResult
            //         {
            //             Id = errorId,
            //             ErrorCode = "IN0001",
            //             Description = $"Ceph status '{item.Detail.Ceph.Status.health.status.Value}'",
            //             Context = DiagnosticResultContext.Node,
            //             SubContext = "Ceph",
            //             Gravity = item.Detail.Ceph.Status.health.status.Value == "HEALTH_WARN" ?
            //                         DiagnosticResultGravity.Critical :
            //                         DiagnosticResultGravity.Warning
            //         });
            //     }

            //     //PGs active+clean
            //     if (((IEnumerable<dynamic>)item.Detail.Ceph.Status.pgmap.pgs_by_state)
            //             .Where(a => a.state_name == "active+clean").Count() == 0)
            //     {
            //         result.Add(new DiagnosticResult
            //         {
            //             Id = errorId,
            //             ErrorCode = "IN0001",
            //             Description = "Ceph PGs not active+clean",
            //             Context = DiagnosticResultContext.Node,
            //             SubContext = "Ceph",
            //             Gravity = DiagnosticResultGravity.Warning
            //         });
            //     }

            //     //Osd
            //     void OsdRic(IEnumerable<dynamic> children)
            //     {
            //         result.AddRange(children.Where(a => a.type == "osd" && a.status != "up" && a.host == item.node)
            //                         .Select(a => new DiagnosticResult
            //                         {
            //                             Id = errorId,
            //                             ErrorCode = "WN0002",
            //                             Description = $"Osd '{a.name}' not up",
            //                             Context = DiagnosticResultContext.Node,
            //                             SubContext = "Ceph",
            //                             Gravity = DiagnosticResultGravity.Critical,
            //                         }));

            //         foreach (var item in children)
            //         {
            //             if (item.children != null) { OsdRic(item.children); }
            //         }
            //     }
            //     OsdRic(item.Detail.Ceph.Osd.root.children);
            // }

            #region Replication
            var replCount = node.Replication.Count(a => a.ExtensionData != null
                                                        && a.ExtensionData.ContainsKey("errors"));
            if (replCount > 0)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "IN0001",
                    Description = $"{replCount} Replication has errors",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Replication",
                    Gravity = DiagnosticResultGravity.Critical,
                });
            }
            #endregion

            CheckNodeDisk(result, settings, node, errorId);

            #region Update
            var updateCount = node.Apt.Update.Count();
            if (updateCount > 0)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "IN0001",
                    Description = $"{updateCount} Update availble",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Update",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }
            #endregion

            #region Update Important
            var updateImportantCount = node.Apt.Update.Count(a => a.Priority == "important");
            if (updateImportantCount > 0)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "IN0001",
                    Description = $"{updateImportantCount} Update Important availble",
                    Context = DiagnosticResultContext.Node,
                    SubContext = "Update",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            //task history
            CheckTaskHistory(result, node.Tasks, DiagnosticResultContext.Node, errorId);
        }
    }

    private static void CheckNodeDisk(List<DiagnosticResult> result,
                                      Settings settings,
                                      InfoHelper.Info.NodeInfo node,
                                      string id)
    {
        #region Disks
        result.AddRange(node.Disks.List.Where(a => a.Disk.Health != "PASSED" && a.Disk.Health != "OK")
                                       .Select(a => new DiagnosticResult
                                       {
                                           Id = id,
                                           ErrorCode = "CN0003",
                                           Description = $"Disk '{a.Disk.DevPath}' S.M.A.R.T. status problem",
                                           Context = DiagnosticResultContext.Node,
                                           SubContext = "S.M.A.R.T.",
                                           Gravity = DiagnosticResultGravity.Warning,
                                       }));
        #endregion

        #region Sdd Wearout N/A
        result.AddRange(node.Disks.List.Where(a => a.Disk.IsSsd && a.Disk.Wearout == "N/A")
                                       .Select(a => new DiagnosticResult
                                       {
                                           Id = id,
                                           ErrorCode = "CN0003",
                                           Description = $"Disk ssd '{a.Disk.DevPath}' wearout not valid.",
                                           Context = DiagnosticResultContext.Node,
                                           SubContext = "SSD Wearout",
                                           Gravity = DiagnosticResultGravity.Warning,
                                       }));
        #endregion

        #region Sdd Wearout not in range
        CheckThreshold(result,
                       settings.SsdWearoutThreshold,
                       "CN0003",
                       DiagnosticResultContext.Node,
                       "SSD Wearout",
                       node.Disks.List.Where(a => a.Disk.IsSsd && a.Disk.Wearout != "N/A")
                                 .Select(a =>
                                 (
                                     100.0 - a.Disk.WearoutValue,
                                     0d,
                                     id,
                                     $"SSD '{a.Disk.DevPath}'"
                                 )),
                       true,
                       false);
        #endregion

        #region Zfs
        result.AddRange(node.Disks.Zfs.Where(a => a.Zfs.Health != "ONLINE")
                        .Select(a => new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "CN0003",
                            Description = $"Zfs '{a.Zfs.Name}' health problem {a.Zfs.Health}",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "Zfs",
                            Gravity = DiagnosticResultGravity.Critical,
                        }));
        #endregion

        #region Zfs used
        CheckThreshold(result,
                       settings.Storage.Threshold,
                       "CS0001",
                       DiagnosticResultContext.Storage,
                       "Zfs",
                       node.Disks.Zfs.Select(a =>
                       (
                            (double)a.Zfs.Alloc,
                            (double)a.Zfs.Size,
                            $"{id} ({a.Zfs.Name})",
                            $"Zfs '{a.Zfs.Name}'"
                       )),
                       false,
                       true);
        #endregion
    }

    private static void CheckLxc(InfoHelper.Info info,
                                 List<DiagnosticResult> result,
                                 IEnumerable<ClusterResource> resources,
                                 Settings settings)
    {
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                      && a.VmType == VmType.Lxc
                                                      && !a.IsTemplate))
        {
            var node = GetNode(info, item.Node);
            var vm = node.Lxc.FirstOrDefault(a => a.Detail.VmId == item.VmId);
            CheckCommonVm(info, result, settings.Qemu, vm, DiagnosticResultContext.Lxc, node, item.VmId.ToString());
        }
    }

    private static void CheckQemu(InfoHelper.Info info,
                                  List<DiagnosticResult> result,
                                  IEnumerable<ClusterResource> resources,
                                  Settings settings)
    {
        var osNotMaintained = new[] { "win8", "win7", "w2k8", "wxp", "w2k" };

        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                      && a.VmType == VmType.Qemu
                                                      && !a.IsTemplate))
        {
            var node = GetNode(info, item.Node);
            var vm = node.Qemu.FirstOrDefault(a => a.Detail.VmId == item.VmId);
            var errorId = item.VmId.ToString();

            #region Check version OS
            if (vm.Config.OsType == null)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WV0001",
                    Description = "OsType not set!",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "OS",
                    Gravity = DiagnosticResultGravity.Critical,
                });
            }
            else
            {
                if (osNotMaintained.Contains(vm.Config.OsType))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WV0001",
                        Description = $"OS '{vm.Config.OsTypeDecode}' not maintained from vendor!",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "OSNotMaintained",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            #region Agent
            if (!vm.Config.AgentEnabled)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WV0001",
                    Description = "Qemu Agent not enabled",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Agent",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            else
            {
                //agent in quest
                if (item.IsRunning && string.IsNullOrWhiteSpace(vm.Agent.GetHostName?.Result?.HostName))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WV0001",
                        Description = "Qemu Agent in guest not running",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Agent",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            #region Start on boot
            if (!vm.Config.OnBoot)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WV0001",
                    Description = "Start on boot not enabled",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "StartOnBoot",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Protection
            if (!vm.Config.Protection)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WV0001",
                    Description = "For production environment is better VM Protection = enabled",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Protection",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }
            #endregion

            #region Lock
            if (vm.Config.IsLocked)
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WV0001",
                    Description = $"VM is locked by '{vm.Config.Lock}'",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Status",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Check Virtio
            //controller SCSI
            if (vm.Config.ExtensionData.TryGetValue("scsihw", out var scsiHw)
                && !scsiHw.ToString().StartsWith("virtio"))
            {
                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "WV0001",
                    Description = "For more performance switch controller to VirtIO SCSI",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "VirtIO",
                    Gravity = DiagnosticResultGravity.Info,
                });

                //disks
                result.AddRange(vm.Config.Disks.Where(a => !a.Id.StartsWith("virtio"))
                                               .Select(a => new DiagnosticResult
                                               {
                                                   Id = errorId,
                                                   ErrorCode = "WV0001",
                                                   Description = $"For more performance switch '{a.Id}' hdd to VirtIO",
                                                   Context = DiagnosticResultContext.Qemu,
                                                   SubContext = "VirtIO",
                                                   Gravity = DiagnosticResultGravity.Info,
                                               }));
            }

            //network
            for (int i = 0; i < 256; i++)
            {
                var id = $"net{i}";
                if (vm.Config.ExtensionData.TryGetValue(id, out object value))
                {
                    var data = value + "";
                    if (data != null && !data.StartsWith("virtio"))
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = errorId,
                            ErrorCode = "WV0001",
                            Description = $"For more performance switch '{id}' network to VirtIO",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "VirtIO",
                            Gravity = DiagnosticResultGravity.Info,
                        });
                    }
                }
            }
            #endregion

            #region Unused disk
            foreach (var unused in vm.Config.ExtensionData.Keys.Where(a => a.StartsWith("unused")))
            {
                var volume = vm.Config.ExtensionData[unused].ToString();
                var data = volume.Split(":");
                var storage = node.Storages.FirstOrDefault(a => a.Detail.Storage == data[0]);
                var size = storage != null
                            ? ByteSize.FromBytes(storage.Content.FirstOrDefault(a => a.Volume == volume).Size).ToString()
                            : "";

                result.Add(new DiagnosticResult
                {
                    Id = errorId,
                    ErrorCode = "IV0001",
                    Description = $"disk '{unused}' {size} ",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Hardware",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Cdrom
            foreach (string value in vm.Config.ExtensionData.Values)
            {
                if (value.Contains("media=cdrom") && value != "none,media=cdrom")
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = errorId,
                        ErrorCode = "WV0002",
                        Description = "Cdrom mounted",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Hardware",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            CheckCommonVm(info, result, settings.Qemu, vm, DiagnosticResultContext.Qemu, node, errorId);
        }
    }

    private static void CheckCommonVm<TDetail, TConfig>(InfoHelper.Info info,
                                                        List<DiagnosticResult> result,
                                                        SettingsThresholdHost thresholdHost,
                                                        InfoHelper.Info.NodeInfo.VmBaseInfo<TDetail, TConfig> vm,
                                                        DiagnosticResultContext context,
                                                        InfoHelper.Info.NodeInfo node,
                                                        string id)
        where TDetail : NodeVmBase
        where TConfig : VmConfig
    {
        var vmid = vm.Detail.VmId;

        #region Vm State
        result.AddRange(vm.Pending.Where(a => a.Key == "vmstate")
                            .Select(a => new DiagnosticResult
                            {
                                Id = id,
                                ErrorCode = "WV0001",
                                Description = $"Found vmstate '{a.Value}'",
                                Context = DiagnosticResultContext.Qemu,
                                SubContext = "VM State",
                                Gravity = DiagnosticResultGravity.Critical,
                            }));
        #endregion

        #region Backup
        //configured backup get vmdId
        var found = info.Cluster.Backups.Any(a => a.Enabled && a.All);
        if (!found)
        {
            //in all backup
            found = info.Cluster.Backups.Where(a => a.Enabled)
                                       .SelectMany(a => a.VmId.Split(','))
                                       .Any(a => Convert.ToInt64(a) == vmid);

            if (!found)
            {
                //in pool
                foreach (var item in info.Cluster.Backups
                                                 .Where(a => a.Enabled && !string.IsNullOrWhiteSpace(a.Pool))
                                                 .Select(a => a.Pool))
                {
                    found = info.Pools.Where(a => a.Id == item)
                                             .SelectMany(a => a.Detail.Members)
                                             .Any(a => a.ResourceType == ClusterResourceType.Vm && a.VmId == vmid);

                    if (found) { break; }
                }
            }
        }

        if (!found)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "CC0001",
                Description = "vzdump backup not configured",
                Context = context,
                SubContext = "Backup",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        //check disk no backup
        result.AddRange(vm.Config.Disks.Where(a => !a.Backup)
                            .Select(a => new DiagnosticResult
                            {
                                Id = id,
                                ErrorCode = "WV0001",
                                Description = $"Disk '{a.Id}' disabled for backup",
                                Context = DiagnosticResultContext.Qemu,
                                SubContext = "Backup",
                                Gravity = DiagnosticResultGravity.Critical,
                            }));


        //check exists backup and recent
        var dayOld = 60;
        var foundOldBackup = node.Storages.Where(a => a.Detail.Content.Split(",").Contains("backup"))
                                          .SelectMany(a => a.Content)
                                          .Where(a => a.VmId == vmid
                                                      && a.Content == "backup"
                                                      && a.CreationDate.Date <= info.Date.Date.AddDays(-dayOld));
        if (foundOldBackup.Any())
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "CC0001",
                Description = $"{foundOldBackup.Count()} backup" +
                              $" {ByteSize.FromBytes(foundOldBackup.Sum(a => a.Size))} more {dayOld} days are found!",
                Context = context,
                SubContext = "Backup",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        //check exists backup and recent
        var foundBackup = node.Storages.Where(a => a.Detail.Content.Split(",").Contains("backup"))
                                       .SelectMany(a => a.Content)
                                       .Any(a => a.VmId == vmid
                                                 && a.Content == "backup"
                                                 && a.CreationDate.Date <= info.Date.Date.AddDays(1));
        if (!foundBackup)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "CC0001",
                Description = "No recent backups found!",
                Context = context,
                SubContext = "Backup",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
        #endregion

        CheckTaskHistory(result,
                         node.Tasks.Where(a => a.VmId == vmid.ToString()),
                         context,
                         id);

        CheckSnapshots(result, vm.Snapshots, info.Date, id, context);

        var rrdData = thresholdHost.TimeSeries switch
        {
            SettingsTimeSeriesType.Day => vm.RrdData.Day,
            SettingsTimeSeriesType.Week => vm.RrdData.Week,
            _ => null,
        };

        CheckThresholdHost(result,
                           thresholdHost,
                           context,
                           id,
                           rrdData.Select(a => ((IMemory)a, (INetIO)a, (ICpu)a)));
    }

    private static void CheckTaskHistory(List<DiagnosticResult> result,
                                         IEnumerable<NodeTask> tasks,
                                         DiagnosticResultContext context,
                                         string id)
    {
        var tasksCount = tasks.Count(a => !a.StatusOk);
        if (tasksCount > 0)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "IN0001",
                Description = $"{tasksCount} Task history has errors",
                Context = context,
                SubContext = "Tasks",
                Gravity = DiagnosticResultGravity.Critical,
            });
        }
    }

    private static void CheckThresholdHost(List<DiagnosticResult> result,
                                           SettingsThresholdHost thresholdHost,
                                           DiagnosticResultContext context,
                                           string id,
                                           IEnumerable<(IMemory Memory, INetIO NetIO, ICpu Cpu)> rrdData)
    {
        CheckThreshold(result,
                       thresholdHost.Cpu,
                       "WV0002",
                       context,
                       "Usage",
                       new[] { (rrdData.Average(a => a.Cpu.CpuUsagePercentage) * 100,
                                0d,
                                id,
                                $"CPU (rrd {thresholdHost.TimeSeries} AVERAGE)") },
                       true,
                       false);

        CheckThreshold(result,
                       thresholdHost.Memory,
                       "WV0002",
                       context,
                       "Usage",
                       new[] { (rrdData.Average(a => a.Memory.MemoryUsage),
                                rrdData.Average(a => a.Memory.MemorySize),
                                id,
                                $"Memory (rrd {thresholdHost.TimeSeries} AVERAGE)") },
                       false,
                       true);

        CheckThreshold(result,
                       thresholdHost.Network,
                        "WV0002",
                        context,
                        "Usage",
                        new[] { (rrdData.Average(a => a.NetIO.NetIn),
                                 0d,
                                 id,
                                 $"NetIn (rrd {thresholdHost.TimeSeries} AVERAGE)") },
                        true,
                        false);

        CheckThreshold(result,
                       thresholdHost.Network,
                       "WV0002",
                       context,
                       "Usage",
                       new[] { (rrdData.Average(a => a.NetIO.NetOut),
                                0d,
                                id,
                                $"NetIn (rrd {thresholdHost.TimeSeries} AVERAGE)") },
                       true,
                       false);
    }

    private static void CheckNodeRrd(List<DiagnosticResult> result,
                                     Settings settings,
                                     string id,
                                     IEnumerable<NodeRrdData> rrdData)
    {
        CheckThresholdHost(result,
                           settings.Node,
                           DiagnosticResultContext.Node,
                           id,
                           rrdData.Select(a => ((IMemory)a, (INetIO)a, (ICpu)a)));

        CheckThreshold(result,
                       settings.Node.Cpu,
                       "WV0002",
                       DiagnosticResultContext.Node,
                       "Usage",
                       new[] { (rrdData.Average(a => a.IoWait) * 100,
                                0d,
                                id,
                                $"IOWait (rrd {settings.Node.TimeSeries} AVERAGE)") },
                       true,
                       false);

        CheckThreshold(result,
                       settings.Storage.Threshold,
                       "WV0002",
                       DiagnosticResultContext.Node,
                       "Usage",
                       new[] { (rrdData.Average(a => a.RootUsage),
                                rrdData.Average(a => a.RootSize),
                                id,
                                $"Root space (rrd {settings.Node.TimeSeries} AVERAGE)") },
                       false,
                       true);

        CheckThreshold(result,
                       settings.Storage.Threshold,
                       "WV0002",
                       DiagnosticResultContext.Node,
                       "Usage",
                       new[] { (rrdData.Average(a => a.SwapUsage),
                                rrdData.Average(a =>  a.SwapSize) ,
                                id,
                                $"SWAP (rrd {settings.Node.TimeSeries} AVERAGE)") },
                       false,
                       true);
    }

    private static void CheckThreshold(List<DiagnosticResult> result,
                                       SettingsThreshold<double> threshold,
                                       string errorCode,
                                       DiagnosticResultContext context,
                                       string subContext,
                                       IEnumerable<(double Usage, double Size, string Id, string PrefixDescription)> data,
                                       bool isValue,
                                       bool formatByte)
    {
        if (threshold.Warning == 0 || threshold.Critical == 0) { return; }

        var ranges = new[] { threshold.Warning, threshold.Critical, threshold.Critical * 100 };
        var gravity = new[] { DiagnosticResultGravity.Warning, DiagnosticResultGravity.Critical };

        for (int i = 0; i < 3; i++)
        {
            double GetValue(double usage, double size) => Math.Round(isValue ? usage : usage / size * 100.0, 1);

            string MakeDescription(string PrefixDescription, double usage, double size)
            {
                var txt = $"{PrefixDescription} usage {GetValue(usage, size)}%";
                if (formatByte) { txt += $" - {ByteSize.FromBytes(usage)} of {ByteSize.FromBytes(size)}"; }
                return txt;
            }

            result.AddRange(data.Where(a => GetValue(a.Usage, a.Size) >= ranges[i]
                                            && GetValue(a.Usage, a.Size) <= ranges[i + 1])
                                .Select(a => new DiagnosticResult
                                {
                                    Id = a.Id,
                                    ErrorCode = errorCode,
                                    Description = MakeDescription(a.PrefixDescription, a.Usage, a.Size),
                                    Context = context,
                                    SubContext = subContext,
                                    Gravity = gravity[i],
                                }));
        }
    }

    // private static void CheckThreshold(List<DiagnosticResult> result,
    //                                     SettingsThreshold<double> threshold,
    //                                     string errorCode,
    //                                     DiagnosticResultContext context,
    //                                     string subContext,
    //                                     IEnumerable<(double Usage, string Id, string PrefixDescription)> data)
    // {
    //     if (threshold.Warning == 0 || threshold.Critical == 0) { return; }

    //     var ranges = new[] { threshold.Warning, threshold.Critical, threshold.Critical * 100 };
    //     var gravity = new[] { DiagnosticResultGravity.Warning, DiagnosticResultGravity.Critical };

    //     for (int i = 0; i < 3; i++)
    //     {
    //         result.AddRange(data.Where(a => a.Usage >= ranges[i] && a.Usage <= ranges[i + 1])
    //                             .Select(a => new DiagnosticResult
    //                             {
    //                                 Id = a.Id,
    //                                 ErrorCode = errorCode,
    //                                 Description = $"{a.PrefixDescription} usage {Math.Round(a.Usage, 1)}%",
    //                                 Context = context,
    //                                 SubContext = subContext,
    //                                 Gravity = gravity[i],
    //                             }));
    //     }
    // }

    private static void CheckSnapshots(List<DiagnosticResult> result,
                                       IEnumerable<VmSnapshot> snapshots,
                                       DateTime execution,
                                       string id,
                                       DiagnosticResultContext context)
    {
        //autosnap
        if (!snapshots.Any(a => a.Description == "cv4pve-autosnap"))
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WV0003",
                Description = "cv4pve-autosnap not configured",
                Context = context,
                SubContext = "AutoSnapshot",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        //old autosnap
        if (snapshots.Any(a => a.Description == "eve4pve-autosnap"))
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WV0003",
                Description = $"Old AutoSnap 'eve4pve-autosnap' are present. Update new version",
                Context = context,
                SubContext = "AutoSnapshot",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }

        //old month
        var snapOldCount = snapshots.Count(a => a.Name != "current" && a.Date < execution.AddMonths(-1));
        if (snapOldCount > 0)
        {
            result.Add(new DiagnosticResult
            {
                Id = id,
                ErrorCode = "WV0003",
                Description = $"{snapOldCount} snapshots older than 1 month",
                Context = context,
                SubContext = "SnapshotOld",
                Gravity = DiagnosticResultGravity.Warning,
            });
        }
    }
}
