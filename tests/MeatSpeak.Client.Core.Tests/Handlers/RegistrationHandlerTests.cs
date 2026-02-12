using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Client.Core.State;
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
    public async Task HandleWelcome_AddsMessageToServerPm()
    {
        var handler = new RegistrationHandler();
        var connection = CreateConnection();
        var message = new IrcMessage(null, "irc.test.com", "001", ["testnick", "Welcome to the test server"]);

        await handler.HandleAsync(connection, message);

        var serverPm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "Server");
        Assert.NotNull(serverPm);
        Assert.Single(serverPm.Messages);
        Assert.Equal("Welcome to the test server", serverPm.Messages[0].Content);
        Assert.Equal(ChatMessageType.System, serverPm.Messages[0].Type);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleWelcome_NullTrailing_DoesNotCreatePm()
    {
        var handler = new RegistrationHandler();
        var connection = CreateConnection();
        // No trailing — welcomeMsg is null, should not add PM message
        var message = new IrcMessage(null, "irc.test.com", "001", []);

        await handler.HandleAsync(connection, message);

        var serverPm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "Server");
        Assert.Null(serverPm);

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
