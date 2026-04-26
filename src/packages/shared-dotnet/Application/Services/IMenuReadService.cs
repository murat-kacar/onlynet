namespace TabFlow.Shared.Application.Services;

/// <summary>
/// Customer-tier menu read surface used by `/api/menu` (and, post
/// TD-0021, the `/api/public/menu` shim that replaces it).
/// Introduced under TD-0022 step 1.
/// </summary>
public interface IMenuReadService
{
    /// <summary>
    /// Returns every menu item flagged available, sorted by name.
    /// </summary>
    Task<IReadOnlyList<MenuItemDto>> GetMenuItemsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns every available menu item filtered by category, sorted
    /// by name.
    /// </summary>
    Task<IReadOnlyList<MenuItemDto>> GetMenuItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default);
}

public sealed record MenuItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    Guid CategoryId);
