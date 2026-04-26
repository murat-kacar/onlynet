using TabFlow.Shared.Domain.DataProtection;

namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class CustomerAccessTicket
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public bool IsValid { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Opaque value the server set as an HttpOnly cookie on the
    /// browser that scanned this ticket's QR. AC-030 requires that
    /// `POST /api/public/orders` carry an open customer session
    /// **for the submitting device**; the device half is enforced by
    /// matching the cookie value the browser sends back against this
    /// column. Per TD-0017.
    /// </summary>
    [DataClass(DataClassification.Restricted)]
    public string DeviceCookieValue { get; private set; } = default!;

    private CustomerAccessTicket() { }

    internal static CustomerAccessTicket Create(Guid sessionId, string deviceCookieValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceCookieValue);

        return new CustomerAccessTicket
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            IsValid = true,
            CreatedAt = DateTimeOffset.UtcNow,
            DeviceCookieValue = deviceCookieValue,
        };
    }

    public void Invalidate() => IsValid = false;
}
