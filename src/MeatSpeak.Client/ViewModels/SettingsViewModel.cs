using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Services;

namespace MeatSpeak.Client.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly UserPreferences _preferences;
    private readonly IThemeService _themeService;

    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private bool _showTimestamps;
    [ObservableProperty] private bool _desktopNotifications;
    [ObservableProperty] private bool _notifyOnMention;
    [ObservableProperty] private bool _notifyOnPm;
    [ObservableProperty] private string _timestampFormat;
    [ObservableProperty] private int _maxMessagesPerChannel;

    public event Action? CloseRequested;

    public SettingsViewModel(UserPreferences preferences, IThemeService themeService)
    {
        _preferences = preferences;
        _themeService = themeService;

        // Load current values
        _isDarkTheme = _preferences.Theme == "Dark";
        _showTimestamps = _preferences.ShowTimestamps;
        _desktopNotifications = _preferences.DesktopNotifications;
        _notifyOnMention = _preferences.NotifyOnMention;
        _notifyOnPm = _preferences.NotifyOnPm;
        _timestampFormat = _preferences.TimestampFormat;
        _maxMessagesPerChannel = _preferences.MaxMessagesPerChannel;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _themeService.SetTheme(value ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light);
    }

    partial void OnShowTimestampsChanged(bool value) => _preferences.ShowTimestamps = value;
    partial void OnDesktopNotificationsChanged(bool value) => _preferences.DesktopNotifications = value;
    partial void OnNotifyOnMentionChanged(bool value) => _preferences.NotifyOnMention = value;
    partial void OnNotifyOnPmChanged(bool value) => _preferences.NotifyOnPm = value;
    partial void OnTimestampFormatChanged(string value) => _preferences.TimestampFormat = value;
    partial void OnMaxMessagesPerChannelChanged(int value) => _preferences.MaxMessagesPerChannel = value;

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }
}
