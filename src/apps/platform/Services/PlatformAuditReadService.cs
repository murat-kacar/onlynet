using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Services;

public interface IPlatformAuditReadService
{
    Task<IReadOnlyList<PlatformAuditEntryDto>> GetEntriesAsync(CancellationToken ct = default);
}

public sealed class PlatformAuditReadService : IPlatformAuditReadService
{
    private readonly PlatformDbContext _context;

    public PlatformAuditReadService(PlatformDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PlatformAuditEntryDto>> GetEntriesAsync(CancellationToken ct = default)
    {
        return await _context.AuditLog
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new PlatformAuditEntryDto(
                entry.Id,
                entry.ActorEmail,
                entry.Action,
                entry.ResourceType,
                entry.ResourceId,
                entry.Ip,
                entry.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed record PlatformAuditEntryDto(
    Guid Id,
    string ActorEmail,
    string Action,
    string ResourceType,
    string ResourceId,
    string? Ip,
    DateTimeOffset CreatedAt);
