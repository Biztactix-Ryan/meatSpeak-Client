using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class ChannelListViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;

    [ObservableProperty] private string? _selectedChannelName;
    [ObservableProperty] private string? _serverDisplayName;

    public ClientState ClientState => _connectionManager.ClientState;

    public ChannelListViewModel(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public ObservableCollection<ChannelState>? Channels =>
        ClientState.ActiveServer?.Channels;

    public ObservableCollection<PrivateMessageState>? PrivateMessages =>
        ClientState.ActiveServer?.PrivateMessages;

    public ObservableCollection<VoiceChannelState>? VoiceChannels =>
        ClientState.ActiveServer?.VoiceChannels;

    public bool ShowVoiceChannels =>
        ClientState.ActiveServer?.HasVoiceCapability ?? false;

    [RelayCommand]
    private void SelectChannel(string channelName)
    {
        SelectedChannelName = channelName;
        var server = ClientState.ActiveServer;
        if (server is not null)
        {
            server.ActiveChannelName = channelName;
            var channel = server.FindChannel(channelName);
            channel?.ClearUnread();
        }
        OnPropertyChanged(nameof(Channels));
    }

    [RelayCommand]
    private void SelectPm(string nick)
    {
        SelectedChannelName = nick;
        var server = ClientState.ActiveServer;
        if (server is not null)
        {
            server.ActiveChannelName = nick;
            var pm = server.PrivateMessages.FirstOrDefault(p => p.Nick == nick);
            pm?.ClearUnread();
        }
    }

    [RelayCommand]
    private async Task JoinChannelAsync(string channelName)
    {
        var server = ClientState.ActiveServer;
        if (server is null) return;

        var connection = _connectionManager.FindConnection(server.ConnectionId);
        if (connection is not null)
            await connection.JoinChannelAsync(channelName);
    }

    [RelayCommand]
    private async Task PartChannelAsync(string channelName)
    {
        var server = ClientState.ActiveServer;
        if (server is null) return;

        var connection = _connectionManager.FindConnection(server.ConnectionId);
        if (connection is not null)
            await connection.PartChannelAsync(channelName);
    }

    public void RefreshForServer()
    {
        OnPropertyChanged(nameof(Channels));
        OnPropertyChanged(nameof(PrivateMessages));
        OnPropertyChanged(nameof(VoiceChannels));
        OnPropertyChanged(nameof(ShowVoiceChannels));
        ServerDisplayName = ClientState.ActiveServer?.Profile.Name
                           ?? ClientState.ActiveServer?.ServerName;
    }
}
