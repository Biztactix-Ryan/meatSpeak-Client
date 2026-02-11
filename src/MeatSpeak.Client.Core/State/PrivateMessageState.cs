using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MeatSpeak.Client.Core.State;

public partial class PrivateMessageState : ObservableObject
{
    public string Nick { get; }

    [ObservableProperty] private int _unreadCount;
    [ObservableProperty] private bool _hasMention;

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public PrivateMessageState(string nick)
    {
        Nick = nick;
    }

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
        if (!message.IsOwnMessage)
            UnreadCount++;
    }

    public void ClearUnread()
    {
        UnreadCount = 0;
        HasMention = false;
    }
}
