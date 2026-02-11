using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class NickHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands => [IrcConstants.NICK];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var (oldNick, _, _) = message.ParsePrefix();
        var newNick = message.Trailing ?? message.GetParam(0);
        if (oldNick is null || newNick is null) return Task.CompletedTask;

        var state = connection.ServerState;

        // Update own nick
        if (oldNick.Equals(state.CurrentNick, StringComparison.OrdinalIgnoreCase))
            state.CurrentNick = newNick;

        // Update in all channels
        foreach (var channel in state.Channels)
        {
            var member = channel.FindMember(oldNick);
            if (member is not null)
            {
                member.Nick = newNick;
                channel.AddMessage(new State.ChatMessage
                {
                    SenderNick = oldNick,
                    Content = $"{oldNick} is now known as {newNick}",
                    Type = State.ChatMessageType.NickChange,
                });
            }
        }

        return Task.CompletedTask;
    }
}
