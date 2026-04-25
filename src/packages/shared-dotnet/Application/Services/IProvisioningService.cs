namespace TabFlow.Shared.Application.Services;

public interface IProvisioningService
{
    Task<Guid> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default);
}

public sealed record CreateTenantRequest(
    string Code,
    string DisplayName,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone,
    string IntendedOwnerEmail);
