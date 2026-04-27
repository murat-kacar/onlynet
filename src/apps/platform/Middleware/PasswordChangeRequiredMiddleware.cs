using System.Security.Claims;

namespace TabFlow.Platform.Middleware;

/// <summary>
/// Forces an authenticated principal carrying the
/// <see cref="MustChangePasswordClaim"/> claim through
/// <c>/change-password</c> on every request until the claim is
/// removed. Closes TD-0002 step 3.
///
/// Mechanics:
/// <list type="bullet">
///   <item>The claim is added by <c>BootstrapAdminCommand</c> on the
///   freshly created admin and is removed by the
///   <c>ChangePassword</c> page on a successful
///   <c>UserManager.ChangePasswordAsync</c>. The claim is the single
///   piece of state the middleware reads.</item>
///   <item>The middleware is wired after
///   <c>UseAuthentication</c>/<c>UseAuthorization</c> so that
///   <c>HttpContext.User</c> is fully populated.</item>
///   <item><see cref="ExemptPaths"/> below lists the path prefixes
///   that must continue to serve while the claim is set: the
///   change-password page itself (otherwise we'd loop), the auth
///   surface, the Blazor/SignalR plumbing, the static asset roots,
///   and the API and health endpoints (API callers use header auth
///   and would not see the redirect anyway).</item>
/// </list>
///
/// The claim type is namespaced under <c>tabflow:</c> so that it does
/// not collide with the standard <c>http://schemas.microsoft.com</c>
/// or <c>http://schemas.xmlsoap.org</c> claim URIs that ASP.NET Core
/// Identity emits by default.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware
{
    /// <summary>
    /// Claim type carried by an authenticated principal that has not
    /// yet rotated the bootstrap-issued password. Value is the literal
    /// string <c>"true"</c>; presence is what the middleware checks.
    /// </summary>
    public const string MustChangePasswordClaim = "tabflow:must_change_password";

    /// <summary>
    /// Path prefixes that bypass the redirect even when the claim is
    /// set. Order does not matter; the comparison is
    /// <see cref="StringComparison.OrdinalIgnoreCase"/> via
    /// <see cref="PathString.StartsWithSegments(PathString)"/>.
    /// </summary>
    private static readonly PathString[] ExemptPaths =
    [
        new("/change-password"),
        new("/settings"),
        new("/login"),
        new("/logout"),
        new("/_blazor"),
        new("/_framework"),
        new("/_content"),
        new("/health"),
        new("/api"),
        new("/lib"),
        new("/css"),
        new("/js"),
    ];

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true &&
            HasMustChangePasswordClaim(user) &&
            !IsExempt(context.Request.Path))
        {
            context.Response.Redirect("/settings?tab=security");
            return;
        }

        await _next(context);
    }

    private static bool HasMustChangePasswordClaim(ClaimsPrincipal user)
    {
        foreach (var claim in user.Claims)
        {
            if (string.Equals(claim.Type, MustChangePasswordClaim, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsExempt(PathString path)
    {
        for (var i = 0; i < ExemptPaths.Length; i++)
        {
            if (path.StartsWithSegments(ExemptPaths[i]))
            {
                return true;
            }
        }
        return false;
    }
}
