using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class CapNegotiationHandler : IMessageHandler
{
    private static readonly string[] DesiredCaps =
    [
        "multi-prefix",
        "server-time",
        "account-tag",
        "sasl",
        "meatspeakvoice",
        "meatspeakauth",
    ];

    public IEnumerable<string> HandledCommands => [IrcConstants.CAP];

    public async Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        // CAP * LS :multi-prefix sasl meatspeakvoice ...
        // CAP * ACK :multi-prefix sasl
        // CAP * NAK :...
        var subCommand = message.GetParam(1)?.ToUpperInvariant();

        switch (subCommand)
        {
            case "LS":
                await HandleLs(connection, message, ct);
                break;
            case "ACK":
                HandleAck(connection, message);
                break;
            case "NAK":
                // Server rejected caps, proceed anyway
                break;
        }
    }

    private async Task HandleLs(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct)
    {
        var capsString = message.Trailing ?? message.GetParam(2) ?? string.Empty;
        var availableCaps = capsString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Detect meatSpeak capabilities
        foreach (var cap in availableCaps)
        {
            var capName = cap.Split('=')[0]; // Handle cap=value format
            if (capName.Equals("meatspeakvoice", StringComparison.OrdinalIgnoreCase))
                connection.ServerState.HasVoiceCapability = true;
            if (capName.Equals("meatspeakauth", StringComparison.OrdinalIgnoreCase))
                connection.ServerState.HasAuthCapability = true;
        }

        connection.ServerState.IsMeatSpeak =
            connection.ServerState.HasVoiceCapability || connection.ServerState.HasAuthCapability;

        // Request caps we want that the server supports
        var toRequest = availableCaps
            .Select(c => c.Split('=')[0])
            .Where(c => DesiredCaps.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (toRequest.Count > 0)
        {
            await connection.SendAsync($"CAP REQ :{string.Join(' ', toRequest)}", ct);
        }
        else
        {
            await connection.SendAsync("CAP END", ct);
        }
    }

    private void HandleAck(Connection.ServerConnection connection, IrcMessage message)
    {
        var ackedCaps = (message.Trailing ?? message.GetParam(2) ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var cap in ackedCaps)
        {
            connection.ServerState.EnabledCapabilities.Add(cap);
        }
    }
}
