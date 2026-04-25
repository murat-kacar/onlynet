using Microsoft.AspNetCore.SignalR;

namespace TabFlow.Tenant.Hubs;

public class TenantHub : Hub
{
    public async Task JoinTable(Guid tableId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"table_{tableId}");
    }

    public async Task LeaveTable(Guid tableId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"table_{tableId}");
    }

    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }
}
