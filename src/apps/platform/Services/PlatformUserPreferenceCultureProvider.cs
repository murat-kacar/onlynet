using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace TabFlow.Platform.Services;

public sealed class PlatformUserPreferenceCultureProvider : RequestCultureProvider
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

        var preferenceService = httpContext.RequestServices.GetRequiredService<PlatformUserPreferenceService>();
        var preference = await preferenceService.GetForUserAsync(userId, httpContext.RequestAborted);

        return new ProviderCultureResult(preference.LanguageCode, preference.LanguageCode);
    }
}
