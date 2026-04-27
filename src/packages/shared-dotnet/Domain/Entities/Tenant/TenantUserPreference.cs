namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class TenantUserPreference
{
    public Guid UserId { get; private set; }
    public string LanguageCode { get; private set; } = default!;
    public string TimeZone { get; private set; } = default!;
    public string Density { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TenantUserPreference()
    {
    }

    public static TenantUserPreference Create(Guid userId, string languageCode, string timeZone, string density)
    {
        var now = DateTimeOffset.UtcNow;

        return new TenantUserPreference
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
