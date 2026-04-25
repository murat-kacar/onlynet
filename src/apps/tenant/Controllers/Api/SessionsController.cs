using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ICustomerSessionService _sessionService;

    public SessionsController(ICustomerSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("open")]
    public async Task<ActionResult<OpenSessionResult>> OpenSession([FromBody] OpenSessionRequest request, CancellationToken ct)
    {
        var result = await _sessionService.OpenSessionAsync(request.QrTokenValue, ct);
        return Ok(result);
    }

    [HttpGet("{ticketId:guid}")]
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
    public async Task<ActionResult> CloseSession(Guid sessionId, CancellationToken ct)
    {
        await _sessionService.CloseSessionAsync(sessionId, ct);
        return NoContent();
    }
}

public record OpenSessionRequest(string QrTokenValue);
