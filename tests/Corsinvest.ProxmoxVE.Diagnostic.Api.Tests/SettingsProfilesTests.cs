/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// The --fast / --full profiles. Standard must stay identical to the defaults, so running
/// without options behaves as before the profiles existed.
/// </summary>
public class SettingsProfilesTests
{
    [Fact]
    public void Standard_is_the_defaults()
    {
        var standard = Settings.Standard();
        var defaults = new Settings();

        Assert.Equal(defaults.Backup.Enabled, standard.Backup.Enabled);
        Assert.Equal(defaults.Snapshot.Enabled, standard.Snapshot.Enabled);
        Assert.Equal(defaults.Node.Smart.Enabled, standard.Node.Smart.Enabled);
        Assert.Equal(defaults.Node.NodeStorage.ZfsDetail, standard.Node.NodeStorage.ZfsDetail);
        Assert.Equal(defaults.Node.NodeStorage.LvmThinMetadata, standard.Node.NodeStorage.LvmThinMetadata);
        Assert.Equal(defaults.Cve.NvdEnabled, standard.Cve.NvdEnabled);
        Assert.Equal(defaults.IncludeOkResult, standard.IncludeOkResult);
    }

    [Fact]
    public void Fast_skips_the_heavy_reads()
    {
        var fast = Settings.Fast();

        Assert.False(fast.Backup.Enabled);
        Assert.False(fast.Snapshot.Enabled);
        Assert.False(fast.Node.NodeStorage.LvmThinMetadata);
        Assert.False(fast.Node.Smart.Enabled);
        Assert.False(fast.Node.NodeStorage.ZfsDetail);
        Assert.False(fast.Cve.NvdEnabled);
    }

    [Fact]
    public void Full_turns_on_every_optional_check()
    {
        var full = Settings.Full();

        Assert.True(full.Backup.Enabled);
        Assert.True(full.Snapshot.Enabled);
        Assert.True(full.Node.NodeStorage.LvmThinMetadata);
        Assert.True(full.Node.Smart.Enabled);
        Assert.True(full.Node.NodeStorage.ZfsDetail);
        Assert.True(full.Cve.NvdEnabled);
        Assert.True(full.IncludeOkResult);
    }
}
