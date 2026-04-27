namespace TabFlow.Shared.Application.Services;

public interface ITableCommandService
{
    Task<TableDetailDto> CreateAsync(CreateTableRequest request, CancellationToken ct = default);
    Task<TableDetailDto?> UpdateAsync(Guid id, UpdateTableRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed record CreateTableRequest(
    string Name,
    string Code,
    string Color,
    string Type,
    int SortOrder);

public sealed record UpdateTableRequest(
    string Name,
    string Code,
    string Color,
    string Type,
    bool IsActive,
    int SortOrder);
