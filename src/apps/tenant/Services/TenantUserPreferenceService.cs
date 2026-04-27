using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

public sealed class TenantUserPreferenceService
{
    public const string DefaultLanguageCode = "en-GB";
    public const string DefaultTimeZone = "Europe/London";
    public const string DefaultDensity = "compact";

    private readonly IDbContextFactory<TenantDbContext> _dbContextFactory;

    public TenantUserPreferenceService(IDbContextFactory<TenantDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TenantUserPreferenceModel> GetForPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Defaults();
        }

        return await GetForUserAsync(userId.Value, ct);
    }

    public async Task<TenantUserPreferenceModel> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var preference = await dbContext.TenantUserPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        return preference is null
            ? Defaults()
            : new TenantUserPreferenceModel(preference.LanguageCode, preference.TimeZone, preference.Density);
    }

    public async Task SaveForPrincipalAsync(ClaimsPrincipal principal, TenantUserPreferenceModel model, CancellationToken ct = default)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            throw new InvalidOperationException("Authenticated tenant user id is missing.");
        }

        var languageCode = NormalizeLanguage(model.LanguageCode);
        var timeZone = NormalizeTimeZone(model.TimeZone);
        var density = NormalizeDensity(model.Density);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var preference = await dbContext.TenantUserPreferences
            .SingleOrDefaultAsync(x => x.UserId == userId.Value, ct);

        if (preference is null)
        {
            preference = TenantUserPreference.Create(userId.Value, languageCode, timeZone, density);
            await dbContext.TenantUserPreferences.AddAsync(preference, ct);
        }
        else
        {
            preference.Update(languageCode, timeZone, density);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public static TenantUserPreferenceModel Defaults() =>
        new(DefaultLanguageCode, DefaultTimeZone, DefaultDensity);

    public static string NormalizeLanguage(string? languageCode) =>
        languageCode switch
        {
            "tr-TR" => "tr-TR",
            _ => DefaultLanguageCode,
        };

    public static string NormalizeTimeZone(string? timeZone) =>
        timeZone switch
        {
            "UTC" => "UTC",
            "Europe/Istanbul" => "Europe/Istanbul",
            "America/New_York" => "America/New_York",
            _ => DefaultTimeZone,
        };

    public static string NormalizeDensity(string? density) =>
        density switch
        {
            "comfortable" => "comfortable",
            _ => DefaultDensity,
        };

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}

public sealed record TenantUserPreferenceModel(
    string LanguageCode,
    string TimeZone,
    string Density);
