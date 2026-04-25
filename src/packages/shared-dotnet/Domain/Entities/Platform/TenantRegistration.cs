using TabFlow.Shared.Domain.Enums;

namespace TabFlow.Shared.Domain.Entities.Platform;

public sealed class TenantRegistration
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public TenantStatus Status { get; private set; }
    public string PrimaryDomain { get; private set; } = default!;
    public string LanguageCode { get; private set; } = default!;
    public string CurrencyCode { get; private set; } = default!;
    public string TimeZone { get; private set; } = default!;
    public string IntendedOwnerEmail { get; private set; } = default!;
    public string? DatabaseName { get; private set; }
    public string? DatabaseUser { get; private set; }
    public string? DatabasePassword { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TenantRegistration() { }

    public static TenantRegistration Create(
        string code,
        string displayName,
        string primaryDomain,
        string languageCode,
        string currencyCode,
        string timeZone,
        string intendedOwnerEmail)
    {
        var now = DateTimeOffset.UtcNow;
        return new TenantRegistration
        {
            Id = Guid.NewGuid(),
            Code = code,
            DisplayName = displayName,
            Status = TenantStatus.Provisioning,
            PrimaryDomain = primaryDomain,
            LanguageCode = languageCode,
            CurrencyCode = currencyCode,
            TimeZone = timeZone,
            IntendedOwnerEmail = intendedOwnerEmail,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetStatus(TenantStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRegionalSettings(string languageCode, string currencyCode, string timeZone)
    {
        LanguageCode = languageCode;
        CurrencyCode = currencyCode;
        TimeZone = timeZone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDatabaseConnection(string dbName, string dbUser, string dbPassword)
    {
        DatabaseName = dbName;
        DatabaseUser = dbUser;
        DatabasePassword = dbPassword;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
