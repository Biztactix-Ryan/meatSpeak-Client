using System.Net.Security;
using System.Net.Sockets;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Client.Core.State;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Connection;

public sealed class ServerConnection : IDisposable
{
    private TcpClient? _tcpClient;
    private Stream? _stream;
    private IrcMessageSender? _sender;
    private IrcMessageReceiver? _receiver;
    private CancellationTokenSource? _cts;
    private readonly MessageDispatcher _dispatcher;
    private int _reconnectAttempts;
    private static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
    ];

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public ServerState ServerState { get; }
    public bool IsMeatSpeak => ServerState.IsMeatSpeak;

    public event Action<ServerConnection>? Connected;
    public event Action<ServerConnection>? Disconnected;
    public event Action<ServerConnection, string>? ErrorOccurred;

    public ServerConnection(ServerProfile profile, MessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        ServerState = new ServerState(Id, profile);
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (ServerState.ConnectionState is ConnectionState.Connected or ConnectionState.Connecting or ConnectionState.Registering)
            return;

        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ServerState.ConnectionState = ConnectionState.Connecting;
        ServerState.ErrorMessage = null;

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ServerState.Profile.Host, ServerState.Profile.Port, _cts.Token);

            _stream = _tcpClient.GetStream();

            if (ServerState.Profile.UseSsl)
            {
                var sslStream = new SslStream(_stream, false, (_, _, _, _) => true);
                await sslStream.AuthenticateAsClientAsync(ServerState.Profile.Host);
                _stream = sslStream;
            }

            _sender = new IrcMessageSender(_stream);
            _receiver = new IrcMessageReceiver(_stream);

            _receiver.MessageReceived += msg => OnMessageReceived(msg);
            _receiver.Disconnected += () => OnDisconnected();
            _receiver.Error += ex => ErrorOccurred?.Invoke(this, ex.Message);

            // Start reading in background
            _ = Task.Run(() => _receiver.RunAsync(_cts.Token), _cts.Token);

            // Begin registration
            ServerState.ConnectionState = ConnectionState.Registering;
            await PerformRegistrationAsync(_cts.Token);

            _reconnectAttempts = 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ServerState.ConnectionState = ConnectionState.Error;
            ServerState.ErrorMessage = ex.Message;
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private async Task PerformRegistrationAsync(CancellationToken ct)
    {
        var profile = ServerState.Profile;

        // CAP negotiation
        await SendAsync("CAP LS 302", ct);

        // Server password
        if (!string.IsNullOrEmpty(profile.Password))
            await SendAsync($"PASS {profile.Password}", ct);

        // NICK + USER
        await SendAsync($"NICK {profile.Nickname}", ct);
        var username = profile.Username ?? profile.Nickname;
        var realname = profile.Realname ?? profile.Nickname;
        await SendAsync($"USER {username} 0 * :{realname}", ct);
    }

    private void OnMessageReceived(IrcMessage message)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _dispatcher.DispatchAsync(this, message);

                // Handle post-CAP-ACK flow
                if (message.Command.Equals("CAP", StringComparison.OrdinalIgnoreCase))
                {
                    var sub = message.GetParam(1)?.ToUpperInvariant();
                    if (sub == "ACK")
                    {
                        if (ServerState.EnabledCapabilities.Contains("sasl") &&
                            !string.IsNullOrEmpty(ServerState.Profile.SaslUsername))
                        {
                            await SaslHandler.InitiateSaslAsync(this, CancellationToken.None);
                        }
                        else
                        {
                            await SendAsync("CAP END");
                        }
                    }
                }

                // Auto-join channels after welcome
                if (message.Command == Numerics.Format(Numerics.RPL_ENDOFMOTD) ||
                    message.Command == "422")
                {
                    Connected?.Invoke(this);
                    await AutoJoinAsync();
                    ServerState.AvailableChannels.Clear();
                    await SendAsync("LIST");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        });
    }

    private async Task AutoJoinAsync()
    {
        foreach (var channel in ServerState.Profile.AutoJoinChannels)
        {
            if (!string.IsNullOrWhiteSpace(channel))
                await JoinChannelAsync(channel);
        }
    }

    private void OnDisconnected()
    {
        if (ServerState.ConnectionState == ConnectionState.Error) return;

        ServerState.ConnectionState = ConnectionState.Disconnected;
        Disconnected?.Invoke(this);
    }

    public async Task SendAsync(string rawLine, CancellationToken ct = default)
    {
        if (_sender is not null)
            await _sender.SendAsync(rawLine, ct);
    }

    public Task SendMessageAsync(string target, string text) =>
        SendAsync($"PRIVMSG {target} :{text}");

    public Task JoinChannelAsync(string channel, string? key = null) =>
        key is null
            ? SendAsync($"JOIN {channel}")
            : SendAsync($"JOIN {channel} {key}");

    public Task PartChannelAsync(string channel, string? reason = null) =>
        reason is null
            ? SendAsync($"PART {channel}")
            : SendAsync($"PART {channel} :{reason}");

    public Task ChangeNickAsync(string newNick) =>
        SendAsync($"NICK {newNick}");

    public async Task DisconnectAsync(string reason = "Leaving")
    {
        _cts?.Cancel();
        try
        {
            if (_sender is not null)
                await _sender.SendAsync($"QUIT :{reason}");
        }
        catch { }

        Cleanup();
        ServerState.ConnectionState = ConnectionState.Disconnected;
    }

    public async Task ReconnectAsync(CancellationToken ct = default)
    {
        var delay = ReconnectDelays[Math.Min(_reconnectAttempts, ReconnectDelays.Length - 1)];
        _reconnectAttempts++;
        ServerState.ConnectionState = ConnectionState.Reconnecting;

        Cleanup();
        await Task.Delay(delay, ct);
        await ConnectAsync(ct);
    }

    private void Cleanup()
    {
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _stream = null;
        _tcpClient = null;
        _sender = null;
        _receiver = null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        Cleanup();
    }
}
