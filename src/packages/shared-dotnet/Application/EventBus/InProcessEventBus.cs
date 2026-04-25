using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TabFlow.Shared.Domain.Events;

namespace TabFlow.Shared.Application.EventBus;

public sealed class InProcessEventBus : IEventBus
{
    private readonly List<Channel<TenantEvent>> _subscribers = [];
    private readonly Lock _lock = new();

    public void Publish(TenantEvent @event)
    {
        List<Channel<TenantEvent>> snapshot;
        lock (_lock)
        {
            snapshot = [.. _subscribers];
        }

        foreach (var channel in snapshot)
            channel.Writer.TryWrite(@event);
    }

    public async IAsyncEnumerable<TenantEvent> Subscribe(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<TenantEvent>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        lock (_lock)
            _subscribers.Add(channel);

        try
        {
            await foreach (var @event in channel.Reader.ReadAllAsync(cancellationToken))
                yield return @event;
        }
        finally
        {
            lock (_lock)
                _subscribers.Remove(channel);
            channel.Writer.TryComplete();
        }
    }
}
