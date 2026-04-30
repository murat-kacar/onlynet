using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Services;

/// <summary>
/// EF Core implementation of <see cref="ITenantRegistryService"/>.
/// Owns the <see cref="PlatformDbContext"/> reads and writes behind
/// <c>TenantsController</c> so the controller stays a transport
/// adapter.
/// </summary>
public sealed class TenantRegistryService : ITenantRegistryService
{
    private readonly PlatformDbContext _context;

    public TenantRegistryService(PlatformDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TenantDto>> GetTenantsAsync(CancellationToken ct = default)
    {
        return await _context.Tenants
            .Select(t => new TenantDto(
                t.Id,
                t.Code,
                t.DisplayName,
                t.Status.ToString(),
                t.PrimaryDomain,
                t.LanguageCode,
                t.CurrencyCode,
                t.TimeZone))
            .ToListAsync(ct);
    }

    public async Task<TenantDetailedDto?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, ct);
        if (tenant is null)
        {
            return null;
        }

        return new TenantDetailedDto(
            tenant.Id,
            tenant.Code,
            tenant.DisplayName,
            tenant.Status.ToString(),
            tenant.PrimaryDomain,
            tenant.LanguageCode,
            tenant.CurrencyCode,
            tenant.TimeZone,
            tenant.IntendedOwnerEmail,
            tenant.DatabaseName,
            tenant.DatabaseUser);
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        var tenant = TenantRegistration.Create(
            request.Code,
            request.DisplayName,
            request.PrimaryDomain,
            request.LanguageCode,
            request.CurrencyCode,
            request.TimeZone,
            request.IntendedOwnerEmail);

        _context.Tenants.Add(tenant);
        _context.ProvisioningJobs.Add(ProvisioningJob.CreateTenantCreate(
            tenant.Id,
            JsonSerializer.Serialize(new
            {
                request.Code,
                request.DisplayName,
                request.PrimaryDomain,
                request.LanguageCode,
                request.CurrencyCode,
                request.TimeZone,
                request.IntendedOwnerEmail,
            })));
        await _context.SaveChangesAsync(ct);

        return new TenantDto(
            tenant.Id,
            tenant.Code,
            tenant.DisplayName,
            tenant.Status.ToString(),
            tenant.PrimaryDomain,
            tenant.LanguageCode,
            tenant.CurrencyCode,
            tenant.TimeZone);
    }

    public async Task<bool> UpdateTenantRegionalSettingsAsync(Guid id, UpdateTenantRequest request, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, ct);
        if (tenant is null)
        {
            return false;
        }

        tenant.UpdateRegionalSettings(request.LanguageCode, request.CurrencyCode, request.TimeZone);
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(ct);

        return true;
    }
}
