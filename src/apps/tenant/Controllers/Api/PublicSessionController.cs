using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;
using TabFlow.Tenant.Services;

namespace TabFlow.Tenant.Controllers.Api;

/// <summary>
/// Customer-tier public surface for the session-open and
/// session-state queries, mounted at <c>/api/public/session</c> per
/// TD-0021 step 1. Replaces the customer-tier slice of
/// <c>SessionsController</c>; the staff-tier close action stays at
/// <c>POST /api/sessions/{sessionId}/close</c> with
/// `[Authorize(Policy = "Tenant:Write")]` (covered in
/// `internal-api.md`).
///
/// Carries `[AllowAnonymous]` at the controller level — the
/// customer never authenticates against ASP.NET Core Identity. The
/// open action sets the `tabflow_session_device` HttpOnly cookie
/// that TD-0017 introduced; subsequent customer-tier requests
/// (catalog, cart, order submit) echo it automatically.
///
/// Legacy customer-tier routes
/// (`POST /api/sessions/open`, `GET /api/sessions/{ticketId}`) stay
/// operational during the deprecation window declared in TD-0021
/// step 3.
/// </summary>
[ApiController]
[Route("api/public/session")]
[AllowAnonymous]
public sealed class PublicSessionController : ControllerBase
{
    private readonly ICustomerSessionService _service;

    public PublicSessionController(ICustomerSessionService service)
    {
        _service = service;
    }

    [HttpPost("open")]
    public async Task<ActionResult<OpenSessionResponse>> OpenSession([FromBody] OpenSessionRequest request, CancellationToken ct)
    {
        var result = await _service.OpenSessionAsync(request.QrTokenValue, ct);

        // TD-0017 (AC-030 device-binding): set the opaque device cookie
        // the browser must echo on every subsequent customer-tier
        // request. Secure is keyed off Request.IsHttps so dev (HTTP)
        // can still issue the cookie; production (HTTPS) sets Secure.
        // SameSite=Strict so the cookie never leaves the customer's
        // table flow.
        Response.Cookies.Append(
            CustomerSessionCookie.Name,
            result.DeviceCookieValue,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = CustomerSessionCookie.MaxAge,
            });

        // The response body intentionally omits DeviceCookieValue: it
        // travels via the Set-Cookie header only.
        return Ok(new OpenSessionResponse(result.SessionId, result.TicketId, result.TableId, result.TableLabel));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<CustomerSessionState>> GetSessionState(Guid ticketId, CancellationToken ct)
    {
        var state = await _service.GetSessionStateAsync(ticketId, ct);
        return state is null ? NotFound() : Ok(state);
    }
}
