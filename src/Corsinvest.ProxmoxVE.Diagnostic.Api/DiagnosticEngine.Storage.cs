/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Storage;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Compliance;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private const string StoragePluginPbs = "pbs";

    private static readonly HashSet<string> _thinProvisioningTypes = new(StringComparer.OrdinalIgnoreCase)
        { "lvmthin", "zfspool", "rbd", "cephfs" };

    private static readonly HashSet<string> _sharedStorageTypes = new(StringComparer.OrdinalIgnoreCase)
        { "nfs", "cifs", "cephfs", "rbd", "iscsi", "iscsidirect", "glusterfs" };

    private static bool IsPbsPluginType(string pluginType)
        => string.Equals(pluginType, StoragePluginPbs, StringComparison.OrdinalIgnoreCase);

    private bool IsPbsStorage(string storageName)
        => !string.IsNullOrWhiteSpace(storageName)
           && _storageResources.Any(s => s.Storage == storageName && IsPbsPluginType(s.PluginType));

    private record StorageContent(string Id,
                                  string Node,
                                  bool Shared,
                                  string Volume,
                                  string Storage,
                                  long VmId,
                                  string FileName,
                                  long Size);

    // /storage (the storage configuration): retention, disabled flag and node restriction, none of
    // which /cluster/resources carries. Used by the storage and the backup job checks: fetched once.
    private Task<IReadOnlyList<StorageItem>?>? _storageConfigTask;
    private Task<IReadOnlyList<StorageItem>?> GetStorageConfigAsync()
        => _storageConfigTask ??= client.Storage.GetAsync().ToSafeEnumOrNull(_result, "cluster/storage", DiagnosticResultContext.Storage, "storage configuration");

    // Nodes a storage is restricted to by its 'nodes' option; empty = every node.
    private static string[] StorageNodes(StorageItem? config)
        => (config?.Nodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // RAM state saved by a snapshot with RAM or by hibernation (vm-100-state-<name>): referenced from
    // the snapshot or pending sections, not from the disks of the current config.
    [GeneratedRegex(@"^vm-\d+-state-", RegexOptions.IgnoreCase)]
    private static partial Regex VmStateVolumeRegex();

    private async Task CheckStorageAsync()
    {
        var storageConfig = await GetStorageConfigAsync();

        // A storage disabled on purpose is left out of /cluster/resources, so it is read from the
        // configuration. Disabling is not a fault in itself: it is reported only while an enabled
        // backup job or a guest disk still points at it — those will fail.
        var usedBy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        void AddUse(string? storage, string who)
        {
            if (string.IsNullOrWhiteSpace(storage)) { return; }
            if (!usedBy.TryGetValue(storage, out var list)) { usedBy[storage] = list = []; }
            if (!list.Contains(who)) { list.Add(who); }
        }
        foreach (var job in _clusterBackups.Where(a => a.Enabled)) { AddUse(job.Storage, $"backup job '{job.Id}'"); }
        foreach (var (vmId, config) in _vmConfigs)
        {
            foreach (var disk in config.DisksAll) { AddUse(disk.Storage, $"guest {vmId}"); }
        }

        CreateResultPerItem(
            items: (storageConfig ?? []).Where(a => a.Disable).ToList(),
            isItemOk: a => !usedBy.ContainsKey(a.Storage),
            itemId: a => $"cluster/storage/{a.Storage}",
            itemDescriptionKo: a => $"Storage '{a.Storage}' is disabled but still used by {string.Join(", ", usedBy[a.Storage])}",
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "No disabled storage is used by a backup job or a guest",
            errorCode: "WS0008",
            subContext: "Status",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Iso27001.A_8_16,
                ComplianceControls.Nis2.Art_21_f,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.Ens.OP_MON_3,
                ComplianceControls.Iso27017.CLD_6_3_1,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.Acn.ID_IM_04,
                ComplianceControls.Acn.DE_CM_01,
                ComplianceControls.Iso22301.C_8_3_5,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
            ]);

        // Storage not reachable from the node — VMs on that node cannot read/write.
        // Every node is checked: a shared storage can be down on one node only.
        // Storages disabled on purpose are not in /cluster/resources (handled above).
        CreateResultPerItem(
            items: _resources.Where(a => a.ResourceType == ClusterResourceType.Storage)
                             .OrderBy(a => a.Node, StringComparer.Ordinal)
                             .ToList(),
            isItemOk: a => a.IsAvailable,
            itemId: a => a.GetWebUrl(),
            itemDescriptionKo: a => $"Storage not available on node '{a.Node}'",
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "All non-disabled storages are reachable",
            errorCode: "CS0001",
            subContext: "Status",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Iso27001.A_8_16,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.Ens.OP_MON_3,
                ComplianceControls.Iso27017.CLD_6_3_1,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.Acn.ID_IM_04,
                ComplianceControls.Acn.DE_CM_01,
                ComplianceControls.Iso22301.C_8_3_5,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
            ]);

        // Storage usage above configured Warning/Critical thresholds
        CheckThreshold(
            threshold: settings.Storage.Threshold,
            errorCode: "WS0001",
            context: DiagnosticResultContext.Storage,
            subContext: "Usage",
            data: _storageResources.Where(a => a.IsAvailable)
                                    .Select(a => new ThresholdDataPoint(Convert.ToDouble(a.DiskUsage),
                                                                        Convert.ToDouble(a.DiskSize),
                                                                        a.GetWebUrl(),
                                                                        "Storage")),
            isValue: false,
            formatByte: true,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Iso27001.A_8_16,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.Ens.OP_PL_4,
                ComplianceControls.Iso27017.CLD_6_3_1,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.Acn.ID_IM_04,
                ComplianceControls.Acn.DE_CM_01,
                ComplianceControls.Iso22301.C_8_3_5,
                ComplianceControls.Iso22301.C_8_3_4,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A17,
                ComplianceControls.BsiGrundschutz.SYS_1_8_A13,
            ]);

        #region Orphaned Images and Backups
        // _storageResources is already deduplicated: shared appears once, non-shared once per node.
        // One content call per storage returns disk images, container volumes and backups together.
        var storagesImages = new List<StorageContent>();
        foreach (var item in _storageResources.Where(a => a.IsAvailable && a.Content != null))
        {
            var contentTypes = item.Content.Split(',');
            var wantImages = _orphanChecksEnabled && (contentTypes.Contains("images") || contentTypes.Contains("rootdir"));
            var wantBackups = _backupChecksEnabled && contentTypes.Contains("backup");
            if (!wantImages && !wantBackups) { continue; }

            // Populate _sharedStorageNames for use in BackupStorageKey (CheckCommonAsync)
            if (item.Shared) { _sharedStorageNames.Add(item.Storage); }

            var content = await client.Nodes[item.Node].Storage[item.Storage].Content.GetAsync()
                                      .ToSafeEnumOrNull(_result, item.GetWebUrl(), DiagnosticResultContext.Storage, $"content of storage '{item.Storage}'");

            if (wantImages && content != null)
            {
                // images = VM disks, rootdir = container volumes (subvol-*, and raw images on
                // storages that hold both).
                storagesImages.AddRange(content.Where(a => a.Content is "images" or "rootdir")
                                               .Select(a => new StorageContent(item.GetWebUrl(),
                                                                               item.Node,
                                                                               item.Shared,
                                                                               a.Volume,
                                                                               item.Storage,
                                                                               a.VmId,
                                                                               a.FileName,
                                                                               a.Size)));
            }

            if (wantBackups)
            {
                // An unreadable storage is not an empty one: remember it, so the per-guest backup
                // checks skip it instead of reporting "No recent backups found!" for every guest.
                var storageKey = BackupStorageKey(item.Node, item.Storage);
                if (content == null) { _backupContentUnavailable.Add(storageKey); }
                else { _backupContentByStorage[storageKey] = [.. content.Where(a => a.Content == "backup")]; }
            }
        }

        // Skipped when the account cannot see every guest (VM.Audit, see CheckPermissionsAsync):
        // the disks and backups of the guests it cannot see would all look orphaned.
        if (_orphanChecksEnabled)
        {
            // Backup files whose VMID no longer exists in the cluster — orphaned backups waste storage.
            // Every existing guest counts, including those whose config could not be read.
            // One finding per guest and storage, not per backup file: a retention of 30 would
            // otherwise give 30 findings for the same deleted guest.
            var orphanedBackups = _backupContentByStorage
                .SelectMany(kv => kv.Value
                                    .Where(b => !_existingGuestIds.Contains(b.VmId))
                                    .GroupBy(b => b.VmId)
                                    .Select(g => (StorageKey: kv.Key, VmId: g.Key, Backups: g.ToList())))
                .ToList();
            CreateResultPerItem(
                items: orphanedBackups,
                isItemOk: _ => false,
                itemId: ob =>
                {
                    // storageKey is either "<storage>" (shared) or "<node>/<storage>" (non-shared)
                    var parts = ob.StorageKey.Split('/');
                    var node = parts.Length == 2 ? parts[0] : _storageResources.FirstOrDefault(s => s.Storage == ob.StorageKey)?.Node ?? "";
                    var storage = parts.Length == 2 ? parts[1] : ob.StorageKey;
                    return _storageResources.FirstOrDefault(s => s.Node == node && s.Storage == storage)?.GetWebUrl() ?? $"nodes/{node}/storage/{storage}";
                },
                itemDescriptionKo: ob => ob.Backups.Count == 1
                                            ? $"Orphaned backup {FormatHelper.FromBytes(ob.Backups[0].Size)} '{ob.Backups[0].FileName}' — VMID {ob.VmId} no longer exists"
                                            : $"{ob.Backups.Count} orphaned backups ({FormatHelper.FromBytes(ob.Backups.Sum(b => b.Size))}) — VMID {ob.VmId} no longer exists",
                aggregatedIdOk: "cluster/storage",
                aggregatedDescriptionOk: _ => "No orphaned backup files found on any storage",
                errorCode: "WS0003",
                subContext: "Backup",
                context: DiagnosticResultContext.Storage,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance: []);

            // Volumes referenced by a guest config — every entry (data disks, CD-ROM, cloud-init,
            // unused) — with the nodes of the guests that reference them.
            var referencedOn = new Dictionary<(string Storage, string FileName), List<(long VmId, string Node)>>();
            foreach (var item in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
            {
                foreach (var disk in _vmConfigs[item.VmId].DisksAll.Where(d => !string.IsNullOrWhiteSpace(d.Storage)))
                {
                    var key = (disk.Storage, disk.FileName);
                    if (!referencedOn.TryGetValue(key, out var owners)) { referencedOn[key] = owners = []; }
                    owners.Add((item.VmId, item.Node));
                }
            }

            // A volume is attached when a guest references it and the volume is where the guest can
            // use it: on shared storage, on the guest's node, or on a node it is replicated to.
            // A copy left on another node (after a migration, or a deleted replication job) is not.
            bool IsAttached(StorageContent a)
                => referencedOn.TryGetValue((a.Storage, a.FileName), out var owners)
                   && owners.Any(o => a.Shared || o.Node == a.Node || _replicatedVmIds.Contains(o.VmId));

            CreateResultPerItem(
                items: storagesImages.Where(a => !IsAttached(a)
                                                 // config unreadable: its volumes are unknown, not orphaned
                                                 && !(_existingGuestIds.Contains(a.VmId) && !_vmConfigs.ContainsKey(a.VmId))
                                                 // RAM state of a snapshot or of a hibernated guest
                                                 && !(_existingGuestIds.Contains(a.VmId) && VmStateVolumeRegex().IsMatch(a.FileName)))
                                     .ToList(),
                isItemOk: _ => false,
                itemId: a => a.Id,
                itemDescriptionKo: a => $"Image Orphaned {FormatHelper.FromBytes(a.Size)} file {a.FileName}",
                aggregatedIdOk: "cluster/storage",
                aggregatedDescriptionOk: _ => "No orphaned disk images found",
                errorCode: "WS0002",
                subContext: "Image",
                context: DiagnosticResultContext.Storage,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance: []);
        }
        #endregion

        // Allocated disk size per storage for the thin provisioning check — only real data disks
        // count, CD-ROM/cloud-init are not provisioned. Keyed like _storageResources: a non-shared
        // storage is a separate pool on every node, so each node counts only its own guests.
        // Exclude LXC mount points (mp*): they may be bind mounts reporting the full device/pool
        // capacity rather than thin-allocated size. Only volumes with no explicit MountPoint count
        // (QEMU disks, LXC rootfs).
        var sharedNames = _storageResources.Where(s => s.Shared).Select(s => s.Storage).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string AllocationKey(string node, string storage) => sharedNames.Contains(storage) ? storage : $"{node}/{storage}";
        var allocatedByStorage = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
        {
            foreach (var disk in _vmConfigs[item.VmId].Disks.Where(d => string.IsNullOrWhiteSpace(d.MountPoint)
                                                                         && !string.IsNullOrWhiteSpace(d.Storage)
                                                                         && d.SizeBytes > 0))
            {
                var key = AllocationKey(item.Node, disk.Storage);
                allocatedByStorage[key] = allocatedByStorage.GetValueOrDefault(key) + disk.SizeBytes;
            }
        }

        #region Thin provisioning overcommit
        // Thin-provisioned storage (LVM-thin, ZFS, Ceph RBD) allows allocating more disk space to VMs
        // than physically available. If the sum of all VM disk sizes exceeds the storage capacity
        // the storage will silently fill up and VMs will crash or freeze.
        CreateResultPerItem(
            items: _storageResources.Where(a => a.IsAvailable
                                                  && _thinProvisioningTypes.Contains(a.PluginType ?? "")
                                                  && a.DiskSize > 0
                                                  && allocatedByStorage.ContainsKey(AllocationKey(a.Node, a.Storage))).ToList(),
            isItemOk: s => allocatedByStorage[AllocationKey(s.Node, s.Storage)] <= (long)s.DiskSize,
            itemId: s => s.GetWebUrl(),
            itemDescriptionKo: s =>
            {
                var allocated = allocatedByStorage[AllocationKey(s.Node, s.Storage)];
                var pct = Math.Round((double)allocated / s.DiskSize * 100.0, 1);
                return $"Storage '{s.Storage}' is overcommitted: {FormatHelper.FromBytes(allocated)} allocated vs {FormatHelper.FromBytes(s.DiskSize)} physical ({pct}%)";
            },
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "No thin-provisioned storage is overcommitted",
            errorCode: "WS0004",
            subContext: "ThinOvercommit",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Gdpr.Art_32_1_b,
                ComplianceControls.Ens.OP_PL_4,
                ComplianceControls.Soc2.A1_1,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.Acn.ID_IM_04,
                ComplianceControls.Iso22301.C_8_3_5,
                ComplianceControls.Iso22301.C_8_3_4,
                ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ComplianceControls.BsiGrundschutz.SYS_1_8_A13,
            ]);
        #endregion

        #region No storage with backup content type
        // If no storage in the cluster has 'backup' as a content type, vzdump has nowhere to save backups.
        var hasBackupStorage = _storageResources.Any(a => a.Content?.Split(',').Contains("backup") is true);
        CreateResult(
            isOk: hasBackupStorage,
            id: "cluster",
            errorCode: "WS0006",
            subContext: "Backup",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            descriptionKo: "No storage has 'backup' content type configured — backups cannot be stored",
            descriptionOk: "At least one storage is configured with 'backup' content type",
            compliance:
            [
                ComplianceControls.Iso27001.A_8_13,
                ComplianceControls.Nis2.Art_21_c,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.Gdpr.Art_32_1_c,
                ComplianceControls.AgId.ABSC_10_1,
                ComplianceControls.AgId.ABSC_10_3,
                ComplianceControls.AgId.ABSC_10_4,
                ComplianceControls.Ens.MP_INFO_6,
                ComplianceControls.C5.OPS_06,
                ComplianceControls.Soc2.A1_2,
                ComplianceControls.Nist80053.CP_9,
                ComplianceControls.Iso27018.A_12_3_1,
                ComplianceControls.Cis.C_11,
                ComplianceControls.NistCsf.PR_DS_11,
                ComplianceControls.NistCsf.RC_RP_01,
                ComplianceControls.Acn.PR_DS_11,
                ComplianceControls.Iso22301.C_8_3_5,
                ComplianceControls.BsiGrundschutz.CON_3_A5,
            ]);
        #endregion

        #region Backup storage not reachable from all nodes
        // A backup job targets a specific storage. If that storage is not mounted on the node
        // where a VM resides, the backup will fail for that VM. Disabled jobs never run, and a job
        // restricted with 'node' runs only there.
        var onlineNodeNames = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                        .Select(a => a.Node)
                                        .ToList();
        var jobNodePairs = _clusterBackups
            .Where(b => b.Enabled && !string.IsNullOrWhiteSpace(b.Storage))
            .SelectMany(job => onlineNodeNames.Where(node => string.IsNullOrWhiteSpace(job.Node)
                                                             || job.Node.Split(',').Select(n => n.Trim()).Contains(node))
                                              .Select(node => (Job: job, Node: node)))
            .ToList();
        CreateResultPerItem(
            items: jobNodePairs,
            isItemOk: jn => _resources.Any(r => r.ResourceType == ClusterResourceType.Storage
                                                 && r.Node == jn.Node
                                                 && r.Storage == jn.Job.Storage
                                                 && r.IsAvailable),
            itemId: jn => $"nodes/{jn.Node}",
            itemDescriptionKo: jn => $"Backup job '{jn.Job.Id}' storage '{jn.Job.Storage}' is not available on node '{jn.Node}' — VMs on this node will not be backed up",
            aggregatedIdOk: "cluster",
            aggregatedDescriptionOk: _ => "All backup job storages are reachable from every online node",
            errorCode: "WS0007",
            subContext: "Backup",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance:
            [
                ComplianceControls.Iso27001.A_8_13,
                ComplianceControls.Nis2.Art_21_c,
                ComplianceControls.Dora.Art_11,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.Gdpr.Art_32_1_c,
                ComplianceControls.AgId.ABSC_10_1,
                ComplianceControls.AgId.ABSC_10_3,
                ComplianceControls.AgId.ABSC_10_4,
                ComplianceControls.Ens.MP_INFO_6,
                ComplianceControls.C5.OPS_06,
                ComplianceControls.Soc2.A1_2,
                ComplianceControls.Nist80053.CP_9,
                ComplianceControls.Iso27018.A_12_3_1,
                ComplianceControls.Cis.C_11,
                ComplianceControls.NistCsf.PR_DS_11,
                ComplianceControls.NistCsf.RC_RP_01,
                ComplianceControls.Acn.PR_DS_11,
                ComplianceControls.Iso22301.C_8_3_5,
                ComplianceControls.BsiGrundschutz.CON_3_A5,
            ]);
        #endregion

        #region Shared storage used by only one node
        // Shared storage types (NFS, iSCSI, Ceph, etc.) are meant to be accessible from multiple nodes.
        // If a shared storage appears on only one node it may indicate a misconfiguration or a mount failure
        // on the other nodes, defeating the purpose of the shared storage.
        var totalOnlineNodes = _resources.Count(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
        if (totalOnlineNodes > 1)
        {
            // Use full _resources to count how many nodes mount each storage.
            // Group by storage name; flag every shared-type group whose mount count is 1 — unless
            // the storage is restricted to a single node on purpose ('nodes' option).
            var sharedGroups = _resources.Where(a => a.ResourceType == ClusterResourceType.Storage && a.IsAvailable)
                                          .GroupBy(a => a.Storage)
                                          .Where(g => _sharedStorageTypes.Contains(g.First().PluginType ?? "")
                                                      && StorageNodes(storageConfig?.FirstOrDefault(c => c.Storage == g.Key)).Length != 1)
                                          .Select(g => g.First())
                                          .ToList();
            CreateResultPerItem(
                items: sharedGroups,
                isItemOk: a => _resources.Count(r => r.ResourceType == ClusterResourceType.Storage
                                                      && r.IsAvailable
                                                      && r.Storage == a.Storage) > 1,
                itemId: a => a.GetWebUrl(),
                itemDescriptionKo: a => $"Shared storage '{a.Storage}' (type: {a.PluginType}) is only mounted on node '{a.Node}' — other nodes cannot access it",
                aggregatedIdOk: "cluster/storage",
                aggregatedDescriptionOk: _ => "All shared storages are mounted on more than one node",
                errorCode: "WS0005",
                subContext: "Shared",
                context: DiagnosticResultContext.Storage,
                gravityKo: DiagnosticResultGravity.Warning,
                compliance:
                [
                    ComplianceControls.Iso27001.A_5_30,
                    ComplianceControls.Dora.Art_11,
                    ComplianceControls.Ens.OP_CONT_4,
                    ComplianceControls.Iso27017.CLD_6_3_1,
                    ComplianceControls.NistCsf.PR_IR_04,
                    ComplianceControls.Acn.ID_IM_04,
                    ComplianceControls.Iso22301.C_8_3_5,
                    ComplianceControls.BsiGrundschutz.SYS_1_5_A20,
                ]);
        }
        #endregion
    }
}
