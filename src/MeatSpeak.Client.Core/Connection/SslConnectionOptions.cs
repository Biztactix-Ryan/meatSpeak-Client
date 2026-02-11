namespace MeatSpeak.Client.Core.Connection;

public sealed class SslConnectionOptions
{
    public bool UseSsl { get; init; }
    public bool AcceptAllCertificates { get; init; }
    public string? ClientCertificatePath { get; init; }
}
