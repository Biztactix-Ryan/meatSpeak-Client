namespace MeatSpeak.Client.Core.State;

public enum ChatMessageType
{
    Normal,
    Action,
    Notice,
    System,
    Join,
    Part,
    Quit,
    Kick,
    NickChange,
    TopicChange,
    ModeChange,
}

public sealed class ChatMessage
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string SenderNick { get; init; } = string.Empty;
    public string? SenderPrefix { get; init; }
    public string Content { get; init; } = string.Empty;
    public ChatMessageType Type { get; init; } = ChatMessageType.Normal;
    public bool IsOwnMessage { get; init; }
}
