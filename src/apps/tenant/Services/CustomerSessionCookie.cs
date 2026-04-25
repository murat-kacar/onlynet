namespace TabFlow.Tenant.Services;

/// <summary>
/// Constants for the customer-session device-binding cookie. The
/// cookie is set by <c>SessionsController.OpenSession</c> when a
/// customer scans a join QR; it is read by
/// <c>PublicOrdersController.SubmitOrder</c> and enforced inside
/// <see cref="TabFlow.Shared.Application.Services.IOrderService.SubmitAsync"/>
/// against the persisted <c>CustomerAccessTicket.DeviceCookieValue</c>.
///
/// The value carried by this cookie is opaque (server-issued GUID).
/// It is independent of the ticket id so that a leaked URL containing
/// the ticket id does not also leak the device-binding secret.
///
/// See AC-030 and TD-0017 for the contract this cookie implements.
/// </summary>
public static class CustomerSessionCookie
{
    /// <summary>
    /// HTTP cookie name. Stable across releases; clients persist this
    /// cookie for the duration of a customer session.
    /// </summary>
    public const string Name = "tabflow_session_device";

    /// <summary>
    /// Maximum age of the cookie. Aligned with the in-flight customer
    /// session window so that a stale cookie cannot resurrect a
    /// session that the server already closed.
    /// </summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(8);
}
