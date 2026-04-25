using Microsoft.AspNetCore.SignalR.Client;

namespace TabFlow.Tenant.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event Action<string, object>? OnOrderSubmitted;
    public event Action<string, object>? OnOrderStatusChanged;
    public event Action<string, object>? OnDeviceConnected;
    public event Action<string, object>? OnDeviceDisconnected;

    public async Task StartAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("/hub/tenant")
            .Build();

        _hubConnection.On("OrderSubmitted", (string orderId, object data) => OnOrderSubmitted?.Invoke(orderId, data));
        _hubConnection.On("OrderStatusChanged", (string orderItemId, object data) => OnOrderStatusChanged?.Invoke(orderItemId, data));
        _hubConnection.On("DeviceConnected", (string tableId, object data) => OnDeviceConnected?.Invoke(tableId, data));
        _hubConnection.On("DeviceDisconnected", (string tableId, object data) => OnDeviceDisconnected?.Invoke(tableId, data));

        await _hubConnection.StartAsync();
    }

    public async Task JoinTableAsync(Guid tableId)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.InvokeAsync("JoinTable", tableId);
        }
    }

    public async Task LeaveTableAsync(Guid tableId)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.InvokeAsync("LeaveTable", tableId);
        }
    }

    public async Task JoinSessionAsync(Guid sessionId)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.InvokeAsync("JoinSession", sessionId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
