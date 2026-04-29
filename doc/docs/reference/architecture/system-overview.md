# System Overview

TabFlow is a multi-tenant cafe and restaurant operations platform. The
system is split between a platform control plane and one tenant runtime
per venue. Both sides run as single ASP.NET Core host processes built
with Blazor Web App.

This document is the primary architecture snapshot for the repository.

User-facing product branding is `IoTable`. Internal source names,
assemblies, and namespaces remain `TabFlow`.

## System Shape

The diagram below shows the top-level processes and the direction of
traffic between them. The platform host and worker share the platform
database; each tenant host is one process bound to exactly one tenant
database; ESP32 table devices and staff/customer browsers talk to the
tenant host over WebSocket (`/ws/tables/`) and HTTP respectively.

```mermaid
flowchart TB
    subgraph plat["Platform Plane"]
        PH["Platform Host<br/>Blazor Web App<br/>(Interactive Server)"]
        PW["Platform Worker<br/>BackgroundService"]
        PH -->|enqueues provisioning jobs| PW
    end

    subgraph runtime["Tenant Runtime Plane"]
        TH_A["Tenant Host A<br/>Blazor Web App"]
        TH_N["Tenant Host N<br/>Blazor Web App"]
    end

    DB[("PostgreSQL<br/>platform + tenant_*")]

    PH --- DB
    PW --- DB
    PW -.->|bootstraps| TH_A
    PW -.->|bootstraps| TH_N
    TH_A --- DB
    TH_N --- DB

    ESP_A["ESP32 table device"] -.->|wss /ws/tables/| TH_A
    BR_A["Staff and customer browsers"] -->|HTTP / SignalR| TH_A
    ESP_N["ESP32 table device"] -.->|wss /ws/tables/| TH_N
    BR_N["Staff and customer browsers"] -->|HTTP / SignalR| TH_N
```

Notes:

- The platform host and the platform worker share the platform
  database.
- Each tenant host is one process, bound to one tenant database.
- Nginx terminates TLS and proxies to the platform host or the tenant
  host that matches the requested domain. It is omitted from the
  diagram for readability; see
  [`../../how-to/deploy-to-production.md`](../../how-to/deploy-to-production.md).

## Stack

| Layer | Choice |
| --- | --- |
| Runtime | .NET 10 |
| Web framework | ASP.NET Core 10 with Blazor Web App |
| Data access | EF Core 10 with Npgsql |
| Storage | PostgreSQL 17 |
| Authentication | Keycloak via OIDC clients in ASP.NET Core hosts |
| Real-time fan-out | In-process `System.Threading.Channels` event bus |
| Firmware | ESP32-C3 Arduino sketch generated per table |

The stack choice is recorded in
[`./decisions.md`](./decisions.md) under AD-0002, AD-0007, AD-0008, and
AD-0016.

## Source Tree

```text
src/apps/
  platform/                 ASP.NET Core host for the platform control plane
  platform-worker/          Background worker for tenant provisioning jobs
  tenant/                   ASP.NET Core host for one tenant runtime

src/packages/
  shared-dotnet/            Shared domain, application service, and primitive code
  firmware/                 ESP32 firmware source and generation inputs

src/infra/
  postgres/                 Migrations and database assets

docs/                       Documentation tree
```

`src/` is the canonical source root for tooling. Each host project
references the shared packages it needs. There are no web-tier or API-tier
subprojects; Blazor components, minimal API endpoints, and hosted services
live inside the same host project.

The platform host runs the control-plane admin surface. The platform
worker is a separate `BackgroundService` process that picks up
provisioning jobs from the platform database and drives the runtime
orchestration described in
[`../../how-to/provision-tenant.md`](../../how-to/provision-tenant.md).
Keeping it separate from the admin host keeps long-running provisioning
work off the request pipeline.

## Application Boundaries

### `src/apps/platform`

The platform host.

Owns:

- Platform admin authorization and local operator preferences
- Tenant registry
- Tenant domain assignment
- Tenant status and regional settings
- Provisioning job creation, pickup, and visibility
- Platform-level audit log

The platform host is not a tenant runtime. It must not access tenant business
tables during normal request handling.

### `src/apps/tenant`

The tenant host. One instance per tenant, bound to one tenant database.

Owns:

- Tenant schema bootstrap through EF Core migrations
- Tenant-local authorization mapping and operator preferences
- Tenant profile and public catalog surfaces
- Table and device state
- QR token lifecycle, table session lifecycle, and customer access tickets
- Customer cart and order submission
- Tenant admin, manager, cashier, and station surfaces
- Tenant-level audit log
- ESP32 device WebSocket endpoint

Each tenant host runs in its own process, against its own database, under its
own systemd unit.

## Runtime Surfaces

The full route map, role matrix, render mode per surface, event-bus
topology, and per-surface product notes live in
[`./runtime-surfaces.md`](./runtime-surfaces.md). That document is the
single authority; every other reference to a specific route, role, or
surface reads back into it.

