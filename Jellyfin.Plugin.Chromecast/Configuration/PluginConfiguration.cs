using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Chromecast.Configuration;

/// <summary>
/// Configuration for the Chromecast plugin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        DiscoveryIntervalSeconds = 30;
        DeviceStaleAfterSeconds = 120;
        DeviceNamePrefix = string.Empty;
        UseUnstableReceiver = true;
        OpenReceiverOnConnect = true;
        SkipSpeedTest = false;
        StreamingBitrateMbps = 20;
        TranscodeDolbyAudio = true;
        LastCastTargets = [];
        DeviceSettings = [];
        EnableDebugLogging = false;
    }

    /// <summary>
    /// Gets or sets how often (in seconds) the plugin scans the local network for Chromecast devices.
    /// </summary>
    public int DiscoveryIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets how long (in seconds) a Chromecast device may go unseen before its session is
    /// marked inactive and removed from the "Play On" device list.
    /// </summary>
    public int DeviceStaleAfterSeconds { get; set; }

    /// <summary>
    /// Gets or sets an optional prefix added to the device name shown in the Jellyfin cast menu,
    /// e.g. "Chromecast - " so it is easy to tell apart from other session types.
    /// </summary>
    public string DeviceNamePrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to launch the "unstable" build of Jellyfin's cast
    /// receiver (app ID 6F511C87) instead of the "stable" build (F007D354). Defaults to true because
    /// the stable build can't load external subtitle files until upstream tags a release with
    /// jellyfin-chromecast 6be49ae (see CLAUDE.md). Matches the same choice available to Chrome
    /// users under Settings &gt; Playback &gt; Google Cast version.
    /// </summary>
    public bool UseUnstableReceiver { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to open the receiver's "ready" screen as soon as a
    /// Jellyfin client connects to a remote player. Clients don't say which target they picked, so
    /// this opens the Chromecast that user cast to last (or the only one there is); a wrong guess
    /// is closed again as soon as the user sends anything to the Chromecast they actually chose.
    /// </summary>
    public bool OpenReceiverOnConnect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the receiver is told what bitrate to use
    /// (<see cref="StreamingBitrateMbps"/>) instead of measuring the connection itself. Its
    /// measurement takes 4 to 8 seconds before the first playback after the receiver opens.
    /// </summary>
    public bool SkipSpeedTest { get; set; }

    /// <summary>
    /// Gets or sets the bitrate, in megabits per second, used when <see cref="SkipSpeedTest"/> is on.
    /// </summary>
    public int StreamingBitrateMbps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Dolby audio codecs (AC-3 and E-AC-3) are
    /// removed from the transcoding profiles a casting Chromecast sends to Jellyfin, so that
    /// audio is transcoded rather than copied into fMP4 HLS segments. On by default: a Cast
    /// device's Dolby support comes from the display's HDMI EDID and is often wrong, in which
    /// case playback starts and stops immediately at position 0 with no error. Direct play of a
    /// Dolby track is unaffected - that path uses the platform demuxer and works. Only the audio
    /// is re-encoded and the video stream is copied, so this stays cheap on a low-powered server.
    /// </summary>
    public bool TranscodeDolbyAudio { get; set; }

    /// <summary>
    /// Gets or sets the Chromecast each user cast to last. Maintained by the plugin, not shown in
    /// the settings page; kept here so the connect guess survives server restarts.
    /// </summary>
    public LastCastTarget[] LastCastTargets { get; set; }

    /// <summary>
    /// Gets or sets per-Chromecast choices made in the settings page (custom name, hidden).
    /// </summary>
    public ChromecastDeviceSettings[] DeviceSettings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether routine discovery output and CastV2 protocol
    /// detail are written to the server log (see <c>VerboseGatedLogger</c>).
    /// </summary>
    public bool EnableDebugLogging { get; set; }
}

/// <summary>
/// The Chromecast a user cast to last.
/// </summary>
public class LastCastTarget
{
    /// <summary>
    /// Gets or sets the user's id.
    /// </summary>
    public System.Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the Chromecast session's device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// An admin's choices for one Chromecast.
/// </summary>
public class ChromecastDeviceSettings
{
    /// <summary>
    /// Gets or sets the Chromecast session's device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name to show in Jellyfin instead of the Chromecast's own; empty for none.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this Chromecast is left out of the cast menu.
    /// </summary>
    public bool Hidden { get; set; }
}
