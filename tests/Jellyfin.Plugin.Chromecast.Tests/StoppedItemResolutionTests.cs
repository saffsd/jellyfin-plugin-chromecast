using System;
using Jellyfin.Plugin.Chromecast.Session;
using Xunit;

namespace Jellyfin.Plugin.Chromecast.Tests;

/// <summary>
/// The receiver blanks ItemId in the playbackstop it broadcasts when a clip reaches its end.
/// Jellyfin's SessionManager.OnPlaybackStopped looks the item up by id, gets nothing for an empty
/// one, and returns before it clears NowPlayingItem - leaving the session reporting a finished
/// item as still playing. These cover the fallback that lets the stop land anyway.
/// </summary>
public class StoppedItemResolutionTests
{
    private static readonly Guid Playing = Guid.Parse("a3e018d2-c569-5998-d5e4-3e7f36345480");
    private static readonly Guid Other = Guid.Parse("cd104e20-380f-d2ae-c0e6-ca9b34fe2433");

    [Fact]
    public void AReportedItemIdIsAlwaysUsedAsIs()
    {
        // The common case: mid-playback transitions carry a real id and must not be second-guessed.
        Assert.Equal(Playing, ChromecastSessionController.ResolveStoppedItemId(Playing, Other, Other));
    }

    [Fact]
    public void ABlankItemIdFallsBackToWhatTheReceiverWasLastSeenPlaying()
    {
        // End of a clip: ItemId blanked, so the last item the receiver reported identifies it.
        Assert.Equal(Playing, ChromecastSessionController.ResolveStoppedItemId(Guid.Empty, Playing, Guid.Empty));
    }

    [Fact]
    public void ABlankItemIdFallsBackToTheSessionsOwnItemWhenNothingElseIsKnown()
    {
        // Nothing seen from the receiver, but the server itself knows what is on screen.
        Assert.Equal(Playing, ChromecastSessionController.ResolveStoppedItemId(Guid.Empty, Guid.Empty, Playing));
    }

    [Fact]
    public void TheReceiversReportWinsOverTheSessionsStaleView()
    {
        // After a queue advance the session may still show the previous item; the receiver is the
        // authority on what it actually played.
        Assert.Equal(Other, ChromecastSessionController.ResolveStoppedItemId(Guid.Empty, Other, Playing));
    }

    [Fact]
    public void WithNothingKnownTheStopIsLeftAloneRatherThanInvented()
    {
        // Inventing an id here would stop playback of an item that was never playing.
        Assert.Equal(Guid.Empty, ChromecastSessionController.ResolveStoppedItemId(Guid.Empty, Guid.Empty, Guid.Empty));
    }
}
