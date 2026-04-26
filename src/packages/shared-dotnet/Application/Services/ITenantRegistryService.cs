namespace TabFlow.Shared.Application.Services;

/// <summary>
/// Platform-tier registry surface for tenant CRUD used by
/// `/api/tenants` (`GET / GET {id} / POST / PUT {id}`). Introduced
/// under TD-0022 step 1. The write actions emit a `tenant.create`
/// provisioning job and write a `tenant.create` audit row to
/// `platform_audit_log` (AC-071) inside the same transaction; the
/// audit-row half is gated on
/// [TD-0019](/doc/buildlog/tech-debt-ledger.md) (audit row
/// instrumentation across the read paths) and is left unimplemented
/// in the read paths until that lands.
/// </summary>
public interface ITenantRegistryService
{
    /// <summary>
    /// Returns every tenant.
    /// </summary>
    Task<IReadOnlyList<TenantDto>> GetTenantsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the detail view for a single tenant, or <c>null</c>
    /// when no tenant has the supplied id.
    /// </summary>
    Task<TenantDetailedDto?> GetTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new tenant from a registration request. Returns the
    /// summary of the newly-created tenant.
    /// </summary>
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the regional-settings fields on an existing tenant.
    /// Returns <c>true</c> when the tenant existed and the update
    /// succeeded; <c>false</c> when no tenant has the supplied id.
    /// </summary>
    Task<bool> UpdateTenantRegionalSettingsAsync(Guid id, UpdateTenantRequest request, CancellationToken ct = default);
}

public sealed record TenantDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);

public sealed record TenantDetailedDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone,
    string IntendedOwnerEmail,
    string? DatabaseName,
    string? DatabaseUser);

// CreateTenantRequest is shared with IProvisioningService.cs in the
// same namespace; the registry service reuses the existing record so
// the two surfaces speak the same shape on the wire.

public sealed record UpdateTenantRequest(
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);
