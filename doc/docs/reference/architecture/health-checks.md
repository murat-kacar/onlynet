# Health Check Endpoints

Both hosts expose two health endpoints, addressable only on the loopback
interface. They are the contract between TabFlow processes and any
supervisor (systemd, Kubernetes, runit, etc.) plus the release gate.

## Endpoints

Both the platform host and every tenant host expose the same two
endpoints:

| Endpoint | Purpose | Probe Set |
| --- | --- | --- |
| `GET /health/live`  | Process liveness — answers if the process can serve any HTTP at all | none |
| `GET /health/ready` | Readiness — answers if the process can serve real traffic | tagged probes |

`/health/live` exists so a container or systemd `WatchdogSec` can
distinguish a dead process from a degraded one without taking
dependencies down.

`/health/ready` exists so the reverse proxy and the release gate know
whether to route traffic.

## Response Shape

Both endpoints return `application/health+json` per the
[IETF draft `health-check-response-format`](https://datatracker.ietf.org/doc/draft-inadarei-api-health-check/)
shape:

```json
{
  "status": "pass",
  "version": "1.0.0",
  "releaseId": "<git-sha>",
  "checks": {
    "platform-db:ping": [
      { "componentType": "datastore", "status": "pass", "time": "2026-04-25T15:00:00Z", "observedValue": 4, "observedUnit": "ms" }
    ]
  }
}
```

| HTTP Status | Body `status` |
| --- | --- |
| `200` | `pass` |
| `200` | `warn` (degraded but routable; e.g. event-bus saturation) |
| `503` | `fail` |

The reverse proxy uses HTTP status only. Operators read the body for
diagnostic detail.

## Probe Set — Platform Host

`/health/ready` runs:

| Probe ID | Component | Failure Means |
| --- | --- | --- |
| `platform-db:ping` | `PlatformDbContext.Database.CanConnectAsync()` | Cannot read the platform DB; reject traffic |
| `platform-db:migrations` | `__EFMigrationsHistory` matches the assembly's expected head | Pending migration; reject traffic |
| `worker-heartbeat` | A row exists in `worker_heartbeats` newer than `30s` | Worker is dead; degrade (`warn`) — admin UI still works |

`/health/live` returns `pass` unless the process is shutting down.

## Probe Set — Tenant Host

`/health/ready` runs:

| Probe ID | Component | Failure Means |
| --- | --- | --- |
| `tenant-db:ping` | `TenantDbContext.Database.CanConnectAsync()` | Cannot read the tenant DB; reject traffic |
| `tenant-db:migrations` | Tenant DB migration head matches assembly | Pending migration; reject traffic |
| `event-bus:capacity` | The in-process event bus has free capacity per [AD-0006](./decisions.md#ad-0006-in-process-event-bus-for-real-time-surfaces) | Bus saturated; degrade (`warn`) |
| `tenant-context` | The `TABFLOW_TENANT_CODE` env var resolves to an `active` row in the platform's `tenant_registry` | Tenant disabled; reject |

`/health/live` returns `pass` unless the process is shutting down.

## Authentication

Health endpoints are **unauthenticated** and `[AllowAnonymous]`. They
are addressable only on the loopback interface (`127.0.0.1`); the
reverse proxy may expose `/health/live` on the public surface for
uptime probes but never exposes `/health/ready` (its body leaks
internal probe names).

## SignalR / WebSocket

The tenant host's WebSocket endpoint (`/ws/tables/{tableNumber}`) does
not participate in `/health/ready`. ESP32 device connectivity is
observed through `device.connected` / `device.disconnected` events on
the audit log, not through the readiness endpoint.

## Caching

Probes MUST NOT cache. Each request runs the probe set fresh. If a
probe becomes expensive (e.g. a slow DB ping), the fix is at the probe
level, not a stale cache.

## Implementation

```csharp
// Platform host:
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PlatformDbContext>(
        name: "platform-db:ping",
        tags: new[] { "ready" })
    .AddCheck<MigrationHeadHealthCheck<PlatformDbContext>>(
        name: "platform-db:migrations",
        tags: new[] { "ready" })
    .AddCheck<WorkerHeartbeatHealthCheck>(
        name: "worker-heartbeat",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready" });

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,  // no probes, just liveness
    ResponseWriter = HealthJsonWriter.Write
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthJsonWriter.Write
});
```

`HealthJsonWriter` produces the IETF-format response above. It lives in
`TabFlow.Shared.Infrastructure.Diagnostics` because both hosts share
it.

## Smoke Verification

After deployment:

```bash
curl -fsS http://127.0.0.1:5000/health/live   # platform liveness
curl -fsS http://127.0.0.1:5000/health/ready  # platform readiness
curl -fsS http://127.0.0.1:5001/health/live   # tenant liveness
curl -fsS http://127.0.0.1:5001/health/ready  # tenant readiness
```

All four MUST return `200` with `status: "pass"`. A `warn` is
acceptable for `worker-heartbeat` immediately after worker restart but
MUST recover within `60s`.

## Anti-Patterns

- Adding business probes (e.g. "open orders count") to `/health/ready`.
  Health is about routability, not domain state.
- Returning `200` with `status: "fail"`. The reverse proxy reads HTTP
  status only.
- Authenticating `/health/live`. The endpoint is the supervisor's
  contract, not a user contract.
- Combining `live` and `ready` into one endpoint. Their failure modes
  are fundamentally different.

## Related

- [`./runtime-surfaces.md`](./runtime-surfaces.md) — full surface map
  (HTML and HTTP)
- [`../../how-to/supervise-processes.md`](../../how-to/supervise-processes.md)
  — supervisor unit files consume `/health/ready`
- [`../../how-to/deploy-to-production.md`](../../how-to/deploy-to-production.md)
  — smoke checks invoke these endpoints
- [`../../meta/release-gate.md`](../../meta/release-gate.md) —
  observability gate items
- [`./decisions.md`](./decisions.md) AD-0006 — event bus probed by
  `event-bus:capacity`
