using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

/// <summary>
/// Customer-tier public surface for the menu catalog, mounted at
/// <c>/api/public/catalog</c> per TD-0021 step 1. Replaces the
/// transport surface of <c>MenuController</c>; the legacy
/// <c>/api/menu</c> route stays operational during the deprecation
/// window declared in TD-0021 step 3 and is removed in a follow-up
/// PR.
///
/// The shim is deliberately thin: every action calls
/// <see cref="IMenuReadService"/> with the same arguments the
/// legacy controller uses. A reviewer who searches for "customer
/// surface" by route grep finds every customer endpoint under one
/// prefix.
/// </summary>
[ApiController]
[Route("api/public/catalog")]
[AllowAnonymous]
public sealed class PublicCatalogController : ControllerBase
{
    private readonly IMenuReadService _service;

    public PublicCatalogController(IMenuReadService service)
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
