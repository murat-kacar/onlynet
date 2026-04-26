namespace TabFlow.Shared.Application.Services;

/// <summary>
/// Staff-tier read surface for table-layout queries used by
/// `/api/tables` and `/api/tables/{id}`. Introduced under TD-0022
/// step 1. Customer-facing "is my table occupied" answers go through
/// the customer-session model, not this service.
/// </summary>
public interface ITableReadService
{
    /// <summary>
    /// Returns every station ordered by `SortOrder`.
    /// </summary>
    Task<IReadOnlyList<TableDto>> GetTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the detail view for a single table, including whether
    /// any customer session is currently open against it. Returns
    /// <c>null</c> when no station has the supplied id.
    /// </summary>
    Task<TableDetailDto?> GetTableAsync(Guid id, CancellationToken ct = default);
}

public sealed record TableDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    int SortOrder);

public sealed record TableDetailDto(
    Guid Id,
    string Name,
    string Code,
    string Color,
    string Type,
    bool IsActive,
    int SortOrder,
    bool IsOccupied);
