using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Data;

namespace MeatSpeak.Client.ViewModels;

public partial class ServerAddViewModel : ViewModelBase
{
    [ObservableProperty] private string _serverName = string.Empty;
    [ObservableProperty] private string _host = string.Empty;
    [ObservableProperty] private int _port = 6667;
    [ObservableProperty] private bool _useSsl;
    [ObservableProperty] private string _nickname = string.Empty;
    [ObservableProperty] private string? _username;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private string? _saslUsername;
    [ObservableProperty] private string? _saslPassword;
    [ObservableProperty] private bool _useIdentityAuth;
    [ObservableProperty] private string _autoJoinChannels = string.Empty;
    [ObservableProperty] private bool _autoConnect = true;
    [ObservableProperty] private string? _errorMessage;

    public event Action<ServerProfile>? ProfileCreated;
    public event Action? Cancelled;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "Server address is required";
            return;
        }

        if (string.IsNullOrWhiteSpace(Nickname))
        {
            ErrorMessage = "Nickname is required";
            return;
        }

        var profile = new ServerProfile
        {
            Name = string.IsNullOrWhiteSpace(ServerName) ? Host : ServerName,
            Host = Host.Trim(),
            Port = Port,
            UseSsl = UseSsl,
            Nickname = Nickname.Trim(),
            Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
            Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
            SaslUsername = string.IsNullOrWhiteSpace(SaslUsername) ? null : SaslUsername.Trim(),
            SaslPassword = string.IsNullOrWhiteSpace(SaslPassword) ? null : SaslPassword,
            UseIdentityAuth = UseIdentityAuth,
            AutoJoinChannels = AutoJoinChannels
                .Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList(),
            AutoConnect = AutoConnect,
        };

        ProfileCreated?.Invoke(profile);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke();
    }

    partial void OnUseSslChanged(bool value)
    {
        if (value && Port == 6667)
            Port = 6697;
        else if (!value && Port == 6697)
            Port = 6667;
    }
}
