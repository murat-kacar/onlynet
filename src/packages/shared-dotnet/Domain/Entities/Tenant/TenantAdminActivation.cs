namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class TenantAdminActivation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = default!;
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private TenantAdminActivation() { }

    public static TenantAdminActivation Create(Guid userId, string email, string tokenHash, DateTimeOffset expiresAt)
    {
        return new TenantAdminActivation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool IsActive(DateTimeOffset nowUtc) =>
        ConsumedAt is null && ExpiresAt > nowUtc;

    public void Consume()
    {
        ConsumedAt = DateTimeOffset.UtcNow;
    }
}
