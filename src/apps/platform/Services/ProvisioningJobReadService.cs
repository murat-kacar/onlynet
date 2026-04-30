using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Services;

/// <summary>
/// EF Core implementation of
/// <see cref="IProvisioningJobReadService"/>. Owns the
/// <see cref="PlatformDbContext"/> reads behind
/// <c>JobsController</c> so the controller stays a transport adapter.
/// </summary>
public sealed class ProvisioningJobReadService : IProvisioningJobReadService
{
    private readonly PlatformDbContext _context;

    public ProvisioningJobReadService(PlatformDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<JobDto>> GetJobsAsync(CancellationToken ct = default)
    {
        return await _context.ProvisioningJobs
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobDto(
                j.Id,
                j.TenantId,
                j.Type,
                j.Status.ToString(),
                j.ClaimedBy,
                j.CreatedAt,
                j.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<JobDetailDto?> GetJobAsync(Guid id, CancellationToken ct = default)
    {
        var job = await _context.ProvisioningJobs
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null)
        {
            return null;
        }

        var steps = job.Steps
            .Select(s => new JobStepDto(
                s.Id,
                s.Name,
                s.Status,
                s.StartedAt))
            .ToList();

        return new JobDetailDto(
            job.Id,
            job.TenantId,
            job.Type,
            job.Status.ToString(),
            job.ClaimedBy,
            job.Payload,
            job.Result,
            job.FailureDetail,
            job.CreatedAt,
            job.UpdatedAt,
            steps);
    }
}
