using Microsoft.JSInterop;

namespace TabFlow.Tenant.Services;

public sealed class CustomerSessionBrowserStore : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public CustomerSessionBrowserStore(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "/js/customerSessionStorage.js").AsTask());
    }

    public async Task<CustomerSessionSnapshot> GetSnapshotAsync()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<CustomerSessionSnapshot>("getCustomerSessionSnapshot");
    }

    public async Task SetSnapshotAsync(Guid sessionId, Guid ticketId, string tableLabel)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(
            "setCustomerSessionSnapshot",
            new CustomerSessionPayload(sessionId.ToString(), ticketId.ToString(), tableLabel));
    }

    public async Task ClearAsync()
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clearCustomerSessionSnapshot");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_moduleTask.IsValueCreated)
        {
            return;
        }

        var module = await _moduleTask.Value;
        await module.DisposeAsync();
    }

    public sealed record CustomerSessionSnapshot(string? SessionId, string? TicketId, string? TableLabel);

    private sealed record CustomerSessionPayload(string SessionId, string TicketId, string TableLabel);
}
