# Local Development

How a contributor stands up TabFlow on their own machine for development
and tests. The production deployment guides assume an operator-managed
host and the bootstrap how-to assumes a clean platform; this tutorial
covers the developer working setup.

## Goals

- A contributor can run `platform`, `platform-worker`, and `tenant` hosts
  against local PostgreSQL.
- Local databases follow the production naming rules so seeded code paths
  stay identical.
- No production credentials, no production data, no production endpoints
  ever touch the local environment.

## Reserved Identifiers For Development

To preserve the production naming contract while keeping local data
isolated, the project reserves the following identifiers:

| Purpose | Reserved Value |
| --- | --- |
| Dev tenant code | `dev-local` |
| Dev platform DB | `tabflow_platform` |
| Dev platform role | `tabflow_platform_app` |
| Dev tenant DB (per AD-0001) | `tabflow_dev-local` |
| Dev tenant role | `tabflow_dev-local_app` |
| Dev platform-host port | `5000` |
| Dev tenant-host port | `5001` |
| Dev platform-worker | no listener |
| Design-time scratch DB (platform) | `tabflow_platform_design` |
| Design-time scratch DB (tenant) | `tabflow_tenant_design` |

Names like `tabflow_demo`, `tabflow_test`, `tabflow_local` are **not**
allowed. They violate AD-0007 and the schema convention because they do
not correspond to a registered tenant code.

`dev-local` is a real tenant code reserved for development. Its tenant
database participates in the same provisioning flow used in production —
the only difference is that nobody serves traffic for `dev-local` outside
the contributor's machine.

## Required Local Tools

- .NET 10 SDK
- PostgreSQL 17 (running locally, listening on default port `5432`)
- Node tooling — only if working on `firmware/` or generated documentation

The repository does not depend on Docker; running PostgreSQL in a container
is acceptable but not required.

## First-Run Setup

### 1. Create The PostgreSQL Roles And Databases

```sql
-- Run as postgres superuser, once per workstation:
CREATE ROLE tabflow_platform_app WITH LOGIN PASSWORD 'devpassword'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;

CREATE ROLE "tabflow_dev-local_app" WITH LOGIN PASSWORD 'devpassword'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;

CREATE DATABASE tabflow_platform OWNER tabflow_platform_app;
CREATE DATABASE "tabflow_dev-local" OWNER "tabflow_dev-local_app";

-- Design-time scratch databases used only by the EF Core CLI:
CREATE DATABASE tabflow_platform_design OWNER tabflow_platform_app;
CREATE DATABASE tabflow_tenant_design OWNER "tabflow_dev-local_app";
```

The dev passwords are deliberate plain strings. They never leave the
contributor's machine. Production credentials are generated per
[`../how-to/bootstrap-platform.md`](../how-to/bootstrap-platform.md) and
live outside the repository.

### 2. Configure `appsettings.Development.json`

Each host has its own development overrides. These files are committed
and contain **only** the local values listed above. They do not contain
production secrets.

`src/apps/platform/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "PlatformDb": "Host=localhost;Database=tabflow_platform;Username=tabflow_platform_app;Password=devpassword"
  },
  "Urls": "http://localhost:5000"
}
```

`src/apps/tenant/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "TenantDb": "Host=localhost;Database=tabflow_dev-local;Username=tabflow_dev-local_app;Password=devpassword"
  },
  "Urls": "http://localhost:5001"
}
```

`src/apps/platform-worker/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "PlatformDb": "Host=localhost;Database=tabflow_platform;Username=tabflow_platform_app;Password=devpassword"
  }
}
```

### 3. Apply Migrations

```bash
dotnet ef database update \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context PlatformDbContext

dotnet ef database update \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context TenantDbContext \
  --connection "Host=localhost;Database=tabflow_dev-local;Username=tabflow_dev-local_app;Password=devpassword"
```

### 4. Create The Dev Bootstrap Admin

```bash
dotnet run --project src/apps/platform/TabFlow.Platform.csproj -- \
  bootstrap-admin --email dev@localhost
```

The command prints a generated password once. Capture it; first login at
`http://localhost:5000/login` forces a password change.

### 5. Run The Hosts

In three terminals:

```bash
dotnet run --project src/apps/platform/TabFlow.Platform.csproj
dotnet run --project src/apps/platform-worker/TabFlow.PlatformWorker.csproj
dotnet run --project src/apps/tenant/TabFlow.Tenant.csproj
```

Verify:

- `http://localhost:5000` — platform admin home (after login)
- `http://localhost:5001` — tenant home for the `dev-local` tenant
- `http://localhost:5001/health/ready` — `200`

## Daily Workflow

| Task | Command |
| --- | --- |
| Build the solution | `dotnet build` |
| Run unit + integration tests | `dotnet test` |
| Add a platform migration | `dotnet ef migrations add <Name> --project src/infra/postgres/TabFlow.Migrations.csproj --context PlatformDbContext --output-dir Migrations/Platform` |
| Add a tenant migration | `dotnet ef migrations add <Name> --project src/infra/postgres/TabFlow.Migrations.csproj --context TenantDbContext --output-dir Migrations/Tenant` |
| Reset platform DB | `dropdb tabflow_platform && createdb -O tabflow_platform_app tabflow_platform && dotnet ef database update --context PlatformDbContext --project src/infra/postgres/TabFlow.Migrations.csproj` |
| Reset tenant DB | `dropdb "tabflow_dev-local" && createdb -O "tabflow_dev-local_app" "tabflow_dev-local" && dotnet ef database update --context TenantDbContext --project src/infra/postgres/TabFlow.Migrations.csproj --connection "..."` |

## Test Data

End-to-end tests target the `dev-local` tenant database. The test runner
is responsible for seeding fixtures inside test setup and cleaning them
in test teardown. Production migration files MUST NOT contain test
fixtures (see AD-0008 — migrations are schema authority, not seed
authority).

Seed for hand testing (categories, stations, tables, menu items) lives
in a separate dev-only seeder invoked by `dotnet run -- seed-dev`. It is
never called in production.

## Anti-Patterns

- Naming a local database `tabflow_demo`, `tabflow_test`, or
  `tabflow_local`. Use `tabflow_platform` and `tabflow_dev-local`.
- Connecting any host to PostgreSQL as the `postgres` superuser.
- Embedding the platform admin in a migration file with a hard-coded
  hash (see [`../how-to/bootstrap-platform.md`](../how-to/bootstrap-platform.md)).
- Sharing the design-time scratch databases with running hosts. The
  `_design` databases are for EF Core tooling only.
- Committing real production connection strings to
  `appsettings.Development.json`.

## Related

- [`../how-to/bootstrap-platform.md`](../how-to/bootstrap-platform.md) —
  first-run for a production platform
- [`../how-to/setup-migrations.md`](../how-to/setup-migrations.md) —
  migrations project layout and design-time factories
- [`../reference/database/schema.md`](../reference/database/schema.md) —
  naming convention
- [`../how-to/provision-tenant.md`](../how-to/provision-tenant.md) —
  provisioning a real tenant (uses the same flow `dev-local` follows in
  development)
