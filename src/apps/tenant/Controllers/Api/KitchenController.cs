using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class KitchenController : ControllerBase
{
    private readonly IKitchenReadService _service;

    public KitchenController(IKitchenReadService service)
    {
        _service = service;
    }

    [HttpGet("orders")]
    [Authorize(Policy = "Tenant:Read")]
    public async Task<ActionResult<IReadOnlyList<KitchenOrderDto>>> GetKitchenOrders(CancellationToken ct)
    {
        var orders = await _service.GetOrdersInProgressAsync(ct);
        return Ok(orders);
    }

    [HttpPut("items/{itemId:guid}/status")]
    [Authorize(Policy = "Tenant:Write")]
    public async Task<ActionResult> UpdateItemStatus(
        Guid itemId,
        [FromBody] UpdateItemStatusRequest request,
        CancellationToken ct)
    {
        var ok = await _service.UpdateItemStatusAsync(itemId, request.Status, ct);
        return ok ? NoContent() : NotFound();
    }
}

public record UpdateItemStatusRequest(string Status);
