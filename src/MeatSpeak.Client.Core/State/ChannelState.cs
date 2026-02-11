using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MeatSpeak.Client.Core.State;

public partial class ChannelState : ObservableObject
{
    public string Name { get; }

    [ObservableProperty] private string _topic = string.Empty;
    [ObservableProperty] private string? _topicSetBy;
    [ObservableProperty] private DateTimeOffset? _topicSetAt;
    [ObservableProperty] private string _modes = string.Empty;
    [ObservableProperty] private int _unreadCount;
    [ObservableProperty] private bool _hasMention;
    [ObservableProperty] private bool _isJoined;

    public ObservableCollection<ChatMessage> Messages { get; } = [];
    public ObservableCollection<UserState> Members { get; } = [];
    public ObservableCollection<BanEntry> Bans { get; } = [];

    public event Action? MemberPrefixChanged;

    public void RaiseMemberPrefixChanged() => MemberPrefixChanged?.Invoke();

    public ChannelState(string name)
    {
        Name = name;
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

    public UserState? FindMember(string nick) =>
        Members.FirstOrDefault(m => m.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase));

    public void RemoveMember(string nick)
    {
        var member = FindMember(nick);
        if (member is not null)
            Members.Remove(member);
    }
}
