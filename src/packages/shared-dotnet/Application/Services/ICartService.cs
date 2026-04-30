namespace TabFlow.Shared.Application.Services;

public interface ICartService
{
    Task<CartItemDto> AddItemAsync(AddCartItemRequest request, string deviceCookieValue, CancellationToken ct = default);
    Task RemoveItemAsync(Guid cartItemId, string deviceCookieValue, CancellationToken ct = default);
    Task UpdateItemQuantityAsync(Guid cartItemId, int quantity, string deviceCookieValue, CancellationToken ct = default);
    Task<IReadOnlyList<CartItemDto>> GetCartItemsAsync(Guid sessionId, string deviceCookieValue, CancellationToken ct = default);
}

public sealed record AddCartItemRequest(Guid SessionId, Guid MenuItemId, int Quantity, string? Note = null);
public sealed record CartItemDto(Guid Id, Guid MenuItemId, string MenuItemName, int Quantity, decimal UnitPrice, string? Note);
