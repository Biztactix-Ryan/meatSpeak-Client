using System.Text;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class SaslHandler : IMessageHandler
{
    public IEnumerable<string> HandledCommands => [IrcConstants.AUTHENTICATE, "900", "903", "904", "905"];

    public async Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        switch (message.Command.ToUpperInvariant())
        {
            case "AUTHENTICATE":
                await HandleAuthChallenge(connection, message, ct);
                break;
            case "900":
                // RPL_LOGGEDIN
                break;
            case "903":
                // RPL_SASLSUCCESS
                await connection.SendAsync("CAP END", ct);
                break;
            case "904":
            case "905":
                // ERR_SASLFAIL / ERR_SASLTOOLONG
                connection.ServerState.ErrorMessage = "SASL authentication failed";
                await connection.SendAsync("CAP END", ct);
                break;
        }
    }

    private static async Task HandleAuthChallenge(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct)
    {
        var challenge = message.GetParam(0);
        if (challenge != "+") return;

        var profile = connection.ServerState.Profile;
        if (string.IsNullOrEmpty(profile.SaslUsername) || string.IsNullOrEmpty(profile.SaslPassword))
        {
            await connection.SendAsync("AUTHENTICATE *", ct);
            return;
        }

        // SASL PLAIN: base64(authzid\0authcid\0password)
        var payload = $"{profile.SaslUsername}\0{profile.SaslUsername}\0{profile.SaslPassword}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        await connection.SendAsync($"AUTHENTICATE {encoded}", ct);
    }

    public static async Task InitiateSaslAsync(Connection.ServerConnection connection, CancellationToken ct)
    {
        await connection.SendAsync("AUTHENTICATE PLAIN", ct);
    }
}
