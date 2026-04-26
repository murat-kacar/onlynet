using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

/// <summary>
/// Customer-tier public surface for cart manipulation, mounted at
/// <c>/api/public/cart</c> per TD-0021 step 1. Replaces the
/// transport surface of <c>CartController</c>; the legacy
/// <c>/api/cart</c> route stays operational during the deprecation
/// window declared in TD-0021 step 3.
///
/// Authorisation is keyed off the customer session id rather than
/// ASP.NET Core Identity. The service-layer enforcement of "the
/// caller's cookie maps to this session" is the same gate that
/// TD-0017 introduced on <c>OrderService.SubmitAsync</c>; replicating
/// that gate on cart writes is a follow-up to TD-0015 step 1.
/// </summary>
[ApiController]
[Route("api/public/cart")]
[AllowAnonymous]
public sealed class PublicCartController : ControllerBase
{
    private readonly ICartService _service;

    public PublicCartController(ICartService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CartItemDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var item = await _service.AddItemAsync(request, ct);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        await _service.RemoveItemAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/quantity")]
    public async Task<ActionResult> UpdateQuantity(Guid id, [FromBody] UpdateCartQuantityRequest request, CancellationToken ct)
    {
        await _service.UpdateItemQuantityAsync(id, request.Quantity, ct);
        return NoContent();
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CartItemDto>>> GetCartItems(Guid sessionId, CancellationToken ct)
    {
        var items = await _service.GetCartItemsAsync(sessionId, ct);
        return Ok(items);
    }
}

public sealed record UpdateCartQuantityRequest(int Quantity);
