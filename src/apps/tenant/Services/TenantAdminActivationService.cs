using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Shared.Domain.Entities.Tenant;

namespace TabFlow.Tenant.Services;

public sealed class TenantAdminActivationService
{
    private readonly TenantDbContext _context;
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public TenantAdminActivationService(
        TenantDbContext context,
        UserManager<IdentityUser<Guid>> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ActivationPreview?> GetPreviewAsync(string token, CancellationToken ct = default)
    {
        var activation = await FindActiveActivationAsync(token, ct);
        if (activation is null)
        {
            return null;
        }

        return new ActivationPreview(activation.Email, activation.ExpiresAt);
    }

    public async Task<ActivationResult> ActivateAsync(string token, string password, CancellationToken ct = default)
    {
        var activation = await FindActiveActivationAsync(token, ct);
        if (activation is null)
        {
            return ActivationResult.InvalidOrExpired;
        }

        var user = await _userManager.FindByIdAsync(activation.UserId.ToString());
        if (user is null)
        {
            return ActivationResult.InvalidOrExpired;
        }

        if (await _userManager.HasPasswordAsync(user))
        {
            return ActivationResult.InvalidOrExpired;
        }

        var addPassword = await _userManager.AddPasswordAsync(user, password);
        if (!addPassword.Succeeded)
        {
            return ActivationResult.FromErrors(addPassword.Errors.Select(error => error.Description));
        }

        user.EmailConfirmed = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ActivationResult.FromErrors(updateResult.Errors.Select(error => error.Description));
        }

        activation.Consume();
        await _context.SaveChangesAsync(ct);

        return ActivationResult.Success(user);
    }

    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    private async Task<TenantAdminActivation?> FindActiveActivationAsync(string token, CancellationToken ct)
    {
        var tokenHash = HashToken(token);
        var activation = await _context.TenantAdminActivations
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, ct);

        if (activation is null || !activation.IsActive(DateTimeOffset.UtcNow))
        {
            return null;
        }

        return activation;
    }
}

public sealed record ActivationPreview(string Email, DateTimeOffset ExpiresAt);

public sealed class ActivationResult
{
    private ActivationResult(bool succeeded, IdentityUser<Guid>? user, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        User = user;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public IdentityUser<Guid>? User { get; }
    public IReadOnlyList<string> Errors { get; }

    public static ActivationResult InvalidOrExpired { get; } =
        new(false, null, ["This activation link is invalid or expired."]);

    public static ActivationResult Success(IdentityUser<Guid> user) =>
        new(true, user, Array.Empty<string>());

    public static ActivationResult FromErrors(IEnumerable<string> errors) =>
        new(false, null, errors.ToArray());
}
