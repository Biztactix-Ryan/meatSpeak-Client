namespace MeatSpeak.Client.Audio.Stubs;

public sealed class NullAudioPlayback : IAudioPlayback
{
    public bool IsPlaying => false;
    public void Start(string? deviceId = null) { }
    public void Stop() { }
    public void SubmitFrame(uint ssrc, ReadOnlyMemory<byte> pcmData) { }
    public void Dispose() { }
}
