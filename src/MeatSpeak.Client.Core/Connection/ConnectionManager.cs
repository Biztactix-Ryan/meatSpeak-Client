using System.Collections.ObjectModel;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.Core.Connection;

public sealed class ConnectionManager : IDisposable
{
    private readonly Func<MessageDispatcher> _dispatcherFactory;

    public ObservableCollection<ServerConnection> Connections { get; } = [];
    public ClientState ClientState { get; } = new();

    public ConnectionManager(Func<MessageDispatcher> dispatcherFactory)
    {
        _dispatcherFactory = dispatcherFactory;
    }

    public async Task<ServerConnection> AddAndConnectAsync(ServerProfile profile, CancellationToken ct = default)
    {
        var dispatcher = _dispatcherFactory();
        var connection = new ServerConnection(profile, dispatcher);
        Connections.Add(connection);
        ClientState.AddServer(connection.ServerState);

        if (ClientState.ActiveServer is null)
            ClientState.ActiveServer = connection.ServerState;

        connection.Disconnected += OnDisconnected;

        await connection.ConnectAsync(ct);
        return connection;
    }

    public async Task DisconnectAsync(string connectionId, string reason = "Leaving")
    {
        var connection = FindConnection(connectionId);
        if (connection is null) return;

        await connection.DisconnectAsync(reason);
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var connection in Connections.ToList())
        {
            await connection.DisconnectAsync();
        }
    }

    public async Task ReconnectAsync(string connectionId, CancellationToken ct = default)
    {
        var connection = FindConnection(connectionId);
        if (connection is null) return;

        await connection.ReconnectAsync(ct);
    }

    public void RemoveConnection(string connectionId)
    {
        var connection = FindConnection(connectionId);
        if (connection is null) return;

        connection.Dispose();
        Connections.Remove(connection);
        ClientState.RemoveServer(connectionId);
    }

    public async Task AutoConnectSavedServersAsync(IEnumerable<ServerProfile> profiles, CancellationToken ct = default)
    {
        var autoConnect = profiles.Where(p => p.AutoConnect).OrderBy(p => p.SortOrder).ToList();
        foreach (var profile in autoConnect)
        {
            try
            {
                await AddAndConnectAsync(profile, ct);
            }
            catch
            {
                // Don't fail startup if one server fails
            }
        }
    }

    public ServerConnection? FindConnection(string connectionId) =>
        Connections.FirstOrDefault(c => c.Id == connectionId);

    public ServerConnection? FindConnectionByProfileId(Guid profileId) =>
        Connections.FirstOrDefault(c => c.ServerState.Profile.Id == profileId);

    private void OnDisconnected(ServerConnection connection)
    {
        // Could trigger auto-reconnect here
    }

    public void Dispose()
    {
        foreach (var connection in Connections)
            connection.Dispose();
        Connections.Clear();
    }
}
