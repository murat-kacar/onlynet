using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using TabFlow.Tenant.Localization;

namespace TabFlow.Tenant.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly IStringLocalizer<TenantText> _localizer;

    public LoginModel(SignInManager<IdentityUser<Guid>> signInManager, IStringLocalizer<TenantText> localizer)
    {
        _signInManager = signInManager;
        _localizer = localizer;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _signInManager.PasswordSignInAsync(Email, Password, false, false);
        if (result.Succeeded)
        {
            return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/tables" : ReturnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("/LoginWith2Fa", new { ReturnUrl });
        }

        ModelState.AddModelError(string.Empty, _localizer["InvalidLoginAttempt"]);
        return Page();
    }
}
