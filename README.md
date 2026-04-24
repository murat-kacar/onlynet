# TabFlow - Multi-Tenant Cafe && Restaurant Operations Platform

TabFlow is a multi-tenant cafe and restaurant operations platform built with .NET 10 and Blazor Web App.

## Project Structure

```
src/
  apps/
    platform/          - Control plane host (ASP.NET Core Blazor)
    platform-worker/     - Background worker for provisioning jobs
    tenant/              - Per-tenant runtime host (ASP.NET Core Blazor)
  packages/
    shared-dotnet/       - Shared domain, application services
    firmware/            - ESP32 firmware source and templates
  infra/
    postgres/migrations/ - EF Core migrations (platform + tenant)

tests/                 - Unit, integration, and E2E tests
docs/                  - Documentation (existing)
deploy/                - Deployment scripts, nginx, systemd configs
tools/                 - CLI tools for firmware generation and migrations
```

## Quick Start

1. Install .NET 10 SDK and PostgreSQL 17
2. Run migrations: `dotnet ef database update`
3. Start platform: `dotnet run --project src/apps/platform`
4. Start worker: `dotnet run --project src/apps/platform-worker`
5. Provision a tenant via platform admin UI

## Documentation

See `docs/` directory for full architecture documentation.
