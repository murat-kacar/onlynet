namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class QrToken
{
    public Guid Id { get; private set; }
    public string Value { get; private set; } = default!;
    public Guid TableId { get; private set; }
    public bool IsCheckoutProof { get; private set; }
    public bool IsConsumed { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private QrToken() { }

    public static QrToken CreateJoinToken(Guid tableId, string value, DateTimeOffset expiresAt)
    {
        return new QrToken
        {
            Id = Guid.NewGuid(),
            Value = value,
            TableId = tableId,
            IsCheckoutProof = false,
            IsConsumed = false,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static QrToken CreateCheckoutProof(Guid tableId, string value, DateTimeOffset expiresAt)
    {
        return new QrToken
        {
            Id = Guid.NewGuid(),
            Value = value,
            TableId = tableId,
            IsCheckoutProof = true,
            IsConsumed = false,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    public void Consume() => IsConsumed = true;
}
