namespace MeatSpeak.Client.Audio;

public interface IAudioMixer
{
    void AddSource(uint ssrc);
    void RemoveSource(uint ssrc);
    void SubmitFrame(uint ssrc, ReadOnlyMemory<byte> pcmData);
    ReadOnlyMemory<byte> MixFrame();
    void SetVolume(uint ssrc, float volume);
}
