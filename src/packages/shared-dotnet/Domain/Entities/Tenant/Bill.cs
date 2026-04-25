using TabFlow.Shared.Domain.Enums;

namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class Bill
{
    public Guid Id { get; private set; }
    public Guid TableId { get; private set; }
    public BillStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? PaymentMethod { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Bill() { }

    public static Bill Open(Guid tableId)
    {
        return new Bill
        {
            Id = Guid.NewGuid(),
            TableId = tableId,
            Status = BillStatus.Open,
            TotalAmount = 0,
            OpenedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddAmount(decimal amount) => TotalAmount += amount;

    public void Close(string paymentMethod)
    {
        Status = BillStatus.Closed;
        PaymentMethod = paymentMethod;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    public void Reassign(Guid newTableId) => TableId = newTableId;
}
