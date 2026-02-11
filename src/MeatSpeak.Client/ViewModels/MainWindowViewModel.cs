using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.State;
using MeatSpeak.Client.Services;

namespace MeatSpeak.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;
    private readonly ClientDatabase _db;
    private readonly IThemeService _themeService;

    [ObservableProperty] private ServerListViewModel _serverList;
    [ObservableProperty] private ChannelListViewModel _channelList;
    [ObservableProperty] private ChatViewModel _chat;
    [ObservableProperty] private MemberListViewModel _memberList;
    [ObservableProperty] private MessageInputViewModel _messageInput;
    [ObservableProperty] private VoiceStatusBarViewModel _voiceStatusBar;
    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private SettingsViewModel _settings;
    [ObservableProperty] private string _title = "MeatSpeak";

    public ClientState ClientState => _connectionManager.ClientState;

    public MainWindowViewModel(
        ConnectionManager connectionManager,
        ClientDatabase db,
        IThemeService themeService,
        ServerListViewModel serverList,
        ChannelListViewModel channelList,
        ChatViewModel chat,
        MemberListViewModel memberList,
        MessageInputViewModel messageInput,
        VoiceStatusBarViewModel voiceStatusBar,
        SettingsViewModel settings)
    {
        _connectionManager = connectionManager;
        _db = db;
        _themeService = themeService;
        _serverList = serverList;
        _channelList = channelList;
        _chat = chat;
        _memberList = memberList;
        _messageInput = messageInput;
        _voiceStatusBar = voiceStatusBar;
        _settings = settings;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var profiles = _db.LoadServerProfiles();
        await _connectionManager.AutoConnectSavedServersAsync(profiles);
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }
}
