namespace TabFlow.Shared.Domain.Events;

public abstract record TenantEvent(DateTimeOffset OccurredAt)
{
    protected TenantEvent() : this(DateTimeOffset.UtcNow) { }
}

public sealed record OrderSubmittedEvent(
    Guid OrderId,
    Guid TableId,
    Guid SessionId,
    IReadOnlyList<OrderItemRoutingInfo> Items) : TenantEvent;

public sealed record OrderStatusChangedEvent(
    Guid OrderItemId,
    Guid OrderId,
    Guid StationId,
    string NewStatus) : TenantEvent;

public sealed record BillOpenedEvent(Guid BillId, Guid TableId) : TenantEvent;
public sealed record BillClosedEvent(Guid BillId, Guid TableId) : TenantEvent;
public sealed record BillMovedEvent(Guid BillId, Guid FromTableId, Guid ToTableId) : TenantEvent;
public sealed record BillMergedEvent(Guid TargetBillId, Guid SourceBillId) : TenantEvent;
public sealed record BillSplitEvent(Guid OriginalBillId, Guid NewBillId) : TenantEvent;

public sealed record TableOpenedEvent(Guid TableId, Guid SessionId) : TenantEvent;
public sealed record TableClosedEvent(Guid TableId, Guid BillId) : TenantEvent;

public sealed record DeviceConnectedEvent(Guid TableId, string TableLabel) : TenantEvent;
public sealed record DeviceDisconnectedEvent(Guid TableId, string TableLabel) : TenantEvent;

public sealed record OrderItemRoutingInfo(Guid OrderItemId, Guid StationId, string StationCode);
