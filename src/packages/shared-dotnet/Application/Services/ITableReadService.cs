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

    /// <summary>
    /// Returns the staff workspace for a single table, including the
    /// active customer session, cart lines, and submitted orders for
    /// the current session when present.
    /// </summary>
    Task<TableWorkspaceDto?> GetTableWorkspaceAsync(Guid id, CancellationToken ct = default);
}

public sealed record TableDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    int SortOrder,
    bool HasOpenSession);

public sealed record TableDetailDto(
    Guid Id,
    string Name,
    string Code,
    string Color,
    string Type,
    bool IsActive,
    int SortOrder,
    bool IsOccupied);

public sealed record TableWorkspaceDto(
    Guid Id,
    string Name,
    string Code,
    string Color,
    string Type,
    bool IsActive,
    int SortOrder,
    bool HasOpenSession,
    TableSessionWorkspaceDto? Session);

public sealed record TableSessionWorkspaceDto(
    Guid SessionId,
    Guid TableId,
    DateTimeOffset OpenedAt,
    int TicketCount,
    IReadOnlyList<TableCartItemDto> CartItems,
    IReadOnlyList<TableSessionOrderDto> Orders);

public sealed record TableCartItemDto(
    Guid Id,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    string? Note);

public sealed record TableSessionOrderDto(
    Guid Id,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt,
    int ItemCount);
