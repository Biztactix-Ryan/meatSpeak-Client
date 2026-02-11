using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class MemberListViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;

    private ObservableCollection<UserState>? _trackedMembers;
    private ChannelState? _trackedChannel;

    [ObservableProperty] private int _memberCount;
    [ObservableProperty] private bool _isCurrentUserOp;
    [ObservableProperty] private IReadOnlyList<UserState> _operators = [];
    [ObservableProperty] private IReadOnlyList<UserState> _voiced = [];
    [ObservableProperty] private IReadOnlyList<UserState> _regular = [];

    public ClientState ClientState => _connectionManager.ClientState;

    public MemberListViewModel(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public void RefreshForChannel()
    {
        // Detach from old channel
        if (_trackedMembers is not null)
            _trackedMembers.CollectionChanged -= OnMembersChanged;
        if (_trackedChannel is not null)
            _trackedChannel.MemberPrefixChanged -= OnMemberPrefixChanged;

        var server = ClientState.ActiveServer;
        var channelName = server?.ActiveChannelName;
        var channel = channelName is not null ? server?.FindChannel(channelName) : null;

        _trackedChannel = channel;
        _trackedMembers = channel?.Members;

        // Attach to new channel
        if (_trackedMembers is not null)
            _trackedMembers.CollectionChanged += OnMembersChanged;
        if (_trackedChannel is not null)
            _trackedChannel.MemberPrefixChanged += OnMemberPrefixChanged;

        RebuildGroups();
    }

    private void OnMembersChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildGroups();

    private void OnMemberPrefixChanged() => RebuildGroups();

    private void RebuildGroups()
    {
        var members = _trackedMembers;
        if (members is null)
        {
            Operators = [];
            Voiced = [];
            Regular = [];
            MemberCount = 0;
            IsCurrentUserOp = false;
            return;
        }

        var ops = new List<UserState>();
        var voiced = new List<UserState>();
        var regular = new List<UserState>();

        foreach (var m in members)
        {
            if (m.ChannelPrefix.Contains('@'))
                ops.Add(m);
            else if (m.ChannelPrefix.Contains('+'))
                voiced.Add(m);
            else
                regular.Add(m);
        }

        ops.Sort(CompareByNick);
        voiced.Sort(CompareByNick);
        regular.Sort(CompareByNick);

        Operators = ops;
        Voiced = voiced;
        Regular = regular;
        MemberCount = members.Count;

        // Check if current user is an operator
        var server = ClientState.ActiveServer;
        if (server is not null)
        {
            var self = _trackedChannel?.FindMember(server.CurrentNick);
            IsCurrentUserOp = self?.ChannelPrefix.Contains('@') ?? false;
        }
        else
        {
            IsCurrentUserOp = false;
        }
    }

    private static int CompareByNick(UserState a, UserState b) =>
        string.Compare(a.Nick, b.Nick, StringComparison.OrdinalIgnoreCase);

    private ServerConnection? GetConnection()
    {
        var server = ClientState.ActiveServer;
        return server is not null ? _connectionManager.FindConnection(server.ConnectionId) : null;
    }

    private string? GetTarget() => ClientState.ActiveServer?.ActiveChannelName;

    [RelayCommand]
    private void MessageUser(string nick)
    {
        var server = ClientState.ActiveServer;
        if (server is null) return;
        server.GetOrCreatePm(nick);
        server.ActiveChannelName = nick;
    }

    [RelayCommand]
    private async Task WhoisAsync(string nick)
    {
        var connection = GetConnection();
        if (connection is not null)
            await connection.SendAsync($"WHOIS {nick}");
    }

    [RelayCommand]
    private async Task OpAsync(string nick)
    {
        var connection = GetConnection();
        var target = GetTarget();
        if (connection is not null && target is not null)
            await connection.SendAsync($"MODE {target} +o {nick}");
    }

    [RelayCommand]
    private async Task DeopAsync(string nick)
    {
        var connection = GetConnection();
        var target = GetTarget();
        if (connection is not null && target is not null)
            await connection.SendAsync($"MODE {target} -o {nick}");
    }

    [RelayCommand]
    private async Task VoiceAsync(string nick)
    {
        var connection = GetConnection();
        var target = GetTarget();
        if (connection is not null && target is not null)
            await connection.SendAsync($"MODE {target} +v {nick}");
    }

    [RelayCommand]
    private async Task DevoiceAsync(string nick)
    {
        var connection = GetConnection();
        var target = GetTarget();
        if (connection is not null && target is not null)
            await connection.SendAsync($"MODE {target} -v {nick}");
    }

    [RelayCommand]
    private async Task KickAsync(string nick)
    {
        var connection = GetConnection();
        var target = GetTarget();
        if (connection is not null && target is not null)
            await connection.SendAsync($"KICK {target} {nick}");
    }

    [RelayCommand]
    private async Task BanAsync(string nick)
    {
        var connection = GetConnection();
        var target = GetTarget();
        if (connection is not null && target is not null)
            await connection.SendAsync($"MODE {target} +b {nick}!*@*");
    }
}
