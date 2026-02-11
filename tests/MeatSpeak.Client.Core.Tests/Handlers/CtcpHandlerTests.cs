using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class CtcpHandlerTests
{
    private static ServerConnection CreateConnection(string nick = "testnick")
    {
        var profile = new ServerProfile { Host = "test", Nickname = nick };
        var conn = new ServerConnection(profile, new MessageDispatcher());
        conn.ServerState.Isupport.ParseTokens(["CHANTYPES=#"]);
        return conn;
    }

    [Fact]
    public async Task HandleAction_AddsActionMessage()
    {
        var handler = new CtcpHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG",
            ["#test", "\u0001ACTION waves hello\u0001"]);

        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Single(channel.Messages);
        Assert.Equal(ChatMessageType.Action, channel.Messages[0].Type);
        Assert.Equal("waves hello", channel.Messages[0].Content);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleAction_PrivateMessage_CreatesPm()
    {
        var handler = new CtcpHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG",
            ["testnick", "\u0001ACTION dances\u0001"]);

        await handler.HandleAsync(connection, message);

        var pm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "sender");
        Assert.NotNull(pm);
        Assert.Single(pm!.Messages);
        Assert.Equal(ChatMessageType.Action, pm.Messages[0].Type);

        connection.Dispose();
    }

    [Fact]
    public async Task NonCtcp_IsIgnored()
    {
        var handler = new CtcpHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG",
            ["#test", "Normal message"]);

        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Empty(channel.Messages);

        connection.Dispose();
    }
}
