using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.DataProtection;
using TabFlow.Shared.Domain.Enums;

namespace TabFlow.Shared.Domain.Entities.Tenant;

/// <summary>
/// A submitted customer order. Per TD-0018 each order carries an
/// <see cref="IdempotencyKey"/>; the unique index over
/// <c>(SessionId, IdempotencyKey)</c> guarantees that a duplicate
/// `POST /api/public/orders` (e.g. a customer who taps Submit twice
/// on a flaky network) cannot produce a second order — the second
/// insert fails on the unique constraint and the service returns the
/// original result.
/// </summary>
[Index(nameof(SessionId), nameof(IdempotencyKey), IsUnique = true)]
public sealed class Order
{
    public Guid Id { get; private set; }
    public Guid TableId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid? BillId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>
    /// Customer-provided note that may contain personal data
    /// (e.g., dietary restrictions). Per TD-0007.
    /// </summary>
    [DataClass(DataClassification.Sensitive)]
    public string? Note { get; private set; }

    /// <summary>
    /// Caller-supplied opaque value used to deduplicate retries. The
    /// scope of uniqueness is the customer session (a different
    /// session can reuse the same key without collision).
    /// </summary>
    public string IdempotencyKey { get; private set; } = default!;

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public static Order Create(
        Guid tableId,
        Guid sessionId,
        Guid ticketId,
        string idempotencyKey,
        IEnumerable<OrderItem> items,
        string? note = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(idempotencyKey);

        var itemList = items.ToList();
        var total = itemList.Sum(i => i.UnitPrice * i.Quantity);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TableId = tableId,
            SessionId = sessionId,
            TicketId = ticketId,
            IdempotencyKey = idempotencyKey,
            TotalAmount = total,
            SubmittedAt = DateTimeOffset.UtcNow,
            Note = note
        };

        order._items.AddRange(itemList);
        return order;
    }

    public void AssignToBill(Guid billId) => BillId = billId;
}
