using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class PingPongHandlerTests
{
    [Fact]
    public void HandlesCorrectCommand()
    {
        var handler = new PingPongHandler();
        Assert.Contains("PING", handler.HandledCommands);
    }

    [Fact]
    public async Task HandleAsync_DoesNotThrowWithoutConnection()
    {
        var handler = new PingPongHandler();
        var message = new IrcMessage(null, null, "PING", ["server123"]);

        var profile = new ServerProfile { Host = "test", Nickname = "test" };
        var dispatcher = new MessageDispatcher();
        var connection = new ServerConnection(profile, dispatcher);

        // SendAsync is a no-op when not connected, should not throw
        await handler.HandleAsync(connection, message);

        connection.Dispose();
    }
}
