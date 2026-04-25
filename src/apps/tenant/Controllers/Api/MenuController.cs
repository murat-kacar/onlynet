using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly TenantDbContext _context;

    public MenuController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenuItems(CancellationToken ct)
    {
        var items = await _context.MenuItems
            .Where(mi => mi.IsAvailable)
            .OrderBy(mi => mi.Name)
            .Select(mi => new MenuItemDto(
                mi.Id,
                mi.Name,
                mi.Price,
                mi.Description,
                mi.CategoryId))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("category/{categoryId:guid}")]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenuItemsByCategory(Guid categoryId, CancellationToken ct)
    {
        var items = await _context.MenuItems
            .Where(mi => mi.CategoryId == categoryId && mi.IsAvailable)
            .OrderBy(mi => mi.Name)
            .Select(mi => new MenuItemDto(
                mi.Id,
                mi.Name,
                mi.Price,
                mi.Description,
                mi.CategoryId))
            .ToListAsync(ct);

        return Ok(items);
    }
}

public record MenuItemDto(Guid Id, string Name, decimal Price, string? Description, Guid CategoryId);

