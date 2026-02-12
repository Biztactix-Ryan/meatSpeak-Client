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
    public async Task HandlePart_Self_RemovesChannel()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.IsJoined = true;
        channel.Members.Add(new UserState { Nick = "other" });

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#test"]);
        await handler.HandleAsync(connection, message);

        Assert.Null(connection.ServerState.FindChannel("#test"));

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_SwitchesActiveChannel()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#stay");
        var channel = connection.ServerState.GetOrCreateChannel("#leave");
        channel.IsJoined = true;
        connection.ServerState.ActiveChannelName = "#leave";

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#leave"]);
        await handler.HandleAsync(connection, message);

        Assert.Equal("#stay", connection.ServerState.ActiveChannelName);

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
    public async Task HandleJoin_Self_AddsToAutoJoinChannels()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();

        var message = new IrcMessage(null, "testnick!user@host", "JOIN", ["#new"]);
        await handler.HandleAsync(connection, message);

        Assert.Contains("#new", connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleJoin_Self_DoesNotDuplicateAutoJoin()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#existing");

        var message = new IrcMessage(null, "testnick!user@host", "JOIN", ["#existing"]);
        await handler.HandleAsync(connection, message);

        Assert.Single(connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleJoin_OtherUser_DoesNotModifyAutoJoin()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#test").IsJoined = true;

        var message = new IrcMessage(null, "other!user@host", "JOIN", ["#test"]);
        await handler.HandleAsync(connection, message);

        Assert.Empty(connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_RemovesFromAutoJoinChannels()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#leaving");
        var channel = connection.ServerState.GetOrCreateChannel("#leaving");
        channel.IsJoined = true;

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#leaving"]);
        await handler.HandleAsync(connection, message);

        Assert.DoesNotContain("#leaving", connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_OtherUser_DoesNotModifyAutoJoin()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#test");
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "other" });

        var message = new IrcMessage(null, "other!user@host", "PART", ["#test"]);
        await handler.HandleAsync(connection, message);

        Assert.Contains("#test", connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleKick_Self_DoesNotRemoveFromAutoJoin()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#test");
        var channel = connection.ServerState.GetOrCreateChannel("#test");
        channel.IsJoined = true;

        var message = new IrcMessage(null, "op!user@host", "KICK", ["#test", "testnick", "Bye"]);
        await handler.HandleAsync(connection, message);

        Assert.Contains("#test", connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleJoin_Self_CaseInsensitiveDedup()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#Test");

        var message = new IrcMessage(null, "testnick!user@host", "JOIN", ["#test"]);
        await handler.HandleAsync(connection, message);

        Assert.Single(connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleJoin_Self_DoesNotFireEventWhenAlreadyInList()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#existing");
        var fired = false;
        connection.ServerState.AutoJoinChanged += () => fired = true;

        var message = new IrcMessage(null, "testnick!user@host", "JOIN", ["#existing"]);
        await handler.HandleAsync(connection, message);

        Assert.False(fired);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_CaseInsensitiveRemoval()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#Test");
        connection.ServerState.GetOrCreateChannel("#Test").IsJoined = true;

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#test"]);
        await handler.HandleAsync(connection, message);

        Assert.Empty(connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_DoesNotFireEventWhenNotInAutoJoin()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.GetOrCreateChannel("#temp").IsJoined = true;
        var fired = false;
        connection.ServerState.AutoJoinChanged += () => fired = true;

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#temp"]);
        await handler.HandleAsync(connection, message);

        Assert.False(fired);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_PreservesOtherAutoJoinChannels()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#stay1");
        connection.ServerState.Profile.AutoJoinChannels.Add("#leaving");
        connection.ServerState.Profile.AutoJoinChannels.Add("#stay2");
        connection.ServerState.GetOrCreateChannel("#leaving").IsJoined = true;

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#leaving"]);
        await handler.HandleAsync(connection, message);

        Assert.Equal(2, connection.ServerState.Profile.AutoJoinChannels.Count);
        Assert.Contains("#stay1", connection.ServerState.Profile.AutoJoinChannels);
        Assert.Contains("#stay2", connection.ServerState.Profile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleKick_Self_DoesNotFireAutoJoinChanged()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#test");
        connection.ServerState.GetOrCreateChannel("#test").IsJoined = true;
        var fired = false;
        connection.ServerState.AutoJoinChanged += () => fired = true;

        var message = new IrcMessage(null, "op!user@host", "KICK", ["#test", "testnick", "Bye"]);
        await handler.HandleAsync(connection, message);

        Assert.False(fired);

        connection.Dispose();
    }

    [Fact]
    public async Task HandleJoin_Self_FiresAutoJoinChanged()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        var fired = false;
        connection.ServerState.AutoJoinChanged += () => fired = true;

        var message = new IrcMessage(null, "testnick!user@host", "JOIN", ["#new"]);
        await handler.HandleAsync(connection, message);

        Assert.True(fired);

        connection.Dispose();
    }

    [Fact]
    public async Task HandlePart_Self_FiresAutoJoinChanged()
    {
        var handler = new ChannelHandler();
        var connection = CreateConnection();
        connection.ServerState.Profile.AutoJoinChannels.Add("#leaving");
        connection.ServerState.GetOrCreateChannel("#leaving").IsJoined = true;
        var fired = false;
        connection.ServerState.AutoJoinChanged += () => fired = true;

        var message = new IrcMessage(null, "testnick!user@host", "PART", ["#leaving"]);
        await handler.HandleAsync(connection, message);

        Assert.True(fired);

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
