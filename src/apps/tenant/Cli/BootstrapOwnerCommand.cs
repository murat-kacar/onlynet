using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TabFlow.Shared.Infrastructure.Data;

namespace TabFlow.Tenant.Cli;

internal static class BootstrapOwnerCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct = default)
    {
        var email = Parse(args, "--email");
        var password = Parse(args, "--password");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("usage: bootstrap-owner --email <address> --password <secret>");
            return 1;
        }

        var builder = Host.CreateApplicationBuilder();
        ConfigureServices(builder);
        using var host = builder.Build();

        await using var scope = host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                Console.Error.WriteLine("bootstrap-owner: failed to create user.");
                foreach (var error in createResult.Errors)
                {
                    Console.Error.WriteLine($"  {error.Code}: {error.Description}");
                }
                return 2;
            }
        }
        else
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!resetResult.Succeeded)
            {
                Console.Error.WriteLine("bootstrap-owner: failed to reset password.");
                return 3;
            }
        }

        const string ownerRole = "owner";
        if (!await roleManager.RoleExistsAsync(ownerRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(ownerRole) { Id = Guid.NewGuid() });
        }

        if (!await userManager.IsInRoleAsync(user, ownerRole))
        {
            await userManager.AddToRoleAsync(user, ownerRole);
        }

        var claims = await userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == "TenantRole" && c.Value == "Read"))
        {
            await userManager.AddClaimAsync(user, new Claim("TenantRole", "Read"));
        }
        if (!claims.Any(c => c.Type == "TenantRole" && c.Value == "Write"))
        {
            await userManager.AddClaimAsync(user, new Claim("TenantRole", "Write"));
        }

        Console.WriteLine($"Tenant owner ready: {email}");
        return 0;
    }

    private static string? Parse(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == key)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static void ConfigureServices(HostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("TenantDb")
            ?? throw new InvalidOperationException("ConnectionStrings:TenantDb is not configured.");

        builder.Services.AddDbContext<TenantDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TenantDbContext>()
            .AddDefaultTokenProviders();
    }
}
