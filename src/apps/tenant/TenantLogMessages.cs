using Microsoft.Extensions.Logging;

namespace TabFlow.Tenant;

/// <summary>
/// Source-generated <see cref="ILogger"/> extension methods for the
/// tenant host. Each call site in this assembly invokes one of these
/// methods instead of the structured-logging
/// <see cref="LoggerExtensions"/> overloads so that the analyser
/// rules CA1848 (LoggerMessage delegates) and CA1873 (evaluation of
/// this argument may be expensive) can be enforced — see TD-0014
/// step 3 for the ratchet plan.
///
/// EventId allocation is per-assembly:
/// 1xx — EventSubscriptionService
/// 2xx — TableWebSocketHandler
///
/// IDs are stable across builds so log search by EventId remains
/// meaningful.
/// </summary>
internal static partial class TenantLogMessages
{
    // -- EventSubscriptionService ---------------------------------------

    [LoggerMessage(EventId = 101, Level = LogLevel.Information,
        Message = "Event subscription service started")]
    public static partial void EventSubscriptionStarted(this ILogger logger);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information,
        Message = "Event subscription service stopped")]
    public static partial void EventSubscriptionStopped(this ILogger logger);

    [LoggerMessage(EventId = 103, Level = LogLevel.Error,
        Message = "Event subscription service error")]
    public static partial void EventSubscriptionFailed(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 104, Level = LogLevel.Debug,
        Message = "Unhandled event type: {EventType}")]
    public static partial void UnhandledEventType(this ILogger logger, string eventType);

    [LoggerMessage(EventId = 105, Level = LogLevel.Error,
        Message = "Error handling event {EventType}")]
    public static partial void EventHandlingFailed(this ILogger logger, string eventType, Exception ex);

    [LoggerMessage(EventId = 106, Level = LogLevel.Information,
        Message = "Order submitted: {OrderId} for table {TableId}")]
    public static partial void OrderSubmitted(this ILogger logger, Guid orderId, Guid tableId);

    [LoggerMessage(EventId = 107, Level = LogLevel.Information,
        Message = "Order item {OrderItemId} status changed to {Status}")]
    public static partial void OrderItemStatusChanged(this ILogger logger, Guid orderItemId, string status);

    [LoggerMessage(EventId = 108, Level = LogLevel.Information,
        Message = "Device connected: Table {TableId} ({TableLabel})")]
    public static partial void DeviceConnected(this ILogger logger, Guid tableId, string tableLabel);

    [LoggerMessage(EventId = 109, Level = LogLevel.Information,
        Message = "Device disconnected: Table {TableId} ({TableLabel})")]
    public static partial void DeviceDisconnected(this ILogger logger, Guid tableId, string tableLabel);

    // -- TableWebSocketHandler ------------------------------------------

    [LoggerMessage(EventId = 201, Level = LogLevel.Information,
        Message = "ESP32 connected to table {TableNumber}")]
    public static partial void Esp32Connected(this ILogger logger, int tableNumber);

    [LoggerMessage(EventId = 202, Level = LogLevel.Debug,
        Message = "Received from table {TableNumber}: {Message}")]
    public static partial void WebSocketReceived(this ILogger logger, int tableNumber, string message);

    [LoggerMessage(EventId = 203, Level = LogLevel.Error,
        Message = "Error handling WebSocket for table {TableNumber}")]
    public static partial void WebSocketHandlingFailed(this ILogger logger, int tableNumber, Exception ex);

    [LoggerMessage(EventId = 204, Level = LogLevel.Information,
        Message = "ESP32 disconnected from table {TableNumber}")]
    public static partial void Esp32Disconnected(this ILogger logger, int tableNumber);

    [LoggerMessage(EventId = 205, Level = LogLevel.Warning,
        Message = "Unknown message type: {MessageType} from table {TableNumber}")]
    public static partial void UnknownMessageType(this ILogger logger, string? messageType, int tableNumber);

    [LoggerMessage(EventId = 206, Level = LogLevel.Error,
        Message = "Error processing message from table {TableNumber}: {Message}")]
    public static partial void MessageProcessingFailed(this ILogger logger, int tableNumber, string message, Exception ex);

    [LoggerMessage(EventId = 207, Level = LogLevel.Information,
        Message = "Table {TableNumber} status: {Status}")]
    public static partial void TableStatus(this ILogger logger, int tableNumber, string? status);

    [LoggerMessage(EventId = 208, Level = LogLevel.Information,
        Message = "Order {OrderId} status: {Status} from table {TableNumber}")]
    public static partial void OrderStatus(this ILogger logger, Guid orderId, string status, int tableNumber);
}
