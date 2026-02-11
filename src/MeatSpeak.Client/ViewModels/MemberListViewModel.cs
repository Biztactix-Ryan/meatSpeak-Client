using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class MemberListViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;

    [ObservableProperty] private int _memberCount;

    public ClientState ClientState => _connectionManager.ClientState;

    public ObservableCollection<UserState>? Members
    {
        get
        {
            var server = ClientState.ActiveServer;
            var channelName = server?.ActiveChannelName;
            if (channelName is null) return null;

            var channel = server?.FindChannel(channelName);
            return channel?.Members;
        }
    }

    public MemberListViewModel(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public void RefreshForChannel()
    {
        OnPropertyChanged(nameof(Members));
        MemberCount = Members?.Count ?? 0;
    }
}
