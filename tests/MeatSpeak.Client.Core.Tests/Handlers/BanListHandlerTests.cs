using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class BanListHandlerTests
{
    private static ServerConnection CreateConnection()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "testnick" };
        return new ServerConnection(profile, new MessageDispatcher());
    }

    [Fact]
    public async Task HandleBanList_PopulatesBansWithCorrectFields()
    {
        var handler = new BanListHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        // :server 367 testnick #test *!bad@host op!user@host 1700000000
        var message = new IrcMessage(null, "server", "367",
            ["testnick", "#test", "*!bad@host", "op!user@host", "1700000000"]);

        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Single(channel.Bans);

        var ban = channel.Bans[0];
        Assert.Equal("*!bad@host", ban.Mask);
        Assert.Equal("op!user@host", ban.SetBy);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), ban.SetAt);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleBanList_MultipleBans_AddsAll()
    {
        var handler = new BanListHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        await handler.HandleAsync(connection, new IrcMessage(null, "server", "367",
            ["testnick", "#test", "*!bad@host1", "op", "1700000000"]));

        await handler.HandleAsync(connection, new IrcMessage(null, "server", "367",
            ["testnick", "#test", "*!bad@host2", "op", "1700000001"]));

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Equal(2, channel.Bans.Count);
        Assert.Equal("*!bad@host1", channel.Bans[0].Mask);
        Assert.Equal("*!bad@host2", channel.Bans[1].Mask);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleEndOfBanList_IsNoOp()
    {
        var handler = new BanListHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        // :server 368 testnick #test :End of channel ban list
        var message = new IrcMessage(null, "server", "368",
            ["testnick", "#test", "End of channel ban list"]);

        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Empty(channel.Bans);

        connection.Dispose();
    }
}
