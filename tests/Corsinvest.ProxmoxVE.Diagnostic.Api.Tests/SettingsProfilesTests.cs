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
    public void Standard_reads_backups_and_snapshots_but_no_optional_detail()
    {
        // The values docs/settings.md documents for the standard profile.
        var standard = Settings.Standard();

        Assert.True(standard.Backup.Enabled);
        Assert.True(standard.Snapshot.Enabled);
        Assert.True(standard.Node.NodeStorage.LvmThinMetadata);
        Assert.False(standard.Node.Smart.Enabled);
        Assert.False(standard.Node.NodeStorage.ZfsDetail);
        Assert.False(standard.Cve.NvdEnabled);
        Assert.False(standard.IncludeOkResult);
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
