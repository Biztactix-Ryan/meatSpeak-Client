using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class CapNegotiationHandlerTests
{
    private static ServerConnection CreateConnection()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "testnick" };
        return new ServerConnection(profile, new MessageDispatcher());
    }

    [Fact]
    public async Task HandleLs_DetectsMeatSpeakCapabilities()
    {
        var handler = new CapNegotiationHandler();
        var connection = CreateConnection();

        // :server CAP * LS :multi-prefix meatspeakvoice meatspeakauth
        var message = new IrcMessage(null, "server", "CAP",
            ["*", "LS", "multi-prefix meatspeakvoice meatspeakauth"]);

        // This will throw because we can't send to a not-connected socket
        try { await handler.HandleAsync(connection, message); } catch { }

        Assert.True(connection.ServerState.IsMeatSpeak);
        Assert.True(connection.ServerState.HasVoiceCapability);
        Assert.True(connection.ServerState.HasAuthCapability);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleLs_StandardIrc_NoMeatSpeakDetected()
    {
        var handler = new CapNegotiationHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "server", "CAP",
            ["*", "LS", "multi-prefix server-time sasl"]);

        try { await handler.HandleAsync(connection, message); } catch { }

        Assert.False(connection.ServerState.IsMeatSpeak);
        Assert.False(connection.ServerState.HasVoiceCapability);
        Assert.False(connection.ServerState.HasAuthCapability);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleAck_AddsToEnabledCaps()
    {
        var handler = new CapNegotiationHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "server", "CAP",
            ["*", "ACK", "multi-prefix server-time"]);

        await handler.HandleAsync(connection, message);

        Assert.Contains("multi-prefix", connection.ServerState.EnabledCapabilities);
        Assert.Contains("server-time", connection.ServerState.EnabledCapabilities);

        connection.Dispose();
    }

    [Fact]
    public void HandlesCapCommand()
    {
        var handler = new CapNegotiationHandler();
        Assert.Contains("CAP", handler.HandledCommands);
    }
}
