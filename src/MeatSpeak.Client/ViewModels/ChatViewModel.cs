using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;

    [ObservableProperty] private string _channelName = string.Empty;
    [ObservableProperty] private string _topic = string.Empty;
    [ObservableProperty] private bool _hasChannel;

    public ClientState ClientState => _connectionManager.ClientState;

    public ObservableCollection<ChatMessage>? Messages
    {
        get
        {
            var server = ClientState.ActiveServer;
            if (server is null) return null;

            var channelName = server.ActiveChannelName;
            if (channelName is null) return null;

            var channel = server.FindChannel(channelName);
            if (channel is not null) return channel.Messages;

            var pm = server.PrivateMessages.FirstOrDefault(p =>
                p.Nick.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            return pm?.Messages;
        }
    }

    public ChatViewModel(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public void RefreshForChannel()
    {
        var server = ClientState.ActiveServer;
        if (server is null)
        {
            HasChannel = false;
            return;
        }

        var name = server.ActiveChannelName;
        if (name is null)
        {
            HasChannel = false;
            return;
        }

        HasChannel = true;
        ChannelName = name;

        var channel = server.FindChannel(name);
        Topic = channel?.Topic ?? string.Empty;

        OnPropertyChanged(nameof(Messages));
    }
}
