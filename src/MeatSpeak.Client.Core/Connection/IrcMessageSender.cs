using System.Text;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Connection;

public sealed class IrcMessageSender
{
    private readonly Stream _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly byte[] _buffer = new byte[IrcConstants.MaxLineLengthWithTags];

    public IrcMessageSender(Stream stream)
    {
        _stream = stream;
    }

    public async Task SendAsync(string rawLine, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var bytes = Encoding.UTF8.GetBytes(rawLine + "\r\n");
            await _stream.WriteAsync(bytes, ct);
            await _stream.FlushAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SendCommandAsync(string command, params string[] parameters)
    {
        await SendCommandAsync(command, CancellationToken.None, parameters);
    }

    public async Task SendCommandAsync(string command, CancellationToken ct, params string[] parameters)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var written = MessageBuilder.Write(_buffer.AsSpan(), null, command, parameters);
            if (written > 0)
            {
                await _stream.WriteAsync(_buffer.AsMemory(0, written), ct);
                await _stream.FlushAsync(ct);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SendWithPrefixAsync(string prefix, string command, CancellationToken ct, params string[] parameters)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var written = MessageBuilder.Write(_buffer.AsSpan(), prefix, command, parameters);
            if (written > 0)
            {
                await _stream.WriteAsync(_buffer.AsMemory(0, written), ct);
                await _stream.FlushAsync(ct);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
