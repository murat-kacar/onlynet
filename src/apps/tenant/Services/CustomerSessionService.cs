using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

public class CustomerSessionService : ICustomerSessionService
{
    private readonly TenantDbContext _context;

    public CustomerSessionService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<OpenSessionResult> OpenSessionAsync(string qrTokenValue, CancellationToken ct = default)
    {
        var qrToken = await _context.QrTokens
            .FirstOrDefaultAsync(t => t.Value == qrTokenValue, ct);

        if (qrToken == null || qrToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired QR token");
        }

        var session = await _context.CustomerSessions
            .FirstOrDefaultAsync(s => s.TableId == qrToken.TableId && s.IsOpen, ct);

        if (session == null)
        {
            throw new InvalidOperationException($"No active session found for table {qrToken.TableId}");
        }

        // TD-0017: generate an opaque device-binding cookie value
        // independent of the ticket id, so that a leaked URL carrying
        // the ticket id alone cannot resurrect the session. The
        // "N" format (32 hex chars, no dashes) keeps the cookie
        // header compact.
        var deviceCookieValue = Guid.NewGuid().ToString("N");
        var ticket = session.IssueTicket(deviceCookieValue);
        await _context.SaveChangesAsync(ct);

        var table = await _context.Stations.FindAsync(new object[] { session.TableId }, ct);
        var tableLabel = table?.Name ?? $"Table {session.TableId}";

        return new OpenSessionResult(session.Id, ticket.Id, tableLabel, deviceCookieValue);
    }

    public async Task<CustomerSessionState?> GetSessionStateAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _context.CustomerAccessTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null || !ticket.IsValid)
        {
            return null;
        }

        var session = await _context.CustomerSessions.FindAsync(new object[] { ticket.SessionId }, ct);
        if (session == null)
        {
            return null;
        }

        var table = await _context.Stations.FindAsync(new object[] { session.TableId }, ct);
        var tableLabel = table?.Name ?? $"Table {session.TableId}";

        var cartItems = await _context.CartItems
            .Join(_context.MenuItems, ci => ci.ItemId, mi => mi.Id, (ci, mi) => new { ci, mi })
            .Where(x => x.ci.SessionId == session.Id)
            .Select(x => new CartItemSummary(x.ci.ItemId, x.mi.Name, x.ci.Quantity, x.mi.Price, x.ci.Note))
            .ToListAsync(ct);

        return new CustomerSessionState(session.Id, ticket.Id, tableLabel, cartItems);
    }

    public async Task CloseSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.CustomerSessions.FindAsync(new object[] { sessionId }, ct);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.Close();
        _context.CustomerSessions.Update(session);
        await _context.SaveChangesAsync(ct);
    }
}
