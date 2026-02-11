using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class QuitPartHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands => [IrcConstants.QUIT];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var (nick, _, _) = message.ParsePrefix();
        if (nick is null) return Task.CompletedTask;

        var reason = message.Trailing ?? string.Empty;
        var state = connection.ServerState;

        foreach (var channel in state.Channels)
        {
            var member = channel.FindMember(nick);
            if (member is null) continue;

            channel.RemoveMember(nick);
            channel.AddMessage(new State.ChatMessage
            {
                SenderNick = nick,
                SenderPrefix = message.Prefix,
                Content = string.IsNullOrEmpty(reason)
                    ? $"{nick} has quit"
                    : $"{nick} has quit ({reason})",
                Type = State.ChatMessageType.Quit,
            });
        }

        return Task.CompletedTask;
    }
}
