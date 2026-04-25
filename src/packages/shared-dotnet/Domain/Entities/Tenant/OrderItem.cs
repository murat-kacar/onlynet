using TabFlow.Shared.Domain.Enums;

namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ItemId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Note { get; private set; }
    public Guid StationId { get; private set; }
    public OrderItemStatus Status { get; private set; }
    public DateTimeOffset? PreparingAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? ServedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    private OrderItem() { }

    public static OrderItem Create(
        Guid itemId,
        string itemName,
        int quantity,
        decimal unitPrice,
        Guid stationId,
        string? note = null)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            ItemName = itemName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            StationId = stationId,
            Note = note,
            Status = OrderItemStatus.Submitted
        };
    }

    internal void SetOrderId(Guid orderId) => OrderId = orderId;

    public void StartPreparing()
    {
        Status = OrderItemStatus.Preparing;
        PreparingAt = DateTimeOffset.UtcNow;
    }

    public void MarkReady()
    {
        Status = OrderItemStatus.Ready;
        ReadyAt = DateTimeOffset.UtcNow;
    }

    public void MarkServed()
    {
        Status = OrderItemStatus.Served;
        ServedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = OrderItemStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }
}
