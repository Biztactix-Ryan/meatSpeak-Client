namespace MeatSpeak.Client.Audio;

public interface IAudioCapture : IDisposable
{
    event Action<ReadOnlyMemory<byte>> FrameCaptured;
    bool IsCapturing { get; }
    void Start(string? deviceId = null);
    void Stop();
}
