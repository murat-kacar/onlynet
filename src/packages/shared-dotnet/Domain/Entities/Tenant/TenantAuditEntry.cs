namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class TenantAuditEntry
{
    public Guid Id { get; private set; }
    public Guid? ActorId { get; private set; }
    public string ActorEmail { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string ResourceType { get; private set; } = default!;
    public string ResourceId { get; private set; } = default!;
    public string? Changes { get; private set; }
    public string? Ip { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TenantAuditEntry() { }

    public static TenantAuditEntry Create(
        Guid? actorId,
        string actorEmail,
        string action,
        string resourceType,
        string resourceId,
        Guid correlationId,
        string? changes = null,
        string? ip = null,
        string? userAgent = null)
    {
        return new TenantAuditEntry
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorEmail = actorEmail,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            CorrelationId = correlationId,
            Changes = changes,
            Ip = ip,
            UserAgent = userAgent,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
