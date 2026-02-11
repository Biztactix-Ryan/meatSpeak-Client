using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MeatSpeak.Client.Core.State;

public partial class VoiceMemberState : ObservableObject
{
    [ObservableProperty] private string _nick = string.Empty;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isDeafened;
    [ObservableProperty] private bool _isSpeaking;
}

public partial class VoiceChannelState : ObservableObject
{
    public string ChannelName { get; }

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isSelfMuted;
    [ObservableProperty] private bool _isSelfDeafened;

    public ObservableCollection<VoiceMemberState> Members { get; } = [];

    public VoiceChannelState(string channelName)
    {
        ChannelName = channelName;
    }
}
