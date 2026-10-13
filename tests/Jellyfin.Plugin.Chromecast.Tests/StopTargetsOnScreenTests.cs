using System;
using Jellyfin.Plugin.Chromecast.Session;
using Xunit;

namespace Jellyfin.Plugin.Chromecast.Tests;

/// <summary>
/// The receiver reports a stop for the previous item *after* the next one has already started
/// (visible when switching videos mid-playback). Treating every stop as "nothing is on screen"
/// marked a playing session idle, and the keep-alive then dropped the TV back to its ready screen
/// mid-cast - the reported "starts, then falls back to the ready screen" symptom.
/// </summary>
public class StopTargetsOnScreenTests
{
    private static readonly Guid Playing = Guid.Parse("a3e018d2-c569-5998-d5e4-3e7f36345480");
    private static readonly Guid Previous = Guid.Parse("b825e2e4cc675f0b9c45a920d23c61d5");

    [Fact]
    public void AStopForTheItemOnScreenEndsIt()
    {
        Assert.True(ChromecastSessionController.StopsTheItemOnScreen(Playing, Playing));
    }

    [Fact]
    public void ABlankStopIsTheReceiversOwnEndOfMediaAndEndsIt()
    {
        // The receiver blanks ItemId on its end-of-media stop.
        Assert.True(ChromecastSessionController.StopsTheItemOnScreen(Guid.Empty, Playing));
    }

    [Fact]
    public void ALateStopForThePreviousItemLeavesTheCurrentOneAlone()
    {
        // The switch-a-video case: previous item's stop arrives while the new item plays.
        Assert.False(ChromecastSessionController.StopsTheItemOnScreen(Previous, Playing));
    }

    [Fact]
    public void AStopWithNothingKnownToBePlayingIsTakenAtFaceValue()
    {
        Assert.True(ChromecastSessionController.StopsTheItemOnScreen(Playing, Guid.Empty));
        Assert.True(ChromecastSessionController.StopsTheItemOnScreen(Guid.Empty, Guid.Empty));
    }
}
