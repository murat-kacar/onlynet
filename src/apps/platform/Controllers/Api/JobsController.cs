using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabFlow.Shared.Application.Services;

namespace TabFlow.Platform.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Platform:Read")]
public sealed class JobsController : ControllerBase
{
    private readonly IProvisioningJobReadService _service;

    public JobsController(IProvisioningJobReadService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobDto>>> GetJobs(CancellationToken ct)
    {
        var jobs = await _service.GetJobsAsync(ct);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailDto>> GetJob(Guid id, CancellationToken ct)
    {
        var job = await _service.GetJobAsync(id, ct);
        return job is null ? NotFound() : Ok(job);
    }
}
