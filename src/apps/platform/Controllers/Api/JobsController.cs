using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Platform:Read")]
public class JobsController : ControllerBase
{
    private readonly PlatformDbContext _context;

    public JobsController(PlatformDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobDto>>> GetJobs(CancellationToken ct)
    {
        var jobs = await _context.ProvisioningJobs
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

        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailDto>> GetJob(Guid id, CancellationToken ct)
    {
        var job = await _context.ProvisioningJobs
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job == null)
        {
            return NotFound();
        }

        var steps = job.Steps.Select(s => new JobStepDto(
            s.Id,
            s.Name,
            s.Status,
            s.StartedAt)).ToList();

        return Ok(new JobDetailDto(
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
            steps));
    }
}

public record JobDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string Status,
    string? ClaimedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record JobDetailDto(
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

public record JobStepDto(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);
