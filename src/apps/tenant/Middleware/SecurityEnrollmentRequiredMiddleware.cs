using System.Security.Claims;
using TabFlow.Shared.Domain;

namespace TabFlow.Tenant.Middleware;

public sealed class SecurityEnrollmentRequiredMiddleware
{
    private static readonly PathString[] ExemptPaths =
    [
        new PathString("/login"),
        new PathString("/login-2fa"),
        new PathString("/activate"),
        new PathString("/logout"),
        new PathString("/settings"),
        new PathString("/_blazor"),
        new PathString("/_framework"),
        new PathString("/_content"),
        new PathString("/health"),
        new PathString("/api"),
        new PathString("/lib"),
        new PathString("/css"),
        new PathString("/js"),
    ];

    private readonly RequestDelegate _next;

    public SecurityEnrollmentRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true &&
            RequiresSecurityEnrollment(user) &&
            !IsExemptPath(context.Request.Path))
        {
            context.Response.Redirect("/settings?tab=security");
            return;
        }

        await _next(context);
    }

    private static bool RequiresSecurityEnrollment(ClaimsPrincipal user) =>
        user.Claims.Any(claim =>
            string.Equals(claim.Type, IdentityClaimTypes.MfaSetupRequired, StringComparison.Ordinal));

    private static bool IsExemptPath(PathString path) =>
        ExemptPaths.Any(exempt => path.StartsWithSegments(exempt, StringComparison.OrdinalIgnoreCase));
}
