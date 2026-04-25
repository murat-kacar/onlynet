namespace TabFlow.Shared.Domain.Entities.Platform;

public sealed class ProvisioningJobStep
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string? Detail { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ProvisioningJobStep() { }

    internal static ProvisioningJobStep Create(Guid jobId, string name)
    {
        return new ProvisioningJobStep
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Name = name,
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    public void Complete(string? detail = null)
    {
        Status = "succeeded";
        Detail = detail;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string detail)
    {
        Status = "failed";
        Detail = detail;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
