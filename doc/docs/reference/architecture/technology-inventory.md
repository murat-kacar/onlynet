# Technology Inventory

This document is the single inventory of the technologies, packages,
tooling, and dependency-injection registrations that make up TabFlow's
runtime hosts.

Use it when you need to answer questions like:

- what stack does TabFlow run on?
- which host depends on which packages?
- where is a given capability wired into DI?
- which services are singleton, scoped, or hosted?

High-level architectural rationale lives in
[`./decisions.md`](./decisions.md). This file is the operational
inventory, not the ADR.

## Runtime Baseline

| Area | Standard |
| --- | --- |
| Runtime | .NET 10 |
| Web framework | ASP.NET Core 10 |
| UI framework | Blazor Web App |
| Data access | EF Core 10 |
| PostgreSQL provider | Npgsql |
| Storage | PostgreSQL 17 |
| Authentication | ASP.NET Core Identity |
| Observability | OpenTelemetry + Serilog |
| Admin UI components | Radzen Blazor |
| Frontend scripting | TypeScript-first |
| Node toolchain | Node 24 LTS |
| Package manager | `pnpm` |
| Node package activation | `corepack` |
| Service supervision | `systemd` |

## Central Package Version Authority

NuGet package versions are centrally pinned in:

- [`/Directory.Packages.props`](/opt/onlynet/Directory.Packages.props)

Repository-wide .NET build defaults are centrally pinned in:

- [`/Directory.Build.props`](/opt/onlynet/Directory.Build.props)

Node and package-manager expectations are pinned in:

- [`/package.json`](/opt/onlynet/package.json)
- [`/.nvmrc`](/opt/onlynet/.nvmrc)
- [`/.node-version`](/opt/onlynet/.node-version)

## Project Inventory

### `src/apps/platform`

Primary role:

- platform admin host

Project file:

- [`TabFlow.Platform.csproj`](/opt/onlynet/src/apps/platform/TabFlow.Platform.csproj)

Important package surface:

- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `Microsoft.Extensions.Hosting.Systemd`
- `Microsoft.EntityFrameworkCore.Design`
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `Radzen.Blazor`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`
- `Serilog.Enrichers.Environment`

Project dependencies:

- [`TabFlow.Shared`](/opt/onlynet/src/packages/shared-dotnet/TabFlow.Shared.csproj)

### `src/apps/tenant`

Primary role:

- tenant runtime host

Project file:

- [`TabFlow.Tenant.csproj`](/opt/onlynet/src/apps/tenant/TabFlow.Tenant.csproj)

Important package surface:

- `Microsoft.AspNetCore.SignalR.Client`
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `Microsoft.Extensions.Hosting.Systemd`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.TypeScript.MSBuild`
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `Radzen.Blazor`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`
- `Serilog.Enrichers.Environment`

Project dependencies:

- [`TabFlow.Shared`](/opt/onlynet/src/packages/shared-dotnet/TabFlow.Shared.csproj)

Frontend build notes:

- TypeScript source lives under [`Interop/`](/opt/onlynet/src/apps/tenant/Interop)
- emitted JavaScript lives under [`wwwroot/js/`](/opt/onlynet/src/apps/tenant/wwwroot/js)
- `dotnet build` compiles the registered `TypeScriptCompile` items

### `src/apps/platform-worker`

Primary role:

- background worker for provisioning jobs

Project file:

- [`TabFlow.PlatformWorker.csproj`](/opt/onlynet/src/apps/platform-worker/TabFlow.PlatformWorker.csproj)

Important package surface:

- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Hosting.Systemd`
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.Http`
- `Npgsql`

Project dependencies:

- [`TabFlow.Shared`](/opt/onlynet/src/packages/shared-dotnet/TabFlow.Shared.csproj)

### `src/packages/shared-dotnet`

Primary role:

- shared domain, data, diagnostics, and service primitives

Project file:

- [`TabFlow.Shared.csproj`](/opt/onlynet/src/packages/shared-dotnet/TabFlow.Shared.csproj)

Important package surface:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Relational`
- `Npgsql.EntityFrameworkCore.PostgreSQL`

Framework references:

- `Microsoft.AspNetCore.App`

