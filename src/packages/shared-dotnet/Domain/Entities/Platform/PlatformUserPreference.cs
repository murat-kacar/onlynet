namespace TabFlow.Shared.Domain.Entities.Platform;

public sealed class PlatformUserPreference
{
    public Guid UserId { get; private set; }
    public string LanguageCode { get; private set; } = default!;
    public string TimeZone { get; private set; } = default!;
    public string Density { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PlatformUserPreference()
    {
    }

    public static PlatformUserPreference Create(Guid userId, string languageCode, string timeZone, string density)
    {
        var now = DateTimeOffset.UtcNow;

        return new PlatformUserPreference
        {
            UserId = userId,
            LanguageCode = languageCode,
            TimeZone = timeZone,
            Density = density,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string languageCode, string timeZone, string density)
    {
        LanguageCode = languageCode;
        TimeZone = timeZone;
        Density = density;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
