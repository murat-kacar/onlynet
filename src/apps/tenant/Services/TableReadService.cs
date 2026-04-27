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
                s.SortOrder,
                _context.CustomerSessions.Any(cs => cs.TableId == s.Id && cs.IsOpen)))
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

    public async Task<TableWorkspaceDto?> GetTableWorkspaceAsync(Guid id, CancellationToken ct = default)
    {
        var table = await _context.Stations.FindAsync(new object[] { id }, ct);
        if (table is null)
        {
            return null;
        }

        var session = await _context.CustomerSessions
            .AsNoTracking()
            .Where(cs => cs.TableId == id && cs.IsOpen)
            .OrderByDescending(cs => cs.OpenedAt)
            .FirstOrDefaultAsync(ct);

        TableSessionWorkspaceDto? sessionWorkspace = null;
        if (session is not null)
        {
            var ticketCount = await _context.CustomerAccessTickets
                .CountAsync(ticket => ticket.SessionId == session.Id && ticket.IsValid, ct);

            var cartItems = await _context.CartItems
                .Where(item => item.SessionId == session.Id)
                .Join(
                    _context.MenuItems,
                    item => item.ItemId,
                    menuItem => menuItem.Id,
                    (item, menuItem) => new TableCartItemDto(
                        item.Id,
                        menuItem.Name,
                        item.Quantity,
                        menuItem.Price,
                        item.Note))
                .ToListAsync(ct);

            var orders = await _context.Orders
                .Where(order => order.SessionId == session.Id)
                .OrderByDescending(order => order.SubmittedAt)
                .Select(order => new TableSessionOrderDto(
                    order.Id,
                    order.TotalAmount,
                    order.SubmittedAt,
                    order.Items.Count))
                .ToListAsync(ct);

            sessionWorkspace = new TableSessionWorkspaceDto(
                session.Id,
                session.TableId,
                session.OpenedAt,
                ticketCount,
                cartItems,
                orders);
        }

        return new TableWorkspaceDto(
            table.Id,
            table.Name,
            table.Code,
            table.Color,
            table.Type,
            table.IsActive,
            table.SortOrder,
            session is not null,
            sessionWorkspace);
    }
}
