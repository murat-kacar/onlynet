using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TabFlow.Platform.Middleware;
using TabFlow.Platform.Services;

namespace TabFlow.Platform.Pages;

[Authorize]
public class ChangePasswordModel : PageModel
{
    public IActionResult OnGet()
    {
        return LocalRedirect("/settings?tab=security");
    }

    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly PlatformIdentityOptions _identityOptions;

    public ChangePasswordModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        IOptions<PlatformIdentityOptions> identityOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _identityOptions = identityOptions.Value;
    }

    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (_identityOptions.EnableExternalIdentity)
        {
            return LocalRedirect("/settings?tab=security");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        if (NewPassword != ConfirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
        if (result.Succeeded)
        {
            // TD-0002 step 3: clear the must-change-password claim that
            // BootstrapAdminCommand stamps on the first admin so
            // PasswordChangeRequiredMiddleware stops bouncing the
            // session through this page. The claim is removed by
            // value ("true") to match what the bootstrap command set;
            // ASP.NET Core Identity's RemoveClaimAsync is a no-op if
            // the claim isn't present, so this is idempotent for
            // subsequent voluntary rotations.
            var existing = await _userManager.GetClaimsAsync(user);
            foreach (var claim in existing)
            {
                if (string.Equals(
                        claim.Type,
                        PasswordChangeRequiredMiddleware.MustChangePasswordClaim,
                        StringComparison.Ordinal))
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }
            }
            await _signInManager.RefreshSignInAsync(user);
            return LocalRedirect("/");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
