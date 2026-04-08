/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private const string VirtioPrefix = "virtio";
    private const string CpuTypeHost = "host";
    private const string CpuTypeKvm64 = "kvm64";
    private const string BiosOvmf = "ovmf";
    private const string DiskCacheUnsafe = "unsafe";
    private const string DiskCacheWriteback = "writeback";
    private const string OsTypeWin11 = "win11";
    private static readonly string[] _cpuSecurityFlags = ["+spec-ctrl", "+ssbd", "+pcid", "+md-clear"];

    // PVE ostype values whose vendor support has fully ended.
    // win10 covers Win10/2016/2019 — Server 2016/2019 still supported, so excluded.
    // win8  covers Win8/2012/2012R2 — all EOL (Oct 2023).
    private static readonly string[] _osNotMaintained = ["win8", "win7", "wvista", "w2k8", "w2k3", "wxp", "w2k"];

    private async Task CheckQemuAsync(bool hasCluster)
    {
        // Build set of VM IDs managed by HA from already-loaded resources
        var haVmIds = _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                           && !string.IsNullOrWhiteSpace(a.HaState))
                               .Select(a => a.VmId)
                               .ToHashSet();

        // vCPU overcommit check — sum of vCPUs per node vs physical CPUs
        // CpuSize on node resource = physical CPU count; on VM resource = assigned vCPUs
        foreach (var nodeGroup in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                       && a.VmType == VmType.Qemu
                                                       && !a.IsTemplate)
                                           .GroupBy(a => a.Node))
        {
            var nodeResource = _resources.FirstOrDefault(a => a.ResourceType == ClusterResourceType.Node
                                                             && a.Node == nodeGroup.Key);
            if (nodeResource == null || nodeResource.CpuSize == 0) { continue; }

            var totalVCpus = nodeGroup.Sum(a => a.CpuSize);
            var ratio = (double)totalVCpus / nodeResource.CpuSize;

            if (ratio > settings.Node.MaxVCpuRatio)
            {
                foreach (var vm in nodeGroup)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = vm.GetWebUrl(),
                        ErrorCode = "WG0036",
                        Description = $"Node '{nodeGroup.Key}' vCPU overcommit ratio is {ratio:F1}x ({totalVCpus} vCPUs / {nodeResource.CpuSize} physical) — exceeds threshold of {settings.Node.MaxVCpuRatio}x",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "CPU",
                        Gravity = DiagnosticResultGravity.Warning,
                    });
                }
            }
        }

        foreach (var item in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                  && a.VmType == VmType.Qemu
                                                  && !a.IsTemplate))
        {
            var nodeApi = client.Nodes[item.Node];
            var vmApi = nodeApi.Qemu[item.VmId];
            var id = item.GetWebUrl();
            var config = (VmConfigQemu)_vmConfigs[item.VmId];

            #region OS
            // OsType drives several PVE defaults (RTC, drivers, etc.) — must be set correctly
            if (config.OsType == null)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WG0001",
                    Description = "OsType not set!",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "OS",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            else if (_osNotMaintained.Contains(config.OsType))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WG0002",
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
                    ErrorCode = "WG0003",
                    Description = "Qemu Agent not enabled",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Agent",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            else if (item.IsRunning)
            {
                // agent/info: verify agent is running and retrieve version for outdated check
                try
                {
                    var agentInfo = await vmApi.Agent.Info.GetAsync();
                    if (agentInfo?.Result == null)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WG0004",
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
                        ErrorCode = "WG0004",
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
            if (config is VmConfigQemu qc && !(qc.ScsiHw ?? string.Empty).StartsWith(VirtioPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IG0001",
                    Description = "For more performance switch controller to VirtIO SCSI",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "VirtIO",
                    Gravity = DiagnosticResultGravity.Info,
                });
                _result.AddRange(config.Disks.Where(a => !a.Id.StartsWith(VirtioPrefix))
                                             .Select(a => new DiagnosticResult
                                             {
                                                 Id = id,
                                                 ErrorCode = "IG0002",
                                                 Description = $"For more performance switch '{a.Id}' hdd to VirtIO",
                                                 Context = DiagnosticResultContext.Qemu,
                                                 SubContext = "VirtIO",
                                                 Gravity = DiagnosticResultGravity.Info,
                                             }));
            }

            // VirtIO network driver has lower CPU overhead and higher throughput than e1000/rtl8139
            _result.AddRange(config.Networks
                .Where(n => !string.IsNullOrWhiteSpace(n.Model) && !n.Model.StartsWith(VirtioPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(n => new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IG0003",
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
                        ErrorCode = "WG0005",
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
                if (cpuType == CpuTypeHost && hasCluster)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WG0006",
                        Description = "CPU type 'host' prevents live migration to nodes with a different CPU model",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "CPU",
                        Gravity = DiagnosticResultGravity.Warning,
                    });

                    // CPU type 'host' + HA enabled: HA requires live migration between nodes,
                    // which is impossible when the CPU type is 'host' (node-specific CPU features).
                    if (haVmIds.Contains(item.VmId))
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "CG0004",
                            Description = "CPU type 'host' is incompatible with HA — HA requires live migration which needs a portable CPU type",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "CPU",
                            Gravity = DiagnosticResultGravity.Critical,
                        });
                    }
                }

                // "kvm64" is a very old baseline lacking AVX, SSE4 and other modern extensions.
                // x86-64-v2 is the minimum recommended for current Linux/Windows guests.
                // We only flag explicit "kvm64" — if Cpu is unset, the cluster default applies and we cannot know it.
                if (cpuType == CpuTypeKvm64)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "IG0004",
                        Description = "CPU type 'kvm64' is outdated, consider x86-64-v2 or higher for better performance",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "CPU",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }

                #region CPU security flags
                // When cpu type is not 'host', security mitigations flags are not inherited automatically.
                // Missing flags expose guests to Spectre/Meltdown/MDS variants.
                if (!string.IsNullOrWhiteSpace(cpuType)
                    && !cpuType.Equals(CpuTypeHost, StringComparison.OrdinalIgnoreCase))
                {
                    var cpuFlags = qemuConfig.Cpu ?? string.Empty;
                    var missingFlags = _cpuSecurityFlags
                                        .Where(f => !cpuFlags.Contains(f, StringComparison.OrdinalIgnoreCase))
                                        .ToList();

                    if (missingFlags.Count > 0)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WG0037",
                            Description = $"CPU type '{cpuType}' is missing security flags: {string.Join(", ", missingFlags)} — add to cpu flags to mitigate Spectre/Meltdown/MDS",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "CPU",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }
                #endregion

                #region CPU hotplug
                // CPU hotplug allows adding vCPUs to a running VM without restarting it.
                // Windows guests do not support CPU hotplug — they enumerate CPUs only at boot.
                // Enabling it on a Windows VM wastes resources (PVE reserves CPU slots) and may confuse the guest.
                if (!string.IsNullOrWhiteSpace(qemuConfig.Hotplug)
                    && qemuConfig.Hotplug != "0"
                    && qemuConfig.Hotplug.Split(',').Any(p => p.Trim() == "cpu")
                    && config.OsType?.StartsWith("win", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "WG0007",
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
                        ErrorCode = "IG0005",
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
                    if (disk.Cache == DiskCacheUnsafe)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WG0008",
                            Description = $"Disk '{disk.Id}' uses cache=unsafe, data loss risk on host crash",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "Hardware",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }

                    // cache=writeback improves performance but data in the host page cache is not yet on disk.
                    // If backup is also disabled for that disk, a crash can cause data loss with no recovery option.
                    if (disk.Cache == DiskCacheWriteback && !disk.Backup)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WG0009",
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
                if (config.OsType == OsTypeWin11)
                {
                    if (qemuConfig.Bios != BiosOvmf)
                    {
                        _result.Add(new DiagnosticResult
                        {
                            Id = id,
                            ErrorCode = "WG0010",
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
                            ErrorCode = "WG0011",
                            Description = "Windows 11 requires TPM 2.0 (tpmstate0) for SecureBoot",
                            Context = DiagnosticResultContext.Qemu,
                            SubContext = "SecureBoot",
                            Gravity = DiagnosticResultGravity.Warning,
                        });
                    }
                }
                #endregion

                #region Memory balloon overcommit
                // Balloon very close to Memory means ballooning has no room to reclaim memory
                if (qemuConfig.Balloon > 0 && config.Memory > 0
                    && (double)qemuConfig.Balloon / config.Memory > 0.95)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "IG0006",
                        Description = $"VM memory balloon ({qemuConfig.Balloon}MB) is >95% of total memory ({config.Memory}MB) — ballooning has no room to reclaim memory",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Balloon",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }
                #endregion

                #region RNG device
                // virtio-rng is rarely needed — may indicate misconfiguration
                if (!string.IsNullOrWhiteSpace(qemuConfig.Rng0))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "IG0007",
                        Description = "VM has a virtio-rng (RNG) device configured — verify this is intentional",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Hardware",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }
                #endregion

                #region Serial console
                // Serial device can expose sensitive data if not intentional
                var serialKeys = config.ExtensionData?.Keys
                    .Where(k => k.StartsWith("serial", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? [];
                if (serialKeys.Count > 0)
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "IG0008",
                        Description = $"VM has serial console configured ({string.Join(", ", serialKeys)}) — verify this is intentional",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "Hardware",
                        Gravity = DiagnosticResultGravity.Info,
                    });
                }
                #endregion
            }
            #endregion

            #region HA with local storage
            // HA requires live migration. If any disk is on non-shared storage the migration fails.
            if (haVmIds.Contains(item.VmId))
            {
                foreach (var disk in config.Disks.Where(d => !d.IsUnused
                                                             && !string.IsNullOrWhiteSpace(d.Storage)
                                                             && !_storageResources.Any(s => s.Storage == d.Storage && s.Shared)))
                {
                    _result.Add(new DiagnosticResult
                    {
                        Id = id,
                        ErrorCode = "CG0005",
                        Description = $"Disk '{disk.Id}' is on non-shared storage '{disk.Storage}' but VM is managed by HA — live migration will fail",
                        Context = DiagnosticResultContext.Qemu,
                        SubContext = "HA",
                        Gravity = DiagnosticResultGravity.Critical,
                    });
                }
            }
            #endregion

            #region Machine type
            // An empty machine type means QEMU picks the default at startup, which may change across
            // PVE upgrades and cause unexpected guest behaviour after an upgrade.
            if (config is VmConfigQemu qemuMachine && string.IsNullOrWhiteSpace(qemuMachine.Machine))
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "IG0012",
                    Description = "Machine type not set — QEMU will use the default, which may change across PVE upgrades",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Hardware",
                    Gravity = DiagnosticResultGravity.Info,
                });
            }
            #endregion

            #region No network interface
            // A VM with no network interface is completely isolated — likely a misconfiguration
            if (!config.Networks.Any())
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WG0034",
                    Description = "VM has no network interface configured — completely isolated from network",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Network",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            #region Firewall and IP filter
            CheckVmFirewall(_result, await vmApi.Firewall.Options.GetAsync(), id, DiagnosticResultContext.Qemu);
            #endregion

            #region USB/PCI passthrough
            // USB or PCI passthrough binds the VM to a specific node — prevents live migration and HA failover
            var passthroughKeys = config.ExtensionData?.Keys
                .Where(k => k.StartsWith("usb", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("hostpci", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? [];

            if (passthroughKeys.Count > 0)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WG0012",
                    Description = $"VM has USB/PCI passthrough configured ({string.Join(", ", passthroughKeys)}) — live migration and HA failover are not possible",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Hardware",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion

            await CheckCommonVmAsync(settings,
                                     settings.Qemu,
                                     config,
                                     await vmApi.Pending.GetAsync(),
                                     settings.Snapshot.Enabled
                                        ? await vmApi.Snapshot.GetAsync()
                                        : [],
                                     await vmApi.Rrddata.GetAsync(settings.Qemu.Rrd.TimeFrame, settings.Qemu.Rrd.Consolidation),
                                     DiagnosticResultContext.Qemu,
                                     item.Node,
                                     item.VmId,
                                     id,
                                     _backupStoragesByNode.GetValueOrDefault(item.Node, []));
        }

        // Duplicate MAC check — collect all MACs across all VMs and flag duplicates
        var allMacs = _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                           && a.VmType == VmType.Qemu
                                           && !a.IsTemplate)
                               .SelectMany(a => _vmConfigs[a.VmId].Networks
                                                    .Where(n => !string.IsNullOrWhiteSpace(n.MacAddress))
                                                    .Select(n => new
                                                    {
                                                        a.VmId,
                                                        Url = a.GetWebUrl(),
                                                        Mac = n.MacAddress.ToUpperInvariant()
                                                    }))
                               .ToList();

        var duplicateMacs = allMacs.GroupBy(x => x.Mac)
                                   .Where(g => g.Count() > 1);

        foreach (var group in duplicateMacs)
        {
            foreach (var entry in group)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = entry.Url,
                    ErrorCode = "WG0033",
                    Description = $"Duplicate MAC address {group.Key} shared with VM(s) {string.Join(", ", group.Where(x => x.VmId != entry.VmId).Select(x => x.VmId))} — causes network conflicts",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Network",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }

        // Template checks — config already pre-fetched
        foreach (var item in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                  && a.VmType == VmType.Qemu
                                                  && a.IsTemplate))
        {
            var id = item.GetWebUrl();
            var config = (VmConfigQemu)_vmConfigs[item.VmId];

            #region Template with QEMU agent enabled
            // QEMU agent on a template is useless — the template is never running.
            // Worse: clones inherit the setting and may generate spurious "agent not running" warnings
            // until the guest installs the agent, creating noise in diagnostics.
            if (config.AgentEnabled)
            {
                _result.Add(new DiagnosticResult
                {
                    Id = id,
                    ErrorCode = "WG0014",
                    Description = "Template has QEMU agent enabled — agent is unused on templates and clones will inherit this setting",
                    Context = DiagnosticResultContext.Qemu,
                    SubContext = "Agent",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
            #endregion
        }
    }
}
