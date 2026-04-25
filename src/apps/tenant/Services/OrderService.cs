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
        var session = await _context.CustomerSessions.FindAsync(new object[] { request.SessionId }, ct);
        if (session == null || !session.IsOpen)
        {
            throw new InvalidOperationException($"Invalid session {request.SessionId}");
        }

        var checkoutToken = await _context.QrTokens
            .FirstOrDefaultAsync(t => t.Value == request.CheckoutProofToken && t.IsCheckoutProof, ct);

        if (checkoutToken == null || checkoutToken.ExpiresAt < DateTime.UtcNow)
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
        await _context.SaveChangesAsync(ct);

        session.Close();
        _context.CustomerSessions.Update(session);
        await _context.SaveChangesAsync(ct);

        return new SubmitOrderResult(order.Id, totalAmount);
    }
}
