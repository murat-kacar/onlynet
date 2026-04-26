using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TabFlow.Shared.Infrastructure.Diagnostics;

/// <summary>
/// Health check that surfaces the gap between the migration history
/// recorded in the live database and the migration set the running
/// binary was built against. Fires `Unhealthy` when at least one
/// migration is pending; fires `Healthy` when the model is at HEAD.
///
/// Registered under the `ready` tag on every host that owns a
/// <typeparamref name="TContext"/>. Per the spec at
/// <c>/doc/docs/reference/architecture/health-checks.md</c>, a host
/// that boots against an out-of-date database MUST report unready
/// rather than serve traffic with implicit schema mismatches.
///
/// Ledger: closes TD-0013 step 2.
/// </summary>
/// <typeparam name="TContext">
/// The EF Core <see cref="DbContext"/> whose migration history is
/// inspected. The platform host registers
/// <c>MigrationHeadHealthCheck&lt;PlatformDbContext&gt;</c>; the
/// tenant host registers
/// <c>MigrationHeadHealthCheck&lt;TenantDbContext&gt;</c>.
/// </typeparam>
public sealed class MigrationHeadHealthCheck<TContext> : IHealthCheck
    where TContext : DbContext
{
    private readonly TContext _context;

    public MigrationHeadHealthCheck(TContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingArray = pending.ToArray();
            if (pendingArray.Length == 0)
            {
                return HealthCheckResult.Healthy("Database schema is at the migration head.");
            }

            return HealthCheckResult.Unhealthy(
                $"{pendingArray.Length} pending migration(s): {string.Join(", ", pendingArray)}.");
        }
        catch (Exception ex)
        {
            // Connection failures, missing __EFMigrationsHistory table,
            // and provider-specific errors all surface as Unhealthy
            // with the exception message; the basic *-db:ping probe
            // is the canonical "is the DB reachable" gate, so this
            // probe's job is the migration-head delta only.
            return HealthCheckResult.Unhealthy(
                "Failed to read migration history.",
                ex);
        }
    }
}
