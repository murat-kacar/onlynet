using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Tenant.Controllers.Api;

// Staff-tier table-layout surface. Customer-facing table data
// (e.g. "is my table currently occupied") goes through the customer
// session model, not this controller.
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Tenant:Read")]
public sealed class TablesController : ControllerBase
{
    private readonly ITableReadService _service;

    public TablesController(ITableReadService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TableDto>>> GetTables(CancellationToken ct)
    {
        var tables = await _service.GetTablesAsync(ct);
        return Ok(tables);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TableDetailDto>> GetTable(Guid id, CancellationToken ct)
    {
        var table = await _service.GetTableAsync(id, ct);
        return table is null ? NotFound() : Ok(table);
    }
}
