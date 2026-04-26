using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TabFlow.Shared.Infrastructure.Data;
using Xunit;

namespace Tenant.Tests.Infrastructure;

/// <summary>
/// Base class for Integration-tier test classes that need a real
/// <see cref="TenantDbContext"/>. The base opens a database
/// transaction in <see cref="InitializeAsync"/> and rolls it back
/// in <see cref="DisposeAsync"/>, so every <c>[Fact]</c> sees an
/// empty schema (modulo any seed data the test inserts) and leaves
/// nothing behind.
///
/// Subclasses MUST be decorated with
/// <c>[Collection(nameof(TenantDatabaseCollection))]</c> so the
/// shared <see cref="TenantDatabaseFixture"/> is injected through
/// the constructor, and with
/// <c>[Trait("Category", "Integration")]</c> so the PR workflow's
/// Integration step runs them.
///
/// Closes the base-class half of TD-0010 step 5.
/// </summary>
public abstract class TenantTransactionalTestBase : IAsyncLifetime
{
    private readonly TenantDatabaseFixture _fixture;
    private IDbContextTransaction _transaction = null!;

    protected TenantTransactionalTestBase(TenantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// The <see cref="TenantDbContext"/> for this test. The context
    /// is constructed inside <see cref="InitializeAsync"/> and
    /// disposed inside <see cref="DisposeAsync"/>; consumers MUST
    /// NOT cache the reference outside the test method.
    /// </summary>
    protected TenantDbContext Db { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        Db = new TenantDbContext(options);
        _transaction = await Db.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
        await Db.DisposeAsync();
    }
}
