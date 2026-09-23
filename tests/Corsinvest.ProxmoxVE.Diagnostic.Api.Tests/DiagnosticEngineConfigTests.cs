/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Newtonsoft.Json;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// Guest and node config parsing behind CG0006/WG0041, WN0013, CN0002, WN0005, IG0002/IG0004/WG0037
/// and WG0012. The data comes from a real two-node PVE 8.4 cluster where each of these was wrong.
/// </summary>
public class DiagnosticEngineConfigTests
{
    // ----- CG0006 / WG0041: raw lxc.* entries -----
    [Fact]
    public void Raw_lxc_entries_are_read_from_the_lxc_pair_list()
    {
        // As the API returns them: "lxc": [[key, value], ...]
        var config = JsonConvert.DeserializeObject<VmConfigLxc>(
            """{"unprivileged":0,"lxc":[["lxc.apparmor.profile","unconfined"],["lxc.cgroup2.devices.allow","a"],["lxc.cap.drop",""]]}""")!;

        var entries = DiagnosticEngine.RawLxcEntries(config);

        Assert.Equal(["lxc.apparmor.profile", "lxc.cgroup2.devices.allow", "lxc.cap.drop"], entries.Select(e => e.Key));
        Assert.Contains(("lxc.apparmor.profile", "unconfined"), entries);
    }

    [Fact]
    public void Container_without_raw_entries_has_none()
        => Assert.Empty(DiagnosticEngine.RawLxcEntries(JsonConvert.DeserializeObject<VmConfigLxc>("""{"hostname":"ct"}""")!));

    // ----- WN0013: reboot for a newer kernel -----
    private static readonly string[] _installedKernels =
    [
        "proxmox-kernel-6.8",
        "proxmox-kernel-6.8.12-43-pve-signed",
        "proxmox-kernel-6.8.12-20-pve-signed",
        "proxmox-kernel-6.8.12-9-pve-signed",
        "proxmox-kernel-6.2.16-19-pve",
        "pve-manager",
    ];

    [Fact]
    public void Newer_installed_kernel_means_reboot()
        => Assert.Equal("6.8.12-43-pve", DiagnosticEngine.NewerInstalledKernel("6.8.12-20-pve", _installedKernels));

    [Fact]
    public void Running_the_newest_kernel_needs_no_reboot()
        => Assert.Null(DiagnosticEngine.NewerInstalledKernel("6.8.12-43-pve", _installedKernels));

    [Fact]
    public void Kernel_versions_compare_numerically_not_as_text()
        // 6.8.12-9 is older than 6.8.12-20 even though "9" > "2" as text.
        => Assert.Null(DiagnosticEngine.NewerInstalledKernel("6.8.12-20-pve", ["proxmox-kernel-6.8.12-9-pve-signed"]));

    // ----- CN0002: package versions -----
    private static NodeAptVersion Pkg(string package, string version) => new() { Package = package, Version = version };

    [Fact]
    public void Old_kernels_kept_on_one_node_are_not_a_version_difference()
    {
        NodeAptVersion[] cc01 = [Pkg("pve-manager", "8.4.21"), Pkg("proxmox-kernel-6.8.12-20-pve-signed", "6.8.12-20")];
        NodeAptVersion[] cc02 = [.. cc01, Pkg("proxmox-kernel-6.5.13-3-pve-signed", "6.5.13-3"), Pkg("proxmox-kernel-6.2.16-19-pve", "6.2.16-19")];

        Assert.Empty(DiagnosticEngine.PackageVersionDifferences(cc01, cc02));
        Assert.Empty(DiagnosticEngine.PackageVersionDifferences(cc02, cc01));
    }

    [Fact]
    public void Same_package_with_different_version_is_reported()
        => Assert.Equal(["pve-manager 8.4.21 vs 8.4.20"],
                        DiagnosticEngine.PackageVersionDifferences([Pkg("pve-manager", "8.4.21")], [Pkg("pve-manager", "8.4.20")]));

    // ----- WN0005: /etc/hosts -----
    [Fact]
    public void Hosts_differing_only_in_comments_and_spaces_are_equal()
    {
        // The real files: a different commented line, and a trailing space on one entry.
        string[] cc01 = ["127.0.0.1 localhost.localdomain localhost", "#185.31.65.36 ", "192.168.0.2 cc02.corsinvest.it cc02 "];
        string[] cc02 = ["127.0.0.1\tlocalhost.localdomain localhost", "#185.31.65.37", "192.168.0.2 cc02.corsinvest.it cc02", ""];

        Assert.True(DiagnosticEngine.HostsEntries(cc01).SetEquals(DiagnosticEngine.HostsEntries(cc02)));
    }

    [Fact]
    public void Hosts_with_a_different_entry_differ()
        => Assert.False(DiagnosticEngine.HostsEntries(["192.168.0.2 cc02"]).SetEquals(DiagnosticEngine.HostsEntries(["192.168.0.3 cc02"])));

    // ----- IG0004 / WG0037: CPU type -----
    [Theory]
    [InlineData(null, "kvm64")]
    [InlineData("", "kvm64")]
    [InlineData("host", "host")]
    [InlineData("x86-64-v2-AES,flags=+aes", "x86-64-v2-aes")]
    [InlineData("cputype=kvm64,flags=+pcid", "kvm64")]
    public void Cpu_type_defaults_to_kvm64_when_unset(string? cpu, string expected)
        => Assert.Equal(expected, DiagnosticEngine.CpuTypeOf(cpu));

    // ----- WG0012: passthrough -----
    [Theory]
    [InlineData("usb0", "spice", false)]
    [InlineData("usb0", "host=spice,usb3=1", false)]
    [InlineData("usb3", "1", true)]
    [InlineData("usb0", "host=1-2,usb3=1", true)]
    [InlineData("usb1", "host=046d:c52b", true)]
    [InlineData("hostpci0", "0000:01:00.0,pcie=1", true)]
    [InlineData("usbX", "spice", false)]
    public void Only_host_devices_count_as_passthrough(string key, string value, bool expected)
        => Assert.Equal(expected, DiagnosticEngine.IsHostPassthrough(key, value));

    // ----- IG0002: disk bus -----
    [Theory]
    [InlineData("ide0", true)]
    [InlineData("sata1", true)]
    [InlineData("scsi0", true)]
    [InlineData("virtio2", true)]
    [InlineData("efidisk0", false)]
    [InlineData("tpmstate0", false)]
    [InlineData("unused0", false)]
    public void Only_data_disk_buses_are_checked(string id, bool expected)
        => Assert.Equal(expected, DiagnosticEngine.IsDiskBus(id));
}
