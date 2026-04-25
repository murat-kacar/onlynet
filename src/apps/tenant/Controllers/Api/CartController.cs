using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

// Customer-facing surface: cart manipulation is part of the customer
// session flow. Authorisation is keyed off the customer session id
// rather than ASP.NET Core Identity. The service-layer enforcement of
// "the caller's cookie maps to this session" is tracked as a follow-up
// to TD-0015 step 1; this attribute documents the *transport* level
// decision.
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost]
    public async Task<ActionResult<CartItemDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var item = await _cartService.AddItemAsync(request, ct);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        await _cartService.RemoveItemAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/quantity")]
    public async Task<ActionResult> UpdateQuantity(Guid id, [FromBody] UpdateQuantityRequest request, CancellationToken ct)
    {
        await _cartService.UpdateItemQuantityAsync(id, request.Quantity, ct);
        return NoContent();
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CartItemDto>>> GetCartItems(Guid sessionId, CancellationToken ct)
    {
        var items = await _cartService.GetCartItemsAsync(sessionId, ct);
        return Ok(items);
    }
}

public record UpdateQuantityRequest(int Quantity);
