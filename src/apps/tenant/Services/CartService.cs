using Microsoft.EntityFrameworkCore;
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

    public async Task<CartItemDto> AddItemAsync(AddCartItemRequest request, CancellationToken ct = default)
    {
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

    public async Task RemoveItemAsync(Guid cartItemId, CancellationToken ct = default)
    {
        var cartItem = await _context.CartItems.FindAsync(new object[] { cartItemId }, ct);
        if (cartItem == null)
        {
            throw new InvalidOperationException($"Cart item {cartItemId} not found");
        }

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateItemQuantityAsync(Guid cartItemId, int quantity, CancellationToken ct = default)
    {
        var cartItem = await _context.CartItems.FindAsync(new object[] { cartItemId }, ct);
        if (cartItem == null)
        {
            throw new InvalidOperationException($"Cart item {cartItemId} not found");
        }

        cartItem.UpdateQuantity(quantity);
        _context.CartItems.Update(cartItem);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CartItemDto>> GetCartItemsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var cartItems = await _context.CartItems
            .Join(_context.MenuItems, ci => ci.ItemId, mi => mi.Id, (ci, mi) => new { ci, mi })
            .Where(x => x.ci.SessionId == sessionId)
            .Select(x => new CartItemDto(x.ci.Id, x.ci.ItemId, x.mi.Name, x.ci.Quantity, x.mi.Price, x.ci.Note))
            .ToListAsync(ct);

        return cartItems.AsReadOnly();
    }
}
