using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class PingPongHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands => [IrcConstants.PING];

    public async Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var token = message.Trailing ?? message.GetParam(0) ?? string.Empty;
        await connection.SendAsync($"PONG :{token}", ct);
    }
}
