using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Tests.Handlers;

public class ChannelHandlerTests
{
    private static ServerConnection CreateConnection(string nick = "testnick")
    {
        var profile = new ServerProfile { Host = "test", Nickname = nick };
        return new ServerConnection(profile, new MessageDispatcher());
    }

    [Fact]
    public async Task HandleJoin_Self_CreatesChannelAndSetsJoined()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "testnick!user@host", "JOIN", ["#test"]);
        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test");
        Assert.NotNull(channel);
        Assert.True(channel!.IsJoined);
        Assert.Single(channel.Messages);
        Assert.Equal(ChatMessageType.Join, channel.Messages[0].Type);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleJoin_OtherUser_AddsMember()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test").IsJoined = true;

        var message = new IrcMessage(null, "other!user@host", "JOIN", ["#test"]);
        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.NotNull(channel.FindMember("other"));

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_ClearsChannel()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.IsJoined = true;
        channel.Members.Add(new UserState { Nick = "other" });

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#test"]);
        await handler.HandleAsync(connection, message);

        Assert.False(channel.IsJoined);
        Assert.Empty(channel.Members);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_OtherUser_RemovesMember()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "other" });

        var message = new IrcMessage(null, "other!user@host", "PART", ["#test", "Goodbye"]);
        await handler.HandleAsync(connection, message);

        Assert.Null(channel.FindMember("other"));
        Assert.Contains(channel.Messages, m => m.Type == ChatMessageType.Part);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleKick_RemovesTarget()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "victim" });

        var message = new IrcMessage(null, "op!user@host", "KICK", ["#test", "victim", "Bad behavior"]);
        await handler.HandleAsync(connection, message);

        Assert.Null(channel.FindMember("victim"));
        Assert.Contains(channel.Messages, m => m.Type == ChatMessageType.Kick);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleTopic_UpdatesChannelTopic()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test");

        var message = new IrcMessage(null, "op!user@host", "TOPIC", ["#test", "New topic here"]);
        await handler.HandleAsync(connection, message);

        var channel = connection.ServerState.FindChannel("#test")!;
        Assert.Equal("New topic here", channel.Topic);
        Assert.Equal("op", channel.TopicSetBy);

        connection.Dispose();
    }
}
