using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TabFlow.Tenant.Services;

namespace TabFlow.Tenant.Pages;

public sealed class ActivateModel : PageModel
{
    private readonly TenantAdminActivationService _activationService;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public ActivateModel(
        TenantAdminActivationService activationService,
        SignInManager<IdentityUser<Guid>> signInManager)
    {
        _activationService = activationService;
        _signInManager = signInManager;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [MinLength(12)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? Email { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            StatusMessage = "This activation link is invalid or expired.";
            return Page();
        }

        var preview = await _activationService.GetPreviewAsync(Token, ct);
        if (preview is null)
        {
            StatusMessage = "This activation link is invalid or expired.";
            return Page();
        }

        Email = preview.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return await OnGetAsync(ct);
        }

        if (!ModelState.IsValid)
        {
            return await OnGetAsync(ct);
        }

        var result = await _activationService.ActivateAsync(Token, Password, ct);
        if (!result.Succeeded || result.User is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return await OnGetAsync(ct);
        }

        await _signInManager.SignInAsync(result.User, isPersistent: false);
        return Redirect("/settings?tab=security");
    }
}
