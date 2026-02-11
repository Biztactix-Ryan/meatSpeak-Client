namespace MeatSpeak.Client.Audio;

public sealed class VoiceEngine : IDisposable
{
    private readonly IAudioCapture _capture;
    private readonly IAudioPlayback _playback;
    private readonly IOpusCodec _codec;
    private VoiceConnection? _connection;

    public bool IsActive => _connection?.IsConnected ?? false;
    public bool IsMuted { get; private set; }
    public bool IsDeafened { get; private set; }

    public VoiceEngine(IAudioCapture capture, IAudioPlayback playback, IOpusCodec codec)
    {
        _capture = capture;
        _playback = playback;
        _codec = codec;

        _capture.FrameCaptured += OnFrameCaptured;
    }

    public async Task ConnectAsync(string host, int port, byte[] sessionToken, CancellationToken ct = default)
    {
        _connection = new VoiceConnection();
        await _connection.ConnectAsync(host, port, sessionToken, ct);

        _connection.PacketReceived += OnPacketReceived;
        _playback.Start();

        if (!IsMuted)
            _capture.Start();
    }

    public async Task DisconnectAsync()
    {
        _capture.Stop();
        _playback.Stop();

        if (_connection is not null)
        {
            await _connection.DisconnectAsync();
            _connection.Dispose();
            _connection = null;
        }
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        if (muted)
            _capture.Stop();
        else if (_connection?.IsConnected == true)
            _capture.Start();
    }

    public void SetDeafened(bool deafened)
    {
        IsDeafened = deafened;
        if (deafened)
        {
            SetMuted(true);
            _playback.Stop();
        }
        else
        {
            _playback.Start();
        }
    }

    private void OnFrameCaptured(ReadOnlyMemory<byte> pcmData)
    {
        if (IsMuted || _connection is null) return;

        var encoded = _codec.Encode(pcmData.Span);
        _ = _connection.SendAudioAsync(encoded);
    }

    private void OnPacketReceived(uint ssrc, ReadOnlyMemory<byte> opusData)
    {
        if (IsDeafened) return;

        var decoded = _codec.Decode(opusData.Span);
        _playback.SubmitFrame(ssrc, decoded);
    }

    public void Dispose()
    {
        _capture.FrameCaptured -= OnFrameCaptured;
        _connection?.Dispose();
        _capture.Dispose();
        _playback.Dispose();
        _codec.Dispose();
    }
}
