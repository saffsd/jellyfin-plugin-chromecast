using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Chromecast.Playback;

/// <summary>
/// Stops the Dolby audio codecs (AC-3 and E-AC-3) being stream-copied into fMP4 HLS segments
/// when a casting Chromecast negotiates playback, so the server transcodes that audio instead.
/// <para>
/// Why: the Cast receiver builds its own device profile on the device and advertises AC-3 and
/// E-AC-3 based on <c>castContext.canDisplayType()</c>, which answers from the display's HDMI
/// EDID. That answer is frequently wrong. When it says yes, the server copies the raw Dolby
/// bitstream into the fMP4 segments it hands to the receiver - and HLS plays through MSE, which
/// refuses audio the platform demuxer would have handled. The result is playback that starts and
/// immediately stops at position 0, with nothing shown and no error.
/// </para>
/// <para>
/// Direct play of a Dolby track is left alone: a complete MP4 goes through the platform demuxer
/// rather than MSE, and passes the bitstream through correctly. Only the transcoding profiles are
/// edited, which is where the fMP4 HLS output is described. This keeps a fully compatible file on
/// the cheap direct-play path and only forces a transcode where the copy would have broken.
/// </para>
/// <para>
/// Re-encoding audio alone is cheap (measured ~45x realtime on an ordinary desktop CPU) and the
/// video stream is still copied, so this stays affordable on a low-powered server - unlike a
/// bitrate cap, which forces a full video re-encode.
/// </para>
/// </summary>
public sealed class DolbyAudioTranscodeFilter : IAsyncActionFilter
{
    /// <summary>
    /// The client name Jellyfin's cast receiver reports. This plugin mints the receiver's access
    /// token under the same name, so it identifies casts this plugin started.
    /// </summary>
    private const string ReceiverClient = "Chromecast";

    /// <summary>The claim Jellyfin populates with the authenticated client's name.</summary>
    private const string ClientClaim = "Jellyfin-Client";

    /// <summary>
    /// The spellings the Dolby codecs appear under in a device profile. The receiver writes
    /// "ac3"/"eac3"; the others are accepted aliases, removed defensively so a profile from a
    /// different receiver build is handled too.
    /// </summary>
    private static readonly string[] DolbyAliases =
        ["ac3", "eac3", "ec3", "ec-3", "mp4a.a5", "mp4a.a6"];

    /// <summary>Caches the reflected <c>DeviceProfile</c> property per request-DTO type.</summary>
    private static readonly Dictionary<Type, PropertyInfo?> ProfileProperties = [];

    private readonly ILogger<DolbyAudioTranscodeFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DolbyAudioTranscodeFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DolbyAudioTranscodeFilter(ILogger<DolbyAudioTranscodeFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (Plugin.Instance?.Configuration.TranscodeDolbyAudio != true)
        {
            return next();
        }

        var client = context.HttpContext.User.FindFirst(ClientClaim)?.Value;
        if (!string.Equals(client, ReceiverClient, StringComparison.OrdinalIgnoreCase))
        {
            return next();
        }

        var stripped = 0;
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var profile = GetDeviceProfile(argument);
            if (profile is not null)
            {
                stripped += StripDolbyFromTranscodingProfiles(profile);
            }
        }

        if (stripped > 0)
        {
            _logger.LogInformation(
                "Removed Dolby audio (AC-3/E-AC-3) from {Count} Chromecast transcoding profile entr{Plural}, so Jellyfin will transcode that audio instead of copying it into fMP4 HLS segments",
                stripped,
                stripped == 1 ? "y" : "ies");
        }

        return next();
    }

    /// <summary>
    /// Reads the <c>DeviceProfile</c> off a request DTO (PlaybackInfoDto, OpenLiveStreamDto, ...)
    /// without a compile-time reference to Jellyfin.Api, which is not shipped as a plugin
    /// dependency.
    /// </summary>
    private static DeviceProfile? GetDeviceProfile(object argument)
    {
        var type = argument.GetType();
        PropertyInfo? property;
        lock (ProfileProperties)
        {
            if (!ProfileProperties.TryGetValue(type, out property))
            {
                property = type.GetProperty(nameof(DeviceProfile), typeof(DeviceProfile));
                ProfileProperties[type] = property;
            }
        }

        return property?.GetValue(argument) as DeviceProfile;
    }

    /// <summary>
    /// Removes the Dolby codecs from the audio codec list of every transcoding profile. Only
    /// <see cref="DeviceProfile.TranscodingProfiles"/> is touched: those describe the fMP4 HLS
    /// output that breaks, while the direct-play profiles describe the platform demuxer path,
    /// which handles Dolby correctly.
    /// </summary>
    /// <returns>The number of entries changed.</returns>
    private static int StripDolbyFromTranscodingProfiles(DeviceProfile profile)
    {
        var changed = 0;

        foreach (var transcodingProfile in profile.TranscodingProfiles)
        {
            var current = transcodingProfile.AudioCodec;
            if (string.IsNullOrEmpty(current))
            {
                continue;
            }

            var codecs = current.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var remaining = codecs.Where(codec => !IsDolby(codec)).ToArray();
            if (remaining.Length == codecs.Length)
            {
                continue;
            }

            changed++;
            transcodingProfile.AudioCodec = string.Join(',', remaining);
        }

        return changed;
    }

    private static bool IsDolby(string codec)
        => DolbyAliases.Contains(codec.Trim(), StringComparer.OrdinalIgnoreCase);
}
