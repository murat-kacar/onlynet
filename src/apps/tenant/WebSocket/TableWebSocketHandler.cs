using System.Net.WebSockets;
using TabFlow.Shared.Application.EventBus;
using TabFlow.Shared.Domain.Events;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.WebSocket;

public class TableWebSocketHandler
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TableWebSocketHandler> _logger;
    private readonly IEventBus _eventBus;

    public TableWebSocketHandler(
        TenantDbContext context,
        ILogger<TableWebSocketHandler> logger,
        IEventBus eventBus)
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task HandleAsync(HttpContext context, int tableNumber)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        _logger.LogInformation("ESP32 connected to table {TableNumber}", tableNumber);

        var table = await _context.Stations.FindAsync(new object[] { tableNumber }, CancellationToken.None);
        var tableLabel = table?.Name ?? $"Table {tableNumber}";
        var tableId = table?.Id ?? Guid.NewGuid();

        _eventBus.Publish(new DeviceConnectedEvent(tableId, tableLabel));

        try
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                // Handle incoming messages from ESP32
                var message = System.Text.Encoding.UTF8.GetString(
                    buffer, 0, receiveResult.Count);

                _logger.LogDebug("Received from table {TableNumber}: {Message}", tableNumber, message);

                // Process message and send response
                await ProcessMessageAsync(message, tableNumber, CancellationToken.None);

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket for table {TableNumber}", tableNumber);
        }
        finally
        {
            _logger.LogInformation("ESP32 disconnected from table {TableNumber}", tableNumber);

            var disconnectTable = await _context.Stations.FindAsync(new object[] { tableNumber }, CancellationToken.None);
            var disconnectLabel = disconnectTable?.Name ?? $"Table {tableNumber}";
            var disconnectTableId = disconnectTable?.Id ?? Guid.NewGuid();

            _eventBus.Publish(new DeviceDisconnectedEvent(disconnectTableId, disconnectLabel));
        }
    }

    private async Task ProcessMessageAsync(string message, int tableNumber, CancellationToken ct)
    {
        try
        {
            var messageData = System.Text.Json.JsonDocument.Parse(message);
            var messageType = messageData.RootElement.GetProperty("type").GetString();

            switch (messageType)
            {
                case "table_status":
                    await HandleTableStatusAsync(messageData, tableNumber, ct);
                    break;
                case "order_update":
                    await HandleOrderUpdateAsync(messageData, tableNumber, ct);
                    break;
                default:
                    _logger.LogWarning("Unknown message type: {MessageType} from table {TableNumber}", messageType, tableNumber);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from table {TableNumber}: {Message}", tableNumber, message);
        }
    }

    private async Task HandleTableStatusAsync(System.Text.Json.JsonDocument messageData, int tableNumber, CancellationToken ct)
    {
        var status = messageData.RootElement.GetProperty("status").GetString();
        _logger.LogInformation("Table {TableNumber} status: {Status}", tableNumber, status);

        // TODO: Publish table status event via EventBus
        // Example:
        // if (status == "occupied")
        //     _eventBus.Publish(new TableOpenedEvent(tableNumber, sessionId));
        // else if (status == "available")
        //     _eventBus.Publish(new TableClosedEvent(tableNumber, billId));
    }

    private async Task HandleOrderUpdateAsync(System.Text.Json.JsonDocument messageData, int tableNumber, CancellationToken ct)
    {
        var orderId = messageData.RootElement.GetProperty("orderId").GetGuid();
        var status = messageData.RootElement.GetProperty("status").GetString() ?? "unknown";
        var orderItemId = messageData.RootElement.TryGetProperty("orderItemId", out var oi) ? oi.GetGuid() : Guid.Empty;
        _logger.LogInformation("Order {OrderId} status: {Status} from table {TableNumber}", orderId, status, tableNumber);

        var table = await _context.Stations.FindAsync(new object[] { tableNumber }, ct);
        var tableId = table?.Id ?? Guid.NewGuid();

        // Publish order update event via EventBus
        if (orderItemId != Guid.Empty)
        {
            _eventBus.Publish(new OrderStatusChangedEvent(orderItemId, orderId, tableId, status));
        }
    }
}
