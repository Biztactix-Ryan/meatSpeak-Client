namespace MeatSpeak.Client.Core.Data;

public enum ServerType
{
    Auto,
    MeatSpeak,
    StandardIrc,
}

public sealed class ServerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6667;
    public bool UseSsl { get; set; }
    public string Nickname { get; set; } = "meatuser";
    public string? Username { get; set; }
    public string? Realname { get; set; }
    public string? Password { get; set; }
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public bool UseIdentityAuth { get; set; }
    public string? IdentityDomain { get; set; }
    public List<string> AutoJoinChannels { get; set; } = [];
    public bool AutoConnect { get; set; }
    public int SortOrder { get; set; }
    public ServerType Type { get; set; } = ServerType.Auto;
}
