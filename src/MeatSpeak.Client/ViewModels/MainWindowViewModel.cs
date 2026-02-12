using System.ComponentModel;
using Avalonia.Threading;
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
    private ServerState? _trackedServer;

    [ObservableProperty] private ServerListViewModel _serverList;
    [ObservableProperty] private ChannelListViewModel _channelList;
    [ObservableProperty] private ChatViewModel _chat;
    [ObservableProperty] private MemberListViewModel _memberList;
    [ObservableProperty] private MessageInputViewModel _messageInput;
    [ObservableProperty] private VoiceStatusBarViewModel _voiceStatusBar;
    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private SettingsViewModel _settings;
    [ObservableProperty] private string _title = "MeatSpeak";
    [ObservableProperty] private ServerAddViewModel? _serverAddDialog;

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

        // Settings close
        _settings.CloseRequested += () => IsSettingsOpen = false;

        // Add Server dialog lifecycle
        _serverList.PropertyChanged += OnServerListPropertyChanged;

        // Server selection -> downstream refresh
        _connectionManager.ClientState.PropertyChanged += OnClientStatePropertyChanged;

        _ = InitializeAsync();
    }

    private void OnServerListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServerListViewModel.IsAddServerOpen) && ServerList.IsAddServerOpen)
        {
            var dialog = new ServerAddViewModel();
            dialog.ProfileCreated += profile =>
            {
                ServerList.AddServerCommand.Execute(profile);
                ServerList.IsAddServerOpen = false;
                ServerAddDialog = null;
            };
            dialog.Cancelled += () =>
            {
                ServerList.IsAddServerOpen = false;
                ServerAddDialog = null;
            };
            ServerAddDialog = dialog;
        }
    }

    private void OnClientStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ClientState.ActiveServer)) return;

        // Detach old server tracking
        if (_trackedServer is not null)
            _trackedServer.PropertyChanged -= OnActiveServerPropertyChanged;

        _trackedServer = _connectionManager.ClientState.ActiveServer;

        // Attach new server tracking
        if (_trackedServer is not null)
            _trackedServer.PropertyChanged += OnActiveServerPropertyChanged;

        Dispatcher.UIThread.Post(() =>
        {
            ChannelList.RefreshForServer();
            Chat.RefreshForChannel();
            MemberList.RefreshForChannel();
            MessageInput.UpdatePlaceholder();
        });
    }

    private void OnActiveServerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ServerState.ActiveChannelName)) return;

        Dispatcher.UIThread.Post(() =>
        {
            Chat.RefreshForChannel();
            MemberList.RefreshForChannel();
            MessageInput.UpdatePlaceholder();
        });
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
