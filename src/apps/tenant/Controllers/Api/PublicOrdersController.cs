using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

/// <summary>
/// Customer-tier public surface for order submission, mounted at
/// <c>/api/public/orders</c> per AC-030 and AC-031.
///
/// AC-030 requires that submission carries an open customer session
/// for the table. AC-031 requires a fresh QR checkout-proof token
/// produced by a second scan of the table QR at submit time. Both
/// gates are enforced inside <see cref="IOrderService.SubmitAsync"/>
/// today; this controller is the routed entry point so that the
/// customer-tier surface is reachable at the contract URL and is
/// clearly separated from the staff-tier <c>OrdersController</c>.
///
/// The session-token-vs-cookie binding (the missing half of AC-030's
/// device-binding requirement) is a follow-up to TD-0015 step 3 and
/// is the reason this controller carries a deliberate
/// <c>[AllowAnonymous]</c>: ASP.NET Core Identity is not the
/// authentication model for customer ordering.
/// </summary>
[ApiController]
[Route("api/public/orders")]
[AllowAnonymous]
public class PublicOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public PublicOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Customer order submission. The request payload identifies the
    /// originating customer session and carries the QR
    /// checkout-proof token; the service layer validates both before
    /// persisting the order.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SubmitOrderResult>> SubmitOrder(
        [FromBody] SubmitOrderRequest request,
        CancellationToken ct)
    {
        var result = await _orderService.SubmitAsync(request, ct);
        return Ok(result);
    }
}
