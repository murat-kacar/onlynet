using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TabFlow.Platform.Services;

public sealed class PlatformUserIdentityService
{
    public Guid? GetStableUserId(ClaimsPrincipal principal)
    {
        var rawNameIdentifier = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(rawNameIdentifier, out var parsedGuid))
        {
            return parsedGuid;
        }

        var subject = principal.FindFirstValue("sub") ?? rawNameIdentifier;
        var issuer = principal.FindFirstValue("iss") ?? "tabflow";
        if (string.IsNullOrWhiteSpace(subject))
        {
            var email = GetDisplayEmail(principal);
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            subject = email;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{issuer}|{subject}"));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    public string? GetDisplayEmail(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email) ??
        principal.FindFirstValue("email") ??
        principal.FindFirstValue("preferred_username") ??
        principal.Identity?.Name;
}
