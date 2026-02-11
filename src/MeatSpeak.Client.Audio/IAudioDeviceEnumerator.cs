namespace MeatSpeak.Client.Audio;

public sealed record AudioDevice(string Id, string Name, bool IsDefault);

public interface IAudioDeviceEnumerator
{
    IReadOnlyList<AudioDevice> GetInputDevices();
    IReadOnlyList<AudioDevice> GetOutputDevices();
}
