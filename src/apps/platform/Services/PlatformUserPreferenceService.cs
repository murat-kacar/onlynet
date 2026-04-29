using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Services;

public sealed class PlatformUserPreferenceService
{
    public const string DefaultLanguageCode = "en-GB";
    public const string DefaultTimeZone = "Europe/London";
    public const string DefaultDensity = "compact";

    private readonly IDbContextFactory<PlatformDbContext> _dbContextFactory;
    private readonly PlatformUserIdentityService _identityService;

    public PlatformUserPreferenceService(
        IDbContextFactory<PlatformDbContext> dbContextFactory,
        PlatformUserIdentityService identityService)
    {
        _dbContextFactory = dbContextFactory;
        _identityService = identityService;
    }

    public async Task<PlatformUserPreferenceModel> GetForPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var userId = _identityService.GetStableUserId(principal);
        if (userId is null)
        {
            return Defaults();
        }

        return await GetForUserAsync(userId.Value, ct);
    }

    public async Task<PlatformUserPreferenceModel> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var preference = await dbContext.PlatformUserPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        return preference is null
            ? Defaults()
            : new PlatformUserPreferenceModel(preference.LanguageCode, preference.TimeZone, preference.Density);
    }

    public async Task SaveForPrincipalAsync(ClaimsPrincipal principal, PlatformUserPreferenceModel model, CancellationToken ct = default)
    {
        var userId = _identityService.GetStableUserId(principal);
        if (userId is null)
        {
            throw new InvalidOperationException("Authenticated platform user id is missing.");
        }

        var languageCode = NormalizeLanguage(model.LanguageCode);
        var timeZone = NormalizeTimeZone(model.TimeZone);
        var density = NormalizeDensity(model.Density);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var preference = await dbContext.PlatformUserPreferences
            .SingleOrDefaultAsync(x => x.UserId == userId.Value, ct);

        if (preference is null)
        {
            preference = PlatformUserPreference.Create(userId.Value, languageCode, timeZone, density);
            await dbContext.PlatformUserPreferences.AddAsync(preference, ct);
        }
        else
        {
            preference.Update(languageCode, timeZone, density);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public static PlatformUserPreferenceModel Defaults() =>
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
}

public sealed record PlatformUserPreferenceModel(
    string LanguageCode,
    string TimeZone,
    string Density);
