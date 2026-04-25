using TabFlow.Shared.Domain.Enums;

namespace TabFlow.Shared.Domain.Entities.Platform;

public sealed class ProvisioningJob
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Type { get; private set; } = default!;
    public ProvisioningJobStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? ClaimedBy { get; private set; }
    public string? Payload { get; private set; }
    public string? Result { get; private set; }
    public string? FailureDetail { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ProvisioningJobStep> Steps => _steps.AsReadOnly();
    private readonly List<ProvisioningJobStep> _steps = [];

    private ProvisioningJob() { }

    public static ProvisioningJob CreateTenantCreate(Guid tenantId, string payload)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProvisioningJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = ProvisioningJobTypes.TenantCreate,
            Status = ProvisioningJobStatus.Pending,
            AttemptCount = 0,
            Payload = payload,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Claim(string workerId)
    {
        Status = ProvisioningJobStatus.Claimed;
        ClaimedBy = workerId;
        AttemptCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRunning() { Status = ProvisioningJobStatus.Running; UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkSucceeded(string result) { Status = ProvisioningJobStatus.Succeeded; Result = result; UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkFailed(string detail) { Status = ProvisioningJobStatus.Failed; FailureDetail = detail; UpdatedAt = DateTimeOffset.UtcNow; }

    public ProvisioningJobStep AddStep(string name)
    {
        var step = ProvisioningJobStep.Create(Id, name);
        _steps.Add(step);
        return step;
    }
}

public static class ProvisioningJobTypes
{
    public const string TenantCreate = "tenant.create";
}
