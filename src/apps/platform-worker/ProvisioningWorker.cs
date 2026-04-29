using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text.Json;
using TabFlow.Shared.Domain.Entities.Platform;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Domain.Enums;
using TabFlow.Shared.Domain;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.PlatformWorker;

public class ProvisioningWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProvisioningWorker> _logger;
    private readonly string _postgresAdminConnectionString;

    public ProvisioningWorker(
        IServiceProvider serviceProvider,
        ILogger<ProvisioningWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var platformConnectionString = configuration.GetConnectionString("PlatformDb")
            ?? throw new InvalidOperationException("ConnectionStrings:PlatformDb is not configured.");
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(platformConnectionString)
        {
            Database = "postgres"
        };
        _postgresAdminConnectionString = builder.ConnectionString;
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
                _logger.ErrorProcessingProvisioningJobs(ex);
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
                _logger.ErrorClaimingJob(job.Id, ex);
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

        _logger.ClaimedJob(job.Id, job.TenantId);

        try
        {
            var result = await ProvisionTenantAsync(job.TenantId, ct);
            job.MarkSucceeded(result);
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex.Message);
            _logger.FailedToProvisionTenant(job.TenantId, ex);
        }

        context.ProvisioningJobs.Update(job);
        await context.SaveChangesAsync(ct);

        _logger.CompletedJob(job.Id);
    }

    private async Task<string> ProvisionTenantAsync(Guid tenantId, CancellationToken ct)
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

        // Step 4: Bootstrap tenant admin
        var activationUrl = await BootstrapTenantAdminAsync(
            dbName,
            tenant.IntendedOwnerEmail,
            ct);

        // Step 5: Update tenant with connection info
        tenant.SetDatabaseConnection(dbName, dbUser, dbPassword);
        tenant.SetStatus(TenantStatus.Active);
        context.Tenants.Update(tenant);
        await context.SaveChangesAsync(ct);

        return JsonSerializer.Serialize(new
        {
            activationUrl,
            activationExpiresAt = DateTimeOffset.UtcNow.AddHours(72),
            adminEmail = tenant.IntendedOwnerEmail,
            primaryDomain = tenant.PrimaryDomain
        });
    }

    private async Task CreateDatabaseAsync(string dbName, CancellationToken ct)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_postgresAdminConnectionString);
        await connection.OpenAsync(ct);

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = """
                SELECT 1
                FROM pg_database
                WHERE datname = @dbName
                """;
            existsCommand.Parameters.AddWithValue("dbName", dbName);
            if (await existsCommand.ExecuteScalarAsync(ct) is not null)
            {
                return;
            }
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ApplyMigrationsAsync(string dbName, CancellationToken ct)
    {
        var connectionString = BuildTenantAdminConnectionString(dbName);
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly("TabFlow.Migrations")
                    .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .Options;

        await using var context = new TenantDbContext(options);
        await context.Database.MigrateAsync(ct);
    }

    private async Task CreateDatabaseUserAsync(string dbName, string dbUser, string dbPassword, CancellationToken ct)
    {
        await using var connection = new Npgsql.NpgsqlConnection(BuildTenantAdminConnectionString(dbName));
        await connection.OpenAsync(ct);

        await using (var roleCommand = connection.CreateCommand())
        {
            roleCommand.CommandText = """
                SELECT 1
                FROM pg_roles
                WHERE rolname = @roleName
                """;
            roleCommand.Parameters.AddWithValue("roleName", dbUser);
            if (await roleCommand.ExecuteScalarAsync(ct) is null)
            {
                await using var createRoleCommand = connection.CreateCommand();
                createRoleCommand.CommandText = $"""CREATE ROLE "{dbUser}" WITH LOGIN PASSWORD '{dbPassword}';""";
                await createRoleCommand.ExecuteNonQueryAsync(ct);
            }
            else
            {
                await using var alterRoleCommand = connection.CreateCommand();
                alterRoleCommand.CommandText = $"""ALTER ROLE "{dbUser}" WITH LOGIN PASSWORD '{dbPassword}';""";
                await alterRoleCommand.ExecuteNonQueryAsync(ct);
            }
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
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

    private string BuildTenantAdminConnectionString(string dbName)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_postgresAdminConnectionString)
        {
            Database = dbName
        };

        return builder.ConnectionString;
    }

    private async Task<string> BootstrapTenantAdminAsync(
        string dbName,
        string email,
        CancellationToken ct)
    {
        await using var connection = new Npgsql.NpgsqlConnection(BuildTenantAdminConnectionString(dbName));
        await connection.OpenAsync(ct);

        var normalizedEmail = email.ToUpperInvariant();
        var userId = await FindUserIdByEmailAsync(connection, normalizedEmail, ct) ?? Guid.NewGuid();

        var user = new IdentityUser<Guid>
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            LockoutEnabled = true,
        };
        await UpsertUserAsync(connection, user, passwordHash: null, ct);

        var roleId = await EnsureRoleAsync(connection, "admin", ct);
        await EnsureUserRoleAsync(connection, userId, roleId, ct);
        await EnsureClaimAsync(connection, userId, "TenantRole", "Read", ct);
        await EnsureClaimAsync(connection, userId, "TenantRole", "Write", ct);
        await EnsureClaimAsync(connection, userId, IdentityClaimTypes.MfaSetupRequired, "true", ct);

        var token = GenerateActivationToken();
        var tokenHash = HashActivationToken(token);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(72);

        await ReplaceActivationAsync(connection, TenantAdminActivation.Create(userId, email, tokenHash, expiresAt), ct);

        return $"https://{email.Split('@')[1]}/activate?token={token}";
    }

    private static async Task<Guid?> FindUserIdByEmailAsync(
        Npgsql.NpgsqlConnection connection,
        string normalizedEmail,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Id"
            FROM "AspNetUsers"
            WHERE "NormalizedEmail" = @normalizedEmail
            LIMIT 1
            """;
        command.Parameters.AddWithValue("normalizedEmail", normalizedEmail);

        var result = await command.ExecuteScalarAsync(ct);
        return result is Guid id ? id : null;
    }

    private static async Task UpsertUserAsync(
        Npgsql.NpgsqlConnection connection,
        IdentityUser<Guid> user,
        string? passwordHash,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "AspNetUsers" (
                "Id",
                "UserName",
                "NormalizedUserName",
                "Email",
                "NormalizedEmail",
                "EmailConfirmed",
                "PasswordHash",
                "SecurityStamp",
                "ConcurrencyStamp",
                "PhoneNumberConfirmed",
                "TwoFactorEnabled",
                "LockoutEnabled",
                "AccessFailedCount"
            )
            VALUES (
                @id,
                @userName,
                @normalizedUserName,
                @email,
                @normalizedEmail,
                TRUE,
                @passwordHash,
                @securityStamp,
                @concurrencyStamp,
                FALSE,
                FALSE,
                TRUE,
                0
            )
            ON CONFLICT ("Id") DO UPDATE
            SET
                "UserName" = EXCLUDED."UserName",
                "NormalizedUserName" = EXCLUDED."NormalizedUserName",
                "Email" = EXCLUDED."Email",
                "NormalizedEmail" = EXCLUDED."NormalizedEmail",
                "EmailConfirmed" = EXCLUDED."EmailConfirmed",
                "PasswordHash" = EXCLUDED."PasswordHash",
                "SecurityStamp" = EXCLUDED."SecurityStamp",
                "ConcurrencyStamp" = EXCLUDED."ConcurrencyStamp",
                "LockoutEnabled" = EXCLUDED."LockoutEnabled"
            """;
        command.Parameters.AddWithValue("id", user.Id);
        command.Parameters.AddWithValue("userName", user.UserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("normalizedUserName", user.NormalizedUserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("email", user.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("normalizedEmail", user.NormalizedEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("passwordHash", passwordHash ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("securityStamp", user.SecurityStamp ?? Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("concurrencyStamp", user.ConcurrencyStamp ?? Guid.NewGuid().ToString("N"));
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<Guid> EnsureRoleAsync(
        Npgsql.NpgsqlConnection connection,
        string roleName,
        CancellationToken ct)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();

        await using (var findCommand = connection.CreateCommand())
        {
            findCommand.CommandText = """
                SELECT "Id"
                FROM "AspNetRoles"
                WHERE "NormalizedName" = @normalizedName
                LIMIT 1
                """;
            findCommand.Parameters.AddWithValue("normalizedName", normalizedRoleName);

            var existing = await findCommand.ExecuteScalarAsync(ct);
            if (existing is Guid roleId)
            {
                return roleId;
            }
        }

        var newRoleId = Guid.NewGuid();

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
            VALUES (@id, @name, @normalizedName, @concurrencyStamp)
            ON CONFLICT ("NormalizedName") DO NOTHING
            """;
        insertCommand.Parameters.AddWithValue("id", newRoleId);
        insertCommand.Parameters.AddWithValue("name", roleName);
        insertCommand.Parameters.AddWithValue("normalizedName", normalizedRoleName);
        insertCommand.Parameters.AddWithValue("concurrencyStamp", Guid.NewGuid().ToString("N"));
        await insertCommand.ExecuteNonQueryAsync(ct);

        return await FindRoleIdAsync(connection, normalizedRoleName, ct)
            ?? throw new InvalidOperationException($"Failed to ensure tenant role '{roleName}'.");
    }

    private static async Task<Guid?> FindRoleIdAsync(
        Npgsql.NpgsqlConnection connection,
        string normalizedRoleName,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Id"
            FROM "AspNetRoles"
            WHERE "NormalizedName" = @normalizedName
            LIMIT 1
            """;
        command.Parameters.AddWithValue("normalizedName", normalizedRoleName);

        var result = await command.ExecuteScalarAsync(ct);
        return result is Guid id ? id : null;
    }

    private static async Task EnsureUserRoleAsync(
        Npgsql.NpgsqlConnection connection,
        Guid userId,
        Guid roleId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
            VALUES (@userId, @roleId)
            ON CONFLICT DO NOTHING
            """;
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("roleId", roleId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureClaimAsync(
        Npgsql.NpgsqlConnection connection,
        Guid userId,
        string claimType,
        string claimValue,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "AspNetUserClaims" ("UserId", "ClaimType", "ClaimValue")
            SELECT @userId, @claimType, @claimValue
            WHERE NOT EXISTS (
                SELECT 1
                FROM "AspNetUserClaims"
                WHERE "UserId" = @userId
                  AND "ClaimType" = @claimType
                  AND "ClaimValue" = @claimValue
            )
            """;
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("claimType", claimType);
        command.Parameters.AddWithValue("claimValue", claimValue);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task ReplaceActivationAsync(
        Npgsql.NpgsqlConnection connection,
        TenantAdminActivation activation,
        CancellationToken ct)
    {
        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.CommandText = """
                DELETE FROM tenant_admin_activations
                WHERE "UserId" = @userId
                """;
            deleteCommand.Parameters.AddWithValue("userId", activation.UserId);
            await deleteCommand.ExecuteNonQueryAsync(ct);
        }

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO tenant_admin_activations ("Id", "UserId", "Email", "TokenHash", "ExpiresAt", "CreatedAt", "ConsumedAt")
            VALUES (@id, @userId, @email, @tokenHash, @expiresAt, @createdAt, @consumedAt)
            """;
        insertCommand.Parameters.AddWithValue("id", activation.Id);
        insertCommand.Parameters.AddWithValue("userId", activation.UserId);
        insertCommand.Parameters.AddWithValue("email", activation.Email);
        insertCommand.Parameters.AddWithValue("tokenHash", activation.TokenHash);
        insertCommand.Parameters.AddWithValue("expiresAt", activation.ExpiresAt);
        insertCommand.Parameters.AddWithValue("createdAt", activation.CreatedAt);
        insertCommand.Parameters.AddWithValue("consumedAt", activation.ConsumedAt ?? (object)DBNull.Value);
        await insertCommand.ExecuteNonQueryAsync(ct);
    }

    private static string GenerateActivationToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashActivationToken(string token)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
