using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

// Customer-facing surface: every action returns publicly readable
// menu data with no per-customer state. Anonymous reads are
// intentional and documented under TD-0015. If a per-tenant menu
// becomes private, add [Authorize] action-by-action.
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class MenuController : ControllerBase
{
    private readonly IMenuReadService _service;

    public MenuController(IMenuReadService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenuItems(CancellationToken ct)
    {
        var items = await _service.GetMenuItemsAsync(ct);
        return Ok(items);
    }

    [HttpGet("category/{categoryId:guid}")]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenuItemsByCategory(Guid categoryId, CancellationToken ct)
    {
        var items = await _service.GetMenuItemsByCategoryAsync(categoryId, ct);
        return Ok(items);
    }
}
