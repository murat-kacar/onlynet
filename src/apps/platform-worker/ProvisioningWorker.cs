using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Domain.Enums;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.PlatformWorker;

public class ProvisioningWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProvisioningWorker> _logger;

    public ProvisioningWorker(
        IServiceProvider serviceProvider,
        ILogger<ProvisioningWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing provisioning jobs");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessJobsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var pendingJobs = await context.ProvisioningJobs
            .Where(j => j.Status == ProvisioningJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);

        foreach (var job in pendingJobs)
        {
            try
            {
                await ClaimAndProcessJobAsync(context, job, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming/processing job {JobId}", job.Id);
            }
        }
    }

    private async Task ClaimAndProcessJobAsync(PlatformDbContext context, ProvisioningJob job, CancellationToken ct)
    {
        var workerId = Environment.MachineName;

        job.Claim(workerId);
        job.MarkRunning();

        context.ProvisioningJobs.Update(job);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Claimed job {JobId} for tenant {TenantId}", job.Id, job.TenantId);

        try
        {
            await ProvisionTenantAsync(job.TenantId, ct);
            job.MarkSucceeded("Tenant provisioned successfully");
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex.Message);
            _logger.LogError(ex, "Failed to provision tenant {TenantId}", job.TenantId);
        }

        context.ProvisioningJobs.Update(job);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Completed job {JobId}", job.Id);
    }

    private async Task ProvisionTenantAsync(Guid tenantId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var tenant = await context.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {tenantId} not found");
        }

        var dbName = $"tabflow_{tenant.Code.ToLowerInvariant()}";
        var dbUser = $"tabflow_{tenant.Code.ToLowerInvariant()}_app";
        var dbPassword = GeneratePassword();

        // Step 1: Create database
        await CreateDatabaseAsync(dbName, ct);

        // Step 2: Apply migrations
        await ApplyMigrationsAsync(dbName, ct);

        // Step 3: Create database user
        await CreateDatabaseUserAsync(dbName, dbUser, dbPassword, ct);

        // Step 4: Update tenant with connection info
        tenant.SetDatabaseConnection(dbName, dbUser, dbPassword);
        context.Tenants.Update(tenant);
        await context.SaveChangesAsync(ct);
    }

    private async Task CreateDatabaseAsync(string dbName, CancellationToken ct)
    {
        var connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=changeme";
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ApplyMigrationsAsync(string dbName, CancellationToken ct)
    {
        var connectionString = $"Host=localhost;Database={dbName};Username=postgres;Password=changeme";
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new TenantDbContext(options);
        await context.Database.MigrateAsync(ct);
    }

    private async Task CreateDatabaseUserAsync(string dbName, string dbUser, string dbPassword, CancellationToken ct)
    {
        var connectionString = $"Host=localhost;Database={dbName};Username=postgres;Password=changeme";
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE ROLE ""{dbUser}"" WITH LOGIN PASSWORD '{dbPassword}';
            GRANT CONNECT ON DATABASE ""{dbName}"" TO ""{dbUser}"";
            GRANT ALL PRIVILEGES ON SCHEMA public TO ""{dbUser}"";
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO ""{dbUser}"";
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO ""{dbUser}"";
        ";
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[new Random().Next(s.Length)]).ToArray());
    }
}
