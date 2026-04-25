namespace TabFlow.Shared.Application.Services;

public interface IOrderService
{
    /// <summary>
    /// Customer-tier order submission. Per AC-030 and TD-0017 the
    /// caller MUST forward the value of the
    /// <c>tabflow_session_device</c> HttpOnly cookie the browser
    /// presented; the implementation validates that it matches the
    /// ticket's stored value before persisting the order. A null or
    /// empty value MUST cause the submit to fail.
    /// </summary>
    Task<SubmitOrderResult> SubmitAsync(
        SubmitOrderRequest request,
        string deviceCookieValue,
        CancellationToken ct = default);
}

public sealed record SubmitOrderRequest(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string CheckoutProofToken,
    string IdempotencyKey,
    string? Note);

public sealed record SubmitOrderResult(Guid OrderId, decimal TotalAmount);
