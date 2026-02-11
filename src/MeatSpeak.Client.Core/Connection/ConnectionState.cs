namespace MeatSpeak.Client.Core.Connection;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Registering,
    Connected,
    Authenticated,
    Reconnecting,
    Error,
}
