using System.Net;
using System.Net.Sockets;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Audio;

public sealed class VoiceConnection : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private uint _ssrc;
    private ushort _sequence;
    private uint _timestamp;
    private byte[]? _sessionToken;

    public bool IsConnected => _udpClient is not null;
    public event Action<uint, ReadOnlyMemory<byte>>? PacketReceived;

    public async Task ConnectAsync(string host, int port, byte[] sessionToken, CancellationToken ct = default)
    {
        _sessionToken = sessionToken;
        _ssrc = (uint)Random.Shared.Next();
        _sequence = 0;
        _timestamp = 0;

        _udpClient = new UdpClient();
        _udpClient.Connect(host, port);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Send initial keepalive
        await SendKeepaliveAsync();

        // Start receive loop
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
    }

    public async Task SendAudioAsync(ReadOnlyMemory<byte> opusData)
    {
        if (_udpClient is null) return;

        var buffer = new byte[VoicePacket.HeaderSize + opusData.Length];
        var written = VoicePacket.Write(
            buffer,
            VoicePacketType.Audio,
            VoicePacketFlags.None,
            _ssrc,
            _sequence++,
            _timestamp,
            opusData.Span);

        _timestamp += 960; // 20ms @ 48kHz

        if (written > 0)
            await _udpClient.SendAsync(buffer.AsMemory(0, written));
    }

    private async Task SendKeepaliveAsync()
    {
        if (_udpClient is null) return;

        var buffer = new byte[VoicePacket.HeaderSize];
        var written = VoicePacket.Write(
            buffer,
            VoicePacketType.Keepalive,
            VoicePacketFlags.None,
            _ssrc, 0, 0,
            ReadOnlySpan<byte>.Empty);

        if (written > 0)
            await _udpClient.SendAsync(buffer.AsMemory(0, written));
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        if (_udpClient is null) return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);
                if (VoicePacket.TryParse(result.Buffer, out var packet))
                {
                    if (packet.Type == VoicePacketType.Audio)
                    {
                        PacketReceived?.Invoke(packet.Ssrc, packet.Payload.ToArray());
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();

        // Send final keepalive as goodbye
        try { await SendKeepaliveAsync(); } catch { }

        _udpClient?.Dispose();
        _udpClient = null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _udpClient?.Dispose();
    }
}
