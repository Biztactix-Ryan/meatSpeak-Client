using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class ErrorHandlerTests
{
    private static ServerConnection CreateConnection()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "testnick" };
        return new ServerConnection(profile, new MessageDispatcher());
    }

    [Fact]
    public async Task HandleGenericError_AddsToServerPm()
    {
        var handler = new ErrorHandler();
        var connection = CreateConnection();
        // :server 401 testnick badnick :No such nick/channel
        var message = new IrcMessage(null, "irc.test.com", "401", ["testnick", "badnick", "No such nick/channel"]);

        await handler.HandleAsync(connection, message);

        var serverPm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "Server");
        Assert.NotNull(serverPm);
        Assert.Single(serverPm.Messages);
        Assert.Contains("No such nick/channel", serverPm.Messages[0].Content);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleChannelError_AddsToChannel()
    {
        var handler = new ErrorHandler();
        var connection = CreateConnection();
        // Pre-create the channel so the error routes there
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        // :server 404 testnick #test :Cannot send to channel
        var message = new IrcMessage(null, "irc.test.com", "404", ["testnick", "#test", "Cannot send to channel"]);

        await handler.HandleAsync(connection, message);

        Assert.Single(channel.Messages);
        Assert.Equal("Cannot send to channel", channel.Messages[0].Content);
        // Should NOT appear in Server PM
        var serverPm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "Server");
        Assert.Null(serverPm);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleErrorCommand_SetsErrorState()
    {
        var handler = new ErrorHandler();
        var connection = CreateConnection();
        var message = new IrcMessage(null, "irc.test.com", "ERROR", ["Closing Link: test (Quit)"]);

        await handler.HandleAsync(connection, message);

        Assert.Equal(ConnectionState.Error, connection.ServerState.ConnectionState);
        Assert.Equal("Closing Link: test (Quit)", connection.ServerState.ErrorMessage);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleNumericError_NullTrailing_UsesUnknownError()
    {
        var handler = new ErrorHandler();
        var connection = CreateConnection();
        // No trailing — Trailing will be null, errorText falls back to "Unknown error"
        var message = new IrcMessage(null, "irc.test.com", "451", []);

        await handler.HandleAsync(connection, message);

        var serverPm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "Server");
        Assert.NotNull(serverPm);
        Assert.Single(serverPm.Messages);
        Assert.Contains("Unknown error", serverPm.Messages[0].Content);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMultipleErrors_AccumulateInServerPm()
    {
        var handler = new ErrorHandler();
        var connection = CreateConnection();
        var msg1 = new IrcMessage(null, "irc.test.com", "401", ["testnick", "badnick1", "No such nick"]);
        var msg2 = new IrcMessage(null, "irc.test.com", "401", ["testnick", "badnick2", "No such nick"]);

        await handler.HandleAsync(connection, msg1);
        await handler.HandleAsync(connection, msg2);

        var serverPm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "Server");
        Assert.NotNull(serverPm);
        Assert.Equal(2, serverPm.Messages.Count);

        connection.Dispose();
    }
}
