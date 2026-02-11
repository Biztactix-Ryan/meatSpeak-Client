using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;

namespace MeatSpeak.Client.Core.Tests.Connection;

public class ServerConnectionTests
{
    [Fact]
    public void Constructor_SetsInitialState()
    {
        var profile = new ServerProfile
        {
            Name = "Test Server",
            Host = "irc.test.com",
            Port = 6667,
            Nickname = "testuser",
        };

        var dispatcher = new MessageDispatcher();
        var connection = new ServerConnection(profile, dispatcher);

        Assert.Equal(ConnectionState.Disconnected, connection.ServerState.ConnectionState);
        Assert.Equal("testuser", connection.ServerState.CurrentNick);
        Assert.False(connection.IsMeatSpeak);
        Assert.NotNull(connection.Id);
    }

    [Fact]
    public void Constructor_ProfileIsAccessible()
    {
        var profile = new ServerProfile
        {
            Name = "Test",
            Host = "localhost",
            Port = 6667,
            Nickname = "nick",
        };

        var dispatcher = new MessageDispatcher();
        var connection = new ServerConnection(profile, dispatcher);

        Assert.Equal("Test", connection.ServerState.Profile.Name);
        Assert.Equal("localhost", connection.ServerState.Profile.Host);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var profile = new ServerProfile { Host = "localhost", Nickname = "test" };
        var dispatcher = new MessageDispatcher();
        var connection = new ServerConnection(profile, dispatcher);

        // Should not throw
        connection.Dispose();
    }
}
