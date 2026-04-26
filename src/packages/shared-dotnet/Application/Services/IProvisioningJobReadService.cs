namespace TabFlow.Shared.Application.Services;

/// <summary>
/// Platform-tier read surface for provisioning jobs used by
/// `/api/jobs` (`GET / GET {id}`). Introduced under TD-0022 step 1.
/// The job-write paths (claim, mark step started/completed, mark
/// failed) live in the platform worker, not in this read service.
/// </summary>
public interface IProvisioningJobReadService
{
    /// <summary>
    /// Returns every provisioning job ordered newest-first by
    /// `CreatedAt`.
    /// </summary>
    Task<IReadOnlyList<JobDto>> GetJobsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the detail view for a single job, including its step
    /// list. Returns <c>null</c> when no job has the supplied id.
    /// </summary>
    Task<JobDetailDto?> GetJobAsync(Guid id, CancellationToken ct = default);
}

public sealed record JobDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string Status,
    string? ClaimedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record JobDetailDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string Status,
    string? ClaimedBy,
    string? Payload,
    string? Result,
    string? FailureDetail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<JobStepDto> Steps);

public sealed record JobStepDto(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset StartedAt);
