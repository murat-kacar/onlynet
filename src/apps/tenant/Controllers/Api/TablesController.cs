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
    private readonly ICustomerSessionService _customerSessionService;
    private readonly ITableCommandService _commandService;

    public TablesController(
        ITableReadService service,
        ICustomerSessionService customerSessionService,
        ITableCommandService commandService)
    {
        _service = service;
        _customerSessionService = customerSessionService;
        _commandService = commandService;
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

    [HttpPost]
    [Authorize(Policy = "Tenant:Write")]
    public async Task<ActionResult<TableDetailDto>> Create([FromBody] CreateTableRequest request, CancellationToken ct)
    {
        try
        {
            var table = await _commandService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Tenant:Write")]
    public async Task<ActionResult<TableDetailDto>> Update(Guid id, [FromBody] UpdateTableRequest request, CancellationToken ct)
    {
        try
        {
            var table = await _commandService.UpdateAsync(id, request, ct);
            return table is null ? NotFound() : Ok(table);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Tenant:Write")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _commandService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/workspace")]
    public async Task<ActionResult<TableWorkspaceDto>> GetWorkspace(Guid id, CancellationToken ct)
    {
        var workspace = await _service.GetTableWorkspaceAsync(id, ct);
        return workspace is null ? NotFound() : Ok(workspace);
    }

    [HttpPost("{id:guid}/checkout-proof")]
    [Authorize(Policy = "Tenant:Write")]
    public async Task<ActionResult<CheckoutProofDto>> IssueCheckoutProof(Guid id, CancellationToken ct)
    {
        var proof = await _customerSessionService.IssueCheckoutProofAsync(id, ct);
        return Ok(proof);
    }
}
