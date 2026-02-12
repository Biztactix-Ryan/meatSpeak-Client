using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MeatSpeak.Client.Core.Data;

namespace MeatSpeak.Client.Core.State;

public partial class ServerState : ObservableObject
{
    public string ConnectionId { get; }
    public ServerProfile Profile { get; }

    [ObservableProperty] private string _currentNick = string.Empty;
    [ObservableProperty] private Connection.ConnectionState _connectionState = Connection.ConnectionState.Disconnected;
    [ObservableProperty] private string? _serverName;
    [ObservableProperty] private string? _motd;
    [ObservableProperty] private bool _isMeatSpeak;
    [ObservableProperty] private bool _hasVoiceCapability;
    [ObservableProperty] private bool _hasAuthCapability;
    [ObservableProperty] private string? _activeChannelName;
    [ObservableProperty] private string? _errorMessage;

    public IsupportTokens Isupport { get; } = new();
    public ObservableCollection<ChannelState> Channels { get; } = [];
    public ObservableCollection<PrivateMessageState> PrivateMessages { get; } = [];
    public ObservableCollection<VoiceChannelState> VoiceChannels { get; } = [];
    public ObservableCollection<ListedChannel> AvailableChannels { get; } = [];
    public HashSet<string> EnabledCapabilities { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> MotdLines { get; } = [];

    public event Action? AutoJoinChanged;

    public void OnAutoJoinChanged() => AutoJoinChanged?.Invoke();

    public ServerState(string connectionId, ServerProfile profile)
    {
        ConnectionId = connectionId;
        Profile = profile;
        CurrentNick = profile.Nickname;
    }

    public ChannelState GetOrCreateChannel(string name)
    {
        var existing = FindChannel(name);
        if (existing is not null) return existing;
        var channel = new ChannelState(name);
        Channels.Add(channel);
        return channel;
    }

    public ChannelState? FindChannel(string name) =>
        Channels.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public PrivateMessageState GetOrCreatePm(string nick)
    {
        var existing = PrivateMessages.FirstOrDefault(p => p.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var pm = new PrivateMessageState(nick);
        PrivateMessages.Add(pm);
        return pm;
    }

    public void RemoveChannel(string name)
    {
        var channel = FindChannel(name);
        if (channel is not null)
            Channels.Remove(channel);
    }

    public VoiceChannelState GetOrCreateVoiceChannel(string name)
    {
        var existing = VoiceChannels.FirstOrDefault(v => v.ChannelName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var voice = new VoiceChannelState(name);
        VoiceChannels.Add(voice);
        return voice;
    }
}
