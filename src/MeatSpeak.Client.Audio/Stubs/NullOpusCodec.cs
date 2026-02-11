namespace MeatSpeak.Client.Audio.Stubs;

public sealed class NullOpusCodec : IOpusCodec
{
    public ReadOnlyMemory<byte> Encode(ReadOnlySpan<byte> pcmData) => pcmData.ToArray();
    public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> opusData) => opusData.ToArray();
    public void Dispose() { }
}
