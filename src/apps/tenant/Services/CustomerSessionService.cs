using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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

        if (qrToken == null || qrToken.IsCheckoutProof || qrToken.IsConsumed || qrToken.IsExpired)
        {
            throw new InvalidOperationException("Invalid or expired QR token");
        }

        var session = await _context.CustomerSessions
            .FirstOrDefaultAsync(s => s.TableId == qrToken.TableId && s.IsOpen, ct);

        if (session == null)
        {
            session = CustomerSession.Open(qrToken.TableId);
            _context.CustomerSessions.Add(session);
        }

        // TD-0017: generate an opaque device-binding cookie value
        // independent of the ticket id, so that a leaked URL carrying
        // the ticket id alone cannot resurrect the session. The
        // "N" format (32 hex chars, no dashes) keeps the cookie
        // header compact.
        var deviceCookieValue = Guid.NewGuid().ToString("N");
        var ticket = session.IssueTicket(deviceCookieValue);
        _context.CustomerAccessTickets.Add(ticket);

        // AC-022: a join QR is single-use. Consume it in the same
        // SaveChanges call that creates the access ticket so replaying
        // the same QR value cannot attach another browser.
        qrToken.Consume();
        await _context.SaveChangesAsync(ct);

        var table = await _context.Stations.FindAsync(new object[] { session.TableId }, ct);
        var tableLabel = table?.Name ?? $"Table {session.TableId}";

        return new OpenSessionResult(session.Id, ticket.Id, session.TableId, tableLabel, deviceCookieValue);
    }

    public async Task<CustomerSessionState?> GetSessionStateAsync(
        Guid ticketId,
        string deviceCookieValue,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(deviceCookieValue))
        {
            return null;
        }

        var ticket = await _context.CustomerAccessTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null || !ticket.IsValid)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(ticket.DeviceCookieValue),
                Encoding.UTF8.GetBytes(deviceCookieValue)))
        {
            return null;
        }

        var session = await _context.CustomerSessions.FindAsync(new object[] { ticket.SessionId }, ct);
        if (session == null || !session.IsOpen)
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

        return new CustomerSessionState(session.Id, ticket.Id, session.TableId, tableLabel, cartItems);
    }

    public async Task CloseSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.CustomerSessions.FindAsync(new object[] { sessionId }, ct);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.Close();
        await InvalidateTicketsAsync(session.Id, ct);
        _context.CustomerSessions.Update(session);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<CheckoutProofDto> IssueCheckoutProofAsync(Guid tableId, CancellationToken ct = default)
    {
        var hasOpenSession = await _context.CustomerSessions
            .AnyAsync(session => session.TableId == tableId && session.IsOpen, ct);

        if (!hasOpenSession)
        {
            throw new InvalidOperationException("An open session is required before issuing checkout proof.");
        }

        var tokenValue = $"checkout-{Guid.NewGuid():N}"[..21];
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        var token = QrToken.CreateCheckoutProof(tableId, tokenValue, expiresAt);

        _context.QrTokens.Add(token);
        await _context.SaveChangesAsync(ct);

        return new CheckoutProofDto(token.Value, tableId, token.ExpiresAt);
    }

    private async Task InvalidateTicketsAsync(Guid sessionId, CancellationToken ct)
    {
        var tickets = await _context.CustomerAccessTickets
            .Where(ticket => ticket.SessionId == sessionId && ticket.IsValid)
            .ToListAsync(ct);

        foreach (var ticket in tickets)
        {
            ticket.Invalidate();
        }
    }
}
