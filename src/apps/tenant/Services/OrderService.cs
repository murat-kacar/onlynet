using System.Security.Cryptography;
using System.Text;
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

    public async Task<SubmitOrderResult> SubmitAsync(
        SubmitOrderRequest request,
        string deviceCookieValue,
        CancellationToken ct = default)
    {
        // AC-030 (device-binding half, TD-0017): the caller MUST
        // forward the `tabflow_session_device` cookie value the
        // browser holds; an empty value means the call did not
        // originate from a real customer browser and fails closed.
        if (string.IsNullOrEmpty(deviceCookieValue))
        {
            throw new InvalidOperationException("Device cookie missing.");
        }

        var ticket = await _context.CustomerAccessTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Invalid ticket {request.TicketId}");
        }
        if (ticket.SessionId != request.SessionId)
        {
            throw new InvalidOperationException("Ticket does not belong to the requested session.");
        }
        // Constant-time compare so a timing oracle on the device
        // cookie cannot leak prefix bits.
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(ticket.DeviceCookieValue),
                Encoding.UTF8.GetBytes(deviceCookieValue)))
        {
            throw new InvalidOperationException("Device cookie mismatch.");
        }

        // TD-0018: retrying the same submit with the same idempotency
        // key should return the original result, even though the first
        // successful submit has already consumed the checkout proof and
        // closed the session.
        var existingOrder = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.SessionId == request.SessionId &&
                     o.IdempotencyKey == request.IdempotencyKey,
                ct);

        if (existingOrder != null)
        {
            if (existingOrder.TicketId != request.TicketId || existingOrder.TableId != request.TableId)
            {
                throw new InvalidOperationException("Idempotency key reused with a different order request.");
            }

            return new SubmitOrderResult(existingOrder.Id, existingOrder.TotalAmount);
        }

        if (!ticket.IsValid)
        {
            throw new InvalidOperationException($"Invalid ticket {request.TicketId}");
        }

        // AC-030 (still-open half): every customer order requires a
        // still-open session.
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
            request.IdempotencyKey,
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

    public async Task<OrderDetailDto?> GetOrderDetailAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return null;
        }

        var table = await _context.Stations.FindAsync(new object[] { order.TableId }, ct);
        var tableLabel = table?.Name ?? $"Table {order.TableId}";

        var items = order.Items
            .Select(i => new OrderItemDto(
                i.Id,
                i.ItemName,
                i.Quantity,
                i.UnitPrice,
                i.Status.ToString()))
            .ToList();

        // The aggregate-level order status is tracked under TD-0019;
        // until that lands the read returns the placeholder
        // "Submitted" so the surface stays stable.
        return new OrderDetailDto(
            order.Id,
            tableLabel,
            order.TotalAmount,
            "Submitted",
            items);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersBySessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Where(o => o.SessionId == sessionId)
            .OrderByDescending(o => o.SubmittedAt)
            .Select(o => new OrderSummaryDto(o.Id, o.TotalAmount, o.SubmittedAt))
            .ToListAsync(ct);
    }
}
