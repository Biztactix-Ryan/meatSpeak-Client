using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.Services;

public sealed class NavigationService : INavigationService
{
    private readonly ConnectionManager _connectionManager;

    public event Action<string>? ServerChanged;
    public event Action<string, string>? ChannelChanged;
    public event Action? SettingsRequested;

    public NavigationService(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public void NavigateToServer(string connectionId)
    {
        var connection = _connectionManager.FindConnection(connectionId);
        if (connection is not null)
        {
            _connectionManager.ClientState.ActiveServer = connection.ServerState;
            ServerChanged?.Invoke(connectionId);
        }
    }

    public void NavigateToChannel(string connectionId, string channelName)
    {
        var connection = _connectionManager.FindConnection(connectionId);
        if (connection is not null)
        {
            connection.ServerState.ActiveChannelName = channelName;
            ChannelChanged?.Invoke(connectionId, channelName);
        }
    }

    public void NavigateToPm(string connectionId, string nick)
    {
        NavigateToChannel(connectionId, nick);
    }

    public void NavigateToSettings()
    {
        SettingsRequested?.Invoke();
    }
}
