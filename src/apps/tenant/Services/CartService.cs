using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

public class CartService : ICartService
{
    private readonly TenantDbContext _context;

    public CartService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<CartItemDto> AddItemAsync(
        AddCartItemRequest request,
        string deviceCookieValue,
        CancellationToken ct = default)
    {
        await EnsureSessionDeviceAsync(request.SessionId, deviceCookieValue, ct);

        var menuItem = await _context.MenuItems.FindAsync(new object[] { request.MenuItemId }, ct);
        if (menuItem == null)
        {
            throw new InvalidOperationException($"Menu item {request.MenuItemId} not found");
        }

        var cartItem = CartItem.Create(request.SessionId, request.MenuItemId, request.Quantity, request.Note);
        _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync(ct);

        return new CartItemDto(cartItem.Id, cartItem.ItemId, menuItem.Name, cartItem.Quantity, menuItem.Price, cartItem.Note);
    }

    public async Task RemoveItemAsync(
        Guid cartItemId,
        string deviceCookieValue,
        CancellationToken ct = default)
    {
        var cartItem = await _context.CartItems.FindAsync(new object[] { cartItemId }, ct);
        if (cartItem == null)
        {
            throw new InvalidOperationException($"Cart item {cartItemId} not found");
        }

        await EnsureSessionDeviceAsync(cartItem.SessionId, deviceCookieValue, ct);

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateItemQuantityAsync(
        Guid cartItemId,
        int quantity,
        string deviceCookieValue,
        CancellationToken ct = default)
    {
        var cartItem = await _context.CartItems.FindAsync(new object[] { cartItemId }, ct);
        if (cartItem == null)
        {
            throw new InvalidOperationException($"Cart item {cartItemId} not found");
        }

        await EnsureSessionDeviceAsync(cartItem.SessionId, deviceCookieValue, ct);

        cartItem.UpdateQuantity(quantity);
        _context.CartItems.Update(cartItem);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CartItemDto>> GetCartItemsAsync(
        Guid sessionId,
        string deviceCookieValue,
        CancellationToken ct = default)
    {
        await EnsureSessionDeviceAsync(sessionId, deviceCookieValue, ct);

        var cartItems = await _context.CartItems
            .Join(_context.MenuItems, ci => ci.ItemId, mi => mi.Id, (ci, mi) => new { ci, mi })
            .Where(x => x.ci.SessionId == sessionId)
            .Select(x => new CartItemDto(x.ci.Id, x.ci.ItemId, x.mi.Name, x.ci.Quantity, x.mi.Price, x.ci.Note))
            .ToListAsync(ct);

        return cartItems.AsReadOnly();
    }

    private async Task EnsureSessionDeviceAsync(
        Guid sessionId,
        string deviceCookieValue,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(deviceCookieValue))
        {
            throw new UnauthorizedAccessException("Device cookie missing.");
        }

        var session = await _context.CustomerSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null || !session.IsOpen)
        {
            throw new UnauthorizedAccessException("Customer session is not open.");
        }

        var tickets = await _context.CustomerAccessTickets
            .AsNoTracking()
            .Where(ticket => ticket.SessionId == sessionId && ticket.IsValid)
            .Select(ticket => ticket.DeviceCookieValue)
            .ToListAsync(ct);

        var presented = Encoding.UTF8.GetBytes(deviceCookieValue);
        var matches = tickets.Any(stored =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(stored),
                presented));

        if (!matches)
        {
            throw new UnauthorizedAccessException("Device cookie mismatch.");
        }
    }
}
