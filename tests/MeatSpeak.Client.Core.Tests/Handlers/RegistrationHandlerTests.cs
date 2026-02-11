using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class RegistrationHandlerTests
{
    private static ServerConnection CreateConnection()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "testnick" };
        return new ServerConnection(profile, new MessageDispatcher());
    }

    [Fact]
    public async Task HandleWelcome_SetsNickAndConnectionState()
    {
        var handler = new RegistrationHandler();
        var connection = CreateConnection();
        var message = new IrcMessage(null, "irc.test.com", "001", ["testnick", "Welcome to the test server"]);

        await handler.HandleAsync(connection, message);

        Assert.Equal("testnick", connection.ServerState.CurrentNick);
        Assert.Equal("irc.test.com", connection.ServerState.ServerName);
        Assert.Equal(ConnectionState.Connected, connection.ServerState.ConnectionState);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleWelcome_UpdatesNickFromServer()
    {
        var handler = new RegistrationHandler();
        var connection = CreateConnection();
        // Server might assign a different nick
        var message = new IrcMessage(null, "irc.test.com", "001", ["servernick", "Welcome"]);

        await handler.HandleAsync(connection, message);

        Assert.Equal("servernick", connection.ServerState.CurrentNick);

        connection.Dispose();
    }

    [Fact]
    public void HandlesCorrectNumerics()
    {
        var handler = new RegistrationHandler();
        var commands = handler.HandledCommands.ToList();

        Assert.Contains("001", commands);
        Assert.Contains("002", commands);
        Assert.Contains("003", commands);
        Assert.Contains("004", commands);
    }
}
