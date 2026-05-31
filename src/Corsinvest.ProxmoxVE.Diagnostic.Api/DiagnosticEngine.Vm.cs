/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;

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

    private record VmFetchData(ClusterResource Item,
                               VmConfigQemu Config,
                               VmFirewallOptions? Firewall,
                               IReadOnlyList<KeyValue> Pending,
                               IReadOnlyList<VmSnapshot> Snapshots,
                               object? AgentInfo);

    private async Task<VmFetchData> FetchVmDataAsync(ClusterResource item)
    {
        var vmApi = client.Nodes[item.Node].Qemu[item.VmId];
        var id = item.GetWebUrl();
        var config = (VmConfigQemu)_vmConfigs[item.VmId];
        var firewallTask = vmApi.Firewall.Options.GetAsync().ToSafeSingle(_result, id, DiagnosticResultContext.Qemu, $"firewall options of VM {item.VmId}");
        var pendingTask = vmApi.Pending.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Qemu, $"pending changes of VM {item.VmId}");
        var snapshotTask = settings.Snapshot.Enabled
                            ? vmApi.Snapshot.GetAsync().ToSafeEnum(_result, id, DiagnosticResultContext.Qemu, $"snapshots of VM {item.VmId}")
                            : Task.FromResult<IReadOnlyList<VmSnapshot>>([]);
        await Task.WhenAll(firewallTask, pendingTask, snapshotTask);

        object? agentInfo = null;
        if (config.AgentEnabled && item.IsRunning)
        {
            try { agentInfo = await vmApi.Agent.Info.GetAsync(); }
            catch { /* agent not running — handled in check */ }
        }

        return new VmFetchData(item, config, firewallTask.Result, pendingTask.Result, snapshotTask.Result, agentInfo);
    }

    private async Task CheckVmAsync(bool hasCluster)
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
                    CreateResult(
                        isOk: false,
                        id: vm.GetWebUrl(),
                        errorCode: "WG0036",
                        subContext: "CPU",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: $"Node '{nodeGroup.Key}' vCPU overcommit ratio is {ratio:F1}x ({totalVCpus} vCPUs / {nodeResource.CpuSize} physical) — exceeds threshold of {settings.Node.MaxVCpuRatio}x",
                        descriptionOk: "",
                        compliance: []);
                }
            }
        }

        // Pre-fetch all VM data in parallel
        var vmFetchResults = await RunParallelAsync(_resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                                            && a.VmType == VmType.Qemu
                                                                            && !a.IsTemplate),
                                                    FetchVmDataAsync);

        foreach (var fetch in vmFetchResults)
        {
            var item = fetch.Item;
            var config = fetch.Config;
            var id = item.GetWebUrl();

            #region OS
            // OsType drives several PVE defaults (RTC, drivers, etc.) — must be set correctly
            CreateResult(
                isOk: config.OsType != null,
                id: id,
                errorCode: "WG0001",
                subContext: "OS",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: "OsType not set!",
                descriptionOk: $"OsType set to '{config.OsTypeDecode}'",
                compliance: []);
            if (config.OsType != null)
            {
                CreateResult(
                    isOk: !_osNotMaintained.Contains(config.OsType),
                    id: id,
                    errorCode: "WG0002",
                    subContext: "OSNotMaintained",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: $"OS '{config.OsTypeDecode}' not maintained from vendor!",
                    descriptionOk: $"OS '{config.OsTypeDecode}' is supported by the vendor",
                    compliance:
                    [
                        ComplianceControls.Iso27001.A_8_8,
                        ComplianceControls.Nis2.Art_21_e,
                        ComplianceControls.PciDss.R_6_3,
                        ComplianceControls.Gdpr.Art_32_1_b,
                        ComplianceControls.AgId.ABSC_2_3,
                        ComplianceControls.AgId.ABSC_4_1,
                        ComplianceControls.AgId.ABSC_4_4,
                        ComplianceControls.Cis.C_7,
                        ComplianceControls.NistCsf.PR_PS_02,
                        ComplianceControls.NistCsf.ID_RA_01,
                        ComplianceControls.Iso27017.CLD_9_5_2,
                    ]);
            }
            #endregion

            #region Agent
            // QEMU Guest Agent enables freeze-consistent snapshots, IP reporting and graceful shutdown
            CreateResult(
                isOk: config.AgentEnabled,
                id: id,
                errorCode: "WG0003",
                subContext: "Agent",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: "Qemu Agent not enabled",
                descriptionOk: "Qemu Agent is enabled",
                compliance: []);
            if (config.AgentEnabled && item.IsRunning)
            {
                CreateResult(
                    isOk: fetch.AgentInfo != null,
                    id: id,
                    errorCode: "WG0004",
                    subContext: "Agent",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Warning,
                    descriptionKo: "Qemu Agent in guest not running",
                    descriptionOk: "Qemu Agent is running inside the guest",
                    compliance: []);
            }
            #endregion

            #region Virtio
            // VirtIO SCSI controller and disk bus offer significantly better throughput than IDE/SATA emulation
            if (config is VmConfigQemu qc)
            {
                var scsiHwIsVirtIO = (qc.ScsiHw ?? "").StartsWith(VirtioPrefix, StringComparison.OrdinalIgnoreCase);
                CreateResult(
                    isOk: scsiHwIsVirtIO,
                    id: id,
                    errorCode: "IG0001",
                    subContext: "VirtIO",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "For more performance switch controller to VirtIO SCSI",
                    descriptionOk: $"SCSI controller is VirtIO ({qc.ScsiHw})",
                    compliance: []);

                // IG0002 is only meaningful when the SCSI HW is NOT VirtIO; otherwise individual disks
                // riding on a non-VirtIO controller would be misreported.
                if (!scsiHwIsVirtIO)
                {
                    CreateResultPerItem(
                        items: config.Disks.ToList(),
                        isItemOk: a => a.Id.StartsWith(VirtioPrefix),
                        itemId: _ => id,
                        itemDescriptionKo: a => $"For more performance switch '{a.Id}' hdd to VirtIO",
                        aggregatedIdOk: id,
                        aggregatedDescriptionOk: _ => "All disks use the VirtIO bus",
                        errorCode: "IG0002",
                        subContext: "VirtIO",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Info,
                        compliance: []);
                }
            }

            // VirtIO network driver has lower CPU overhead and higher throughput than e1000/rtl8139
            CreateResultPerItem(
                items: config.Networks.Where(n => !string.IsNullOrWhiteSpace(n.Model)).ToList(),
                isItemOk: n => n.Model.StartsWith(VirtioPrefix, StringComparison.OrdinalIgnoreCase),
                itemId: _ => id,
                itemDescriptionKo: n => $"For more performance switch '{n.Id}' network to VirtIO",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => "All network interfaces use the VirtIO driver",
                errorCode: "IG0003",
                subContext: "VirtIO",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Info,
                compliance: []);
            #endregion


            #region Cdrom
            // A mounted ISO left in the drive is harmless but wastes storage and may confuse OS reinstalls.
            // Cloud-init drives are excluded (Kind == CloudInit) — they always look like a cdrom but are
            // legitimate and would otherwise generate a noisy false positive.
            // Empty drives (Storage "none") are also skipped.
            CreateResultPerItem(
                items: config.DisksAll.Where(d => d.Kind == VmDiskKind.Cdrom).ToList(),
                isItemOk: d => d.Storage == "none",
                itemId: _ => id,
                itemDescriptionKo: cdrom => $"Cdrom mounted on '{cdrom.Id}' ({cdrom.Storage}:{cdrom.FileName})",
                aggregatedIdOk: id,
                aggregatedDescriptionOk: _ => "No CD-ROM drives have media mounted",
                errorCode: "WG0005",
                subContext: "Hardware",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance: []);
            #endregion

            #region CPU Type
            if (config is VmConfigQemu qemuConfig)
            {
                var cpuType = qemuConfig.Cpu?.Split(',')[0].Trim().ToLower();

                // "host" exposes all physical CPU features to the guest but prevents live migration
                // between nodes with different CPU models. Only relevant in a multi-node cluster.
                if (hasCluster)
                {
                    CreateResult(
                        isOk: cpuType != CpuTypeHost,
                        id: id,
                        errorCode: "WG0006",
                        subContext: "CPU",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: "CPU type 'host' prevents live migration to nodes with a different CPU model",
                        descriptionOk: $"CPU type '{cpuType}' allows live migration",
                        compliance: []);

                    // CPU type 'host' + HA enabled: HA requires live migration between nodes,
                    // which is impossible when the CPU type is 'host' (node-specific CPU features).
                    if (haVmIds.Contains(item.VmId))
                    {
                        CreateResult(
                            isOk: cpuType != CpuTypeHost,
                            id: id,
                            errorCode: "CG0004",
                            subContext: "CPU",
                            context: DiagnosticResultContext.Qemu,
                            gravityKo: DiagnosticResultGravity.Critical,
                            descriptionKo: "CPU type 'host' is incompatible with HA — HA requires live migration which needs a portable CPU type",
                            descriptionOk: $"CPU type '{cpuType}' is compatible with HA live migration",
                            compliance: []);
                    }
                }

                // "kvm64" is a very old baseline lacking AVX, SSE4 and other modern extensions.
                // x86-64-v2 is the minimum recommended for current Linux/Windows guests.
                // We only flag explicit "kvm64" — if Cpu is unset, the cluster default applies and we cannot know it.
                if (!string.IsNullOrWhiteSpace(cpuType))
                {
                    CreateResult(
                        isOk: cpuType != CpuTypeKvm64,
                        id: id,
                        errorCode: "IG0004",
                        subContext: "CPU",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Info,
                        descriptionKo: "CPU type 'kvm64' is outdated, consider x86-64-v2 or higher for better performance",
                        descriptionOk: $"CPU type '{cpuType}' is not the outdated kvm64 baseline",
                        compliance: []);
                }

                #region CPU security flags
                // When cpu type is not 'host', security mitigations flags are not inherited automatically.
                // Missing flags expose guests to Spectre/Meltdown/MDS variants.
                if (!string.IsNullOrWhiteSpace(cpuType)
                    && !cpuType.Equals(CpuTypeHost, StringComparison.OrdinalIgnoreCase))
                {
                    var cpuFlags = qemuConfig.Cpu ?? "";
                    var missingFlags = _cpuSecurityFlags
                                        .Where(f => !cpuFlags.Contains(f, StringComparison.OrdinalIgnoreCase))
                                        .ToList();

                    CreateResult(
                        isOk: missingFlags.Count == 0,
                        id: id,
                        errorCode: "WG0037",
                        subContext: "CPU",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: $"CPU type '{cpuType}' is missing security flags: {string.Join(", ", missingFlags)} — add to cpu flags to mitigate Spectre/Meltdown/MDS",
                        descriptionOk: $"CPU type '{cpuType}' has all Spectre/Meltdown/MDS mitigation flags configured",
                        compliance:
                        [
                            ComplianceControls.Iso27001.A_8_8,
                            ComplianceControls.Nis2.Art_21_e,
                            ComplianceControls.PciDss.R_6_3,
                            ComplianceControls.Gdpr.Art_32_1_b,
                            ComplianceControls.AgId.ABSC_2_3,
                            ComplianceControls.AgId.ABSC_4_1,
                            ComplianceControls.AgId.ABSC_4_4,
                            ComplianceControls.Cis.C_7,
                            ComplianceControls.NistCsf.PR_PS_02,
                            ComplianceControls.NistCsf.ID_RA_01,
                            ComplianceControls.Iso27017.CLD_9_5_2,
                        ]);
                }
                #endregion

                #region CPU hotplug
                // CPU hotplug allows adding vCPUs to a running VM without restarting it.
                // Windows guests do not support CPU hotplug — they enumerate CPUs only at boot.
                // Enabling it on a Windows VM wastes resources (PVE reserves CPU slots) and may confuse the guest.
                if (config.OsType?.StartsWith("win", StringComparison.OrdinalIgnoreCase) is true)
                {
                    var cpuHotplugEnabled = !string.IsNullOrWhiteSpace(qemuConfig.Hotplug)
                                             && qemuConfig.Hotplug != "0"
                                             && qemuConfig.Hotplug.Split(',').Any(p => p.Trim() == "cpu");
                    CreateResult(
                        isOk: !cpuHotplugEnabled,
                        id: id,
                        errorCode: "WG0007",
                        subContext: "CPU",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: "CPU hotplug is enabled but Windows guests do not support it — disable to avoid resource waste",
                        descriptionOk: "CPU hotplug is not enabled on this Windows guest",
                        compliance: []);
                }
                #endregion

                #region Balloon
                // Balloon=0 disables the virtio-balloon driver — RAM is fully reserved and cannot be reclaimed
                // by the host. Skip this check when hugepages are used (balloon is incompatible with hugepages).
                if (string.IsNullOrWhiteSpace(qemuConfig.Hugepages))
                {
                    CreateResult(
                        isOk: qemuConfig.Balloon != 0,
                        id: id,
                        errorCode: "IG0005",
                        subContext: "Balloon",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Info,
                        descriptionKo: "Balloon driver disabled, RAM is statically allocated",
                        descriptionOk: $"Balloon driver enabled ({qemuConfig.Balloon} MB)",
                        compliance: []);
                }
                #endregion

                #region Disk cache
                ComplianceMapping[] dataIntegrityControls =
                [
                    ComplianceControls.Iso27001.A_8_13,
                    ComplianceControls.Nis2.Art_21_c,
                    ComplianceControls.Gdpr.Art_32_1_b,
                ];

                // cache=unsafe disables all host-side flushing — data loss on host crash even without backup issues
                CreateResultPerItem(
                    items: config.Disks.Where(d => !d.IsUnused && !string.IsNullOrWhiteSpace(d.Cache)).ToList(),
                    isItemOk: d => d.Cache != DiskCacheUnsafe,
                    itemId: _ => id,
                    itemDescriptionKo: d => $"Disk '{d.Id}' uses cache=unsafe, data loss risk on host crash",
                    aggregatedIdOk: id,
                    aggregatedDescriptionOk: _ => "No disk uses cache=unsafe",
                    errorCode: "WG0008",
                    subContext: "Hardware",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Warning,
                    compliance: dataIntegrityControls);

                // cache=writeback improves performance but data in the host page cache is not yet on disk.
                // If backup is also disabled for that disk, a crash can cause data loss with no recovery option.
                CreateResultPerItem(
                    items: config.Disks.Where(d => !d.IsUnused && !string.IsNullOrWhiteSpace(d.Cache)).ToList(),
                    isItemOk: d => !(d.Cache == DiskCacheWriteback && !d.Backup),
                    itemId: _ => id,
                    itemDescriptionKo: d => $"Disk '{d.Id}' has cache=writeback but backup is disabled",
                    aggregatedIdOk: id,
                    aggregatedDescriptionOk: _ => "No disk combines cache=writeback with backup disabled",
                    errorCode: "WG0009",
                    subContext: "Hardware",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Warning,
                    compliance: dataIntegrityControls);
                #endregion

                #region SecureBoot Windows 11
                // Windows 11 requires UEFI (bios=ovmf) + TPM 2.0 (tpmstate0) for SecureBoot.
                // Without these the guest may fail to install or update.
                if (config.OsType == OsTypeWin11)
                {
                    CreateResult(
                        isOk: qemuConfig.Bios == BiosOvmf,
                        id: id,
                        errorCode: "WG0010",
                        subContext: "SecureBoot",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: "Windows 11 requires UEFI (bios=ovmf) for SecureBoot",
                        descriptionOk: "Windows 11 guest is configured with UEFI (ovmf)",
                        compliance: []);

                    CreateResult(
                        isOk: qemuConfig.GetDisk("tpmstate0") is not null,
                        id: id,
                        errorCode: "WG0011",
                        subContext: "SecureBoot",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Warning,
                        descriptionKo: "Windows 11 requires TPM 2.0 (tpmstate0) for SecureBoot",
                        descriptionOk: "Windows 11 guest has TPM 2.0 (tpmstate0) configured",
                        compliance: []);
                }
                #endregion

                #region Memory balloon overcommit
                // Balloon very close to Memory means ballooning has no room to reclaim memory
                if (qemuConfig.Balloon > 0 && config.Memory > 0)
                {
                    var balloonRatio = (double)qemuConfig.Balloon / config.Memory;
                    CreateResult(
                        isOk: balloonRatio <= 0.95,
                        id: id,
                        errorCode: "IG0006",
                        subContext: "Balloon",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Info,
                        descriptionKo: $"VM memory balloon ({qemuConfig.Balloon}MB) is >95% of total memory ({config.Memory}MB) — ballooning has no room to reclaim memory",
                        descriptionOk: $"VM memory balloon ({qemuConfig.Balloon}MB) leaves room to reclaim memory (total {config.Memory}MB)",
                        compliance: []);
                }
                #endregion

                #region RNG device
                // virtio-rng is rarely needed — may indicate misconfiguration
                CreateResult(
                    isOk: string.IsNullOrWhiteSpace(qemuConfig.Rng0),
                    id: id,
                    errorCode: "IG0007",
                    subContext: "Hardware",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "VM has a virtio-rng (RNG) device configured — verify this is intentional",
                    descriptionOk: "VM has no RNG device configured",
                    compliance: []);
                #endregion

                #region Serial console
                // Serial device can expose sensitive data if not intentional
                var serialKeys = config.ExtensionData?.Keys
                    .Where(k => k.StartsWith("serial", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? [];
                CreateResult(
                    isOk: serialKeys.Count == 0,
                    id: id,
                    errorCode: "IG0008",
                    subContext: "Hardware",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: $"VM has serial console configured ({string.Join(", ", serialKeys)}) — verify this is intentional",
                    descriptionOk: "VM has no serial console configured",
                    compliance: []);
                #endregion
            }
            #endregion

            #region HA with local storage
            // HA requires live migration. If any disk is on non-shared storage the migration fails.
            if (haVmIds.Contains(item.VmId))
            {
                CreateResultPerItem(
                    items: config.Disks.Where(d => !d.IsUnused && !string.IsNullOrWhiteSpace(d.Storage)).ToList(),
                    isItemOk: d => _storageResources.Any(s => s.Storage == d.Storage && s.Shared),
                    itemId: _ => id,
                    itemDescriptionKo: d => $"Disk '{d.Id}' is on non-shared storage '{d.Storage}' but VM is managed by HA — live migration will fail",
                    aggregatedIdOk: id,
                    aggregatedDescriptionOk: _ => "All HA VM disks are on shared storage",
                    errorCode: "CG0005",
                    subContext: "HA",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Critical,
                    compliance:
                    [
                        ComplianceControls.Iso27001.A_5_30,
                        ComplianceControls.Nis2.Art_21_c,
                        ComplianceControls.Dora.Art_12,
                        ComplianceControls.Gdpr.Art_32_1_b,
                        ComplianceControls.Cis.C_11,
                        ComplianceControls.NistCsf.PR_IR_04,
                        ComplianceControls.NistCsf.RC_RP_01,
                        ComplianceControls.Iso27017.CLD_6_3_1,
                    ]);
            }
            #endregion

            #region Machine type
            // An empty machine type means QEMU picks the default at startup, which may change across
            // PVE upgrades and cause unexpected guest behaviour after an upgrade.
            if (config is VmConfigQemu qemuMachine)
            {
                CreateResult(
                    isOk: !string.IsNullOrWhiteSpace(qemuMachine.Machine),
                    id: id,
                    errorCode: "IG0012",
                    subContext: "Hardware",
                    context: DiagnosticResultContext.Qemu,
                    gravityKo: DiagnosticResultGravity.Info,
                    descriptionKo: "Machine type not set — QEMU will use the default, which may change across PVE upgrades",
                    descriptionOk: $"Machine type explicitly set to '{qemuMachine.Machine}'",
                    compliance: []);

                // IG0016 — pinned machine type lags behind the latest available on the node.
                // Skipped when the value is empty (IG0012 handles that), when the format isn't
                // pc-<family>-<X.Y> (e.g. "pc-i440fx-latest", bare "q35", "windows"), or when the
                // node's machine catalog couldn't be fetched. Pinning is the right thing to do for
                // stability, but versions accumulate deprecated security/microcode behaviour and
                // should be reviewed during planned maintenance windows.
                if (TryParseMachineVersion(qemuMachine.Machine, out var family, out var current)
                    && _qemuMachinesByNode.TryGetValue(item.Node, out var nodeMachines)
                    && TryFindLatestVersion(nodeMachines, family, out var latest)
                    && CompareMachineVersions(current, latest) < 0)
                {
                    CreateResult(
                        isOk: false,
                        id: id,
                        errorCode: "IG0016",
                        subContext: "Hardware",
                        context: DiagnosticResultContext.Qemu,
                        gravityKo: DiagnosticResultGravity.Info,
                        descriptionKo: $"Machine type '{qemuMachine.Machine}' is outdated — latest available on node '{item.Node}' is '{family}-{latest}' (upgrade requires VM stop/start)",
                        descriptionOk: "",
                        compliance:
                        [
                            ComplianceControls.Iso27001.A_8_8,
                            ComplianceControls.Nis2.Art_21_e,
                            ComplianceControls.PciDss.R_6_3,
                            ComplianceControls.Gdpr.Art_32_1_b,
                            ComplianceControls.AgId.ABSC_2_3,
                            ComplianceControls.Cis.C_7,
                            ComplianceControls.NistCsf.PR_PS_02,
                        ]);
                }
            }
            #endregion

            #region No network interface
            // A VM with no network interface is completely isolated — likely a misconfiguration
            CreateResult(
                isOk: config.Networks.Any(),
                id: id,
                errorCode: "WG0034",
                subContext: "Network",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: "VM has no network interface configured — completely isolated from network",
                descriptionOk: $"VM has {config.Networks.Count()} network interface(s) configured",
                compliance: []);
            #endregion

            #region Firewall and IP filter
            // Firewall is null when its fetch failed — the failure was already recorded, so skip.
            if (fetch.Firewall != null) { CheckVmFirewall(fetch.Firewall, id, DiagnosticResultContext.Qemu); }
            #endregion

            #region USB/PCI passthrough
            // USB or PCI passthrough binds the VM to a specific node — prevents live migration and HA failover
            var passthroughKeys = config.ExtensionData?.Keys
                .Where(k => k.StartsWith("usb", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("hostpci", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? [];

            CreateResult(
                isOk: passthroughKeys.Count == 0,
                id: id,
                errorCode: "WG0012",
                subContext: "Hardware",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: $"VM has USB/PCI passthrough configured ({string.Join(", ", passthroughKeys)}) — live migration and HA failover are not possible",
                descriptionOk: "VM has no USB/PCI passthrough configured",
                compliance: []);
            #endregion

            await CheckCommonVmAsync(settings,
                                     settings.Qemu,
                                     config,
                                     fetch.Pending,
                                     fetch.Snapshots,
                                     await client.Nodes[item.Node].Qemu[item.VmId].Rrddata.GetAsync(settings.Qemu.Rrd.TimeFrame, settings.Qemu.Rrd.Consolidation)
                                                 .ToSafeEnum(_result, id, DiagnosticResultContext.Qemu, $"RRD data for VM {item.VmId}"),
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

        CreateResultPerItem(
            items: allMacs.GroupBy(x => x.Mac)
                                       .Where(g => g.Count() > 1)
                                       .SelectMany(g => g.Select(e => new { Entry = e, Group = g.ToList() }))
                                       .ToList(),
            isItemOk: _ => false,
            itemId: x => x.Entry.Url,
            itemDescriptionKo: x => $"Duplicate MAC address {x.Entry.Mac} shared with VM(s) {string.Join(", ", x.Group.Where(o => o.VmId != x.Entry.VmId).Select(o => o.VmId))} — causes network conflicts",
            aggregatedIdOk: "cluster/vms",
            aggregatedDescriptionOk: _ => "No duplicate MAC addresses across VMs",
            errorCode: "WG0033",
            subContext: "Network",
            context: DiagnosticResultContext.Qemu,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance:
            [
                ComplianceControls.Iso27001.A_8_20,
                ComplianceControls.Iso27001.A_8_22,
                ComplianceControls.Nis2.Art_21_e,
                ComplianceControls.Gdpr.Art_5_1_f,
                ComplianceControls.Cis.C_12,
                ComplianceControls.NistCsf.PR_IR_01,
                ComplianceControls.Iso27017.CLD_13_1_4,
            ]);

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
            CreateResult(
                isOk: !config.AgentEnabled,
                id: id,
                errorCode: "WG0014",
                subContext: "Agent",
                context: DiagnosticResultContext.Qemu,
                gravityKo: DiagnosticResultGravity.Warning,
                descriptionKo: "Template has QEMU agent enabled — agent is unused on templates and clones will inherit this setting",
                descriptionOk: "Template does not have QEMU agent enabled",
                compliance: []);
            #endregion
        }
    }

    // IG0016 helpers — parse "pc-<family>-<version>" identifiers and rank versions
    // numerically (so "8.10" sorts after "8.2"), tolerating optional pve-vendor suffixes
    // (e.g. "pc-i440fx-8.0+pve0"). "pc-i440fx-latest", "q35" (no version) and similar
    // intentional aliases return false and are skipped.

    private static bool TryParseMachineVersion(string? machine, out string family, out string version)
    {
        family = "";
        version = "";
        if (string.IsNullOrWhiteSpace(machine)) { return false; }
        var match = System.Text.RegularExpressions.Regex.Match(machine, @"^(?<family>[a-z][a-z0-9-]*?)-(?<ver>\d+(?:\.\d+)+)(?:\+[a-z0-9.+-]+)?$");
        if (!match.Success) { return false; }
        family = match.Groups["family"].Value;
        version = match.Groups["ver"].Value;
        return true;
    }

    private static bool TryFindLatestVersion(IEnumerable<Helpers.NodeCapabilitiesQemuMachine> machines,
                                             string family,
                                             out string latestVersion)
    {
        latestVersion = "";
        string? best = null;
        foreach (var m in machines)
        {
            if (string.IsNullOrWhiteSpace(m.Id)) { continue; }
            if (!TryParseMachineVersion(m.Id, out var f, out var v)) { continue; }
            if (!string.Equals(f, family, StringComparison.OrdinalIgnoreCase)) { continue; }
            if (best == null || CompareMachineVersions(v, best) > 0) { best = v; }
        }
        if (best == null) { return false; }
        latestVersion = best;
        return true;
    }

    private static int CompareMachineVersions(string a, string b)
    {
        var pa = a.Split('.');
        var pb = b.Split('.');
        var len = Math.Max(pa.Length, pb.Length);
        for (var i = 0; i < len; i++)
        {
            var ia = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
            var ib = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
            if (ia != ib) { return ia.CompareTo(ib); }
        }
        return 0;
    }
}
