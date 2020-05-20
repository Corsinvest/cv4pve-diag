/*
 * This file is part of the cv4pve-diag https://github.com/Corsinvest/cv4pve-diag,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */

using Corsinvest.ProxmoxVE.Api.Extension.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api
{
    /// <summary>
    /// Diagnostic helper
    /// </summary>
    public class Application
    {
        /// <summary>
        /// Analyze
        /// </summary>
        /// <param name="clusterInfo"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static ICollection<DiagnosticResult> Analyze(ClusterInfo clusterInfo, Settings settings)
        {
            if (clusterInfo == null) { return null; }
            var result = new List<DiagnosticResult>();

            CheckUnknown(result, clusterInfo);

            var validResource = clusterInfo.Resources.Where(a => a.status != "unknown");

            CheckStorage(result, validResource, settings);
            CheckNode(clusterInfo, result, validResource, settings);
            CheckQemu(clusterInfo, result, validResource, settings);
            CheckLxc(clusterInfo, result, validResource, settings);

            return result;
        }

        private static void CheckUnknown(List<DiagnosticResult> result, ClusterInfo clusterInfo)
        {
            result.AddRange(clusterInfo.Resources
                            .Where(a => a.status == "unknown")
                            .Select(a => new DiagnosticResult
                            {
                                Id = a.id,
                                ErrorCode = "CU0001",
                                Description = $"Unknown resource {a.type}",
                                Context = DiagnosticResult.DecodeContext(a.type.ToString()),
                                SubContext = "Status",
                                Gravity = DiagnosticResultGravity.Critical,
                            }));
        }

        private static List<(string Id, string Image)> GetVmImages(dynamic vm)
        {
            var ret = new List<(string Id, string image)>();

            for (int i = 0; i < 256; i++)
            {
                foreach (var item in new[] { "ide", "sata", "scsi", "virtio" })
                {
                    var id = $"{item}{i}";
                    var data = vm.Detail.Config[id];
                    if (data != null && !data.Value.Contains("media=cdrom"))
                    {
                        ret.Add((id, data.Value));
                    }
                }
            }

            return ret;
        }

        private static void CheckStorage(List<DiagnosticResult> result,
                                         IEnumerable<dynamic> validResource,
                                         Settings settings)
        {
            //storage
            result.AddRange(validResource
                            .Where(a => a.type == "storage" && a.status != "available")
                            .Select(a => new DiagnosticResult
                            {
                                Id = $"{a.storage} ({a.node})",
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
                           validResource.Where(a => a.type == "storage" && a.status == "available")
                                        .Select(a =>
                                        (
                                            ((double)a.Detail.Status.used / (double)a.Detail.Status.total) * 100.0,
                                            $"{a.storage} ({a.node})",
                                            "Storage"
                                        )));

            #region Orphaned Images
            //images in storage
            var storagesImages = validResource.Where(a => a.type == "storage")
                                                .SelectMany(a => (IEnumerable<dynamic>)a.Detail.Content)
                                                .Where(a => a.content.Value.Contains("images"))
                                                .Select(a => a.volid.Value)
                                                .Distinct()
                                                .ToList();

            foreach (var item in validResource.Where(a => a.type == "qemu")
                                                .SelectMany(a => (List<(string Id, string Image)>)GetVmImages(a)))
            {
                var data = item.Image.Split(',');
                if (storagesImages.Contains(data[0])) { storagesImages.Remove(data[0]); }
            }

            foreach (var item in validResource.Where(a => a.type == "lxc")
                                                .Select(a => a.Detail.Config.rootfs.Value))
            {
                var data = item.Split(",");
                if (storagesImages.Contains(data[0])) { storagesImages.Remove(data[0]); }
            }

            result.AddRange(storagesImages.Select(a => new DiagnosticResult
            {
                Id = a,
                ErrorCode = "WN0001",
                Description = "Image Orphaned",
                Context = DiagnosticResultContext.Storage,
                SubContext = "Image",
                Gravity = DiagnosticResultGravity.Warning,
            }));
            #endregion
        }

        private static void CheckNode(ClusterInfo clusterInfo,
                                      List<DiagnosticResult> result,
                                      IEnumerable<dynamic> validResource,
                                      Settings settings)
        {
            var nodes = validResource.Where(a => a.type == "node" && a.status == "online");

            //node
            foreach (var node in validResource.Where(a => a.type == "node"))
            {
                if (node.status != "online")
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = node.node,
                        ErrorCode = "WN0001",
                        Description = "Node not online",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Status",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                    continue;
                }

                //rdd
                CheckNodeRrd(result, settings, node.node.Value, node.Detail.RrdData);

                var checkInNodes = new bool[] { true, true, true, true };
                foreach (var otherNode in nodes)
                {
                    //version
                    if (checkInNodes[0] &&
                        $"{node.release}{node.version}{node.repoid}" !=
                        $"{otherNode.release}{otherNode.version}{otherNode.repoid}")
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = node.node,
                            ErrorCode = "WN0001",
                            Description = "Nodes version not equal",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "Version",
                            Gravity = DiagnosticResultGravity.Critical,
                        });
                        checkInNodes[0] = false;
                    }

                    //hosts files
                    if (checkInNodes[1] && node.Detail.Hosts != otherNode.Detail.Hosts)
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = node.node,
                            ErrorCode = "WN0001",
                            Description = "Nodes hosts configuration not equal",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "Hosts",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                        checkInNodes[1] = false;
                    }

                    //DNS
                    if (checkInNodes[2] &&
                        (node.Detail.Dns.search != otherNode.Detail.Dns.search ||
                        node.Detail.Dns.dns1 != otherNode.Detail.Dns.dns1 ||
                        node.Detail.Dns.dns2 != otherNode.Detail.Dns.dns2))
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = node.node,
                            ErrorCode = "WN0001",
                            Description = "Nodes DNS not equal",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "DNS",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                        checkInNodes[2] = false;
                    }

                    //timezone
                    if (checkInNodes[3] && node.Detail.Timezone != otherNode.Detail.Timezone)
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = node.node,
                            ErrorCode = "WN0001",
                            Description = "Nodes Timezone not equal",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "Timezone",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                        checkInNodes[3] = false;
                    }
                }

                //network card
                result.AddRange(((IEnumerable<dynamic>)node.Detail.Network)
                              .Where(a => a.type == "eth" && a.active != 1)
                              .Select(a => new DiagnosticResult
                              {
                                  Id = node.node,
                                  ErrorCode = "WN0002",
                                  Description = $"Network card '{a.iface}' not active",
                                  Context = DiagnosticResultContext.Node,
                                  SubContext = "Network",
                                  Gravity = DiagnosticResultGravity.Warning,
                              }));

                //Package Versions
                var addErrPackageVersions = false;
                foreach (var otherNode in nodes)
                {
                    if (addErrPackageVersions) { break; }
                    foreach (var pkg in node.Detail.PackageVersions)
                    {
                        if (!((IEnumerable<dynamic>)node.Detail.PackageVersions)
                            .Any(a => a.Version == pkg.Version &&
                                    a.Title == pkg.Title &&
                                    a.Package == pkg.Package))
                        {
                            addErrPackageVersions = true;
                            result.Add(new DiagnosticResult
                            {
                                Id = node.node,
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

                //Services
                result.AddRange(((IEnumerable<dynamic>)node.Detail.Services)
                                .Where(a => a.state != "running")
                                .Select(a => new DiagnosticResult
                                {
                                    Id = node.node,
                                    ErrorCode = "WN0002",
                                    Description = $"Service '{a.desc}' not running",
                                    Context = DiagnosticResultContext.Node,
                                    SubContext = "Service",
                                    Gravity = DiagnosticResultGravity.Warning,
                                }));

                //certificates                
                result.AddRange(((IEnumerable<dynamic>)node.Detail.Certificates)
                                .Where(a => DateTimeUnixHelper.UnixTimeToDateTime((long)a.notafter) < clusterInfo.Date)
                                .Select(a => new DiagnosticResult
                                {
                                    Id = node.node,
                                    ErrorCode = "WN0002",
                                    Description = $"Certificate '{a.filename}' expired",
                                    Context = DiagnosticResultContext.Node,
                                    SubContext = "Certificates",
                                    Gravity = DiagnosticResultGravity.Critical,
                                }));

                //Ceph
                if (node.Detail.Ceph.Config != null)
                {
                    //Cluster
                    if (node.Detail.Ceph.Status.health.status.Value != "HEALTH_OK")
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = node.node,
                            ErrorCode = "IN0001",
                            Description = $"Ceph status '{node.Detail.Ceph.Status.health.status.Value}'",
                            Context = DiagnosticResultContext.Node,
                            SubContext = "Ceph",
                            Gravity = node.Detail.Ceph.Status.health.status.Value == "HEALTH_WARN" ?
                                        DiagnosticResultGravity.Critical :
                                        DiagnosticResultGravity.Warning
                        });
                    }

                    //Osd
                    void OsdRic(IEnumerable<dynamic> children)
                    {
                        result.AddRange(children.Where(a => a.type == "osd" && a.status != "up" && a.host == node.node)
                                        .Select(a => new DiagnosticResult
                                        {
                                            Id = node.node,
                                            ErrorCode = "WN0002",
                                            Description = $"Osd '{a.name}' not up",
                                            Context = DiagnosticResultContext.Node,
                                            SubContext = "Ceph",
                                            Gravity = DiagnosticResultGravity.Critical,
                                        }));

                        foreach (var item in children)
                        {
                            if (item.children != null) { OsdRic(item.children); }
                        }
                    }
                    OsdRic(node.Detail.Ceph.Osd.root.children);
                }

                CheckNodeDisk(result, settings, node);

                //update
                var updateCount = ((IEnumerable<dynamic>)node.Detail.AptUpdate).Count();
                if (updateCount > 0)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = node.node,
                        ErrorCode = "IN0001",
                        Description = $"{updateCount} Update availble",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Update",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }

                //update important
                var updateImportantCount = ((IEnumerable<dynamic>)node.Detail.AptUpdate)
                                            .Where(a => a.Priority == "important").Count();
                if (updateImportantCount > 0)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = node.node,
                        ErrorCode = "IN0001",
                        Description = $"{updateImportantCount} Update Important availble",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Update",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }

                //task history
                CheckTaskHistory(result,
                                 (IEnumerable<dynamic>)node.Detail.Tasks,
                                 DiagnosticResultContext.Node,
                                 node.node.Value);

                //replication
                var replCount = ((IEnumerable<dynamic>)node.Detail.Replication)
                                    .Where(a => a.error != null)
                                    .Count();
                if (replCount > 0)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = node.node,
                        ErrorCode = "IN0001",
                        Description = $"{replCount} Replication has errors",
                        Context = DiagnosticResultContext.Node,
                        SubContext = "Replication",
                        Gravity = DiagnosticResultGravity.Critical,
                    });
                }
            }
        }

        private static void CheckTaskHistory(List<DiagnosticResult> result,
                                             IEnumerable<dynamic> tasks,
                                             DiagnosticResultContext context,
                                             string id)
        {
            //task history
            var tasksCount = tasks.Count();
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

        private static void CheckNodeDisk(List<DiagnosticResult> result,
                                          Settings settings,
                                          dynamic node)
        {
            //disks
            result.AddRange(((IEnumerable<dynamic>)node.Detail.Disks.List)
                            .Where(a => a.health != "PASSED" && a.health != "OK")
                            .Select(a => new DiagnosticResult
                            {
                                Id = node.node,
                                ErrorCode = "CN0003",
                                Description = $"Disk '{a.devpath}' S.M.A.R.T. status problem",
                                Context = DiagnosticResultContext.Node,
                                SubContext = "S.M.A.R.T.",
                                Gravity = DiagnosticResultGravity.Warning,
                            }));

            //sdd Wearout N/A
            result.AddRange(((IEnumerable<dynamic>)node.Detail.Disks.List)
                            .Where(a => a.type == "ssd" && a.wearout + "" == "N/A")
                            .Select(a => new DiagnosticResult
                            {
                                Id = node.node,
                                ErrorCode = "CN0003",
                                Description = $"Disk ssd '{a.devpath}' wearout not valid",
                                Context = DiagnosticResultContext.Node,
                                SubContext = "SSD Wearout",
                                Gravity = DiagnosticResultGravity.Warning,
                            }));

            //sdd Wearout not in range
            CheckThreshold(result,
                           settings.SsdWearoutThreshold,
                           "CN0003",
                           DiagnosticResultContext.Node,
                           "SSD Wearout",
                            ((IEnumerable<dynamic>)node.Detail.Disks.List)
                            .Where(a => a.type == "ssd" && a.wearout + "" != "N/A")
                            .Select(a =>
                            (
                                100.0 - (double)a.wearout,
                                $"{node.node} ({a.devpath})",
                                $"SSD '{a.devpath}'"
                            )));

            //zfs
            result.AddRange(((IEnumerable<dynamic>)node.Detail.Disks.Zfs)
                            .Where(a => a.health != "ONLINE")
                            .Select(a => new DiagnosticResult
                            {
                                Id = node.node,
                                ErrorCode = "CN0003",
                                Description = $"Zfs '{a.name}' health problem",
                                Context = DiagnosticResultContext.Node,
                                SubContext = "Zfs",
                                Gravity = DiagnosticResultGravity.Critical,
                            }));

            //zfs used
            CheckThreshold(result,
                           settings.Storage.Threshold,
                           "CS0001",
                           DiagnosticResultContext.Storage,
                           "Zfs",
                           ((IEnumerable<dynamic>)node.Detail.Disks.Zfs)
                            .Select(a =>
                            (
                                ((double)a.alloc / (double)a.size) * 100.0,
                                $"{node.node} ({a.name})",
                                $"Zfs '{a.name}'"
                            )));
        }

        private static void CheckLxc(ClusterInfo clusterInfo,
                                     List<DiagnosticResult> result,
                                     IEnumerable<dynamic> validResource,
                                     Settings settings)
        {
            foreach (var vm in validResource.Where(a => a.type == "lxc" && a.template != 1))
            {
                CheckCommonVm(clusterInfo, result, settings, vm, DiagnosticResultContext.Lxc);
            }
        }

        private static void CheckQemu(ClusterInfo clusterInfo,
                                      List<DiagnosticResult> result,
                                      IEnumerable<dynamic> validResource,
                                      Settings settings)
        {
            var osNotMaintained = new Dictionary<string, string>()
            {
                { "win8" ,"8.x/2012/2012r2"},
                { "win7" ,"7/2008r2"},
                { "w2k8" ,"Vista/2008"},
                { "wxp" ,"XP/2003"},
                { "w2k","2000" }
            };

            foreach (var vm in validResource.Where(a => a.type == "qemu" && a.template != 1))
            {
                //check version OS
                if (osNotMaintained.TryGetValue(vm.Detail.Config.ostype.Value as string, out var osTypeDesc))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = vm.id,
                        ErrorCode = "WV0001",
                        Description = $"OS '{osTypeDesc}' not maintained from vendor!",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Agent",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }

                //agent
                if (int.Parse(vm.Detail.Config.id ?? "0") == 0)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = vm.id,
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
                    if (vm.status == "running" && !vm.AgentGuestRunning)
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = vm.id,
                            ErrorCode = "WV0001",
                            Description = "Qemu Agent in guest not running",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Agent",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }

                //start on boot
                if ((vm.Detail.Config.onboot ?? 0) == 0)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = vm.id,
                        ErrorCode = "WV0001",
                        Description = "Start on boot not enabled",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "StartOnBoot",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }

                //protection
                if ((vm.Detail.Config.protection ?? 0) == 0)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = vm.id,
                        ErrorCode = "WV0001",
                        Description = "For production environment is better VM Protection = enabled",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Protection",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }

                //lock
                if (vm.Detail.Config["lock"] != null)
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = vm.id,
                        ErrorCode = "WV0001",
                        Description = $"VM is locked by '{vm.Detail.Config["lock"]}'",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Status",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }

                #region check virtio
                //controller SCSI
                if (vm.Detail.Config.scsihw != null &&
                    !(vm.Detail.Config.scsihw.Value as string).StartsWith("virtio"))
                {
                    result.Add(new DiagnosticResult
                    {
                        Id = vm.id,
                        ErrorCode = "WV0001",
                        Description = "For more performance switch controller to VirtIO SCSI",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "VirtIO",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }

                //network
                for (int i = 0; i < 256; i++)
                {
                    var id = $"net{i}";
                    var data = vm.Detail.Config[id];
                    if (data != null && !data.Value.StartsWith("virtio"))
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = vm.id,
                            ErrorCode = "WV0001",
                            Description = $"For more performance switch '{id}' network to VirtIO",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "VirtIO",
                            Gravity = DiagnosticResultGravity.Info,
                        });
                    }
                }

                //disks
                result.AddRange(((List<(string Id, string Image)>)GetVmImages(vm))
                                    .Where(a => !a.Image.StartsWith("virtio") && !a.Id.StartsWith("virtio"))
                                    .Select(a => new DiagnosticResult
                                    {
                                        Id = vm.id,
                                        ErrorCode = "WV0001",
                                        Description = $"For more performance switch '{a.Id}' hdd to VirtIO",
                                        Context = DiagnosticResultContext.Qemu,
                                        SubContext = "VirtIO",
                                        Gravity = DiagnosticResultGravity.Info,
                                    }));
                #endregion

                //unused
                for (int i = 0; i < 256; i++)
                {
                    if (vm.Detail.Config[$"unused{i}"] != null)
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = vm.id,
                            ErrorCode = "IV0001",
                            Description = $"Unused disk{i}",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Hardware",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }

                //cdrom
                foreach (string value in vm.Detail.Config)
                {
                    if (value.Contains("media=cdrom") && value != "none,media=cdrom")
                    {
                        result.Add(new DiagnosticResult
                        {
                            Id = vm.id,
                            ErrorCode = "WV0002",
                            Description = "Cdrom mounted",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Hardware",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }

                CheckCommonVm(clusterInfo, result, settings, vm, DiagnosticResultContext.Qemu);
            }
        }

        private static void CheckCommonVm(ClusterInfo clusterInfo,
                                          List<DiagnosticResult> result,
                                          Settings settings,
                                          dynamic vm,
                                          DiagnosticResultContext context)
        {
            #region Backup
            //configured backup get vmdId
            var vmsIdBackup = string.Join(",", clusterInfo.Backups
                                                          .Where(a => a.enabled == 1)
                                                          .Select(a => a.vmid))
                                                          .Split(',');

            string vmId = vm.vmid.Value + "";
            if (!vmsIdBackup.Contains(vmId))
            {
                result.Add(new DiagnosticResult
                {
                    Id = vmId,
                    ErrorCode = "CC0001",
                    Description = "vzdump backup not configured",
                    Context = context,
                    SubContext = "Backup",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }

            //check exists backup and recent
            var regex = new Regex(@"^.*:(.*\/)?");
            var foundBackup = false;
            foreach (var backup in ((IEnumerable<dynamic>)vm.Detail.Backups))
            {
                var data = backup.volid.Value.Replace(regex.Match(backup.volid.Value).Value, "").Split('-');
                var date = DateTime.ParseExact(data[3] + "_" + data[4].Substring(0, data[4].IndexOf(".")),
                                                "yyyy_MM_dd_HH_mm_ss",
                                                null);

                if (clusterInfo.Date.Date >= date.Date.AddDays(+1))
                {
                    foundBackup = true;
                    break;
                }
            }

            if (!foundBackup)
            {
                result.Add(new DiagnosticResult
                {
                    Id = vmId,
                    ErrorCode = "CC0001",
                    Description = "No recent backups found!",
                    Context = context,
                    SubContext = "Backup",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            CheckTaskHistory(result,
                             (IEnumerable<dynamic>)vm.Detail.Tasks,
                             context,
                             vmId);

            CheckSnapshots(result,
                           (IEnumerable<dynamic>)vm.Detail.Snapshots,
                           clusterInfo.Date,
                           vmId,
                           context);

            CheckVmRrd(result,
                       settings.Lxc,
                       context,
                       vmId,
                       vm.Detail.RrdData);
        }

        private static void CheckVmRrd(List<DiagnosticResult> result,
                                       SettingsThresholdHost thresholdHost,
                                       DiagnosticResultContext context,
                                       string id,
                                       dynamic rrdData)
        {
            var data = ((IEnumerable<dynamic>)GetTimeSeries(thresholdHost.TimeSeries, rrdData))
                        .Where(a => a.cpu != null);
            if (data.Count() == 0) { return; }

            CheckThresholdHost(result, thresholdHost, context, id, data);
        }

        private static void CheckThresholdHost(List<DiagnosticResult> result,
                                               SettingsThresholdHost thresholdHost,
                                               DiagnosticResultContext context,
                                               string id,
                                               IEnumerable<dynamic> rrdData)
        {
            var count = rrdData.Count();

            CheckThreshold(result,
                           thresholdHost.Cpu,
                           "WV0002",
                           context,
                           "Usage",
                           new[] { ( rrdData.Sum(a => (double)a.cpu) / count * 100.0,
                                    id,
                                    $"CPU (rrd {thresholdHost.TimeSeries} AVERAGE)") });

            var memUsed = 0.0d;
            switch (context)
            {
                case DiagnosticResultContext.Node:
                    memUsed = rrdData.Sum(a => (double)a.memused / (double)a.memtotal);
                    break;

                case DiagnosticResultContext.Lxc:
                case DiagnosticResultContext.Qemu:
                    memUsed = rrdData.Sum(a => (double)a.mem / (double)a.maxmem);
                    break;

                default: break;
            }

            CheckThreshold(result,
                           thresholdHost.Memory,
                           "WV0002",
                           context,
                           "Usage",
                           new[] { (memUsed / count * 100,
                                    id,
                                    $"Memory (rrd {thresholdHost.TimeSeries} AVERAGE)") });

            CheckThreshold(result,
                           thresholdHost.Network,
                            "WV0002",
                            context,
                            "Usage",
                            new[] { (rrdData.Sum(a => (double)a.netin) / count,
                                     id,
                                     $"NetIn (rrd {thresholdHost.TimeSeries} AVERAGE)") });

            CheckThreshold(result,
                           thresholdHost.Network,
                           "WV0002",
                           context,
                           "Usage",
                           new[] { (rrdData.Sum(a => (double)a.netout) / count,
                                    id,
                                    $"NetIn (rrd {thresholdHost.TimeSeries} AVERAGE)") });
        }

        private static void CheckNodeRrd(List<DiagnosticResult> result,
                                         Settings settings,
                                         string id,
                                         dynamic rrdData)
        {
            var data = ((IEnumerable<dynamic>)GetTimeSeries(settings.Node.TimeSeries, rrdData))
                        .Where(a => a.cpu != null);
            if (data.Count() == 0) { return; }

            CheckThresholdHost(result, settings.Node, DiagnosticResultContext.Node, id, data);

            //var loadavg = data.Sum(a => (double)a.loadavg) / data.Count();

            CheckThreshold(result,
                           settings.Node.Cpu,
                           "WV0002",
                           DiagnosticResultContext.Node,
                           "Usage",
                           new[] { (data.Sum(a => (double)a.iowait) / data.Count() * 100,
                                    id,
                                    $"IOWait (rrd {settings.Node.TimeSeries} AVERAGE)") });

            CheckThreshold(result,
                           settings.Storage.Threshold,
                           "WV0002",
                           DiagnosticResultContext.Node,
                           "Usage",
                           new[] { (data.Sum(a => (double)a.rootused / (double)a.roottotal) / data.Count() * 100,
                                    id,
                                    $"Root space (rrd {settings.Node.TimeSeries} AVERAGE)") });

            CheckThreshold(result,
                           settings.Storage.Threshold,
                           "WV0002",
                           DiagnosticResultContext.Node,
                           "Usage",
                           new[] { (data.Sum(a => (double)a.swapused / (double)a.swaptotal) / data.Count() * 100,
                                    id,
                                    $"SWAP (rrd {settings.Node.TimeSeries} AVERAGE)") });
        }

        private static dynamic GetTimeSeries(SettingsTimeSeriesType series, dynamic rrdData)
        {
            switch (series)
            {
                case SettingsTimeSeriesType.Day: return rrdData.Day;
                case SettingsTimeSeriesType.Week: return rrdData.Week;
                default: return rrdData.Day;
            }
        }

        private static void CheckThreshold(List<DiagnosticResult> result,
                                           SettingsThreshold<double> threshold,
                                           string errorCode,
                                           DiagnosticResultContext context,
                                           string subContext,
                                           IEnumerable<(double Usage, string Id, string PrefixDescription)> data)
        {
            if (threshold.Warning == 0 || threshold.Critical == 0) { return; }

            var ranges = new[] { threshold.Warning, threshold.Critical, threshold.Critical * 100 };
            var gravity = new[] { DiagnosticResultGravity.Warning, DiagnosticResultGravity.Critical };
            for (int i = 0; i < 3; i++)
            {
                result.AddRange(data
                                .Where(a => a.Usage >= ranges[i] && a.Usage <= ranges[i + 1])
                                .Select(a => new DiagnosticResult
                                {
                                    Id = a.Id,
                                    ErrorCode = errorCode,
                                    Description = $"{a.PrefixDescription} usage {Math.Round(a.Usage, 1)}%",
                                    Context = context,
                                    SubContext = subContext,
                                    Gravity = gravity[i],
                                }));
            }
        }

        private static void CheckSnapshots(List<DiagnosticResult> result,
                                           IEnumerable<dynamic> snapshots,
                                           DateTime execution,
                                           string id,
                                           DiagnosticResultContext context)
        {
            //autosnap
            if (snapshots.Where(a => a.description == "cv4pve-autosnap").Count() == 0)
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
            var oldAutoSnapCount = snapshots.Where(a => a.description == "eve4pve-autosnap").Count();
            if (oldAutoSnapCount > 0)
            {
                result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0003",
                    Description = $"{oldAutoSnapCount} Old AutoSnap 'eve4pve-autosnap' are present. Update new version",
                    Context = context,
                    SubContext = "AutoSnapshot",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }

            //old month
            var snapOldCount = snapshots.Where(a => a.name != "current" &&
                            DateTimeUnixHelper.UnixTimeToDateTime(a.snaptime.Value) < execution.AddMonths(-1))
                                        .Count();

            if (snapOldCount > 0)
            {
                result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0003",
                    Description = $"{snapOldCount} snapshots older than 1 month",
                    Context = context,
                    SubContext = "Snapshot",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }
    }
}