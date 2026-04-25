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
        _logger.EventSubscriptionStarted();

        try
        {
            await foreach (var @event in _eventBus.Subscribe(stoppingToken))
            {
                await HandleEventAsync(@event, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.EventSubscriptionStopped();
        }
        catch (Exception ex)
        {
            _logger.EventSubscriptionFailed(ex);
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
                    _logger.UnhandledEventType(@event.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.EventHandlingFailed(@event.GetType().Name, ex);
        }
    }

    private async Task HandleOrderSubmittedAsync(OrderSubmittedEvent @event, CancellationToken ct)
    {
        _logger.OrderSubmitted(@event.OrderId, @event.TableId);
        
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
        _logger.OrderItemStatusChanged(@event.OrderItemId, @event.NewStatus);
        
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
        _logger.DeviceConnected(@event.TableId, @event.TableLabel);
        
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
        _logger.DeviceDisconnected(@event.TableId, @event.TableLabel);
        
        // Send notification to all clients
        await _hubContext.Clients.All.SendAsync("DeviceDisconnected", new
        {
            TableId = @event.TableId,
            TableLabel = @event.TableLabel
        }, ct);
        
        await Task.CompletedTask;
    }
}
