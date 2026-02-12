using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;

namespace MeatSpeak.Client.Core.Tests.Connection;

public class ConnectionManagerTests : IDisposable
{
    private readonly ClientDatabase _db;
    private readonly ConnectionManager _manager;

    public ConnectionManagerTests()
    {
        _db = new ClientDatabase(":memory:");
        _manager = new ConnectionManager(() => new MessageDispatcher(), _db);
    }

    [Fact]
    public void AutoJoinChanged_SavesProfile()
    {
        var profile = new ServerProfile
        {
            Name = "Test",
            Host = "localhost",
            Nickname = "test",
            AutoJoinChannels = ["#initial"],
        };

        // Save initial profile so it exists in the DB
        _db.SaveServerProfile(profile);

        // Create connection via manager internals — we can't call AddAndConnectAsync
        // (it tries to open a real TCP connection), so replicate the subscription wiring.
        var dispatcher = new MessageDispatcher();
        var connection = new ServerConnection(profile, dispatcher);
        connection.ServerState.AutoJoinChanged += () => _db.SaveServerProfile(connection.ServerState.Profile);

        // Simulate adding a channel at runtime
        connection.ServerState.Profile.AutoJoinChannels.Add("#new");
        connection.ServerState.OnAutoJoinChanged();

        // Verify the profile was persisted with the new channel
        var saved = _db.LoadServerProfiles();
        var savedProfile = Assert.Single(saved);
        Assert.Contains("#initial", savedProfile.AutoJoinChannels);
        Assert.Contains("#new", savedProfile.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public void AutoJoinChanged_RoundTrip_JoinThenPart()
    {
        var profile = new ServerProfile
        {
            Name = "Test",
            Host = "localhost",
            Nickname = "test",
            AutoJoinChannels = ["#keep"],
        };
        _db.SaveServerProfile(profile);

        var dispatcher = new MessageDispatcher();
        var connection = new ServerConnection(profile, dispatcher);
        connection.ServerState.AutoJoinChanged += () => _db.SaveServerProfile(connection.ServerState.Profile);

        // Join a new channel
        profile.AutoJoinChannels.Add("#temp");
        connection.ServerState.OnAutoJoinChanged();

        var midSave = _db.LoadServerProfiles();
        Assert.Equal(2, Assert.Single(midSave).AutoJoinChannels.Count);

        // Part the channel
        profile.AutoJoinChannels.Remove("#temp");
        connection.ServerState.OnAutoJoinChanged();

        var finalSave = _db.LoadServerProfiles();
        var saved = Assert.Single(finalSave);
        Assert.Single(saved.AutoJoinChannels);
        Assert.Contains("#keep", saved.AutoJoinChannels);

        connection.Dispose();
    }

    [Fact]
    public void AutoJoinChanged_MultipleServers_DoNotInterfere()
    {
        var profile1 = new ServerProfile
        {
            Name = "Server1",
            Host = "host1",
            Nickname = "nick1",
            AutoJoinChannels = ["#s1chan"],
        };
        var profile2 = new ServerProfile
        {
            Name = "Server2",
            Host = "host2",
            Nickname = "nick2",
            AutoJoinChannels = ["#s2chan"],
        };
        _db.SaveServerProfile(profile1);
        _db.SaveServerProfile(profile2);

        var conn1 = new ServerConnection(profile1, new MessageDispatcher());
        var conn2 = new ServerConnection(profile2, new MessageDispatcher());
        conn1.ServerState.AutoJoinChanged += () => _db.SaveServerProfile(conn1.ServerState.Profile);
        conn2.ServerState.AutoJoinChanged += () => _db.SaveServerProfile(conn2.ServerState.Profile);

        // Modify only server 1
        profile1.AutoJoinChannels.Add("#s1new");
        conn1.ServerState.OnAutoJoinChanged();

        var saved = _db.LoadServerProfiles();
        var saved1 = saved.First(p => p.Id == profile1.Id);
        var saved2 = saved.First(p => p.Id == profile2.Id);

        Assert.Equal(2, saved1.AutoJoinChannels.Count);
        Assert.Contains("#s1new", saved1.AutoJoinChannels);
        Assert.Single(saved2.AutoJoinChannels);
        Assert.Contains("#s2chan", saved2.AutoJoinChannels);

        conn1.Dispose();
        conn2.Dispose();
    }

    public void Dispose()
    {
        _manager.Dispose();
        _db.Dispose();
    }
}
