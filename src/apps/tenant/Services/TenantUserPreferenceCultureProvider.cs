using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;

namespace TabFlow.Tenant.Services;

public sealed class TenantUserPreferenceCultureProvider : RequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var authResult = await httpContext.AuthenticateAsync();
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return null;
        }

        var rawUserId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return null;
        }

        var preferenceService = httpContext.RequestServices.GetRequiredService<TenantUserPreferenceService>();
        var preference = await preferenceService.GetForUserAsync(userId, httpContext.RequestAborted);

        return new ProviderCultureResult(preference.LanguageCode, preference.LanguageCode);
    }
}
