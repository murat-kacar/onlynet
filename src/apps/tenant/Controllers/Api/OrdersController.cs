using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly TenantDbContext _context;

    public OrdersController(IOrderService orderService, TenantDbContext context)
    {
        _orderService = orderService;
        _context = context;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<SubmitOrderResult>> SubmitOrder([FromBody] SubmitOrderRequest request, CancellationToken ct)
    {
        var result = await _orderService.SubmitAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order == null)
        {
            return NotFound();
        }

        var table = await _context.Stations.FindAsync(new object[] { order.TableId }, ct);
        var tableLabel = table?.Name ?? $"Table {order.TableId}";

        var items = order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ItemName,
            i.Quantity,
            i.UnitPrice,
            i.Status.ToString())).ToList();

        return Ok(new OrderDetailDto(
            order.Id,
            tableLabel,
            order.TotalAmount,
            "Submitted", // TODO: Get actual status
            items));
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersBySession(Guid sessionId, CancellationToken ct)
    {
        var orders = await _context.Orders
            .Where(o => o.SessionId == sessionId)
            .OrderByDescending(o => o.SubmittedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.TotalAmount,
                o.SubmittedAt))
            .ToListAsync(ct);

        return Ok(orders);
    }
}

public record OrderDetailDto(
    Guid OrderId,
    string TableLabel,
    decimal TotalAmount,
    string Status,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
    Guid Id,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    string Status);

public record OrderDto(
    Guid Id,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt);
