using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Client.Core.State;
using MeatSpeak.Client.ViewModels;

namespace MeatSpeak.Client.Tests.ViewModels;

public class MemberListViewModelTests
{
    private static (MemberListViewModel vm, ConnectionManager cm) CreateVm()
    {
        var db = new ClientDatabase(":memory:");
        var cm = new ConnectionManager(() => new MessageDispatcher(), db);
        var vm = new MemberListViewModel(cm);
        return (vm, cm);
    }

    private static ServerState SetupServer(ConnectionManager cm)
    {
        var state = new ServerState("conn1", new Core.Data.ServerProfile { Host = "test", Nickname = "me" });
        cm.ClientState.AddServer(state);
        cm.ClientState.ActiveServer = state;
        return state;
    }

    [Fact]
    public void RefreshForChannel_NoActiveServer_AllGroupsEmpty()
    {
        var (vm, _) = CreateVm();

        vm.RefreshForChannel();

        Assert.Empty(vm.Operators);
        Assert.Empty(vm.Voiced);
        Assert.Empty(vm.Regular);
        Assert.Equal(0, vm.MemberCount);
    }

    [Fact]
    public void RefreshForChannel_GroupsMembersByPrefix()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "op1", ChannelPrefix = "@" });
        channel.Members.Add(new UserState { Nick = "op2", ChannelPrefix = "@" });
        channel.Members.Add(new UserState { Nick = "voiced1", ChannelPrefix = "+" });
        channel.Members.Add(new UserState { Nick = "regular1" });
        channel.Members.Add(new UserState { Nick = "regular2" });

        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        Assert.Equal(2, vm.Operators.Count);
        Assert.Single(vm.Voiced);
        Assert.Equal(2, vm.Regular.Count);
        Assert.Equal(5, vm.MemberCount);
    }

    [Fact]
    public void RefreshForChannel_SortsMembersAlphabetically()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "zack" });
        channel.Members.Add(new UserState { Nick = "alice" });
        channel.Members.Add(new UserState { Nick = "mike" });

        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        Assert.Equal("alice", vm.Regular[0].Nick);
        Assert.Equal("mike", vm.Regular[1].Nick);
        Assert.Equal("zack", vm.Regular[2].Nick);
    }

    [Fact]
    public void RefreshForChannel_IsCurrentUserOp_TrueWhenSelfHasAtPrefix()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "me", ChannelPrefix = "@" });
        channel.Members.Add(new UserState { Nick = "other" });

        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        Assert.True(vm.IsCurrentUserOp);
    }

    [Fact]
    public void RefreshForChannel_IsCurrentUserOp_FalseWhenSelfHasNoPrefix()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "me" });
        channel.Members.Add(new UserState { Nick = "op", ChannelPrefix = "@" });

        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        Assert.False(vm.IsCurrentUserOp);
    }

    [Fact]
    public void RefreshForChannel_CollectionChanged_RebuildsTriggers()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "alice" });
        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        Assert.Single(vm.Regular);

        // Adding a member should trigger rebuild via CollectionChanged
        channel.Members.Add(new UserState { Nick = "bob" });

        Assert.Equal(2, vm.Regular.Count);
        Assert.Equal(2, vm.MemberCount);
    }

    [Fact]
    public void RefreshForChannel_MemberPrefixChanged_RebuildsTriggers()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        var alice = new UserState { Nick = "alice" };
        channel.Members.Add(alice);
        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        Assert.Single(vm.Regular);
        Assert.Empty(vm.Operators);

        // Simulate mode change: alice gets op
        alice.ChannelPrefix = "@";
        channel.RaiseMemberPrefixChanged();

        Assert.Empty(vm.Regular);
        Assert.Single(vm.Operators);
    }

    [Fact]
    public void RefreshForChannel_SwitchingChannels_DetachesOldEvents()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel1 = server.GetOrCreateChannel("#chan1");
        channel1.Members.Add(new UserState { Nick = "alice" });
        server.ActiveChannelName = "#chan1";
        vm.RefreshForChannel();

        Assert.Single(vm.Regular);

        // Switch to a different channel
        var channel2 = server.GetOrCreateChannel("#chan2");
        channel2.Members.Add(new UserState { Nick = "bob" });
        channel2.Members.Add(new UserState { Nick = "carol" });
        server.ActiveChannelName = "#chan2";
        vm.RefreshForChannel();

        Assert.Equal(2, vm.Regular.Count);

        // Modifying old channel should NOT affect current groups
        channel1.Members.Add(new UserState { Nick = "dave" });
        Assert.Equal(2, vm.Regular.Count);
    }

    [Fact]
    public void MessageUser_CreatesOrSelectsPm()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);
        server.GetOrCreateChannel("#test");
        server.ActiveChannelName = "#test";

        vm.MessageUserCommand.Execute("alice");

        Assert.Single(server.PrivateMessages);
        Assert.Equal("alice", server.PrivateMessages[0].Nick);
        Assert.Equal("alice", server.ActiveChannelName);
    }

    [Fact]
    public void RefreshForChannel_OperatorWithVoice_GoesToOperators()
    {
        var (vm, cm) = CreateVm();
        var server = SetupServer(cm);

        var channel = server.GetOrCreateChannel("#test");
        channel.Members.Add(new UserState { Nick = "admin", ChannelPrefix = "@+" });

        server.ActiveChannelName = "#test";
        vm.RefreshForChannel();

        // @ takes priority — should be in operators, not voiced
        Assert.Single(vm.Operators);
        Assert.Empty(vm.Voiced);
        Assert.Empty(vm.Regular);
    }
}
