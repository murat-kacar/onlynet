using TabFlow.Shared.Domain.Events;

namespace TabFlow.Shared.Application.EventBus;

public interface IEventBus
{
    void Publish(TenantEvent @event);
    IAsyncEnumerable<TenantEvent> Subscribe(CancellationToken cancellationToken = default);
}
