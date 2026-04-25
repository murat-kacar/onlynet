using System.Diagnostics;

namespace TabFlow.Platform.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // TODO: Implement automatic audit logging for mutating requests
        await _next(context);
    }
}
