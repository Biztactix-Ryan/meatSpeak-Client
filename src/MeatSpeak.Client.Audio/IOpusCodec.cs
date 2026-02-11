namespace MeatSpeak.Client.Audio;

public interface IOpusCodec : IDisposable
{
    ReadOnlyMemory<byte> Encode(ReadOnlySpan<byte> pcmData);
    ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> opusData);
}
