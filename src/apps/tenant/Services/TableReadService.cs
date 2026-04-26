using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

/// <summary>
/// EF Core implementation of <see cref="ITableReadService"/>. Owns
/// the <see cref="TenantDbContext"/> reads that `TablesController`
/// used to perform inline. Introduced in PR #29 under TD-0022 step 1.
/// </summary>
public sealed class TableReadService : ITableReadService
{
    private readonly TenantDbContext _context;

    public TableReadService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TableDto>> GetTablesAsync(CancellationToken ct = default)
    {
        return await _context.Stations
            .OrderBy(s => s.SortOrder)
            .Select(s => new TableDto(
                s.Id,
                s.Name,
                s.Code,
                s.IsActive,
                s.SortOrder))
            .ToListAsync(ct);
    }

    public async Task<TableDetailDto?> GetTableAsync(Guid id, CancellationToken ct = default)
    {
        var table = await _context.Stations.FindAsync(new object[] { id }, ct);
        if (table is null)
        {
            return null;
        }

        var hasOpenSession = await _context.CustomerSessions
            .AnyAsync(cs => cs.TableId == id && cs.IsOpen, ct);

        return new TableDetailDto(
            table.Id,
            table.Name,
            table.Code,
            table.Color,
            table.Type,
            table.IsActive,
            table.SortOrder,
            hasOpenSession);
    }
}
