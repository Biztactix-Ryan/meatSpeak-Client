using MeatSpeak.Client.Audio;
using MeatSpeak.Client.Audio.Stubs;
using Xunit;

namespace MeatSpeak.Client.Audio.Tests;

public class VoiceConnectionTests
{
    [Fact]
    public void VoiceConnection_InitialState_IsNotConnected()
    {
        var connection = new VoiceConnection();
        Assert.False(connection.IsConnected);
        connection.Dispose();
    }

    [Fact]
    public void NullAudioCapture_DoesNotThrow()
    {
        var capture = new NullAudioCapture();
        Assert.False(capture.IsCapturing);
        capture.Start();
        Assert.False(capture.IsCapturing);
        capture.Stop();
        capture.Dispose();
    }

    [Fact]
    public void NullAudioPlayback_DoesNotThrow()
    {
        var playback = new NullAudioPlayback();
        Assert.False(playback.IsPlaying);
        playback.Start();
        playback.SubmitFrame(1234, new byte[960]);
        playback.Stop();
        playback.Dispose();
    }

    [Fact]
    public void NullOpusCodec_PassesThrough()
    {
        var codec = new NullOpusCodec();
        var input = new byte[] { 1, 2, 3, 4 };

        var encoded = codec.Encode(input);
        Assert.Equal(input, encoded.ToArray());

        var decoded = codec.Decode(input);
        Assert.Equal(input, decoded.ToArray());

        codec.Dispose();
    }

    [Fact]
    public void VoiceEngine_InitialState()
    {
        var capture = new NullAudioCapture();
        var playback = new NullAudioPlayback();
        var codec = new NullOpusCodec();

        var engine = new VoiceEngine(capture, playback, codec);
        Assert.False(engine.IsActive);
        Assert.False(engine.IsMuted);
        Assert.False(engine.IsDeafened);

        engine.Dispose();
    }

    [Fact]
    public void VoiceEngine_MuteDeafen()
    {
        var capture = new NullAudioCapture();
        var playback = new NullAudioPlayback();
        var codec = new NullOpusCodec();

        var engine = new VoiceEngine(capture, playback, codec);

        engine.SetMuted(true);
        Assert.True(engine.IsMuted);

        engine.SetDeafened(true);
        Assert.True(engine.IsDeafened);
        Assert.True(engine.IsMuted);

        engine.Dispose();
    }
}
