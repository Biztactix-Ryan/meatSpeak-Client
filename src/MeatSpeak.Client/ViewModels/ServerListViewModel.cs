using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class ServerListViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;
    private readonly ClientDatabase _db;

    [ObservableProperty] private ServerState? _selectedServer;
    [ObservableProperty] private bool _isAddServerOpen;

    public ClientState ClientState => _connectionManager.ClientState;
    public ObservableCollection<ServerConnection> Connections => _connectionManager.Connections;

    public ServerListViewModel(ConnectionManager connectionManager, ClientDatabase db)
    {
        _connectionManager = connectionManager;
        _db = db;
    }

    [RelayCommand]
    private void SelectServer(ServerState server)
    {
        SelectedServer = server;
        _connectionManager.ClientState.ActiveServer = server;
    }

    [RelayCommand]
    private void OpenAddServer()
    {
        IsAddServerOpen = true;
    }

    [RelayCommand]
    private async Task AddServerAsync(ServerProfile profile)
    {
        _db.SaveServerProfile(profile);
        await _connectionManager.AddAndConnectAsync(profile);
        IsAddServerOpen = false;
    }

    [RelayCommand]
    private async Task DisconnectServerAsync(string connectionId)
    {
        await _connectionManager.DisconnectAsync(connectionId);
    }

    [RelayCommand]
    private async Task ReconnectServerAsync(string connectionId)
    {
        await _connectionManager.ReconnectAsync(connectionId);
    }

    [RelayCommand]
    private void RemoveServer(string connectionId)
    {
        var conn = _connectionManager.FindConnection(connectionId);
        if (conn is not null)
        {
            _db.DeleteServerProfile(conn.ServerState.Profile.Id);
            _connectionManager.RemoveConnection(connectionId);
        }
    }
}
