using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Handlers;

public sealed class MessageDispatcher
{
    private readonly Dictionary<string, List<IMessageHandler>> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IMessageHandler> _catchAll = [];

    public void Register(IMessageHandler handler)
    {
        foreach (var command in handler.HandledCommands)
        {
            if (!_handlers.TryGetValue(command, out var list))
            {
                list = [];
                _handlers[command] = list;
            }
            list.Add(handler);
        }
    }

    public void RegisterCatchAll(IMessageHandler handler)
    {
        _catchAll.Add(handler);
    }

    public async Task DispatchAsync(Connection.ServerConnection connection, IrcMessage message, CancellationToken ct = default)
    {
        if (_handlers.TryGetValue(message.Command, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(connection, message, ct);
            }
        }

        foreach (var handler in _catchAll)
        {
            await handler.HandleAsync(connection, message, ct);
        }
    }
}
