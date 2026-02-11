using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.Core.Tests.State;

public class ChannelStateTests
{
    [Fact]
    public void AddMessage_IncrementsUnreadCount()
    {
        var channel = new ChannelState("#test");

        channel.AddMessage(new ChatMessage
        {
            SenderNick = "someone",
            Content = "Hello",
        });

        Assert.Single(channel.Messages);
        Assert.Equal(1, channel.UnreadCount);
    }

    [Fact]
    public void AddMessage_OwnMessage_DoesNotIncrementUnread()
    {
        var channel = new ChannelState("#test");

        channel.AddMessage(new ChatMessage
        {
            SenderNick = "me",
            Content = "Hello",
            IsOwnMessage = true,
        });

        Assert.Single(channel.Messages);
        Assert.Equal(0, channel.UnreadCount);
    }

    [Fact]
    public void ClearUnread_ResetsCountAndMention()
    {
        var channel = new ChannelState("#test");
        channel.AddMessage(new ChatMessage { SenderNick = "x", Content = "hi" });
        channel.HasMention = true;

        channel.ClearUnread();

        Assert.Equal(0, channel.UnreadCount);
        Assert.False(channel.HasMention);
    }

    [Fact]
    public void FindMember_CaseInsensitive()
    {
        var channel = new ChannelState("#test");
        channel.Members.Add(new UserState { Nick = "Alice" });

        var found = channel.FindMember("alice");
        Assert.NotNull(found);
        Assert.Equal("Alice", found!.Nick);
    }

    [Fact]
    public void RemoveMember_RemovesCorrectMember()
    {
        var channel = new ChannelState("#test");
        channel.Members.Add(new UserState { Nick = "alice" });
        channel.Members.Add(new UserState { Nick = "bob" });

        channel.RemoveMember("alice");

        Assert.Single(channel.Members);
        Assert.Null(channel.FindMember("alice"));
        Assert.NotNull(channel.FindMember("bob"));
    }

    [Fact]
    public void Bans_InitiallyEmpty()
    {
        var channel = new ChannelState("#test");
        Assert.Empty(channel.Bans);
    }

    [Fact]
    public void Bans_CanAddAndRetrieve()
    {
        var channel = new ChannelState("#test");
        var ban = new BanEntry("*!bad@host", "op", DateTimeOffset.UtcNow);
        channel.Bans.Add(ban);

        Assert.Single(channel.Bans);
        Assert.Equal("*!bad@host", channel.Bans[0].Mask);
        Assert.Equal("op", channel.Bans[0].SetBy);
    }

    [Fact]
    public void RaiseMemberPrefixChanged_FiresEvent()
    {
        var channel = new ChannelState("#test");
        var fired = false;
        channel.MemberPrefixChanged += () => fired = true;

        channel.RaiseMemberPrefixChanged();

        Assert.True(fired);
    }

    [Fact]
    public void RaiseMemberPrefixChanged_NoSubscribers_DoesNotThrow()
    {
        var channel = new ChannelState("#test");
        var exception = Record.Exception(() => channel.RaiseMemberPrefixChanged());
        Assert.Null(exception);
    }
}
