using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

/// <summary>
/// EF Core implementation of <see cref="IMenuReadService"/>. Owns
/// the <see cref="TenantDbContext"/> reads that
/// `MenuController` used to perform inline. Introduced in PR #29
/// under TD-0022 step 1.
/// </summary>
public sealed class MenuReadService : IMenuReadService
{
    private readonly TenantDbContext _context;

    public MenuReadService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetMenuItemsAsync(CancellationToken ct = default)
    {
        return await _context.MenuItems
            .Where(mi => mi.IsAvailable)
            .OrderBy(mi => mi.Name)
            .Select(mi => new MenuItemDto(
                mi.Id,
                mi.Name,
                mi.Price,
                mi.Description,
                mi.CategoryId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetMenuItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        return await _context.MenuItems
            .Where(mi => mi.CategoryId == categoryId && mi.IsAvailable)
            .OrderBy(mi => mi.Name)
            .Select(mi => new MenuItemDto(
                mi.Id,
                mi.Name,
                mi.Price,
                mi.Description,
                mi.CategoryId))
            .ToListAsync(ct);
    }
}
