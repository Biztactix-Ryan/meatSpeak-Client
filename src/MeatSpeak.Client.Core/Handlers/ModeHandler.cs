using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class ModeHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands => [IrcConstants.MODE];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var (nick, _, _) = message.ParsePrefix();
        var target = message.GetParam(0);
        var modeStr = message.GetParam(1);
        if (target is null || modeStr is null) return Task.CompletedTask;

        var state = connection.ServerState;

        if (state.Isupport.IsChannelName(target))
        {
            HandleChannelMode(state, message, nick, target, modeStr);
        }

        return Task.CompletedTask;
    }

    private static void HandleChannelMode(State.ServerState state, IrcMessage message, string? nick, string channelName, string modeStr)
    {
        var channel = state.FindChannel(channelName);
        if (channel is null) return;

        var (modes, prefixes) = state.Isupport.ParsePrefix();
        var paramIndex = 2;
        var adding = true;

        foreach (var c in modeStr)
        {
            if (c == '+') { adding = true; continue; }
            if (c == '-') { adding = false; continue; }

            var modeChar = c.ToString();
            var modeIdx = Array.IndexOf(modes, modeChar);

            if (modeIdx >= 0 && modeIdx < prefixes.Length)
            {
                // User prefix mode (o, v, etc.)
                var targetNick = message.GetParam(paramIndex++);
                if (targetNick is null) continue;

                var member = channel.FindMember(targetNick);
                if (member is not null)
                {
                    var prefix = prefixes[modeIdx];
                    if (adding)
                    {
                        if (!member.ChannelPrefix.Contains(prefix))
                            member.ChannelPrefix = prefix + member.ChannelPrefix;
                    }
                    else
                    {
                        member.ChannelPrefix = member.ChannelPrefix.Replace(prefix, string.Empty);
                    }
                }
            }
        }

        // Build mode change summary
        var paramsList = new List<string>();
        for (int i = 2; i < message.Parameters.Count; i++)
            paramsList.Add(message.Parameters[i]);

        channel.AddMessage(new State.ChatMessage
        {
            SenderNick = nick ?? "server",
            Content = $"{nick ?? "server"} sets mode {string.Join(' ', paramsList)}",
            Type = State.ChatMessageType.ModeChange,
        });
    }
}
