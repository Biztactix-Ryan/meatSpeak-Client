namespace MeatSpeak.Client.Core.State;

public record BanEntry(string Mask, string? SetBy, DateTimeOffset? SetAt);
