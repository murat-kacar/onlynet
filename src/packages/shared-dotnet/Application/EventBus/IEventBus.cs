using TabFlow.Shared.Domain.Events;

namespace TabFlow.Shared.Application.EventBus;

public interface IEventBus
{
    void Publish(TenantEvent @event);
    IAsyncEnumerable<TenantEvent> Subscribe(CancellationToken cancellationToken = default);

    /// <summary>
    /// Diagnostic snapshot used by the
    /// <c>EventBusCapacityHealthCheck</c> registered under TD-0013
    /// step 4. Returns the number of currently-open subscriber
    /// channels and the maximum queued-event depth observed across
    /// them; the per-subscriber bound is fixed by the implementation
    /// (256 today). The pair is sufficient to compute saturation
    /// (max-queue / channel-bound) without exposing the channel
    /// objects themselves.
    /// </summary>
    EventBusCapacityStats GetCapacityStats();
}

public sealed record EventBusCapacityStats(
    int SubscriberCount,
    int MaxQueueDepth,
    int PerSubscriberCapacity);
