namespace TabFlow.Shared.Application.Services;

public interface IOrderService
{
    Task<SubmitOrderResult> SubmitAsync(SubmitOrderRequest request, CancellationToken ct = default);
}

public sealed record SubmitOrderRequest(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string CheckoutProofToken,
    string IdempotencyKey,
    string? Note);

public sealed record SubmitOrderResult(Guid OrderId, decimal TotalAmount);
