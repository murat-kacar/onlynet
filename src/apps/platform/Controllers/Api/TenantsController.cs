using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Platform.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Platform:Read")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantRegistryService _service;

    public TenantsController(ITenantRegistryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetTenants(CancellationToken ct)
    {
        var tenants = await _service.GetTenantsAsync(ct);
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailedDto>> GetTenant(Guid id, CancellationToken ct)
    {
        var tenant = await _service.GetTenantAsync(id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    [Authorize(Policy = "Platform:Write")]
    public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantRequest request, CancellationToken ct)
    {
        var tenant = await _service.CreateTenantAsync(request, ct);
        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Platform:Write")]
    public async Task<ActionResult> UpdateTenant(Guid id, UpdateTenantRequest request, CancellationToken ct)
    {
        var ok = await _service.UpdateTenantRegionalSettingsAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }
}
