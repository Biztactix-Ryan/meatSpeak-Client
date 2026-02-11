using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class NumericHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands =>
    [
        Numerics.Format(Numerics.RPL_ISUPPORT),
        Numerics.Format(Numerics.RPL_LUSERCLIENT),
        Numerics.Format(Numerics.RPL_LUSEROP),
        Numerics.Format(Numerics.RPL_LUSERUNKNOWN),
        Numerics.Format(Numerics.RPL_LUSERCHANNELS),
        Numerics.Format(Numerics.RPL_LUSERME),
        Numerics.Format(Numerics.RPL_MOTDSTART),
        Numerics.Format(Numerics.RPL_MOTD),
        Numerics.Format(Numerics.RPL_ENDOFMOTD),
        Numerics.Format(Numerics.RPL_TOPIC),
        Numerics.Format(Numerics.RPL_TOPICWHOTIME),
        Numerics.Format(Numerics.RPL_NOTOPIC),
        Numerics.Format(Numerics.RPL_CHANNELMODEIS),
        Numerics.Format(Numerics.RPL_NAMREPLY),
        Numerics.Format(Numerics.RPL_ENDOFNAMES),
        Numerics.Format(Numerics.RPL_AWAY),
        Numerics.Format(Numerics.ERR_NICKNAMEINUSE),
        Numerics.Format(Numerics.ERR_ERRONEUSNICKNAME),
    ];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var numeric = int.Parse(message.Command);
        var state = connection.ServerState;

        switch (numeric)
        {
            case Numerics.RPL_ISUPPORT:
                HandleIsupport(state, message);
                break;

            case Numerics.RPL_MOTDSTART:
                state.MotdLines.Clear();
                if (message.Trailing is not null)
                    state.MotdLines.Add(message.Trailing);
                break;

            case Numerics.RPL_MOTD:
                if (message.Trailing is not null)
                    state.MotdLines.Add(message.Trailing);
                break;

            case Numerics.RPL_ENDOFMOTD:
                state.Motd = string.Join('\n', state.MotdLines);
                break;

            case Numerics.RPL_TOPIC:
                HandleTopic(state, message);
                break;

            case Numerics.RPL_TOPICWHOTIME:
                HandleTopicWhoTime(state, message);
                break;

            case Numerics.RPL_NOTOPIC:
                var noTopicChan = message.GetParam(1);
                if (noTopicChan is not null)
                {
                    var channel = state.FindChannel(noTopicChan);
                    if (channel is not null) channel.Topic = string.Empty;
                }
                break;

            case Numerics.RPL_CHANNELMODEIS:
                HandleChannelModes(state, message);
                break;

            case Numerics.RPL_NAMREPLY:
                HandleNames(state, message);
                break;

            case Numerics.RPL_ENDOFNAMES:
                break;

            case Numerics.RPL_AWAY:
                // :server 301 mynick targetNick :away message
                break;

            case Numerics.ERR_NICKNAMEINUSE:
                HandleNickInUse(state, message);
                break;

            case Numerics.ERR_ERRONEUSNICKNAME:
                state.ErrorMessage = $"Erroneous nickname: {message.GetParam(1)}";
                break;
        }

        return Task.CompletedTask;
    }

    private static void HandleIsupport(State.ServerState state, IrcMessage message)
    {
        // :server 005 nick TOKEN1 TOKEN2=value :are supported by this server
        var tokens = new List<string>();
        for (int i = 1; i < message.Parameters.Count - 1; i++)
        {
            var param = message.Parameters[i];
            if (!param.Contains("are supported"))
                tokens.Add(param);
        }
        state.Isupport.ParseTokens(tokens);
    }

    private static void HandleTopic(State.ServerState state, IrcMessage message)
    {
        // :server 332 nick #channel :topic text
        var channelName = message.GetParam(1);
        if (channelName is null) return;

        var channel = state.FindChannel(channelName);
        if (channel is not null)
            channel.Topic = message.Trailing ?? string.Empty;
    }

    private static void HandleTopicWhoTime(State.ServerState state, IrcMessage message)
    {
        // :server 333 nick #channel setter timestamp
        var channelName = message.GetParam(1);
        if (channelName is null) return;

        var channel = state.FindChannel(channelName);
        if (channel is null) return;

        channel.TopicSetBy = message.GetParam(2);
        if (long.TryParse(message.GetParam(3), out var unixTime))
            channel.TopicSetAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
    }

    private static void HandleChannelModes(State.ServerState state, IrcMessage message)
    {
        // :server 324 nick #channel +modes [params]
        var channelName = message.GetParam(1);
        if (channelName is null) return;

        var channel = state.FindChannel(channelName);
        if (channel is not null)
            channel.Modes = message.GetParam(2) ?? string.Empty;
    }

    private static void HandleNames(State.ServerState state, IrcMessage message)
    {
        // :server 353 nick = #channel :@op +voice regular
        var channelName = message.GetParam(2);
        if (channelName is null) return;

        var channel = state.GetOrCreateChannel(channelName);
        var names = (message.Trailing ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var (_, prefixes) = state.Isupport.ParsePrefix();

        foreach (var name in names)
        {
            var prefix = string.Empty;
            var nick = name;

            // Strip leading prefix characters (@, +, etc.)
            while (nick.Length > 0 && prefixes.Any(p => nick.StartsWith(p)))
            {
                prefix += nick[0];
                nick = nick[1..];
            }

            if (string.IsNullOrEmpty(nick)) continue;

            var existing = channel.FindMember(nick);
            if (existing is not null)
            {
                existing.ChannelPrefix = prefix;
            }
            else
            {
                channel.Members.Add(new State.UserState
                {
                    Nick = nick,
                    ChannelPrefix = prefix,
                });
            }
        }
    }

    private static void HandleNickInUse(State.ServerState state, IrcMessage message)
    {
        var nick = message.GetParam(1) ?? state.CurrentNick;
        state.ErrorMessage = $"Nickname '{nick}' is already in use";
    }
}
