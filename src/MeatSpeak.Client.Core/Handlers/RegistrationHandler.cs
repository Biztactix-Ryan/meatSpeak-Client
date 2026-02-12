using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class RegistrationHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands =>
    [
        Numerics.Format(Numerics.RPL_WELCOME),
        Numerics.Format(Numerics.RPL_YOURHOST),
        Numerics.Format(Numerics.RPL_CREATED),
        Numerics.Format(Numerics.RPL_MYINFO),
    ];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var numeric = int.Parse(message.Command);

        switch (numeric)
        {
            case Numerics.RPL_WELCOME:
                // :server 001 nick :Welcome to the ...
                var nick = message.GetParam(0);
                if (nick is not null)
                    connection.ServerState.CurrentNick = nick;
                connection.ServerState.ServerName = message.Prefix;
                connection.ServerState.ConnectionState = Connection.ConnectionState.Connected;

                var welcomeMsg = message.Trailing;
                if (welcomeMsg is not null)
                {
                    connection.ServerState.GetOrCreatePm("Server").AddMessage(new State.ChatMessage
                    {
                        SenderNick = message.Prefix ?? "server",
                        Content = welcomeMsg,
                        Type = State.ChatMessageType.System,
                    });
                }
                break;

            case Numerics.RPL_YOURHOST:
            case Numerics.RPL_CREATED:
                break;

            case Numerics.RPL_MYINFO:
                // :server 004 nick servername version usermodes chanmodes
                break;
        }

        return Task.CompletedTask;
    }
}
