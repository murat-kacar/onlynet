using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

public sealed class TableCommandService : ITableCommandService
{
    private readonly TenantDbContext _context;

    public TableCommandService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<TableDetailDto> CreateAsync(CreateTableRequest request, CancellationToken ct = default)
    {
        await EnsureCodeAvailableAsync(request.Code, null, ct);

        var table = TabFlow.Shared.Domain.Entities.Tenant.Station.Create(
            request.Name.Trim(),
            request.Code.Trim(),
            request.Color.Trim(),
            request.Type.Trim(),
            request.SortOrder);

        _context.Stations.Add(table);
        await _context.SaveChangesAsync(ct);

        return new TableDetailDto(
            table.Id,
            table.Name,
            table.Code,
            table.Color,
            table.Type,
            table.IsActive,
            table.SortOrder,
            false);
    }

    public async Task<TableDetailDto?> UpdateAsync(Guid id, UpdateTableRequest request, CancellationToken ct = default)
    {
        var table = await _context.Stations.FindAsync(new object[] { id }, ct);
        if (table is null)
        {
            return null;
        }

        await EnsureCodeAvailableAsync(request.Code, id, ct);

        table.Update(
            request.Name.Trim(),
            request.Code.Trim(),
            request.Color.Trim(),
            request.Type.Trim(),
            request.IsActive,
            request.SortOrder);

        await _context.SaveChangesAsync(ct);

        var hasOpenSession = await _context.CustomerSessions
            .AnyAsync(session => session.TableId == id && session.IsOpen, ct);

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

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var table = await _context.Stations.FindAsync(new object[] { id }, ct);
        if (table is null)
        {
            return;
        }

        var hasLinkedHistory =
            await _context.CustomerSessions.AnyAsync(session => session.TableId == id, ct) ||
            await _context.Orders.AnyAsync(order => order.TableId == id, ct) ||
            await _context.QrTokens.AnyAsync(token => token.TableId == id, ct) ||
            await _context.MenuItems.AnyAsync(item => item.StationId == id, ct) ||
            await _context.Categories.AnyAsync(category => category.DefaultStationId == id, ct);

        if (hasLinkedHistory)
        {
            throw new InvalidOperationException("This table already has linked session, order, or catalog history and cannot be deleted.");
        }

        _context.Stations.Remove(table);
        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeAvailableAsync(string code, Guid? currentId, CancellationToken ct)
    {
        var normalizedCode = code.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Table code is required.");
        }

        var exists = await _context.Stations
            .AnyAsync(
                station => station.Code == normalizedCode &&
                           (!currentId.HasValue || station.Id != currentId.Value),
                ct);

        if (exists)
        {
            throw new InvalidOperationException("Table code must be unique.");
        }
    }
}
