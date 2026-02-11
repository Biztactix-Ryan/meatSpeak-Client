using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Connection;

public sealed class IrcMessageReceiver
{
    private readonly IrcLineBuffer _lineBuffer;

    public event Action<IrcMessage>? MessageReceived;
    public event Action<Exception>? Error;
    public event Action? Disconnected;

    public IrcMessageReceiver(Stream stream)
    {
        _lineBuffer = new IrcLineBuffer(stream);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var message in _lineBuffer.ReadLinesAsync(ct))
            {
                MessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }
}
