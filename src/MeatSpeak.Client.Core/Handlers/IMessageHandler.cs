using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public interface IMessageHandler
{
    IEnumerable<string> HandledCommands { get; }
    Task HandleAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default);
}
