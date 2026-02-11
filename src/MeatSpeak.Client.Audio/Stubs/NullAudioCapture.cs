namespace MeatSpeak.Client.Audio.Stubs;

public sealed class NullAudioCapture : IAudioCapture
{
    public event Action<ReadOnlyMemory<byte>>? FrameCaptured;
    public bool IsCapturing => false;
    public void Start(string? deviceId = null) { }
    public void Stop() { }
    public void Dispose() { }
}
