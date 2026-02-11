namespace MeatSpeak.Client.Audio;

public interface IAudioPlayback : IDisposable
{
    bool IsPlaying { get; }
    void Start(string? deviceId = null);
    void Stop();
    void SubmitFrame(uint ssrc, ReadOnlyMemory<byte> pcmData);
}
