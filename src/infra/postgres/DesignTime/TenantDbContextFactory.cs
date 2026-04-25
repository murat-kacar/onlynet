using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Migrations.DesignTime;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=tabflow_design;Username=postgres",
                o => o.MigrationsAssembly("TabFlow.Migrations")
                       .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .Options;

        return new TenantDbContext(options);
    }
}
