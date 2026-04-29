using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TabFlow.Platform.Services;

public sealed class PlatformClaimsTransformation : IClaimsTransformation
{
    private readonly PlatformIdentityOptions _options;

    public PlatformClaimsTransformation(IOptions<PlatformIdentityOptions> options)
    {
        _options = options.Value;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        EnsureNameIdentifier(identity);
        EnsurePlatformRoleClaims(identity);
        return Task.FromResult(principal);
    }

    private static void EnsureNameIdentifier(ClaimsIdentity identity)
    {
        if (identity.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier))
        {
            return;
        }

        var subject = identity.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
        }
    }

    private void EnsurePlatformRoleClaims(ClaimsIdentity identity)
    {
        if (identity.HasClaim(claim => claim.Type == "PlatformRole"))
        {
            return;
        }

        var rawRoles = GetRawRoles(identity).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (rawRoles.Contains("platform.read") ||
            (!string.IsNullOrWhiteSpace(_options.PlatformReadRole) && rawRoles.Contains(_options.PlatformReadRole)))
        {
            identity.AddClaim(new Claim("PlatformRole", "Read"));
        }

        if (rawRoles.Contains("platform.write") ||
            (!string.IsNullOrWhiteSpace(_options.PlatformWriteRole) && rawRoles.Contains(_options.PlatformWriteRole)) ||
            (!string.IsNullOrWhiteSpace(_options.PlatformOwnerRole) && rawRoles.Contains(_options.PlatformOwnerRole)))
        {
            if (!identity.HasClaim("PlatformRole", "Read"))
            {
                identity.AddClaim(new Claim("PlatformRole", "Read"));
            }

            identity.AddClaim(new Claim("PlatformRole", "Write"));
        }
    }

    private IEnumerable<string> GetRawRoles(ClaimsIdentity identity)
    {
        foreach (var claim in identity.Claims)
        {
            if (claim.Type is ClaimTypes.Role or "role" or "roles" or "platform_role")
            {
                foreach (var role in SplitFlatRoleClaim(claim.Value))
                {
                    yield return role;
                }
            }
            else if (claim.Type == "realm_access")
            {
                foreach (var role in ReadJsonRoles(claim.Value))
                {
                    yield return role;
                }
            }
            else if (claim.Type == "resource_access" && !string.IsNullOrWhiteSpace(_options.RoleClient))
            {
                foreach (var role in ReadClientRoles(claim.Value, _options.RoleClient))
                {
                    yield return role;
                }
            }
        }
    }

    private static string[] SplitFlatRoleClaim(string value) =>
        value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<string> ReadJsonRoles(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in roles.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                yield return item.GetString()!;
            }
        }
    }

    private static IEnumerable<string> ReadClientRoles(string json, string clientId)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty(clientId, out var clientNode) ||
            clientNode.ValueKind != JsonValueKind.Object ||
            !clientNode.TryGetProperty("roles", out var roles) ||
            roles.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in roles.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                yield return item.GetString()!;
            }
        }
    }
}
