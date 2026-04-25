using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class KitchenController : ControllerBase
{
    private readonly TenantDbContext _context;

    public KitchenController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<KitchenOrderDto>>> GetKitchenOrders(CancellationToken ct)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.SubmittedAt)
            .Where(o => o.Items.Any(i => i.Status.ToString() == "Submitted" || i.Status.ToString() == "Preparing"))
            .ToListAsync(ct);

        var result = new List<KitchenOrderDto>();
        foreach (var order in orders)
        {
            var table = await _context.Stations.FindAsync(new object[] { order.TableId }, ct);
            var tableLabel = table?.Name ?? $"Table {order.TableId}";

            result.Add(new KitchenOrderDto(
                order.Id,
                tableLabel,
                order.TotalAmount,
                order.Items.Where(i => i.Status.ToString() == "Submitted" || i.Status.ToString() == "Preparing")
                    .Select(i => new KitchenItemDto(
                        i.Id,
                        i.ItemName,
                        i.Quantity,
                        i.Status.ToString())).ToList()));
        }

        return Ok(result);
    }

    [HttpPut("items/{itemId:guid}/status")]
    public async Task<ActionResult> UpdateItemStatus(Guid itemId, [FromBody] UpdateItemStatusRequest request, CancellationToken ct)
    {
        var item = await _context.OrderItems.FindAsync(new object[] { itemId }, ct);
        if (item == null)
        {
            return NotFound();
        }

        switch (request.Status.ToLower())
        {
            case "preparing":
                item.StartPreparing();
                break;
            case "ready":
                item.MarkReady();
                break;
            case "served":
                item.MarkServed();
                break;
            case "cancel":
                item.Cancel();
                break;
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record KitchenOrderDto(Guid Id, string TableLabel, decimal TotalAmount, IReadOnlyList<KitchenItemDto> Items);
public record KitchenItemDto(Guid Id, string ItemName, int Quantity, string Status);
public record UpdateItemStatusRequest(string Status);
