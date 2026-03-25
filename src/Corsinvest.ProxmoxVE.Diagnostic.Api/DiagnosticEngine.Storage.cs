/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

public partial class DiagnosticEngine
{
    private record StorageContent(string Id,
                                  string Volume,
                                  string Storage,
                                  long VmId,
                                  string FileName,
                                  long Size);

    private async Task CheckStorageAsync(List<ClusterResource> resources, Dictionary<long, VmConfig> vmConfigs)
    {
        // Storage not reachable from the node — VMs on that node cannot read/write
        _result.AddRange(resources.Where(a => a.ResourceType == ClusterResourceType.Storage && !a.IsAvailable)
                                 .Select(a => new DiagnosticResult
                                 {
                                     Id = a.GetWebUrl(),
                                     ErrorCode = "CS0001",
                                     Description = "Storage not available",
                                     Context = DiagnosticResultContext.Storage,
                                     SubContext = "Status",
                                     Gravity = DiagnosticResultGravity.Critical,
                                 }));

        // Storage usage above configured Warning/Critical thresholds
        CheckThreshold(_result,
                       settings.Storage.Threshold,
                       "WS0001",
                       DiagnosticResultContext.Storage,
                       "Usage",
                       resources.Where(a => a.ResourceType == ClusterResourceType.Storage && a.IsAvailable)
                                .Select(a => new ThresholdDataPoint(Convert.ToDouble(a.DiskUsage),
                                                                    Convert.ToDouble(a.DiskSize),
                                                                    a.GetWebUrl(),
                                                                    "Storage")),
                       false,
                       true);

        #region Orphaned Images and Backups
        // Disk images present in storage but not attached to any VM or LXC (wasted space)
        var activeVmIds = resources.Where(a => a.ResourceType == ClusterResourceType.Vm)
                                   .Select(a => a.VmId)
                                   .ToHashSet();
        var storagesImages = new List<StorageContent>();
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Storage
                                                  && a.IsAvailable
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

            // Backup files whose VMID no longer exists in the cluster — orphaned backups waste storage
            if (settings.Backup.Enabled && item.Content.Split(",").Contains("backup"))
            {
                var content = await nodeApi.Storage[item.Storage].Content.GetAsync(content: "backup");
                _result.AddRange(content.Where(a => !activeVmIds.Contains(a.VmId))
                                        .DistinctBy(a => a.Volume)
                                        .Select(a => new DiagnosticResult
                                        {
                                            Id = item.GetWebUrl(),
                                            ErrorCode = "WS0003",
                                            Description = $"Orphaned backup {FormatHelper.FromBytes(a.Size)} '{a.FileName}' — VMID {a.VmId} no longer exists",
                                            Context = DiagnosticResultContext.Storage,
                                            SubContext = "Backup",
                                            Gravity = DiagnosticResultGravity.Warning,
                                        }));
            }
        }

        // Deduplicate volumes seen from multiple nodes (shared storage)
        storagesImages = [.. storagesImages.DistinctBy(a => a.Volume)];

        // Remove volumes that are actually attached to a VM/LXC disk
        // At the same time accumulate allocated disk size per storage for thin provisioning check
        var allocatedByStorage = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
        {
            var config = vmConfigs[item.VmId];

            foreach (var disk in config.Disks)
            {
                storagesImages.RemoveAll(a => a.VmId == item.VmId
                                              && a.Storage == disk.Storage
                                              && a.FileName == disk.FileName);

                // Parse PVE disk size string (e.g. "32G", "500M") to bytes
                // Exclude LXC mount points (mp*): they may be bind mounts reporting
                // the full device/pool capacity rather than thin-allocated size.
                // Only count volumes with no explicit MountPoint (QEMU disks, LXC rootfs).
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

        _result.AddRange(storagesImages.Select(a => new DiagnosticResult
        {
            Id = a.Id,
            ErrorCode = "WS0002",
            Description = $"Image Orphaned {FormatHelper.FromBytes(a.Size)} file {a.FileName}",
            Context = DiagnosticResultContext.Storage,
            SubContext = "Image",
            Gravity = DiagnosticResultGravity.Warning,
        }));
        #endregion

        #region Thin provisioning overcommit
        // Thin-provisioned storage (LVM-thin, ZFS, Ceph RBD) allows allocating more disk space to VMs
        // than physically available. If the sum of all VM disk sizes exceeds the storage capacity
        // the storage will silently fill up and VMs will crash or freeze.
        var thinTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "lvmthin", "zfspool", "rbd", "cephfs" };

        foreach (var storage in resources.Where(a => a.ResourceType == ClusterResourceType.Storage
                                                     && a.IsAvailable
                                                     && thinTypes.Contains(a.PluginType ?? string.Empty)
                                                     && a.DiskSize > 0))
        {
            if (!allocatedByStorage.TryGetValue(storage.Storage, out var allocated)) { continue; }
            if (allocated > (long)storage.DiskSize)
            {
                var overcommitPct = Math.Round((double)allocated / storage.DiskSize * 100.0, 1);
                _result.Add(new DiagnosticResult
                {
                    Id = storage.GetWebUrl(),
                    ErrorCode = "WS0004",
                    Description = $"Storage '{storage.Storage}' is overcommitted: {FormatHelper.FromBytes(allocated)} allocated vs {FormatHelper.FromBytes(storage.DiskSize)} physical ({overcommitPct}%)",
                    Context = DiagnosticResultContext.Storage,
                    SubContext = "ThinOvercommit",
                    Gravity = DiagnosticResultGravity.Warning,
                });
            }
        }
        #endregion

        #region Shared storage used by only one node
        // Shared storage types (NFS, iSCSI, Ceph, etc.) are meant to be accessible from multiple nodes.
        // If a shared storage appears on only one node it may indicate a misconfiguration or a mount failure
        // on the other nodes, defeating the purpose of the shared storage.
        var totalOnlineNodes = resources.Count(a => a.ResourceType == ClusterResourceType.Node && a.IsOnline);
        if (totalOnlineNodes > 1)
        {
            var sharedStorageTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "nfs", "cifs", "cephfs", "rbd", "iscsi", "iscsidirect", "glusterfs" };

            var storagesByName = resources.Where(a => a.ResourceType == ClusterResourceType.Storage && a.IsAvailable)
                                          .GroupBy(a => a.Storage)
                                          .Where(g => sharedStorageTypes.Contains(g.First().PluginType ?? string.Empty) && g.Count() == 1)
                                          .Select(g => g.First());

            _result.AddRange(storagesByName.Select(a => new DiagnosticResult
            {
                Id = a.GetWebUrl(),
                ErrorCode = "WS0005",
                Description = $"Shared storage '{a.Storage}' (type: {a.PluginType}) is only mounted on node '{a.Node}' — other nodes cannot access it",
                Context = DiagnosticResultContext.Storage,
                SubContext = "Shared",
                Gravity = DiagnosticResultGravity.Warning,
            }));
        }
        #endregion
    }
}
