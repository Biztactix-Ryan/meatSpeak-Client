using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class CtcpHandler : IMessageHandler
{
    private const string ClientVersion = "MeatSpeak Client 1.0";

    public IEnumerable<string> HandledCommands => [IrcConstants.PRIVMSG];

    public async Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var content = message.Trailing ?? string.Empty;
        if (!content.StartsWith('\u0001') || !content.EndsWith('\u0001'))
            return;

        var (nick, _, _) = message.ParsePrefix();
        if (nick is null) return;

        var ctcp = content.Trim('\u0001');
        var spaceIdx = ctcp.IndexOf(' ');
        var command = spaceIdx >= 0 ? ctcp[..spaceIdx].ToUpperInvariant() : ctcp.ToUpperInvariant();
        var param = spaceIdx >= 0 ? ctcp[(spaceIdx + 1)..] : string.Empty;

        switch (command)
        {
            case "ACTION":
                HandleAction(connection, message, nick, param);
                break;

            case "VERSION":
                await connection.SendAsync($"NOTICE {nick} :\u0001VERSION {ClientVersion}\u0001", ct);
                break;

            case "PING":
                await connection.SendAsync($"NOTICE {nick} :\u0001PING {param}\u0001", ct);
                break;

            case "TIME":
                await connection.SendAsync($"NOTICE {nick} :\u0001TIME {DateTimeOffset.Now:R}\u0001", ct);
                break;
        }
    }

    private static void HandleAction(Connection.ServerConnection connection, IrcMessage message, string nick, string actionText)
    {
        var target = message.GetParam(0);
        if (target is null) return;

        var state = connection.ServerState;
        var isOwnMessage = nick.Equals(state.CurrentNick, StringComparison.OrdinalIgnoreCase);

        var chatMessage = new State.ChatMessage
        {
            SenderNick = nick,
            SenderPrefix = message.Prefix,
            Content = actionText,
            Type = State.ChatMessageType.Action,
            IsOwnMessage = isOwnMessage,
        };

        if (state.Isupport.IsChannelName(target))
        {
            state.GetOrCreateChannel(target).AddMessage(chatMessage);
        }
        else
        {
            var pmNick = isOwnMessage ? target : nick;
            state.GetOrCreatePm(pmNick).AddMessage(chatMessage);
        }
    }
}
