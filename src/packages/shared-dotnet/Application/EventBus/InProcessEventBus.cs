using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TabFlow.Shared.Domain.Events;

namespace TabFlow.Shared.Application.EventBus;

public sealed class InProcessEventBus : IEventBus
{
    /// <summary>
    /// Per-subscriber bounded-channel capacity. Matches the value
    /// passed to <c>Channel.CreateBounded</c> in <see cref="Subscribe"/>;
    /// surfaced here so the health-check stats can compute saturation
    /// without re-declaring the constant.
    /// </summary>
    public const int SubscriberChannelCapacity = 256;

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
        var channel = Channel.CreateBounded<TenantEvent>(new BoundedChannelOptions(SubscriberChannelCapacity)
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

    public EventBusCapacityStats GetCapacityStats()
    {
        int subscriberCount;
        int maxDepth = 0;
        lock (_lock)
        {
            subscriberCount = _subscribers.Count;
            foreach (var channel in _subscribers)
            {
                if (channel.Reader.Count > maxDepth)
                {
                    maxDepth = channel.Reader.Count;
                }
            }
        }
        return new EventBusCapacityStats(subscriberCount, maxDepth, SubscriberChannelCapacity);
    }
}
