using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.Core.Tests.State;

public class ServerStateTests
{
    [Fact]
    public void GetOrCreateChannel_CreatesNewChannel()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "nick" };
        var state = new ServerState("conn1", profile);

        var channel = state.GetOrCreateChannel("#test");

        Assert.NotNull(channel);
        Assert.Equal("#test", channel.Name);
        Assert.Single(state.Channels);
    }

    [Fact]
    public void GetOrCreateChannel_ReturnsExisting()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "nick" };
        var state = new ServerState("conn1", profile);

        var first = state.GetOrCreateChannel("#test");
        var second = state.GetOrCreateChannel("#test");

        Assert.Same(first, second);
        Assert.Single(state.Channels);
    }

    [Fact]
    public void FindChannel_CaseInsensitive()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "nick" };
        var state = new ServerState("conn1", profile);
        state.GetOrCreateChannel("#Test");

        var found = state.FindChannel("#test");
        Assert.NotNull(found);
    }

    [Fact]
    public void RemoveChannel_RemovesCorrectChannel()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "nick" };
        var state = new ServerState("conn1", profile);
        state.GetOrCreateChannel("#test");
        state.GetOrCreateChannel("#other");

        state.RemoveChannel("#test");

        Assert.Single(state.Channels);
        Assert.Null(state.FindChannel("#test"));
        Assert.NotNull(state.FindChannel("#other"));
    }

    [Fact]
    public void GetOrCreatePm_CreatesNew()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "nick" };
        var state = new ServerState("conn1", profile);

        var pm = state.GetOrCreatePm("bob");
        Assert.NotNull(pm);
        Assert.Equal("bob", pm.Nick);
    }

    [Fact]
    public void GetOrCreatePm_ReturnsExisting()
    {
        var profile = new ServerProfile { Host = "test", Nickname = "nick" };
        var state = new ServerState("conn1", profile);

        var first = state.GetOrCreatePm("bob");
        var second = state.GetOrCreatePm("bob");

        Assert.Same(first, second);
    }
}
