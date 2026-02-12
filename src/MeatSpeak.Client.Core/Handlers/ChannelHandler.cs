using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class ChannelHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands =>
    [
        IrcConstants.JOIN,
        IrcConstants.PART,
        IrcConstants.KICK,
        IrcConstants.TOPIC,
    ];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        switch (message.Command.ToUpperInvariant())
        {
            case "JOIN":
                HandleJoin(connection, message);
                break;
            case "PART":
                HandlePart(connection, message);
                break;
            case "KICK":
                HandleKick(connection, message);
                break;
            case "TOPIC":
                HandleTopic(connection, message);
                break;
        }

        return Task.CompletedTask;
    }

    private static void HandleJoin(Connection.ServerConnection connection, IrcMessage message)
    {
        var (nick, _, _) = message.ParsePrefix();
        var channelName = message.Trailing ?? message.GetParam(0);
        if (nick is null || channelName is null) return;

        var state = connection.ServerState;
        var channel = state.GetOrCreateChannel(channelName);

        if (nick.Equals(state.CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            channel.IsJoined = true;

            var autoJoin = state.Profile.AutoJoinChannels;
            if (!autoJoin.Contains(channelName, StringComparer.OrdinalIgnoreCase))
            {
                autoJoin.Add(channelName);
                state.OnAutoJoinChanged();
            }
        }
        else
        {
            if (channel.FindMember(nick) is null)
            {
                channel.Members.Add(new State.UserState { Nick = nick });
            }
        }

        channel.AddMessage(new State.ChatMessage
        {
            SenderNick = nick,
            SenderPrefix = message.Prefix,
            Content = $"{nick} has joined {channelName}",
            Type = State.ChatMessageType.Join,
        });
    }

    private static void HandlePart(Connection.ServerConnection connection, IrcMessage message)
    {
        var (nick, _, _) = message.ParsePrefix();
        var channelName = message.GetParam(0);
        if (nick is null || channelName is null) return;

        var state = connection.ServerState;
        var channel = state.FindChannel(channelName);
        if (channel is null) return;

        var reason = message.Trailing ?? string.Empty;

        if (nick.Equals(state.CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            var autoJoin = state.Profile.AutoJoinChannels;
            var idx = autoJoin.FindIndex(c => c.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                autoJoin.RemoveAt(idx);
                state.OnAutoJoinChanged();
            }

            state.RemoveChannel(channelName);

            // Switch to another channel if we were viewing this one
            if (channelName.Equals(state.ActiveChannelName, StringComparison.OrdinalIgnoreCase))
                state.ActiveChannelName = state.Channels.FirstOrDefault()?.Name;
        }
        else
        {
            channel.RemoveMember(nick);

            channel.AddMessage(new State.ChatMessage
            {
                SenderNick = nick,
                SenderPrefix = message.Prefix,
                Content = string.IsNullOrEmpty(reason)
                    ? $"{nick} has left {channelName}"
                    : $"{nick} has left {channelName} ({reason})",
                Type = State.ChatMessageType.Part,
            });
        }
    }

    private static void HandleKick(Connection.ServerConnection connection, IrcMessage message)
    {
        // :kicker!user@host KICK #channel target :reason
        var (kicker, _, _) = message.ParsePrefix();
        var channelName = message.GetParam(0);
        var target = message.GetParam(1);
        var reason = message.Trailing ?? string.Empty;
        if (channelName is null || target is null) return;

        var state = connection.ServerState;
        var channel = state.FindChannel(channelName);
        if (channel is null) return;

        if (target.Equals(state.CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            state.RemoveChannel(channelName);

            if (channelName.Equals(state.ActiveChannelName, StringComparison.OrdinalIgnoreCase))
                state.ActiveChannelName = state.Channels.FirstOrDefault()?.Name;
        }
        else
        {
            channel.RemoveMember(target);

            channel.AddMessage(new State.ChatMessage
            {
                SenderNick = kicker ?? "server",
                Content = string.IsNullOrEmpty(reason)
                    ? $"{target} was kicked by {kicker}"
                    : $"{target} was kicked by {kicker} ({reason})",
                Type = State.ChatMessageType.Kick,
            });
        }
    }

    private static void HandleTopic(Connection.ServerConnection connection, IrcMessage message)
    {
        // :nick!user@host TOPIC #channel :new topic
        var (nick, _, _) = message.ParsePrefix();
        var channelName = message.GetParam(0);
        var newTopic = message.Trailing ?? string.Empty;
        if (channelName is null) return;

        var channel = connection.ServerState.FindChannel(channelName);
        if (channel is null) return;

        channel.Topic = newTopic;
        channel.TopicSetBy = nick;
        channel.TopicSetAt = DateTimeOffset.UtcNow;

        channel.AddMessage(new State.ChatMessage
        {
            SenderNick = nick ?? "server",
            Content = string.IsNullOrEmpty(newTopic)
                ? $"{nick} has cleared the topic"
                : $"{nick} has changed the topic to: {newTopic}",
            Type = State.ChatMessageType.TopicChange,
        });
    }
}