### `src/infra/postgres`

Primary role:

- EF Core migrations project

Project file:

- [`TabFlow.Migrations.csproj`](/opt/onlynet/src/infra/postgres/TabFlow.Migrations.csproj)

## Dependency Injection Inventory

The inventory below records the host startup registrations that are
load-bearing for runtime behaviour. It intentionally lists concrete
registrations, not just lifetime guidance.

### Platform Host DI

Source:

- [`src/apps/platform/Program.cs`](/opt/onlynet/src/apps/platform/Program.cs)

`DbContext` registrations:

- `AddDbContext<PlatformDbContext>`
- `AddDbContextFactory<PlatformDbContext>`

Scoped services:

- `IPlatformAuditService -> PlatformAuditService`
- `IPlatformAuditReadService -> PlatformAuditReadService`
- `ITenantRegistryService -> TenantRegistryService`
- `IProvisioningJobReadService -> ProvisioningJobReadService`
- `PlatformUserPreferenceService`
- `HttpClient` rooted to `NavigationManager.BaseUri`

Identity/auth registrations:

- `AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()`
- cookie configuration for `/login`
- authorization policies:
  - `Platform:Read`
  - `Platform:Write`
  - `Platform:Self`

UI/runtime registrations:

- `AddCascadingAuthenticationState()`
- `AddRadzenComponents()`
- `AddLocalization()`
- `AddRazorPages()`
- `AddControllers()`
- `AddRazorComponents().AddInteractiveServerComponents()`

Observability/runtime infrastructure:

- `AddOpenTelemetry()`
- `Configure<ForwardedHeadersOptions>()`
- `AddHealthChecks()`

Middleware wired in host pipeline:

- `AuditMiddleware`
- `PasswordChangeRequiredMiddleware`

### Tenant Host DI

Source:

- [`src/apps/tenant/Program.cs`](/opt/onlynet/src/apps/tenant/Program.cs)

`DbContext` registrations:

- `AddDbContext<TenantDbContext>`
- `AddDbContextFactory<TenantDbContext>`

Singleton services:

- `TableWebSocketHandler`
- `IEventBus -> InProcessEventBus`

Scoped services:

- `ICustomerSessionService -> CustomerSessionService`
- `ICartService -> CartService`
- `IOrderService -> OrderService`
- `SignalRService`
- `TenantUserPreferenceService`
- `CustomerSessionBrowserStore`
- `HttpClient` rooted to `NavigationManager.BaseUri`

Hosted/background services:

- `EventSubscriptionService`

Identity/auth registrations:

- `AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()`
- cookie configuration for `/login`
- authorization policies:
  - `Tenant:Read`
  - `Tenant:Write`
  - `Tenant:Self`

UI/runtime registrations:

- `AddSignalR()`
- `AddCascadingAuthenticationState()`
- `AddLocalization()`
- `AddRadzenComponents()`
- `AddRazorPages()`
- `AddRazorComponents().AddInteractiveServerComponents()`

Observability/runtime infrastructure:

- `AddOpenTelemetry()`
- `Configure<ForwardedHeadersOptions>()`
- `AddHealthChecks()`

### Platform Worker DI

Source:

- [`src/apps/platform-worker/Program.cs`](/opt/onlynet/src/apps/platform-worker/Program.cs)

Registrations:

- `AddSystemd()`
- `AddDbContext<PlatformDbContext>`
- `AddHostedService<ProvisioningWorker>()`

The worker intentionally keeps a very small DI surface compared to the
web hosts.

## Cross-Cutting External Dependencies

These are not NuGet packages, but they are operational dependencies of
the system:

- PostgreSQL 17
- `systemd`
- nginx reverse proxy
- Node 24 LTS for TypeScript compilation
- `pnpm` for Node package management

## Where To Read Next

- architecture rationale:
  [`./decisions.md`](./decisions.md)
- system map:
  [`./system-overview.md`](./system-overview.md)
- runtime routes and surfaces:
  [`./runtime-surfaces.md`](./runtime-surfaces.md)
- DI coding conventions:
  [`../../explanation/concepts/implementation-patterns.md`](../../explanation/concepts/implementation-patterns.md)
