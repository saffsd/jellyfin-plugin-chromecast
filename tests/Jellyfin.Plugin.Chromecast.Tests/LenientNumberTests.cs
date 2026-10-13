using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Chromecast.Session;
using Xunit;

namespace Jellyfin.Plugin.Chromecast.Tests;

/// <summary>
/// The cast receiver's own clock arithmetic produces fractional tick counts - observed live as
/// "PositionTicks":116658329.99999999. PlaybackProgressInfo.PositionTicks is a long?, and the
/// built-in converter failed the whole payload over it, so a single such value silently discarded
/// an entire status broadcast (a playbackstop among them, which then never cleared the session).
/// </summary>
public class LenientNumberTests
{
    private sealed class Sample
    {
        [JsonPropertyName("PositionTicks")]
        public long? PositionTicks { get; set; }

        [JsonPropertyName("VolumeLevel")]
        public int? VolumeLevel { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new LenientInt64Converter(), new LenientInt32Converter() }
    };

    private static Sample Parse(string json) => JsonSerializer.Deserialize<Sample>(json, Options)!;

    [Fact]
    public void AFractionalTickCountIsRoundedInsteadOfFailingTheWholePayload()
    {
        // The exact value seen in the log.
        Assert.Equal(116658330L, Parse("""{"PositionTicks":116658329.99999999}""").PositionTicks);
        Assert.Equal(12035770L, Parse("""{"PositionTicks":12035769.999999998}""").PositionTicks);
    }

    [Fact]
    public void OrdinaryIntegersStillRoundTripExactly()
    {
        Assert.Equal(902426660L, Parse("""{"PositionTicks":902426660}""").PositionTicks);
        Assert.Equal(0L, Parse("""{"PositionTicks":0}""").PositionTicks);
        Assert.Equal(80, Parse("""{"VolumeLevel":80}""").VolumeLevel);
    }

    [Fact]
    public void NullStaysNull()
    {
        var s = Parse("""{"PositionTicks":null,"VolumeLevel":null}""");
        Assert.Null(s.PositionTicks);
        Assert.Null(s.VolumeLevel);
    }

    [Fact]
    public void AMissingPropertyIsLeftAlone()
    {
        var s = Parse("""{}""");
        Assert.Null(s.PositionTicks);
        Assert.Null(s.VolumeLevel);
    }

    [Fact]
    public void AFractionalVolumeIsAlsoTolerated()
    {
        Assert.Equal(80, Parse("""{"VolumeLevel":80.4}""").VolumeLevel);
    }

    [Fact]
    public void ARealReceiverStopPayloadSurvivesParsing()
    {
        // Trimmed from the live broadcast that used to be discarded wholesale.
        const string payload = """
            {"AudioStreamIndex":1,"CanSeek":true,"IsMuted":false,"IsPaused":false,
             "ItemId":"6f45baef55d7ca4bf1d9519c38a7e38e","LiveStreamId":null,
             "MediaSourceId":"6f45baef55d7ca4bf1d9519c38a7e38e","PlayMethod":"Transcode",
             "PlaySessionId":"370337112ce542c290a0bed755dc180f","PositionTicks":116658329.99999999,
             "RepeatMode":"RepeatNone","SubtitleStreamIndex":null,"VolumeLevel":80}
            """;

        var s = Parse(payload);

        Assert.Equal(116658330L, s.PositionTicks);
        Assert.Equal(80, s.VolumeLevel);
    }
}
