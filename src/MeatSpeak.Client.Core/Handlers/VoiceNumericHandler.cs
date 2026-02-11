using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class VoiceNumericHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands =>
    [
        Numerics.Format(Numerics.RPL_VOICESESSION),
        Numerics.Format(Numerics.RPL_VOICESTATE),
        Numerics.Format(Numerics.RPL_VOICELIST),
        Numerics.Format(Numerics.RPL_ENDOFVOICELIST),
        Numerics.Format(Numerics.RPL_VOICEKEY),
        Numerics.Format(Numerics.RPL_VOICEERROR),
    ];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        if (!connection.ServerState.IsMeatSpeak) return Task.CompletedTask;

        var numeric = int.Parse(message.Command);

        switch (numeric)
        {
            case Numerics.RPL_VOICESESSION:
                // :server 900 nick #channel udp_host udp_port session_token
                HandleVoiceSession(connection, message);
                break;

            case Numerics.RPL_VOICESTATE:
                // :server 901 nick #channel user muted deafened
                break;

            case Numerics.RPL_VOICELIST:
                // :server 902 nick #channel user
                break;

            case Numerics.RPL_ENDOFVOICELIST:
                break;

            case Numerics.RPL_VOICEKEY:
                // :server 904 nick #channel :base64_key
                break;

            case Numerics.RPL_VOICEERROR:
                var error = message.Trailing ?? "Voice error";
                connection.ServerState.ErrorMessage = error;
                break;
        }

        return Task.CompletedTask;
    }

    private static void HandleVoiceSession(Connection.ServerConnection connection, IrcMessage message)
    {
        var channelName = message.GetParam(1);
        if (channelName is null) return;

        var voiceChannel = connection.ServerState.GetOrCreateVoiceChannel(channelName);
        voiceChannel.IsConnected = true;
    }
}
