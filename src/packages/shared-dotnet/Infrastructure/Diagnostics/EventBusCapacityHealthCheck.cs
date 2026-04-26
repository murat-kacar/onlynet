using Microsoft.Extensions.Diagnostics.HealthChecks;
using TabFlow.Shared.Application.EventBus;

namespace TabFlow.Shared.Infrastructure.Diagnostics;

/// <summary>
/// Health check that surfaces saturation of the in-process event bus
/// (<see cref="IEventBus"/>). A subscriber whose queue stays near the
/// per-channel bound is dropping events on the floor (the
/// `DropOldest` mode declared in <c>InProcessEventBus.Subscribe</c>);
/// the readiness signal MUST report that condition rather than serve
/// staff surfaces a stale event stream.
///
/// Saturation is computed as
/// <c>MaxQueueDepth / PerSubscriberCapacity</c>. Two thresholds:
///   - >= <see cref="DegradedThreshold"/> (default 80%) reports
///     <c>Degraded</c>.
///   - >= <see cref="UnhealthyThreshold"/> (default 95%) reports
///     <c>Unhealthy</c>.
/// Otherwise, <c>Healthy</c>. The thresholds are in-band so a tenant
/// with no subscribers (count == 0) reports `Healthy`.
///
/// Registered under the `ready` tag on the tenant host (the host
/// that owns the in-process event bus per AD-0006).
///
/// Ledger: closes TD-0013 step 4.
/// </summary>
public sealed class EventBusCapacityHealthCheck : IHealthCheck
{
    public const double DegradedThreshold = 0.80;
    public const double UnhealthyThreshold = 0.95;

    private readonly IEventBus _eventBus;

    public EventBusCapacityHealthCheck(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stats = _eventBus.GetCapacityStats();

        if (stats.PerSubscriberCapacity <= 0)
        {
            // Defensive: a misconfigured implementation that returns
            // a non-positive capacity would divide-by-zero below.
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Event bus reports a non-positive per-subscriber capacity."));
        }

        if (stats.SubscriberCount == 0)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "No subscribers attached; capacity check is vacuously healthy."));
        }

        var saturation = (double)stats.MaxQueueDepth / stats.PerSubscriberCapacity;

        var description =
            $"subscribers={stats.SubscriberCount} max-depth={stats.MaxQueueDepth}/{stats.PerSubscriberCapacity} ({saturation:P0})";

        if (saturation >= UnhealthyThreshold)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Event bus saturated: {description}."));
        }

        if (saturation >= DegradedThreshold)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Event bus near capacity: {description}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Event bus within capacity: {description}."));
    }
}
