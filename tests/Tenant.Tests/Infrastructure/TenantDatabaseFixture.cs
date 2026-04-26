using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Infrastructure.Data;
using Xunit;

namespace Tenant.Tests.Infrastructure;

/// <summary>
/// xUnit collection fixture that owns the connection string used by
/// every Integration-tier test in this project. The fixture runs
/// `EnsureCreatedAsync()` exactly once per test session, so the
/// schema is in place before any test opens its transaction; the
/// per-test rollback in
/// <see cref="TenantTransactionalTestBase"/> guarantees that no
/// row written by one test leaks into another.
///
/// Connection-string resolution:
///   - <c>INTEGRATION_TENANT_DB</c> environment variable when set
///     (the value the PR workflow's `Run integration tests` step
///     sets per the PostgreSQL service container in
///     <c>/.github/workflows/pr.yml</c>);
///   - otherwise a local default that points at a developer-side
///     PostgreSQL on the loopback interface.
///
/// Closes the fixture half of TD-0010 step 5.
/// </summary>
public sealed class TenantDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString =
            Environment.GetEnvironmentVariable("INTEGRATION_TENANT_DB")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__TenantDb")
            ?? "Host=localhost;Port=5432;Database=tabflow_tenant_test;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new TenantDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// xUnit collection definition that the Integration-tier test
/// classes opt into via <c>[Collection(nameof(TenantDatabaseCollection))]</c>.
/// Forces sequential execution across the collection so transactions
/// on the same connection do not race; the per-test rollback in
/// <see cref="TenantTransactionalTestBase"/> still keeps each test
/// hermetic.
/// </summary>
[CollectionDefinition(nameof(TenantDatabaseCollection))]
#pragma warning disable CA1711 // xUnit's collection definition pattern requires the "Collection" suffix; renaming would break the marker contract.
public sealed class TenantDatabaseCollection : ICollectionFixture<TenantDatabaseFixture>
{
    // No code; the marker is the contract.
}
#pragma warning restore CA1711