Operator-facing surfaces follow a shared `Three-Pane Console Shell`
pattern:

- a collapsible left navigation rail
- a fixed top bar that names the current workspace
- a center work surface
- a collapsible right inspector for contextual detail and secondary action

This shell is part of the platform and tenant admin product contract,
not page-local decoration.

Render-mode strategy is recorded in
[`./render-modes.md`](./render-modes.md). Product reasoning around
surfaces lives in
[`../../explanation/concepts/operational-surfaces.md`](../../explanation/concepts/operational-surfaces.md).

## API Surface

With Blazor Web App, most server logic is invoked directly through dependency
injection from Razor components and hosted services. HTTP endpoints are kept
to the cases where HTTP is the natural contract:

| Endpoint group | Purpose |
| --- | --- |
| `/health`, `/health/live`, `/health/ready` | Liveness and readiness probes |
| `/api/public/**` | Customer-facing contracts that require explicit HTTP semantics (token verify, order submit) |
| `/ws/tables/{tableNumber}` | ESP32 device WebSocket endpoint |

There are no `/api/admin/**` endpoints. Admin and staff surfaces interact with
the domain through Blazor component bindings and application services, not
through an internal HTTP layer.

Full endpoint reference lives in
[`../api/tenant-api.md`](../api/tenant-api.md).

## Data Ownership

### Platform Database

Owns:

- Platform admin identity rows (ASP.NET Core Identity store)
- Tenant registry and domain assignment
- Provisioning jobs
- Platform audit log
- Secret references and registry metadata

The platform database does not hold tenant business state.

### Tenant Database

Owns:

- Tenant profile
- Tenant identity rows (admins, managers, cashiers, and station operators)
- Menu categories, items, and station routing
- Floor layouts, zones, table placements, and fixed floor objects
- Tables and device keys
- QR token, table session, and access ticket state
- Customer cart state bound to the active table session
- Orders and bills
- Station and kitchen-facing operational state
- Tenant audit log

The schema reference lives in [`../database/schema.md`](../database/schema.md).

## Runtime Model

The repository is source-first. Host-side state and secrets live outside the
repository.

High-level runtime split:

- source code stays in `/opt/tabflow`
- published host artifacts live under `/opt/tabflow-deploy/platform` and
  `/opt/tabflow-deploy/tenant`
- host-managed environment and config live under `/etc/tabflow`
- generated runtime artifacts live outside the source tree, for example
  firmware sketches under a runtime-owned output root

Nginx terminates TLS and proxies to the platform host or to the tenant host
that matches the requested domain. Each tenant host listens on a host-local
port selected at provisioning time.

The deployment procedure lives in
[`../../how-to/deploy-to-production.md`](../../how-to/deploy-to-production.md).

## Provisioning Principle

Creating a tenant is a job-oriented control-plane action:

1. Reserve tenant registry state in the platform database.
2. Create a `tenant.create` provisioning job.
3. Let the platform worker perform runtime orchestration.
4. Make lifecycle state visible and retryable through job status.

Infrastructure side effects are not allowed inside synchronous request
handlers. The full provisioning flow lives in
[`../../how-to/provision-tenant.md`](../../how-to/provision-tenant.md) and the
lifecycle reasoning lives in
[`../../explanation/concepts/tenant-lifecycle.md`](../../explanation/concepts/tenant-lifecycle.md).

## Non-Functional Baseline

Quality targets (availability, latency, provisioning success ratio)
live in [`./slos.md`](./slos.md). That document also defines the
measurement sources and the error-budget policy.

Observability direction:

- Structured logs with request id, tenant code when available, actor
  identity when available, and endpoint context
- Step-level provisioning visibility
- Metrics and tracing are exported through the OpenTelemetry .NET SDK;
  the SLO dashboards read from that stream

Security direction:

- No secrets in source control
- Platform and tenant hosts authenticate users through ASP.NET Core
  Identity cookies; handwritten HMAC session schemes are not used
- Admin, manager, cashier, and device credentials remain rotatable
- Raw secret material is only shown at creation or rotation time

## Testing And Tooling Conventions

### Test Layout

- Host and package tests live under sibling `tests/` directories next to the
  code they exercise.
- End-to-end Blazor tests may use `Microsoft.AspNetCore.Mvc.Testing` with
  `WebApplicationFactory` and `bUnit` for component-level tests.

### Build Artifacts

- .NET: `bin/` and `obj/`
- EF Core migrations: checked into `src/infra/postgres/Migrations/**`
- Generated firmware: written to a runtime-owned output root, never committed

The repository intentionally does not impose a synthetic root build directory.

## Platform And Tenant Split

The platform is not a tenant. The platform owns control-plane state and
tenant lifecycle orchestration. Tenants own runtime business state. This
separation is explicit in code, in database boundaries, in host processes, and
in documentation. The rationale lives in
[`./decisions.md`](./decisions.md) under AD-0001 and in
[`../../explanation/concepts/multi-tenancy.md`](../../explanation/concepts/multi-tenancy.md).
