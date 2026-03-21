/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private async Task CheckQemuAsync(List<ClusterResource> resources,
                                      IEnumerable<ClusterBackup> clusterBackups,
                                      bool hasCluster)
    {
        var osNotMaintained = new[] { "win10", "win8", "win7", "w2k8", "wxp", "w2k" };

        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                  && a.VmType == VmType.Qemu
                                                  && !a.IsTemplate))
        {
            var nodeApi = client.Nodes[item.Node];
            var vmApi = nodeApi.Qemu[item.VmId];
            var id = item.GetWebUrl();
            var config = await vmApi.Config.GetAsync();

            #region OS
            // OsType drives several PVE defaults (RTC, drivers, etc.) — must be set correctly
            if (config.OsType == null)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0001",
                    Description = "OsType not set!",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "OS",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            else if (osNotMaintained.Contains(config.OsType))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0001",
                    Description = $"OS '{config.OsTypeDecode}' not maintained from vendor!",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "OSNotMaintained",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Agent
            // QEMU Guest Agent enables freeze-consistent snapshots, IP reporting and graceful shutdown
            if (!config.AgentEnabled)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0001",
                    Description = "Qemu Agent not enabled",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Agent",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            else if (item.IsRunning)
            {
                // Agent enabled in config but not responding inside the guest
                try
                {
                    var agentHost = await vmApi.Agent.GetHostName.GetAsync();
                    if (string.IsNullOrWhiteSpace(agentHost?.Result?.HostName))
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WV0001",
                            Description = "Qemu Agent in guest not running",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Agent",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }
                catch
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WV0001",
                        Description = "Qemu Agent in guest not running",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Agent",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            #region Virtio
            // VirtIO SCSI controller and disk bus offer significantly better throughput than IDE/SATA emulation
            if (config is VmConfigQemu qc && !(qc.ScsiHw ?? string.Empty).StartsWith("virtio", StringComparison.OrdinalIgnoreCase))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0001",
                    Description = "For more performance switch controller to VirtIO SCSI",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "VirtIO",
                    Gravity = DiagnosticResultGravity.Info,
                });
                _result.AddRange(config.Disks.Where(a => !a.Id.StartsWith("virtio"))
                                             .Select(a => new DiagnosticResult
                                             {
                                                 Id = id,
                                                 ErrorCode = "WV0001",
                                                 Description = $"For more performance switch '{a.Id}' hdd to VirtIO",
                                                 Context = DiagnosticResultContext.Qemu,
                                                 SubContext = "VirtIO",
                                                 Gravity = DiagnosticResultGravity.Info,
                                             }));
            }

            // VirtIO network driver has lower CPU overhead and higher throughput than e1000/rtl8139
            _result.AddRange(config.Networks
                .Where(n => !string.IsNullOrWhiteSpace(n.Model) && !n.Model.StartsWith("virtio", StringComparison.OrdinalIgnoreCase))
                .Select(n => new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WV0001",
                    Description = $"For more performance switch '{n.Id}' network to VirtIO",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "VirtIO",
                    Gravity = DiagnosticResultGravity.Info,
                }));
            #endregion


            #region Cdrom
            // A mounted ISO left in the drive is harmless but wastes storage and may confuse OS reinstalls
            foreach (var value in config.ExtensionData.Values.Where(a => a != null).Select(a => a.ToString()!))
            {
                if (value.Contains("media=cdrom") && value != "none,media=cdrom")
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WV0002",
                        Description = "Cdrom mounted",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Hardware",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
            #endregion

            #region CPU Type
            if (config is VmConfigQemu qemuConfig)
            {
                var cpuType = qemuConfig.Cpu?.Split(',')[0].Trim().ToLower();

                // "host" exposes all physical CPU features to the guest but prevents live migration
                // between nodes with different CPU models. Only relevant in a multi-node cluster.
                if (cpuType == "host" && hasCluster)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WV0001",
                        Description = "CPU type 'host' prevents live migration to nodes with a different CPU model",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "CPU",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }

                // "kvm64" (or unset default) is a very old baseline lacking AVX, SSE4 and other modern extensions.
                // x86-64-v2 is the minimum recommended for current Linux/Windows guests.
                if (cpuType == "kvm64" || string.IsNullOrWhiteSpace(cpuType))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WV0001",
                        Description = $"CPU type '{(string.IsNullOrWhiteSpace(cpuType) ? "default (kvm64)" : cpuType)}' is outdated, consider x86-64-v2 or higher for better performance",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "CPU",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }

                #region CPU hotplug
                // CPU hotplug allows adding vCPUs to a running VM without restarting it.
                // Windows guests do not support CPU hotplug — they enumerate CPUs only at boot.
                // Enabling it on a Windows VM wastes resources (PVE reserves CPU slots) and may confuse the guest.
                if (!string.IsNullOrWhiteSpace(qemuConfig.Hotplug)
                    && qemuConfig.Hotplug != "0"
                    && qemuConfig.Hotplug.Split(',').Any(p => p.Trim() == "cpu")
                    && config.OsType != null
                    && config.OsType.StartsWith("win", StringComparison.OrdinalIgnoreCase))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WV0001",
                        Description = "CPU hotplug is enabled but Windows guests do not support it — disable to avoid resource waste",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "CPU",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
                #endregion

                #region Balloon
                // Balloon=0 disables the virtio-balloon driver — RAM is fully reserved and cannot be reclaimed
                // by the host. Skip this check when hugepages are used (balloon is incompatible with hugepages).
                if (qemuConfig.Balloon == 0 && string.IsNullOrWhiteSpace(qemuConfig.Hugepages))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WV0001",
                        Description = "Balloon driver disabled, RAM is statically allocated",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Balloon",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }
                #endregion

                #region Disk cache
                foreach (var disk in config.Disks.Where(d => !d.IsUnused && !string.IsNullOrWhiteSpace(d.Cache)))
                {
                    // cache=unsafe disables all host-side flushing — data loss on host crash even without backup issues
                    if (disk.Cache == "unsafe")
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WV0001",
                            Description = $"Disk '{disk.Id}' uses cache=unsafe, data loss risk on host crash",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Hardware",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }

                    // cache=writeback improves performance but data in the host page cache is not yet on disk.
                    // If backup is also disabled for that disk, a crash can cause data loss with no recovery option.
                    if (disk.Cache == "writeback" && !disk.Backup)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WV0001",
                            Description = $"Disk '{disk.Id}' has cache=writeback but backup is disabled",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Hardware",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }
                #endregion

                #region SecureBoot Windows 11
                // Windows 11 requires UEFI (bios=ovmf) + TPM 2.0 (tpmstate0) for SecureBoot.
                // Without these the guest may fail to install or update.
                if (config.OsType == "win11")
                {
                    if (qemuConfig.Bios != "ovmf")
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WV0001",
                            Description = "Windows 11 requires UEFI (bios=ovmf) for SecureBoot",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "SecureBoot",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }

                    if (string.IsNullOrWhiteSpace(qemuConfig.Tpmstate0))
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WV0001",
                            Description = "Windows 11 requires TPM 2.0 (tpmstate0) for SecureBoot",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "SecureBoot",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }
                #endregion
            }
            #endregion


            var rrdData = settings.Qemu.TimeSeries switch
            {
                SettingsTimeSeriesType.Day => await vmApi.Rrddata.GetAsync(RrdDataTimeFrame.Day, RrdDataConsolidation.Average),
                SettingsTimeSeriesType.Week => await vmApi.Rrddata.GetAsync(RrdDataTimeFrame.Week, RrdDataConsolidation.Average),
                _ => throw new NotImplementedException("settings.Qemu.TimeSeries"),
            };

            await CheckCommonVmAsync(settings,
                                     settings.Qemu,
                                     config,
                                     await vmApi.Pending.GetAsync(),
                                     await vmApi.Snapshot.GetAsync(),
                                     rrdData,
                                     DiagnosticResultContext.Qemu,
                                     item.Node,
                                     item.VmId,
                                     id,
                                     clusterBackups);
        }
    }
}
