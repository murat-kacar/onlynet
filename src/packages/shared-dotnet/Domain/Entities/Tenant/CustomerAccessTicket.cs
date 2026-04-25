namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class CustomerAccessTicket
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public bool IsValid { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CustomerAccessTicket() { }

    internal static CustomerAccessTicket Create(Guid sessionId)
    {
        return new CustomerAccessTicket
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            IsValid = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Invalidate() => IsValid = false;
}
