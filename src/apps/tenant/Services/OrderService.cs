using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

public class OrderService : IOrderService
{
    private readonly TenantDbContext _context;

    public OrderService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<SubmitOrderResult> SubmitAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        // AC-030: every customer order requires a still-open session for
        // the originating device. The session-to-cookie binding is the
        // missing half tracked under TD-0015 step 4 follow-ups.
        var session = await _context.CustomerSessions.FindAsync(new object[] { request.SessionId }, ct);
        if (session == null || !session.IsOpen)
        {
            throw new InvalidOperationException($"Invalid session {request.SessionId}");
        }

        // AC-031: a checkout-proof token MUST be a fresh QR scan for the
        // same table as the order. AC-032: the token MUST NOT be reusable.
        // The query intentionally narrows on `IsCheckoutProof`,
        // `IsConsumed == false`, and the matching `TableId` so a stolen
        // join token, a previously-consumed token, or a token from a
        // neighbouring table all fail closed without leaking which gate
        // tripped.
        var checkoutToken = await _context.QrTokens
            .FirstOrDefaultAsync(
                t => t.Value == request.CheckoutProofToken &&
                     t.IsCheckoutProof &&
                     !t.IsConsumed &&
                     t.TableId == request.TableId,
                ct);

        if (checkoutToken == null || checkoutToken.IsExpired)
        {
            throw new InvalidOperationException("Invalid or expired checkout proof token");
        }

        var cartItems = await _context.CartItems
            .Where(ci => ci.SessionId == request.SessionId)
            .Join(_context.MenuItems, ci => ci.ItemId, mi => mi.Id, (ci, mi) => new { ci, mi })
            .ToListAsync(ct);

        var orderItems = cartItems.Select(x => OrderItem.Create(
            x.ci.ItemId,
            x.mi.Name,
            x.ci.Quantity,
            x.mi.Price,
            request.TableId,
            x.ci.Note)).ToList();

        var totalAmount = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        var order = Order.Create(
            request.TableId,
            request.SessionId,
            request.TicketId,
            orderItems,
            request.Note);
        _context.Orders.Add(order);

        // AC-032: consume the checkout-proof token in the same SaveChanges
        // call as the order insert so a duplicate submit cannot race
        // through between the validation read and the order write.
        // AC-036: a successful submission closes the originating session;
        // a second order from the same cookie MUST require a fresh QR
        // scan, which this Close() guarantees through the IsOpen filter
        // above on the next call.
        checkoutToken.Consume();
        session.Close();
        await _context.SaveChangesAsync(ct);

        return new SubmitOrderResult(order.Id, totalAmount);
    }
}
