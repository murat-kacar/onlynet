using TabFlow.Shared.Domain.DataProtection;

namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class CartItem
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid ItemId { get; private set; }
    public int Quantity { get; private set; }

    /// <summary>
    /// Customer-provided note that may contain personal data
    /// (e.g., dietary restrictions). Per TD-0007.
    /// </summary>
    [DataClass(DataClassification.Sensitive)]
    public string? Note { get; private set; }

    private CartItem() { }

    public static CartItem Create(Guid sessionId, Guid itemId, int quantity, string? note = null)
    {
        return new CartItem
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ItemId = itemId,
            Quantity = quantity,
            Note = note
        };
    }

    public void UpdateQuantity(int quantity) => Quantity = quantity;
    public void UpdateNote(string? note) => Note = note;
}
