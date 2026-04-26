# Supervise TabFlow Processes

This guide describes the **invariants** every TabFlow process supervisor
must honour and provides a reference systemd unit set that satisfies
them. Operators may use any supervisor (systemd, runit, s6, container
orchestrators) so long as the invariants below hold.

## Invariants Every Supervisor Must Honour

These statements are derived from
[`./deploy-to-production.md`](./deploy-to-production.md) and
[AD-0003](../reference/architecture/decisions.md#ad-0003-one-host-process-per-side).

1. **One platform host process per deployment.**
2. **One platform worker process per deployment.**
3. **One tenant host process per tenant.** Multiple tenants run as
   independent processes, not threads inside a shared host.
4. **Each process restarts on crash.** The supervisor relaunches a
   stopped process unless the operator stopped it deliberately.
5. **Each process reads configuration from a host-owned source outside
   the source tree.** No `appsettings.Production.json` lives in the
   repository.
6. **Each process is addressable individually** — by tenant code for
   tenant hosts, by side for platform and worker.
7. **Process supervision survives reboot.** A clean reboot brings every
   process back up.
8. **Logs are stream-captured.** Every process writes structured logs
   to stdout/stderr; the supervisor or a downstream log shipper handles
   persistence.
9. **WebSocket-bearing processes (tenant host) keep a long enough
   `KillSignal` grace** to drain client connections before SIGKILL.

## Naming

Reference unit names. Operators may rename so long as the invariants
hold.

- `tabflow-platform.service` — platform host
- `tabflow-platform-worker.service` — platform worker
- `tabflow-tenant@<tenant-code>.service` — tenant host (templated unit,
  one instance per tenant)

## Reference Unit Files

These files live outside the repository in a host-owned location such
as `/etc/systemd/system/`. They are not committed to version control.

### `tabflow-platform.service`

```ini
[Unit]
Description=TabFlow Platform Host
After=network-online.target postgresql.service
Wants=network-online.target
Requires=postgresql.service

[Service]
Type=notify
User=tabflow
Group=tabflow
WorkingDirectory=/opt/tabflow/platform
ExecStart=/usr/bin/dotnet /opt/tabflow/platform/TabFlow.Platform.dll
Restart=on-failure
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=30
EnvironmentFile=/etc/tabflow/platform.env
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
StandardOutput=journal
StandardError=journal
SyslogIdentifier=tabflow-platform

# Hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/log/tabflow

[Install]
WantedBy=multi-user.target
```

### `tabflow-platform-worker.service`

```ini
[Unit]
Description=TabFlow Platform Worker
After=network-online.target postgresql.service tabflow-platform.service
Wants=network-online.target
Requires=postgresql.service

[Service]
Type=notify
User=tabflow
Group=tabflow
WorkingDirectory=/opt/tabflow/platform-worker
ExecStart=/usr/bin/dotnet /opt/tabflow/platform-worker/TabFlow.PlatformWorker.dll
Restart=on-failure
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=30
EnvironmentFile=/etc/tabflow/platform-worker.env
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
StandardOutput=journal
StandardError=journal
SyslogIdentifier=tabflow-platform-worker

NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/log/tabflow /var/lib/tabflow

[Install]
WantedBy=multi-user.target
```

### `tabflow-tenant@.service`

This is a **systemd template** unit. The instance name (`%i`) is the
tenant code. Each tenant gets its own enabled instance:
`systemctl enable --now tabflow-tenant@dev-local.service`.

```ini
[Unit]
Description=TabFlow Tenant Host (%i)
After=network-online.target postgresql.service
Wants=network-online.target
Requires=postgresql.service

[Service]
Type=notify
User=tabflow
Group=tabflow
WorkingDirectory=/opt/tabflow/tenant
ExecStart=/usr/bin/dotnet /opt/tabflow/tenant/TabFlow.Tenant.dll
Restart=on-failure
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=60
EnvironmentFile=/etc/tabflow/tenants/%i.env
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=TABFLOW_TENANT_CODE=%i
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
StandardOutput=journal
StandardError=journal
SyslogIdentifier=tabflow-tenant-%i

NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/log/tabflow /var/lib/tabflow

[Install]
WantedBy=multi-user.target
```

## Environment Files

Per-process environment files are host-owned and never committed. Each
is read by exactly one unit (or one templated instance).

### `/etc/tabflow/platform.env`

```
ConnectionStrings__PlatformDb=Host=localhost;Database=tabflow_platform;Username=tabflow_platform_app;Password=<from-secrets-manager>
```

### `/etc/tabflow/platform-worker.env`

```
ConnectionStrings__PlatformDb=Host=localhost;Database=tabflow_platform;Username=tabflow_platform_app;Password=<from-secrets-manager>
TABFLOW_TENANT_ARTIFACT_ROOT=/var/lib/tabflow/tenants
TABFLOW_TENANT_CONFIG_ROOT=/etc/tabflow/tenants
TABFLOW_NGINX_VHOST_ROOT=/etc/nginx/sites-enabled
```

### `/etc/tabflow/tenants/<tenant-code>.env`

The platform worker generates one of these per tenant during
provisioning:

```
ConnectionStrings__TenantDb=Host=localhost;Database=tabflow_<tenant-code>;Username=tabflow_<tenant-code>_app;Password=<generated>
ASPNETCORE_URLS=http://127.0.0.1:<allocated-port>
TABFLOW_TENANT_DOMAIN=<tenant-domain>
```

## `Type=notify` Requirement

The hosts use `Type=notify` so systemd considers them started only
after ASP.NET Core signals readiness. This requires the host to call
`UseSystemd()` on the host builder:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemd();
```

`UseSystemd()` is a no-op when not run under systemd, so it is safe in
local development.

> **Implementation status (TD-0026).** As of PR #23, all three host
> projects reference `Microsoft.Extensions.Hosting.Systemd` (10.0.7)
> and call the systemd lifetime hook in `Program.cs`:
>   - `src/apps/platform/Program.cs` calls
>     `builder.Host.UseSystemd()` immediately after `UseSerilog()`.
>   - `src/apps/tenant/Program.cs` calls
>     `builder.Host.UseSystemd()` immediately after `UseSerilog()`.
>   - `src/apps/platform-worker/Program.cs` calls
>     `builder.Services.AddSystemd()` (the `HostApplicationBuilder`
>     equivalent) immediately after the builder is created.
>
> The composition-root regression test (TD-0026 step 3) lands with
> the integration test fixture in TD-0010 step 5. Until that
> regression test ships, the contract is enforced by code review.

## File Layout

Operator-controlled, outside the source tree. The reference layout:

```
/opt/tabflow/
  platform/         # published artifacts of TabFlow.Platform
  platform-worker/  # published artifacts of TabFlow.PlatformWorker
  tenant/           # published artifacts of TabFlow.Tenant (shared by every tenant instance)
/etc/tabflow/
  platform.env
  platform-worker.env
  tenants/
    <tenant-code>.env       # one per tenant, generated by worker
/var/lib/tabflow/
  tenants/
    <tenant-code>/          # per-tenant runtime artifacts (firmware sketches, etc.)
/var/log/tabflow/
  platform-YYYY-MM-DD.log
  tenant-<tenant-code>-YYYY-MM-DD.log
```

The tenant host binary is shared across instances; per-tenant state
lives entirely in the database, the per-tenant env file, and the
per-tenant runtime artifact directory.

## Anti-Patterns

- Running tenant hosts as Linux threads inside one process. This
  violates AD-0003 and removes the failure-domain isolation that lets a
  single tenant crash without affecting peers.
- Hard-coding tenant connection strings inside the unit file. Use
  `EnvironmentFile=` so the worker can rotate credentials without
  editing units.
- Running TabFlow processes as `root`. Use a dedicated `tabflow` system
  user.
- Using `Restart=always` for the platform worker. `on-failure` is
  correct; `always` masks intentional shutdowns during upgrades.
- Binding the tenant host to `0.0.0.0`. Tenant hosts bind to
  `127.0.0.1` and the reverse proxy is the only public face.

## Smoke Verification

After enabling units:

```bash
systemctl is-active tabflow-platform.service          # active
systemctl is-active tabflow-platform-worker.service   # active
systemctl is-active tabflow-tenant@dev-local.service  # active
journalctl -u tabflow-platform.service -n 50          # no fatal errors
curl -fsS http://127.0.0.1:5000/health/ready          # 200
```

## Related

- [`./bootstrap-platform.md`](./bootstrap-platform.md) — first run
  before any unit starts
- [`./deploy-to-production.md`](./deploy-to-production.md) — invariants
  this reference honours
- [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)
  AD-0003 — one host process per side
