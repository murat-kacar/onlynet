using Microsoft.AspNetCore.Http;
using TabFlow.Shared.Domain;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Services;

public interface IPlatformAuditService
{
    Task LogAsync(
        Guid? actorId,
        string actorEmail,
        string action,
        string resourceType,
        string resourceId,
        Guid correlationId,
        string? changes = null,
        string? ip = null,
        string? userAgent = null,
        CancellationToken ct = default);
}

public class PlatformAuditService : IPlatformAuditService
{
    private readonly PlatformDbContext _context;

    public PlatformAuditService(PlatformDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        Guid? actorId,
        string actorEmail,
        string action,
        string resourceType,
        string resourceId,
        Guid correlationId,
        string? changes = null,
        string? ip = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var entry = PlatformAuditEntry.Create(
            actorId,
            actorEmail,
            action,
            resourceType,
            resourceId,
            correlationId,
            changes,
            ip,
            userAgent);

        _context.AuditLog.Add(entry);
        await _context.SaveChangesAsync(ct);
    }
}
