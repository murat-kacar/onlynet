using Microsoft.AspNetCore.SignalR;
using TabFlow.Shared.Application.EventBus;
using TabFlow.Shared.Domain.Events;
using TabFlow.Tenant.Hubs;

namespace TabFlow.Tenant.Services;

public class EventSubscriptionService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventSubscriptionService> _logger;
    private readonly IHubContext<TenantHub> _hubContext;

    public EventSubscriptionService(
        IEventBus eventBus,
        ILogger<EventSubscriptionService> logger,
        IHubContext<TenantHub> hubContext)
    {
        _eventBus = eventBus;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event subscription service started");

        try
        {
            await foreach (var @event in _eventBus.Subscribe(stoppingToken))
            {
                await HandleEventAsync(@event, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Event subscription service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event subscription service error");
        }
    }

    private async Task HandleEventAsync(TenantEvent @event, CancellationToken ct)
    {
        try
        {
            switch (@event)
            {
                case OrderSubmittedEvent orderSubmitted:
                    await HandleOrderSubmittedAsync(orderSubmitted, ct);
                    break;
                case OrderStatusChangedEvent orderStatusChanged:
                    await HandleOrderStatusChangedAsync(orderStatusChanged, ct);
                    break;
                case DeviceConnectedEvent deviceConnected:
                    await HandleDeviceConnectedAsync(deviceConnected, ct);
                    break;
                case DeviceDisconnectedEvent deviceDisconnected:
                    await HandleDeviceDisconnectedAsync(deviceDisconnected, ct);
                    break;
                default:
                    _logger.LogDebug("Unhandled event type: {EventType}", @event.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", @event.GetType().Name);
        }
    }

    private async Task HandleOrderSubmittedAsync(OrderSubmittedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("Order submitted: {OrderId} for table {TableId}", @event.OrderId, @event.TableId);
        
        // Send notification to table group
        await _hubContext.Clients.Group($"table_{@event.TableId}").SendAsync("OrderSubmitted", new
        {
            OrderId = @event.OrderId,
            TableId = @event.TableId
        }, ct);
        
        // TODO: Send notification to kitchen staff
        await Task.CompletedTask;
    }

    private async Task HandleOrderStatusChangedAsync(OrderStatusChangedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("Order item {OrderItemId} status changed to {Status}", @event.OrderItemId, @event.NewStatus);
        
        // Send notification to station group
        await _hubContext.Clients.Group($"station_{@event.StationId}").SendAsync("OrderStatusChanged", new
        {
            OrderItemId = @event.OrderItemId,
            OrderId = @event.OrderId,
            NewStatus = @event.NewStatus
        }, ct);
        
        await Task.CompletedTask;
    }

    private async Task HandleDeviceConnectedAsync(DeviceConnectedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("Device connected: Table {TableId} ({TableLabel})", @event.TableId, @event.TableLabel);
        
        // Send notification to all clients
        await _hubContext.Clients.All.SendAsync("DeviceConnected", new
        {
            TableId = @event.TableId,
            TableLabel = @event.TableLabel
        }, ct);
        
        await Task.CompletedTask;
    }

    private async Task HandleDeviceDisconnectedAsync(DeviceDisconnectedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("Device disconnected: Table {TableId} ({TableLabel})", @event.TableId, @event.TableLabel);
        
        // Send notification to all clients
        await _hubContext.Clients.All.SendAsync("DeviceDisconnected", new
        {
            TableId = @event.TableId,
            TableLabel = @event.TableLabel
        }, ct);
        
        await Task.CompletedTask;
    }
}
