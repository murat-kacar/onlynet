namespace TabFlow.Shared.Application.Services;

public interface ICustomerSessionService
{
    Task<OpenSessionResult> OpenSessionAsync(string qrTokenValue, CancellationToken ct = default);
    Task<CustomerSessionState?> GetSessionStateAsync(Guid ticketId, CancellationToken ct = default);
    Task CloseSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<CheckoutProofDto> IssueCheckoutProofAsync(Guid tableId, CancellationToken ct = default);
}

public sealed record OpenSessionResult(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string TableLabel,
    string DeviceCookieValue);

public sealed record CustomerSessionState(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string TableLabel,
    IReadOnlyList<CartItemSummary> CartItems);

public sealed record CartItemSummary(Guid ItemId, string ItemName, int Quantity, decimal UnitPrice, string? Note);

public sealed record CheckoutProofDto(
    string TokenValue,
    Guid TableId,
    DateTimeOffset ExpiresAt);
