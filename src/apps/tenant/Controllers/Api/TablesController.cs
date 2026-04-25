using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly TenantDbContext _context;

    public TablesController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TableDto>>> GetTables(CancellationToken ct)
    {
        var tables = await _context.Stations
            .Select(s => new TableDto(
                s.Id,
                s.Name,
                s.Code,
                s.IsActive,
                s.SortOrder))
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return Ok(tables);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TableDetailDto>> GetTable(Guid id, CancellationToken ct)
    {
        var table = await _context.Stations.FindAsync(new object[] { id }, ct);
        if (table == null)
        {
            return NotFound();
        }

        var activeSessions = await _context.CustomerSessions
            .Where(cs => cs.TableId == id && cs.IsOpen)
            .CountAsync(ct);

        return Ok(new TableDetailDto(
            table.Id,
            table.Name,
            table.Code,
            table.Color,
            table.Type,
            table.IsActive,
            table.SortOrder,
            activeSessions > 0));
    }
}

public record TableDto(Guid Id, string Name, string Code, bool IsActive, int SortOrder);
public record TableDetailDto(Guid Id, string Name, string Code, string Color, string Type, bool IsActive, int SortOrder, bool IsOccupied);
