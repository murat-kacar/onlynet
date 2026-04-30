using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;
using TabFlow.Tenant.Services;

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
        return await WithDeviceCookieAsync(
            deviceCookie => _cartService.AddItemAsync(request, deviceCookie, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        return await WithDeviceCookieNoContentAsync(
            deviceCookie => _cartService.RemoveItemAsync(id, deviceCookie, ct));
    }

    [HttpPut("{id:guid}/quantity")]
    public async Task<ActionResult> UpdateQuantity(Guid id, [FromBody] UpdateQuantityRequest request, CancellationToken ct)
    {
        return await WithDeviceCookieNoContentAsync(
            deviceCookie => _cartService.UpdateItemQuantityAsync(id, request.Quantity, deviceCookie, ct));
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CartItemDto>>> GetCartItems(Guid sessionId, CancellationToken ct)
    {
        return await WithDeviceCookieAsync(
            deviceCookie => _cartService.GetCartItemsAsync(sessionId, deviceCookie, ct));
    }

    private async Task<ActionResult<T>> WithDeviceCookieAsync<T>(Func<string, Task<T>> action)
    {
        var deviceCookie = Request.Cookies[CustomerSessionCookie.Name];
        if (string.IsNullOrEmpty(deviceCookie))
        {
            return Forbid();
        }

        try
        {
            return Ok(await action(deviceCookie));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private async Task<ActionResult> WithDeviceCookieNoContentAsync(Func<string, Task> action)
    {
        var deviceCookie = Request.Cookies[CustomerSessionCookie.Name];
        if (string.IsNullOrEmpty(deviceCookie))
        {
            return Forbid();
        }

        try
        {
            await action(deviceCookie);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

public record UpdateQuantityRequest(int Quantity);
