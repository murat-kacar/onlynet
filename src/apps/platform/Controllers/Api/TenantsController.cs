using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Platform:Read")]
public class TenantsController : ControllerBase
{
    private readonly PlatformDbContext _context;

    public TenantsController(PlatformDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetTenants(CancellationToken ct)
    {
        var tenants = await _context.Tenants
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

        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> GetTenant(Guid id, CancellationToken ct)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, ct);
        if (tenant == null)
        {
            return NotFound();
        }

        return Ok(new TenantDetailDto(
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
            tenant.DatabaseUser));
    }

    [HttpPost]
    [Authorize(Policy = "Platform:Write")]
    public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantRequest request, CancellationToken ct)
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
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, new TenantDto(
            tenant.Id,
            tenant.Code,
            tenant.DisplayName,
            tenant.Status.ToString(),
            tenant.PrimaryDomain,
            tenant.LanguageCode,
            tenant.CurrencyCode,
            tenant.TimeZone));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Platform:Write")]
    public async Task<ActionResult> UpdateTenant(Guid id, UpdateTenantRequest request, CancellationToken ct)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, ct);
        if (tenant == null)
        {
            return NotFound();
        }

        tenant.UpdateRegionalSettings(request.LanguageCode, request.CurrencyCode, request.TimeZone);
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}

public record TenantDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);

public record TenantDetailDto(
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

public record CreateTenantRequest(
    string Code,
    string DisplayName,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone,
    string IntendedOwnerEmail);

public record UpdateTenantRequest(
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);
