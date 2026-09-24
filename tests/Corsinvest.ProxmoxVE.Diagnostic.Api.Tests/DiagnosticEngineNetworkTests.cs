/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// Guest NIC vs host bridge checks: WN0046 (VLAN outside bridge-vids) and WN0047 (bridge missing on a peer node).
/// </summary>
public class DiagnosticEngineNetworkTests
{
    // ----- VlanIds.Parse -----
    [Fact]
    public void Parse_reads_bridge_vids_and_trunks_formats()
    {
        Assert.Equal([(2, 4094)], VlanIds.Parse("2-4094"));
        Assert.Equal([(10, 10), (20, 20), (100, 200)], VlanIds.Parse("10 20 100-200"));
        Assert.Equal([(10, 10), (20, 20), (30, 40)], VlanIds.Parse("10;20;30-40"));
    }

    [Fact]
    public void Parse_clamps_a_range_past_the_valid_ids_instead_of_dropping_it()
    {
        // Seen on a real cluster: trunks=1-4095 on a router VM, meaning every VLAN.
        Assert.Equal([(1, 4094)], VlanIds.Parse("1-4095"));
        Assert.Equal([(1, 10)], VlanIds.Parse("0-10"));
    }

    [Fact]
    public void Format_compacts_ids_into_ranges()
    {
        Assert.Equal("2-4, 7, 9-10", VlanIds.Format([9, 2, 3, 4, 7, 10, 3]));
        Assert.Equal("", VlanIds.Format([]));
    }

    [Fact]
    public void Trunk_of_every_vlan_on_a_narrowed_bridge_reads_as_ranges()
    {
        // Seen when simulating on real data: trunks=1-4095 on bridge-vids "10 20".
        var outside = DiagnosticEngine.VlansOutsideBridge(null, "1-4095", VlanIds.Parse("10 20"));
        Assert.Equal("2-9, 11-19, 21-4094", VlanIds.Format(outside));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("4095")]
    [InlineData("20-10")]
    public void Parse_skips_empty_and_malformed_items(string? value)
        => Assert.Empty(VlanIds.Parse(value));

    // ----- WN0046 -----
    [Fact]
    public void Tag_inside_bridge_vids_is_allowed()
        => Assert.Empty(DiagnosticEngine.VlansOutsideBridge(20, null, VlanIds.Parse("2-4094")));

    [Fact]
    public void Tag_outside_narrowed_bridge_vids_is_reported()
        => Assert.Equal([30], DiagnosticEngine.VlansOutsideBridge(30, null, VlanIds.Parse("10 20")));

    [Fact]
    public void Trunk_ids_outside_bridge_vids_are_reported()
        => Assert.Equal([30, 31], DiagnosticEngine.VlansOutsideBridge(null, "10;30-31", VlanIds.Parse("10 20")));

    [Fact]
    public void Vlan_1_is_the_default_pvid_and_never_reported()
        => Assert.Empty(DiagnosticEngine.VlansOutsideBridge(1, null, VlanIds.Parse("2-4094")));

    [Fact]
    public void Untagged_nic_uses_no_vlan()
        => Assert.Empty(DiagnosticEngine.VlansOutsideBridge(null, null, VlanIds.Parse("10")));

    // ----- WN0010 -----
    [Fact]
    public void Spare_nics_are_not_in_use_while_bond_slaves_and_nics_with_an_ip_are()
    {
        // A real node: eno1/eno2 are spare ports (down, no cable), the bond runs on the np ports.
        NodeNetwork[] networks =
        [
            new() { Interface = "bond0", Type = "bond", Slaves = "eno1np0 eno2np1" },
            new() { Interface = "eno1", Type = "eth" },
            new() { Interface = "eno1np0", Type = "eth" },
            new() { Interface = "eno2", Type = "eth" },
            new() { Interface = "eno2np1", Type = "eth" },
            new() { Interface = "eno3", Type = "eth", Cidr = "185.31.65.36/24" },
            new() { Interface = "eno4", Type = "eth", Cidr = "192.168.10.1/24" },
            new() { Interface = "vmbr1", Type = "OVSBridge" },
            new() { Interface = "vmbr2", Type = "bridge", BridgePorts = "bond0", Cidr = "192.168.0.1/24" },
        ];

        var used = DiagnosticEngine.UsedInterfaces(networks);

        Assert.DoesNotContain("eno1", used);
        Assert.DoesNotContain("eno2", used);
        Assert.Contains("eno1np0", used);
        Assert.Contains("eno2np1", used);
        Assert.Contains("eno3", used);
        Assert.Contains("eno4", used);
        Assert.Contains("bond0", used);
    }

    [Fact]
    public void Nic_under_a_vlan_interface_is_in_use()
    {
        NodeNetwork[] networks =
        [
            new() { Interface = "eno1", Type = "eth" },
            new() { Interface = "eno2", Type = "eth" },
            new() { Interface = "vlan50", Type = "vlan", VlanRawDevice = "eno2" },
            new() { Interface = "vmbr0", Type = "bridge", BridgePorts = "eno1.100" },
        ];

        var used = DiagnosticEngine.UsedInterfaces(networks);

        Assert.Contains("eno1", used);
        Assert.Contains("eno2", used);
    }

    [Fact]
    public void Ovs_port_member_of_a_bridge_is_in_use()
    {
        NodeNetwork[] networks = [new() { Interface = "eno1", Type = "eth", OvsBridge = "vmbr1" }];

        Assert.Contains("eno1", DiagnosticEngine.UsedInterfaces(networks));
    }

    // ----- WN0047 -----
    private static Dictionary<string, HashSet<string>> Bridges(params (string Node, string[] Bridges)[] nodes)
        => nodes.ToDictionary(n => n.Node, n => n.Bridges.ToHashSet());

    [Fact]
    public void Bridge_missing_on_a_peer_is_reported_once_per_node_and_bridge()
    {
        var bridges = Bridges(("pve1", ["vmbr0", "vmbr5"]), ("pve2", ["vmbr0"]), ("pve3", ["vmbr0"]));
        var guests = new (string, long, IEnumerable<string>)[]
        {
            ("pve1", 101, ["vmbr0", "vmbr5"]),
            ("pve1", 100, ["vmbr5", "vmbr5"]),
        };

        var result = DiagnosticEngine.FindBridgesMissingOnPeers(bridges, guests);

        var item = Assert.Single(result);
        Assert.Equal(("pve1", "vmbr5"), (item.Node, item.Bridge));
        Assert.Equal([100, 101], item.VmIds);
        Assert.Equal(["pve2", "pve3"], item.MissingOn);
    }

    [Fact]
    public void Bridge_present_on_every_node_is_not_reported()
    {
        var bridges = Bridges(("pve1", ["vmbr0"]), ("pve2", ["vmbr0"]));
        var guests = new (string, long, IEnumerable<string>)[] { ("pve1", 100, ["vmbr0"]) };

        Assert.Empty(DiagnosticEngine.FindBridgesMissingOnPeers(bridges, guests));
    }

    [Fact]
    public void Bridge_missing_on_the_guest_node_too_is_skipped_as_possible_sdn_vnet()
    {
        var bridges = Bridges(("pve1", ["vmbr0"]), ("pve2", ["vmbr0"]));
        var guests = new (string, long, IEnumerable<string>)[] { ("pve1", 100, ["myvnet"]) };

        Assert.Empty(DiagnosticEngine.FindBridgesMissingOnPeers(bridges, guests));
    }
}
