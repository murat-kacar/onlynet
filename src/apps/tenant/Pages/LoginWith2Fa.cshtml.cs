using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TabFlow.Tenant.Pages;

public sealed class LoginWith2FaModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public LoginWith2FaModel(SignInManager<IdentityUser<Guid>> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    [Required]
    public string TwoFactorCode { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var authenticatorCode = TwoFactorCode.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, false, false);
        if (result.Succeeded)
        {
            return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/tables" : ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        return Page();
    }
}
