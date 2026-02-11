using CommunityToolkit.Mvvm.ComponentModel;

namespace MeatSpeak.Client.Core.State;

public partial class UserState : ObservableObject
{
    [ObservableProperty] private string _nick = string.Empty;
    [ObservableProperty] private string? _username;
    [ObservableProperty] private string? _hostname;
    [ObservableProperty] private string? _realname;
    [ObservableProperty] private string? _account;
    [ObservableProperty] private bool _isAway;
    [ObservableProperty] private string? _awayMessage;
    [ObservableProperty] private string _channelPrefix = string.Empty; // @, +, etc.
}
