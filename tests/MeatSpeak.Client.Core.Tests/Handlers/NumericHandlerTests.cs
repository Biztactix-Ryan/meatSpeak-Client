using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class NumericHandlerTests
{
    private static ServerConnection CreateConnection()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "testnick" };
        return new ServerConnection(profile, new MessageDispatcher());
    }

    [Fact]
    public async Task HandleIsupport_ParsesTokens()
    {
        var handler = new NumericHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "server", "005",
            ["testnick", "CHANTYPES=#&", "PREFIX=(ov)@+", "NETWORK=TestNet", "are supported by this server"]);

        await handler.HandleAsync(connection, message);

        Assert.Equal("#&", connection.ServerState.Isupport.ChanTypes);
        Assert.Equal("(ov)@+", connection.ServerState.Isupport.Prefix);
        Assert.Equal("TestNet", connection.ServerState.Isupport.Network);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMotd_AccumulatesLines()
    {
        var handler = new NumericHandler();
        var connection = CreateConnection();

        await handler.HandleAsync(connection,
            new IrcMessage(null, "server", "375", ["nick", "- server Message of the Day -"]));

        await handler.HandleAsync(connection,
            new IrcMessage(null, "server", "372", ["nick", "- Welcome to the server"]));

        await handler.HandleAsync(connection,
            new IrcMessage(null, "server", "372", ["nick", "- Enjoy your stay"]));

        await handler.HandleAsync(connection,
            new IrcMessage(null, "server", "376", ["nick", "End of /MOTD command"]));

        Assert.NotNull(connection.ServerState.Motd);
        Assert.Contains("Welcome to the server", connection.ServerState.Motd);
        Assert.Contains("Enjoy your stay", connection.ServerState.Motd);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleNames_PopulatesChannelMembers()
    {
        var handler = new NumericHandler();
        var connection = CreateConnection();

        // Ensure ISUPPORT is set for prefix parsing
        connection.ServerState.Isupport.ParseTokens(["PREFIX=(ov)@+"]);

        // Create channel first
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "server", "353",
            ["testnick", "=", "#test", "@op +voice regular"]);

        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test");
        Assert.NotNull(channel);
        Assert.Equal(3, channel!.Members.Count);

        var op = channel.FindMember("op");
        Assert.NotNull(op);
        Assert.Equal("@", op!.ChannelPrefix);

        var voice = channel.FindMember("voice");
        Assert.NotNull(voice);
        Assert.Equal("+", voice!.ChannelPrefix);

        var regular = channel.FindMember("regular");
        Assert.NotNull(regular);
        Assert.Equal("", regular!.ChannelPrefix);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleNickInUse_SetsError()
    {
        var handler = new NumericHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "server", "433",
            ["*", "testnick", "Nickname is already in use"]);

        await handler.HandleAsync(connection, message);

        Assert.NotNull(connection.ServerState.ErrorMessage);
        Assert.Contains("testnick", connection.ServerState.ErrorMessage!);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleTopic_SetsChannelTopic()
    {
        var handler = new NumericHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "server", "332",
            ["testnick", "#test", "Welcome to the test channel!"]);

        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test");
        Assert.Equal("Welcome to the test channel!", channel!.Topic);

        connection.Dispose();
    }
}
