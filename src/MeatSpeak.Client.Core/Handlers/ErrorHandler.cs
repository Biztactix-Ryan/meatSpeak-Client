using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class ErrorHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands =>
    [
        "ERROR",
        Numerics.Format(Numerics.ERR_NOSUCHNICK),
        Numerics.Format(Numerics.ERR_NOSUCHCHANNEL),
        Numerics.Format(Numerics.ERR_CANNOTSENDTOCHAN),
        Numerics.Format(Numerics.ERR_TOOMANYCHANNELS),
        Numerics.Format(Numerics.ERR_USERNOTINCHANNEL),
        Numerics.Format(Numerics.ERR_NOTONCHANNEL),
        Numerics.Format(Numerics.ERR_NOTREGISTERED),
        Numerics.Format(Numerics.ERR_NEEDMOREPARAMS),
        Numerics.Format(Numerics.ERR_ALREADYREGISTRED),
        Numerics.Format(Numerics.ERR_PASSWDMISMATCH),
        Numerics.Format(Numerics.ERR_CHANNELISFULL),
        Numerics.Format(Numerics.ERR_INVITEONLYCHAN),
        Numerics.Format(Numerics.ERR_BANNEDFROMCHAN),
        Numerics.Format(Numerics.ERR_BADCHANNELKEY),
        Numerics.Format(Numerics.ERR_NOPRIVILEGES),
        Numerics.Format(Numerics.ERR_CHANOPRIVSNEEDED),
    ];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        var state = connection.ServerState;

        if (message.Command.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            state.ErrorMessage = message.Trailing ?? "Connection error";
            state.ConnectionState = Connection.ConnectionState.Error;
            return Task.CompletedTask;
        }

        var errorText = message.Trailing ?? "Unknown error";

        // Route error to appropriate channel if context available
        var target = message.GetParam(1); // Often the channel or nick that caused the error
        if (target is not null && state.Isupport.IsChannelName(target))
        {
            var channel = state.FindChannel(target);
            channel?.AddMessage(new State.ChatMessage
            {
                SenderNick = "server",
                Content = errorText,
                Type = State.ChatMessageType.System,
            });
        }
        else
        {
            // Generic error — show in active channel or server buffer
            state.ErrorMessage = errorText;
            var serverBuf = state.GetOrCreatePm("Server");
            serverBuf.AddMessage(new State.ChatMessage
            {
                SenderNick = "server",
                Content = $"[{message.Command}] {errorText}",
                Type = State.ChatMessageType.System,
            });
        }

        return Task.CompletedTask;
    }
}
