using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Client.Core.State;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class ModeHandlerTests
{
    private static ServerConnection CreateConnection()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "testnick" };
        var conn = new ServerConnection(profile, new MessageDispatcher());
        conn.ServerState.Isupport.ParseTokens(["PREFIX=(ov)@+"]);
        return conn;
    }

    [Fact]
    public async Task HandleMode_PlusO_AddsAtPrefix()
    {
        var handler = new ModeHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "alice" });

        var message = new IrcMessage(null, "op!user@host", "MODE",
            ["#test", "+o", "alice"]);

        await handler.HandleAsync(connection, message);

        var member = channel.FindMember("alice")!;
        Assert.Contains("@", member.ChannelPrefix);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMode_MinusO_RemovesAtPrefix()
    {
        var handler = new ModeHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "alice", ChannelPrefix = "@" });

        var message = new IrcMessage(null, "op!user@host", "MODE",
            ["#test", "-o", "alice"]);

        await handler.HandleAsync(connection, message);

        var member = channel.FindMember("alice")!;
        Assert.DoesNotContain("@", member.ChannelPrefix);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMode_PlusV_AddsPlusPrefix()
    {
        var handler = new ModeHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "bob" });

        var message = new IrcMessage(null, "op!user@host", "MODE",
            ["#test", "+v", "bob"]);

        await handler.HandleAsync(connection, message);

        var member = channel.FindMember("bob")!;
        Assert.Contains("+", member.ChannelPrefix);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMode_MinusV_RemovesPlusPrefix()
    {
        var handler = new ModeHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "bob", ChannelPrefix = "+" });

        var message = new IrcMessage(null, "op!user@host", "MODE",
            ["#test", "-v", "bob"]);

        await handler.HandleAsync(connection, message);

        var member = channel.FindMember("bob")!;
        Assert.DoesNotContain("+", member.ChannelPrefix);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMode_FiresMemberPrefixChangedEvent()
    {
        var handler = new ModeHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "alice" });

        var fired = false;
        channel.MemberPrefixChanged += () => fired = true;

        var message = new IrcMessage(null, "op!user@host", "MODE",
            ["#test", "+o", "alice"]);

        await handler.HandleAsync(connection, message);

        Assert.True(fired);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleMode_MultipleChanges_AppliesBoth()
    {
        var handler = new ModeHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "alice" });
        channel.Members.Add(new UserState { Nick = "bob" });

        // +ov alice bob
        var message = new IrcMessage(null, "op!user@host", "MODE",
            ["#test", "+ov", "alice", "bob"]);

        await handler.HandleAsync(connection, message);

        Assert.Contains("@", channel.FindMember("alice")!.ChannelPrefix);
        Assert.Contains("+", channel.FindMember("bob")!.ChannelPrefix);

        connection.Dispose();
    }
}
