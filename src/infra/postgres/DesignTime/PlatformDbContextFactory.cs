using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Migrations.DesignTime;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=tabflow_platform;Username=tabflow_platform_app;Password=changeme_platform",
                o => o.MigrationsAssembly("TabFlow.Migrations")
                       .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .Options;

        return new PlatformDbContext(options);
    }
}
