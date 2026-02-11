using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MeatSpeak.Client.Core.State;

public partial class ClientState : ObservableObject
{
    public ObservableCollection<ServerState> Servers { get; } = [];

    [ObservableProperty] private ServerState? _activeServer;

    public ServerState? FindServer(string connectionId) =>
        Servers.FirstOrDefault(s => s.ConnectionId == connectionId);

    public void AddServer(ServerState server) => Servers.Add(server);

    public void RemoveServer(string connectionId)
    {
        var server = FindServer(connectionId);
        if (server is not null)
        {
            Servers.Remove(server);
            if (ActiveServer == server)
                ActiveServer = Servers.FirstOrDefault();
        }
    }
}
