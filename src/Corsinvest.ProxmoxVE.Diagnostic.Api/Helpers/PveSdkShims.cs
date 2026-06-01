/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

// =============================================================================
// TEMPORARY SHIM — DELETE WHEN cv4pve-api-dotnet > 9.2.0 IS RELEASED.
//
// The next SDK release will expose:
//   * Corsinvest.ProxmoxVE.Api.Shared.Models.Node.NodeCapabilitiesQemuMachine
//   * GetAsync(this PveCapabilities.PveQemu.PveMachines) in
//     Corsinvest.ProxmoxVE.Api.Extension.ModelsExtensionsAutoGen
// (already wired in cv4pve-api-generator/Languages/CSharp/GeneratorExtensionModel.cs).
//
// Removal checklist when the new SDK version is bumped in Directory.Packages.props:
//   1. Delete this entire file.
//   2. In DiagnosticEngine.Node.cs change
//      `using Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;` to
//      `using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;`
//   3. Build — any leftover reference will fail to compile.
// =============================================================================

using Corsinvest.ProxmoxVE.Api;
using Newtonsoft.Json;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Helpers;

/// <summary>
/// QEMU machine type available on the node (one entry per supported machine).
/// Returned by GET /nodes/{node}/capabilities/qemu/machines.
/// </summary>
internal class NodeCapabilitiesQemuMachine
{
    /// <summary>Full machine identifier (e.g. "pc-i440fx-8.0", "pc-q35-9.2").</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    /// <summary>Machine family (e.g. "i440fx", "q35").</summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "";

    /// <summary>QEMU version of the machine type (e.g. "8.0").</summary>
    [JsonProperty("version")]
    public string Version { get; set; } = "";

    /// <summary>Optional human-readable changelog / notes for this machine version.</summary>
    [JsonProperty("changes")]
    public string Changes { get; set; } = "";
}

/// <summary>
/// Temporary extension shims for SDK endpoints that lack a strong-typed GetAsync in cv4pve-api-dotnet 9.2.0.
/// </summary>
internal static class PveSdkShims
{
    /// <summary>
    /// List the QEMU machine types available on the node.
    /// </summary>
    public static async Task<IEnumerable<NodeCapabilitiesQemuMachine>> GetAsync(
        this global::Corsinvest.ProxmoxVE.Api.PveClient.PveNodes.PveNodeItem.PveCapabilities.PveQemu.PveMachines item)
        => (await item.Types()).ToModel<IEnumerable<NodeCapabilitiesQemuMachine>>();
}
