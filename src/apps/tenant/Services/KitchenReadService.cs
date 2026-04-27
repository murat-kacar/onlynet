using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Services;

/// <summary>
/// EF Core implementation of <see cref="IKitchenReadService"/>.
/// Owns every direct <see cref="TenantDbContext"/> access that the
/// kitchen routes used to perform inline. Introduced in PR #29 under
/// TD-0022 step 1.
/// </summary>
public sealed class KitchenReadService : IKitchenReadService
{
    private readonly TenantDbContext _context;

    public KitchenReadService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<KitchenOrderDto>> GetOrdersInProgressAsync(CancellationToken ct = default)
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
                order.SubmittedAt,
                order.Note,
                order.Items
                    .Where(i => i.Status.ToString() == "Submitted" || i.Status.ToString() == "Preparing")
                    .Select(i => new KitchenItemDto(
                        i.Id,
                        i.ItemName,
                        i.Quantity,
                        i.Status.ToString(),
                        i.Note))
                    .ToList()));
        }

        return result;
    }

    public async Task<bool> UpdateItemStatusAsync(Guid itemId, string statusKeyword, CancellationToken ct = default)
    {
        var item = await _context.OrderItems.FindAsync(new object[] { itemId }, ct);
        if (item is null)
        {
            return false;
        }

        switch (statusKeyword?.ToLowerInvariant())
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
            default:
                // Unknown keyword. The aggregate is the source of
                // truth on the legal transitions; refusing the input
                // here keeps the controller a transport, not a
                // validator.
                return false;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}
