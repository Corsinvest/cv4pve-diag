/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
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
                                  string Volume,
                                  string Storage,
                                  long VmId,
                                  string FileName,
                                  long Size);

    private async Task CheckStorageAsync()
    {
        // Storage deliberately disabled by the admin — not a fault, but worth surfacing
        // (backup jobs / guests may still point at it). Reported separately from a real outage below.
        CreateResultPerItem(
            items: _storageResources,
            isItemOk: a => !string.Equals(a.Status, "disabled", StringComparison.OrdinalIgnoreCase),
            itemId: a => a.GetWebUrl(),
            itemDescriptionKo: a => $"Storage '{a.Storage}' is disabled",
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "No storages are in a disabled state",
            errorCode: "WS0008",
            subContext: "Status",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance:
            [
                ComplianceControls.Iso27001.A_8_16,
                ComplianceControls.Nis2.Art_21_f,
                ComplianceControls.Gdpr.Art_32_1_b,
            ]);

        // Storage not reachable from the node — VMs on that node cannot read/write.
        // Excludes storages disabled on purpose (handled above) — those are not a fault.
        CreateResultPerItem(
            items: _storageResources.Where(a => !string.Equals(a.Status, "disabled", StringComparison.OrdinalIgnoreCase)).ToList(),
            isItemOk: a => a.IsAvailable,
            itemId: a => a.GetWebUrl(),
            itemDescriptionKo: _ => "Storage not available",
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "All non-disabled storages are reachable",
            errorCode: "CS0001",
            subContext: "Status",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Critical,
            compliance:
            [
                ComplianceControls.Iso27001.A_5_30,
                ComplianceControls.Dora.Art_12,
                ComplianceControls.NistCsf.PR_IR_04,
                ComplianceControls.Iso27017.CLD_6_3_1,
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
            ]);

        #region Orphaned Images and Backups
        // Disk images present in storage but not attached to any VM or LXC (wasted space)
        var activeVmIds = _resources.Where(a => a.ResourceType == ClusterResourceType.Vm)
                                    .Select(a => a.VmId)
                                    .ToHashSet();

        // _storageResources is already deduplicated: shared appears once, non-shared once per node.
        // No need for DistinctBy or skip logic — just iterate directly.
        var storagesImages = new List<StorageContent>();
        foreach (var item in _storageResources.Where(a => a.IsAvailable
                                                           && a.Content != null
                                                           && (a.Content.Split(",").Contains("images")
                                                               || (settings.Backup.Enabled && a.Content.Split(",").Contains("backup")))))
        {
            var nodeApi = client.Nodes[item.Node];

            if (item.Content.Split(",").Contains("images"))
            {
                var content = await nodeApi.Storage[item.Storage].Content.GetAsync(content: "images");
                storagesImages.AddRange(content.Select(a => new StorageContent(item.GetWebUrl(),
                                                                               a.Volume,
                                                                               item.Storage,
                                                                               a.VmId,
                                                                               a.FileName,
                                                                               a.Size)));
            }

            // Backup files whose VMID no longer exists in the cluster — orphaned backups waste storage.
            // Collected here per storage; the WS0003 aggregated check runs once after the loop.
            if (settings.Backup.Enabled && item.Content.Split(",").Contains("backup"))
            {
                // Populate _sharedStorageNames for use in BackupStorageKey (CheckCommonAsync)
                if (item.Shared) { _sharedStorageNames.Add(item.Storage); }
                var storageKey = BackupStorageKey(item.Node, item.Storage);
                _backupContentByStorage[storageKey] = [.. await nodeApi.Storage[item.Storage].Content.GetAsync(content: "backup")];
            }
        }

        // Orphaned backups across all storages → single aggregated WS0003 check
        var orphanedBackups = _backupContentByStorage
            .SelectMany(kv => kv.Value
                                .Where(b => !activeVmIds.Contains(b.VmId))
                                .Select(b => (StorageKey: kv.Key, Backup: b)))
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
            itemDescriptionKo: ob => $"Orphaned backup {FormatHelper.FromBytes(ob.Backup.Size)} '{ob.Backup.FileName}' — VMID {ob.Backup.VmId} no longer exists",
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "No orphaned backup files found on any storage",
            errorCode: "WS0003",
            subContext: "Backup",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);

        // Remove volumes that are actually attached to a VM/LXC — match against ALL
        // volume entries (data disks + CD-ROM + cloud-init) so e.g. vm-NNN-cloudinit
        // is not reported as orphaned (WS0002).
        // Separately, accumulate allocated disk size per storage for the thin provisioning
        // check — only real data disks count, CD-ROM/cloud-init are not provisioned.
        var allocatedByStorage = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
        {
            var config = _vmConfigs[item.VmId];

            foreach (var disk in config.DisksAll)
            {
                storagesImages.RemoveAll(a => a.VmId == item.VmId
                                              && a.Storage == disk.Storage
                                              && a.FileName == disk.FileName);
            }

            // Parse PVE disk size string (e.g. "32G", "500M") to bytes.
            // Exclude LXC mount points (mp*): they may be bind mounts reporting
            // the full device/pool capacity rather than thin-allocated size.
            // Only count volumes with no explicit MountPoint (QEMU disks, LXC rootfs).
            foreach (var disk in config.Disks)
            {
                if (string.IsNullOrWhiteSpace(disk.MountPoint)
                    && !string.IsNullOrWhiteSpace(disk.Storage)
                    && !string.IsNullOrWhiteSpace(disk.Size))
                {
                    var sizeBytes = disk.SizeBytes;
                    if (sizeBytes > 0)
                    {
                        allocatedByStorage.TryGetValue(disk.Storage, out var current);
                        allocatedByStorage[disk.Storage] = current + sizeBytes;
                    }
                }
            }
        }

        CreateResultPerItem(
            items: storagesImages,
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
        #endregion

        #region Thin provisioning overcommit
        // Thin-provisioned storage (LVM-thin, ZFS, Ceph RBD) allows allocating more disk space to VMs
        // than physically available. If the sum of all VM disk sizes exceeds the storage capacity
        // the storage will silently fill up and VMs will crash or freeze.
        CreateResultPerItem(
            items: _storageResources.Where(a => a.IsAvailable
                                                  && _thinProvisioningTypes.Contains(a.PluginType ?? "")
                                                  && a.DiskSize > 0
                                                  && allocatedByStorage.ContainsKey(a.Storage)).ToList(),
            isItemOk: s => allocatedByStorage[s.Storage] <= (long)s.DiskSize,
            itemId: s => s.GetWebUrl(),
            itemDescriptionKo: s =>
            {
                var allocated = allocatedByStorage[s.Storage];
                var pct = Math.Round((double)allocated / s.DiskSize * 100.0, 1);
                return $"Storage '{s.Storage}' is overcommitted: {FormatHelper.FromBytes(allocated)} allocated vs {FormatHelper.FromBytes(s.DiskSize)} physical ({pct}%)";
            },
            aggregatedIdOk: "cluster/storage",
            aggregatedDescriptionOk: _ => "No thin-provisioned storage is overcommitted",
            errorCode: "WS0004",
            subContext: "ThinOvercommit",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance: []);
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
                ComplianceControls.Gdpr.Art_32_1_c,
                ComplianceControls.AgId.ABSC_10_1,
                ComplianceControls.Ens.MP_INFO_6,
                ComplianceControls.Nist80053.CP_9,
                ComplianceControls.Soc2.A1_2,
                ComplianceControls.Iso27018.A_12_3_1,
                ComplianceControls.C5.OPS_21,
                ComplianceControls.AgId.ABSC_10_3,
                ComplianceControls.AgId.ABSC_10_4,
                ComplianceControls.Cis.C_11,
                ComplianceControls.NistCsf.PR_DS_11,
                ComplianceControls.NistCsf.RC_RP_01,
            ]);
        #endregion

        #region Backup storage not reachable from all nodes
        // A backup job targets a specific storage. If that storage is not mounted on the node
        // where a VM resides, the backup will fail for that VM.
        var onlineNodeNames = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline)
                                        .Select(a => a.Node)
                                        .ToList();
        var jobNodePairs = _clusterBackups
            .Where(b => !string.IsNullOrWhiteSpace(b.Storage))
            .SelectMany(job => onlineNodeNames.Select(node => (Job: job, Node: node)))
            .ToList();
        CreateResultPerItem(
            items: jobNodePairs,
            isItemOk: jn => _resources.Any(r => r.ResourceType == ClusterResourceType.Storage
                                                 && r.Node == jn.Node
                                                 && r.Storage == jn.Job.Storage
                                                 && r.IsAvailable),
            itemId: _ => "cluster",
            itemDescriptionKo: jn => $"Backup job storage '{jn.Job.Storage}' is not available on node '{jn.Node}' — VMs on this node will not be backed up",
            aggregatedIdOk: "cluster",
            aggregatedDescriptionOk: _ => "All backup job storages are reachable from every online node",
            errorCode: "WS0007",
            subContext: "Backup",
            context: DiagnosticResultContext.Storage,
            gravityKo: DiagnosticResultGravity.Warning,
            compliance:
            [
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
            // Group by storage name; flag every shared-type group whose mount count is 1.
            var sharedGroups = _resources.Where(a => a.ResourceType == ClusterResourceType.Storage && a.IsAvailable)
                                          .GroupBy(a => a.Storage)
                                          .Where(g => _sharedStorageTypes.Contains(g.First().PluginType ?? ""))
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
                compliance: []);
        }
        #endregion
    }
}
