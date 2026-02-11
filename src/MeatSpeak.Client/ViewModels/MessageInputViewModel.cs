using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Helpers;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.ViewModels;

public partial class MessageInputViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;

    [ObservableProperty] private string _messageText = string.Empty;
    [ObservableProperty] private string _placeholder = "Message #channel";

    public MessageInputViewModel(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = MessageText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var server = _connectionManager.ClientState.ActiveServer;
        if (server is null) return;

        var target = server.ActiveChannelName;
        if (target is null) return;

        var connection = _connectionManager.FindConnection(server.ConnectionId);
        if (connection is null) return;

        // Handle client commands
        if (text.StartsWith('/'))
        {
            await HandleCommandAsync(connection, server, target, text);
        }
        else
        {
            await connection.SendMessageAsync(target, text);

            // Add own message to state
            var channel = server.FindChannel(target);
            var pm = server.PrivateMessages.FirstOrDefault(p =>
                p.Nick.Equals(target, StringComparison.OrdinalIgnoreCase));

            var chatMsg = new ChatMessage
            {
                SenderNick = server.CurrentNick,
                Content = text,
                Type = ChatMessageType.Normal,
                IsOwnMessage = true,
            };

            if (channel is not null)
                channel.AddMessage(chatMsg);
            else
                pm?.AddMessage(chatMsg);
        }

        MessageText = string.Empty;
    }

    private async Task HandleCommandAsync(ServerConnection connection, ServerState server, string target, string text)
    {
        var parts = text.Split(' ', 2);
        var command = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        switch (command)
        {
            case "/join":
                if (!string.IsNullOrWhiteSpace(args))
                    await connection.JoinChannelAsync(args.Split(' ')[0], args.Contains(' ') ? args.Split(' ', 2)[1] : null);
                break;

            case "/part":
                var partChannel = string.IsNullOrWhiteSpace(args) ? target : args;
                await connection.PartChannelAsync(partChannel);
                break;

            case "/nick":
                if (!string.IsNullOrWhiteSpace(args))
                    await connection.ChangeNickAsync(args.Trim());
                break;

            case "/me":
                if (!string.IsNullOrWhiteSpace(args))
                {
                    var action = IrcTextFormatter.FormatAction(args);
                    await connection.SendAsync($"PRIVMSG {target} :{action}");

                    var channel = server.FindChannel(target);
                    channel?.AddMessage(new ChatMessage
                    {
                        SenderNick = server.CurrentNick,
                        Content = args,
                        Type = ChatMessageType.Action,
                        IsOwnMessage = true,
                    });
                }
                break;

            case "/msg":
                var msgParts = args.Split(' ', 2);
                if (msgParts.Length == 2)
                {
                    await connection.SendMessageAsync(msgParts[0], msgParts[1]);
                    var pm = server.GetOrCreatePm(msgParts[0]);
                    pm.AddMessage(new ChatMessage
                    {
                        SenderNick = server.CurrentNick,
                        Content = msgParts[1],
                        Type = ChatMessageType.Normal,
                        IsOwnMessage = true,
                    });
                }
                break;

            case "/topic":
                await connection.SendAsync($"TOPIC {target} :{args}");
                break;

            case "/kick":
                var kickParts = args.Split(' ', 2);
                if (kickParts.Length >= 1)
                {
                    var reason = kickParts.Length > 1 ? $" :{kickParts[1]}" : string.Empty;
                    await connection.SendAsync($"KICK {target} {kickParts[0]}{reason}");
                }
                break;

            case "/mode":
                await connection.SendAsync($"MODE {target} {args}");
                break;

            case "/whois":
                if (!string.IsNullOrWhiteSpace(args))
                    await connection.SendAsync($"WHOIS {args.Trim()}");
                break;

            case "/list":
                await connection.SendAsync("LIST");
                break;

            case "/invite":
                if (!string.IsNullOrWhiteSpace(args))
                    await connection.SendAsync($"INVITE {args.Trim()} {target}");
                break;

            case "/quit":
                await connection.DisconnectAsync(string.IsNullOrWhiteSpace(args) ? "Leaving" : args);
                break;

            case "/raw":
                if (!string.IsNullOrWhiteSpace(args))
                    await connection.SendAsync(args);
                break;

            default:
                // Unknown command — send as raw IRC
                await connection.SendAsync(text[1..]);
                break;
        }
    }

    public void UpdatePlaceholder()
    {
        var server = _connectionManager.ClientState.ActiveServer;
        var channel = server?.ActiveChannelName;
        Placeholder = channel is not null ? $"Message {channel}" : "Select a channel";
    }
}
