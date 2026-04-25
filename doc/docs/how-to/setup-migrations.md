# Set Up The Migrations Project

This guide describes how `src/infra/postgres/TabFlow.Migrations.csproj`
is wired so that EF Core design-time tooling
(`dotnet ef migrations add`, `dotnet ef database update`) discovers both
`PlatformDbContext` and `TenantDbContext` without booting either host.

This guide implements [AD-0008](../reference/architecture/decisions.md#ad-0008-ef-core-as-schema-and-migration-authority)
and [AD-0009](../reference/architecture/decisions.md#ad-0009-migrations-live-in-a-standalone-project-with-design-time-factories).

## Why A Standalone Migrations Project

- Both hosts (`platform`, `tenant`) reference `TabFlow.Shared`, where the
  contexts live. Hosting migrations in either host couples migration
  tooling to host startup, which is brittle.
- A class-library migrations project keeps schema authority isolated and
  lets the platform worker re-use it during tenant provisioning without
  starting a host.
- AD-0008 makes EF Core authoritative; this layout is the simplest shape
  that honours it.

## Project Layout

```
src/infra/postgres/
  TabFlow.Migrations.csproj
  DesignTime/
    PlatformDesignTimeDbContextFactory.cs
    TenantDesignTimeDbContextFactory.cs
  Migrations/
    Platform/
      <timestamp>_<Name>.cs
      <timestamp>_<Name>.Designer.cs
      PlatformDbContextModelSnapshot.cs
    Tenant/
      <timestamp>_<Name>.cs
      <timestamp>_<Name>.Designer.cs
      TenantDbContextModelSnapshot.cs
```

The `migrations/` lowercase tree is **not** part of the layout and MUST
NOT exist. The Microsoft .NET convention is `Migrations/` capitalized;
mixing the two creates duplicate model snapshots and silent migration
drift.

## `.csproj` Shape

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <AssemblyName>TabFlow.Migrations</AssemblyName>
    <RootNamespace>TabFlow.Migrations</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\packages\shared-dotnet\TabFlow.Shared.csproj" />
  </ItemGroup>
</Project>
```

`Microsoft.EntityFrameworkCore.Design` MUST be present so that
`dotnet ef` can introspect the contexts.
`Npgsql.EntityFrameworkCore.PostgreSQL` is the provider used by both
contexts.

## Design-Time Factories

EF Core looks for `IDesignTimeDbContextFactory<T>` implementations in
the project that owns the migrations. Without these, the tooling tries
to boot the host's `Program.cs`, which fails because the migrations
project is a library.

### `DesignTime/PlatformDesignTimeDbContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Migrations.DesignTime;

public sealed class PlatformDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("TABFLOW_PLATFORM_DESIGN_DB")
            ?? "Host=localhost;Database=tabflow_platform_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new PlatformDbContext(options);
    }
}
```

### `DesignTime/TenantDesignTimeDbContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Migrations.DesignTime;

public sealed class TenantDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("TABFLOW_TENANT_DESIGN_DB")
            ?? "Host=localhost;Database=tabflow_tenant_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new TenantDbContext(options);
    }
}
```

The `_design` databases are scratch databases the tooling never writes
to in production. They exist solely so design-time queries against
`pg_catalog` have a place to land. The platform worker uses
production-grade connection strings at runtime, never the design-time
defaults.

## Common Failure Modes

### Empty Generated Migration

Symptom: `dotnet ef migrations add InitialCreate` produces a migration
whose `Up` method body is empty.

Root cause: the design-time factory is missing or the snapshot is out
of sync with the model. EF Core compares the model against the snapshot
and emits only the diff; with no snapshot and no factory, the model
resolves to empty.

Fix:

1. Confirm a `IDesignTimeDbContextFactory<T>` exists for that context.
2. Delete the broken migration file pair (`<timestamp>_*.cs` and
   `<timestamp>_*.Designer.cs`) and the affected snapshot.
3. Re-run `dotnet ef migrations add` against the right `--context` and
   `--output-dir`.

### "No DbContext was found"

Symptom: tooling prints `Unable to create a 'DbContext' of type 'X'`.

Root cause: factory class is in the wrong namespace, has the wrong
name, or the project does not reference the assembly that declares the
context.

Fix: factory MUST be public, declared in the migrations project, and
the project MUST reference `TabFlow.Shared`.

### Two Migration Roots

Symptom: both `migrations/` and `Migrations/` exist in the repo.

Root cause: the EF Core CLI defaults to `Migrations/` (capitalized). A
hand-typed `--output-dir migrations/...` once created the lowercase
variant and it was never deleted.

Fix: delete the lowercase tree. Schema authority is the snapshot
inside `Migrations/`.

## Tooling Commands

Generate:

```bash
dotnet ef migrations add <Name> \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context PlatformDbContext \
  --output-dir Migrations/Platform

dotnet ef migrations add <Name> \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context TenantDbContext \
  --output-dir Migrations/Tenant
```

Apply (platform):

```bash
dotnet ef database update \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context PlatformDbContext
```

Apply (tenant — requires explicit `--connection`):

```bash
dotnet ef database update \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context TenantDbContext \
  --connection "Host=localhost;Database=tabflow_<tenant-code>;Username=tabflow_<tenant-code>_app;Password=..."
```

The platform worker invokes the same migration assembly programmatically
via `PlatformDbContext.Database.MigrateAsync()` and
`TenantDbContext.Database.MigrateAsync()` after creating each tenant
database; it does not shell out to `dotnet ef`.

## Related

- [`./bootstrap-platform.md`](./bootstrap-platform.md) — first-run
  database setup
- [`../tutorials/local-development.md`](../tutorials/local-development.md)
  — dev tenant code and scratch DB names
- [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)
  AD-0008, AD-0009
- [`../reference/database/schema.md`](../reference/database/schema.md) —
  naming convention and ownership
