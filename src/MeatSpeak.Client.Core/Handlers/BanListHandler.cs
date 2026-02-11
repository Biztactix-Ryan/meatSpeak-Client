using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class BanListHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands =>
    [
        Numerics.Format(Numerics.RPL_BANLIST),
        Numerics.Format(Numerics.RPL_ENDOFBANLIST),
    ];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var numeric = int.Parse(message.Command);

        if (numeric == Numerics.RPL_BANLIST)
        {
            // :server 367 nick #channel banmask setter timestamp
            var channelName = message.GetParam(1);
            var mask = message.GetParam(2);
            if (channelName is null || mask is null) return Task.CompletedTask;

            var channel = connection.ServerState.FindChannel(channelName);
            if (channel is null) return Task.CompletedTask;

            var setter = message.GetParam(3);
            DateTimeOffset? setAt = null;
            if (long.TryParse(message.GetParam(4), out var unixTime))
                setAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);

            channel.Bans.Add(new State.BanEntry(mask, setter, setAt));
        }

        // 368 (RPL_ENDOFBANLIST) — no action needed

        return Task.CompletedTask;
    }
}
