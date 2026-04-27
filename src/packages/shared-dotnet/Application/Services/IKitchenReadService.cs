namespace TabFlow.Shared.Application.Services;

/// <summary>
/// Read and write surface used by the staff "kitchen display" surface
/// (`/kitchen`) and by the corresponding HTTP routes
/// `GET /api/kitchen/orders` and
/// `PUT /api/kitchen/items/{itemId}/status`. Introduced under TD-0022
/// step 1 to keep `KitchenController` thin and to give the existing
/// service-layer pattern the same shape on every read path.
/// </summary>
public interface IKitchenReadService
{
    /// <summary>
    /// Returns every order that has at least one item in the
    /// `Submitted` or `Preparing` station-state, sorted newest-first
    /// by submission time, with each order's table label resolved
    /// from `Stations`. The set is intentionally narrow: served and
    /// cancelled items do not surface on the kitchen display.
    /// </summary>
    Task<IReadOnlyList<KitchenOrderDto>> GetOrdersInProgressAsync(CancellationToken ct = default);

    /// <summary>
    /// Advances an order item along the station-state machine
    /// (`Submitted` → `Preparing` → `Ready` → `Served`, plus the
    /// `Cancel` exit). The transition is the OrderItem aggregate's
    /// own method (`StartPreparing`, `MarkReady`, `MarkServed`,
    /// `Cancel`); invalid transitions throw
    /// `InvalidOperationException` from the aggregate.
    /// </summary>
    /// <returns>
    /// `true` when the item exists and the transition succeeded;
    /// `false` when no item with that id exists.
    /// </returns>
    Task<bool> UpdateItemStatusAsync(Guid itemId, string statusKeyword, CancellationToken ct = default);
}

public sealed record KitchenOrderDto(
    Guid Id,
    string TableLabel,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt,
    string? Note,
    IReadOnlyList<KitchenItemDto> Items);

public sealed record KitchenItemDto(
    Guid Id,
    string ItemName,
    int Quantity,
    string Status,
    string? Note);
