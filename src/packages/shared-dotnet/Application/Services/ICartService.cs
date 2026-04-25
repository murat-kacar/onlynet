namespace TabFlow.Shared.Application.Services;

public interface ICartService
{
    Task<CartItemDto> AddItemAsync(AddCartItemRequest request, CancellationToken ct = default);
    Task RemoveItemAsync(Guid cartItemId, CancellationToken ct = default);
    Task UpdateItemQuantityAsync(Guid cartItemId, int quantity, CancellationToken ct = default);
    Task<IReadOnlyList<CartItemDto>> GetCartItemsAsync(Guid sessionId, CancellationToken ct = default);
}

public sealed record AddCartItemRequest(Guid SessionId, Guid MenuItemId, int Quantity, string? Note = null);
public sealed record CartItemDto(Guid Id, Guid MenuItemId, string MenuItemName, int Quantity, decimal UnitPrice, string? Note);
