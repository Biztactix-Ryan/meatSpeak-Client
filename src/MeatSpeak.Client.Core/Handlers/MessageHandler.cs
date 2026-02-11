using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class MessageHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands => [IrcConstants.PRIVMSG, IrcConstants.NOTICE];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var (nick, _, _) = message.ParsePrefix();
        var target = message.GetParam(0);
        var content = message.Trailing ?? string.Empty;
        if (nick is null || target is null) return Task.CompletedTask;

        var state = connection.ServerState;
        var isNotice = message.Command.Equals(IrcConstants.NOTICE, StringComparison.OrdinalIgnoreCase);
        var isOwnMessage = nick.Equals(state.CurrentNick, StringComparison.OrdinalIgnoreCase);

        // Check for CTCP (handled by CtcpHandler, skip here)
        if (content.StartsWith('\u0001') && content.EndsWith('\u0001'))
            return Task.CompletedTask;

        var chatMessage = new State.ChatMessage
        {
            SenderNick = nick,
            SenderPrefix = message.Prefix,
            Content = content,
            Type = isNotice ? State.ChatMessageType.Notice : State.ChatMessageType.Normal,
            IsOwnMessage = isOwnMessage,
            Timestamp = ParseServerTime(message) ?? DateTimeOffset.UtcNow,
        };

        if (state.Isupport.IsChannelName(target))
        {
            // Channel message
            var channel = state.GetOrCreateChannel(target);
            channel.AddMessage(chatMessage);

            // Check for mentions
            if (!isOwnMessage && content.Contains(state.CurrentNick, StringComparison.OrdinalIgnoreCase))
                channel.HasMention = true;
        }
        else
        {
            // Private message
            var pmNick = isOwnMessage ? target : nick;
            var pm = state.GetOrCreatePm(pmNick);
            pm.AddMessage(chatMessage);
            if (!isOwnMessage)
                pm.HasMention = true;
        }

        return Task.CompletedTask;
    }

    private static DateTimeOffset? ParseServerTime(IrcMessage message)
    {
        var tags = message.ParsedTags;
        if (tags.TryGetValue("time", out var timeStr) && timeStr is not null)
        {
            if (DateTimeOffset.TryParse(timeStr, out var dt))
                return dt;
        }
        return null;
    }
}
