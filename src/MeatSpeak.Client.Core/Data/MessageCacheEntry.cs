namespace MeatSpeak.Client.Core.Data;

public sealed class MessageCacheEntry
{
    public long Id { get; set; }
    public string ServerId { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string SenderNick { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int MessageType { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public bool IsOwnMessage { get; set; }
}
