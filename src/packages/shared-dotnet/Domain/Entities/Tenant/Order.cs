using TabFlow.Shared.Domain.Enums;

namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class Order
{
    public Guid Id { get; private set; }
    public Guid TableId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid? BillId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public string? Note { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public static Order Create(
        Guid tableId,
        Guid sessionId,
        Guid ticketId,
        IEnumerable<OrderItem> items,
        string? note = null)
    {
        var itemList = items.ToList();
        var total = itemList.Sum(i => i.UnitPrice * i.Quantity);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TableId = tableId,
            SessionId = sessionId,
            TicketId = ticketId,
            TotalAmount = total,
            SubmittedAt = DateTimeOffset.UtcNow,
            Note = note
        };

        order._items.AddRange(itemList);
        return order;
    }

    public void AssignToBill(Guid billId) => BillId = billId;
}
