using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Audio;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class VoiceStatusBarViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;
    private readonly VoiceEngine _voiceEngine;

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _channelName = string.Empty;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isDeafened;

    public VoiceStatusBarViewModel(ConnectionManager connectionManager, VoiceEngine voiceEngine)
    {
        _connectionManager = connectionManager;
        _voiceEngine = voiceEngine;
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _voiceEngine.SetMuted(IsMuted);
    }

    [RelayCommand]
    private void ToggleDeafen()
    {
        IsDeafened = !IsDeafened;
        _voiceEngine.SetDeafened(IsDeafened);
        if (IsDeafened) IsMuted = true;
    }

    [RelayCommand]
    private async Task DisconnectVoiceAsync()
    {
        await _voiceEngine.DisconnectAsync();
        IsVisible = false;
    }

    public void UpdateState()
    {
        IsVisible = _voiceEngine.IsActive;
        IsMuted = _voiceEngine.IsMuted;
        IsDeafened = _voiceEngine.IsDeafened;
        UserName = _connectionManager.ClientState.ActiveServer?.CurrentNick ?? string.Empty;
    }
}
