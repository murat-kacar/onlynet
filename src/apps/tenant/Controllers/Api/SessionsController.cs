using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;
using TabFlow.Tenant.Services;

namespace TabFlow.Tenant.Controllers.Api;

// Mixed surface: open/get are customer-tier (the customer scans a QR
// to open a session and polls the ticket state from their browser);
// close is staff-tier (only a cashier or manager may end a session
// without an order). The default is the most restrictive policy
// (Tenant:Read) so that any future action added without an explicit
// attribute fails closed; customer-tier actions opt out with
// [AllowAnonymous] and the staff-close action raises the bar to
// Tenant:Write per AC-043. ASP.NET Core's authorisation pipeline
// requires this ordering — a default [AllowAnonymous] would silently
// override every [Authorize] attribute on actions (analyser ASP0026).
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Tenant:Read")]
public class SessionsController : ControllerBase
{
    private readonly ICustomerSessionService _sessionService;

    public SessionsController(ICustomerSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("open")]
    [AllowAnonymous]
    public async Task<ActionResult<OpenSessionResponse>> OpenSession([FromBody] OpenSessionRequest request, CancellationToken ct)
    {
        var result = await _sessionService.OpenSessionAsync(request.QrTokenValue, ct);

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
        // travels via the Set-Cookie header only. A client that needs
        // the binding does not need to read it (the cookie is sent
        // automatically with every same-origin request).
        return Ok(new OpenSessionResponse(result.SessionId, result.TicketId, result.TableLabel));
    }

    [HttpGet("{ticketId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerSessionState?>> GetSessionState(Guid ticketId, CancellationToken ct)
    {
        var state = await _sessionService.GetSessionStateAsync(ticketId, ct);
        if (state == null)
        {
            return NotFound();
        }
        return Ok(state);
    }

    [HttpPost("{sessionId:guid}/close")]
    [Authorize(Policy = "Tenant:Write")]
    public async Task<ActionResult> CloseSession(Guid sessionId, CancellationToken ct)
    {
        await _sessionService.CloseSessionAsync(sessionId, ct);
        return NoContent();
    }
}

public record OpenSessionRequest(string QrTokenValue);

/// <summary>
/// Customer-facing response shape for <c>POST /api/sessions/open</c>.
/// Mirrors <see cref="TabFlow.Shared.Application.Services.OpenSessionResult"/>
/// minus the <c>DeviceCookieValue</c>, which is set as an HttpOnly
/// cookie by the controller and intentionally omitted from the
/// response body so it never reaches the JS document context.
/// </summary>
public record OpenSessionResponse(Guid SessionId, Guid TicketId, string TableLabel);
