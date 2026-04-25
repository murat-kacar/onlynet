namespace TabFlow.Shared.Application.Services;

public interface IBillService
{
    Task CloseBillAsync(Guid billId, string paymentMethod, Guid actorId, string actorEmail, CancellationToken ct = default);
    Task MoveBillAsync(Guid billId, Guid targetTableId, Guid actorId, string actorEmail, CancellationToken ct = default);
    Task MergeBillAsync(Guid sourceBillId, Guid targetBillId, Guid actorId, string actorEmail, CancellationToken ct = default);
    Task<Guid> SplitBillAsync(Guid billId, IReadOnlyList<Guid> orderItemIds, Guid targetTableId, Guid actorId, string actorEmail, CancellationToken ct = default);
}
