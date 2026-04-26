using TabFlow.Shared.Domain.DataProtection;

namespace TabFlow.Shared.Domain.Entities.Platform;

public sealed class PlatformAuditEntry
{
    public Guid Id { get; private set; }
    public Guid? ActorId { get; private set; }
    [DataClass(DataClassification.Sensitive)]
    public string ActorEmail { get; private set; } = default!;
    [DataClass(DataClassification.Internal)]
    public string Action { get; private set; } = default!;
    [DataClass(DataClassification.Internal)]
    public string ResourceType { get; private set; } = default!;
    [DataClass(DataClassification.Internal)]
    public string ResourceId { get; private set; } = default!;
    [DataClass(DataClassification.Sensitive)]
    public string? Changes { get; private set; }
    [DataClass(DataClassification.Sensitive)]
    public string? Ip { get; private set; }
    [DataClass(DataClassification.Sensitive)]
    public string? UserAgent { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PlatformAuditEntry() { }

    public static PlatformAuditEntry Create(
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
        return new PlatformAuditEntry
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
