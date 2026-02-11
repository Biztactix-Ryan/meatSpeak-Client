using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class MessageHandlerTests
{
    private static ServerConnection CreateConnection(string nick = "testnick")
    {
        var profile = new ServerProfile { Host = "test", Nickname = nick };
        var conn = new ServerConnection(profile, new MessageDispatcher());
        // Ensure ISUPPORT has channel types
        conn.ServerState.Isupport.ParseTokens(["CHANTYPES=#"]);
        return conn;
    }

    [Fact]
    public async Task HandlePrivmsg_ChannelMessage_AddsToChannel()
    {
        var handler = new MessageHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG", ["#test", "Hello world"]);
        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Single(channel.Messages);
        Assert.Equal("Hello world", channel.Messages[0].Content);
        Assert.Equal("sender", channel.Messages[0].SenderNick);
        Assert.False(channel.Messages[0].IsOwnMessage);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePrivmsg_PrivateMessage_CreatesPm()
    {
        var handler = new MessageHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG", ["testnick", "Hey there"]);
        await handler.HandleAsync(connection, message);

        var pm = connection.ServerState.PrivateMessages.FirstOrDefault(p => p.Nick == "sender");
        Assert.NotNull(pm);
        Assert.Single(pm!.Messages);
        Assert.Equal("Hey there", pm.Messages[0].Content);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePrivmsg_WithMention_SetsMentionFlag()
    {
        var handler = new MessageHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG",
            ["#test", "Hey testnick, check this out"]);
        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.True(channel.HasMention);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleNotice_SetsNoticeType()
    {
        var handler = new MessageHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "server!s@host", "NOTICE", ["#test", "Server notice"]);
        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Equal(ChatMessageType.Notice, channel.Messages[0].Type);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePrivmsg_CtcpIsIgnored()
    {
        var handler = new MessageHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "sender!user@host", "PRIVMSG",
            ["#test", "\u0001ACTION waves\u0001"]);
        await handler.HandleAsync(connection, message);

        // CTCP messages should be ignored by MessageHandler (handled by CtcpHandler)
        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Empty(channel.Messages);

        connection.Dispose();
    }
}
