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

    /// <summary>
    /// Staff-tier read of a single order plus its line items.
    /// Returns <c>null</c> when no order has the supplied id.
    /// Introduced under TD-0022 step 1 to host the projection that
    /// `OrdersController.GetOrder` previously ran inline.
    /// </summary>
    Task<OrderDetailDto?> GetOrderDetailAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Staff-tier list of every order placed under a customer
    /// session, ordered newest-first. Empty list when no orders
    /// exist for the session id; the result does not distinguish
    /// "no orders" from "no session".
    /// </summary>
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersBySessionAsync(Guid sessionId, CancellationToken ct = default);
}

public sealed record SubmitOrderRequest(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string CheckoutProofToken,
    string IdempotencyKey,
    string? Note);

public sealed record SubmitOrderResult(Guid OrderId, decimal TotalAmount);

public sealed record OrderDetailDto(
    Guid OrderId,
    string TableLabel,
    decimal TotalAmount,
    string Status,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    string Status);

public sealed record OrderSummaryDto(
    Guid Id,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt);
