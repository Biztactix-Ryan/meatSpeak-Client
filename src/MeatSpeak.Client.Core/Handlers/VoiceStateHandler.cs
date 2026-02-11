using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class VoiceStateHandler : IMessageHandler
{
    // VOICESTATE is a meatSpeak-specific command for voice member state broadcasts
    public IEnumerable<string> HandledCommands => ["VOICESTATE"];

    public Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        if (!connection.ServerState.IsMeatSpeak) return Task.CompletedTask;

        // :server VOICESTATE #channel nick muted deafened speaking
        var channelName = message.GetParam(0);
        var nick = message.GetParam(1);
        var muted = message.GetParam(2);
        var deafened = message.GetParam(3);
        var speaking = message.GetParam(4);

        if (channelName is null || nick is null) return Task.CompletedTask;

        var voiceChannel = connection.ServerState.GetOrCreateVoiceChannel(channelName);
        var member = voiceChannel.Members.FirstOrDefault(m => m.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase));

        if (member is null)
        {
            member = new State.VoiceMemberState { Nick = nick };
            voiceChannel.Members.Add(member);
        }

        member.IsMuted = muted == "1";
        member.IsDeafened = deafened == "1";
        member.IsSpeaking = speaking == "1";

        return Task.CompletedTask;
    }
}
