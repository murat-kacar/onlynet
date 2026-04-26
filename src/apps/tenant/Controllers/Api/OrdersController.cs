using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

// Staff-tier read surface for orders. The customer-tier write path
// (`POST /api/public/orders`) lives in `PublicOrdersController` so
// that the AC-030 / AC-031 gates (open session + fresh QR
// checkout-proof) are enforced at the route boundary. Staff reads
// of order detail and per-session order lists require Tenant:Read.
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Tenant:Read")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id, CancellationToken ct)
    {
        var order = await _service.GetOrderDetailAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetOrdersBySession(Guid sessionId, CancellationToken ct)
    {
        var orders = await _service.GetOrdersBySessionAsync(sessionId, ct);
        return Ok(orders);
    }
}
