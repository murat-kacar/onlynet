using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TabFlow.Platform.Services;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Platform.Cli;

/// <summary>
/// One-shot bootstrap command that creates the first platform admin
/// per AD-0010 and the procedure documented in
/// <c>/doc/docs/how-to/bootstrap-platform.md</c>.
///
/// Behaviour:
/// 1. refuses to run if any user already exists in <c>AspNetUsers</c>;
/// 2. generates a CSPRNG-backed password;
/// 3. creates the user via <see cref="UserManager{TUser}"/>, so the
///    hash uses ASP.NET Core Identity's current
///    <see cref="IPasswordHasher{TUser}"/>;
/// 4. ensures the <c>owner</c> role exists and assigns it;
/// 5. writes an <c>auth.bootstrap</c> row to <c>platform_audit_log</c>;
/// 6. prints the generated password to stdout exactly once.
///
/// Recovery from a lost admin credential is a separate procedure
/// (<c>/doc/docs/how-to/rotate-secrets.md</c>); it is not a
/// re-bootstrap.
/// </summary>
internal static class BootstrapAdminCommand
{
    private const string OwnerRole = "owner";
    private const int PasswordLengthChars = 24;

    private const string PasswordCharset =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*-_=+";

    public static async Task<int> RunAsync(string[] args, CancellationToken ct = default)
    {
        var email = ParseEmail(args);
        if (email is null)
        {
            Console.Error.WriteLine("usage: bootstrap-admin --email <address>");
            return 1;
        }

        var builder = Host.CreateApplicationBuilder();
        ConfigureServices(builder);
        using var host = builder.Build();

        await using var scope = host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var auditService = sp.GetRequiredService<IPlatformAuditService>();

        // AD-0010: refuse if any user already exists. The check is
        // intentionally a single AnyAsync rather than a count so it
        // short-circuits on the first row.
        var anyUser = await userManager.Users.AnyAsync(ct);
        if (anyUser)
        {
            Console.Error.WriteLine(
                "bootstrap-admin: refusing to run; AspNetUsers is non-empty.");
            Console.Error.WriteLine(
                "Recovery from a lost credential is a separate procedure;");
            Console.Error.WriteLine(
                "see /doc/docs/how-to/rotate-secrets.md.");
            return 2;
        }

        var password = GeneratePassword(PasswordLengthChars);

        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            Console.Error.WriteLine("bootstrap-admin: failed to create user.");
            foreach (var error in createResult.Errors)
            {
                Console.Error.WriteLine($"  {error.Code}: {error.Description}");
            }
            return 3;
        }

        if (!await roleManager.RoleExistsAsync(OwnerRole))
        {
            var roleCreate = await roleManager.CreateAsync(
                new IdentityRole<Guid>(OwnerRole) { Id = Guid.NewGuid() });
            if (!roleCreate.Succeeded)
            {
                Console.Error.WriteLine(
                    "bootstrap-admin: failed to create owner role.");
                return 4;
            }
        }

        var roleAssign = await userManager.AddToRoleAsync(user, OwnerRole);
        if (!roleAssign.Succeeded)
        {
            Console.Error.WriteLine(
                "bootstrap-admin: failed to assign owner role.");
            return 5;
        }

        await auditService.LogAsync(
            actorId: user.Id,
            actorEmail: email,
            action: "auth.bootstrap",
            resourceType: "PlatformUser",
            resourceId: user.Id.ToString(),
            correlationId: Guid.NewGuid(),
            ct: ct);

        Console.WriteLine();
        Console.WriteLine($"Bootstrap admin created: {email}");
        Console.WriteLine();
        Console.WriteLine("Generated password (capture this now; it will not be shown again):");
        Console.WriteLine();
        Console.WriteLine(password);
        Console.WriteLine();
        Console.WriteLine("Sign in at https://<platform-host>/login. The first authenticated request");
        Console.WriteLine("will be redirected through /change-password.");
        Console.WriteLine();
        return 0;
    }

    private static string? ParseEmail(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--email")
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static void ConfigureServices(HostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("PlatformDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:PlatformDb is not configured. " +
                "Bootstrap requires the platform host's configuration to point at the " +
                "platform database before it can create the first admin.");

        builder.Services.AddDbContext<PlatformDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PlatformDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddScoped<IPlatformAuditService, PlatformAuditService>();
    }

    /// <summary>
    /// Generates a high-entropy password using <see cref="RandomNumberGenerator"/>.
    /// The 24-character output across the 73-character charset carries
    /// roughly 148 bits of entropy, well above any reasonable bcrypt
    /// brute-force budget.
    /// </summary>
    private static string GeneratePassword(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
        {
            sb.Append(PasswordCharset[b % PasswordCharset.Length]);
        }
        return sb.ToString();
    }
}
