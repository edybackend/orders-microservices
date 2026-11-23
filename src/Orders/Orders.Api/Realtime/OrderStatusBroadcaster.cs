using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Orders.Application.Abstractions;

namespace Orders.Api.Realtime;

/// <summary>
/// Fans out status changes to every connected browser. Each subscriber gets its own bounded channel,
/// so a slow client drops old updates instead of slowing down the message consumer.
/// Scope: one API instance. With several replicas this would move to a backplane (e.g. Redis pub/sub).
/// </summary>
internal sealed class OrderStatusBroadcaster : IOrderStatusNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<OrderStatusChanged>> _subscribers = new();

    public ValueTask NotifyAsync(OrderStatusChanged change, CancellationToken cancellationToken)
    {
        foreach (var channel in _subscribers.Values)
        {
            channel.Writer.TryWrite(change);
        }

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<OrderStatusChanged> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<OrderStatusChanged>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
        _subscribers[id] = channel;
        try
        {
            await foreach (var change in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return change;
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }
}
