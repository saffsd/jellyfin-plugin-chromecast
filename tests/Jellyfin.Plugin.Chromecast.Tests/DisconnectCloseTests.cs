using System;
using Jellyfin.Plugin.Chromecast.Session;
using Xunit;

namespace Jellyfin.Plugin.Chromecast.Tests;

/// <summary>
/// The receiver disables CAF's own idle timeout, so if the plugin never closes it the TV sits on
/// the Jellyfin "ready to cast" screen forever after the user picks Disconnect. Closing immediately
/// on the disconnect is also wrong: a web client briefly drops its remote-player subscription while
/// the user picks a different video, and closing on that put the TV back on the ready screen
/// mid-flow. Hence the grace period.
/// </summary>
public class DisconnectCloseTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NothingHappensWhenTheClientHasNotDisconnected()
    {
        Assert.False(ChromecastSessionController.ShouldCloseAfterDisconnect(0, Now));
    }

    [Fact]
    public void AMomentaryDisconnectDoesNotCloseTheReceiver()
    {
        // The web client dropping and re-establishing its subscription while the user picks a
        // different video: activity clears the marker well before the grace period.
        Assert.False(ChromecastSessionController.ShouldCloseAfterDisconnect(Now.AddSeconds(-2).Ticks, Now));
        Assert.False(ChromecastSessionController.ShouldCloseAfterDisconnect(Now.AddSeconds(-9).Ticks, Now));
    }

    [Fact]
    public void ARealDisconnectClosesTheReceiverAfterTheGracePeriod()
    {
        Assert.True(ChromecastSessionController.ShouldCloseAfterDisconnect(Now.AddSeconds(-11).Ticks, Now));
        Assert.True(ChromecastSessionController.ShouldCloseAfterDisconnect(Now.AddMinutes(-5).Ticks, Now));
    }
}
