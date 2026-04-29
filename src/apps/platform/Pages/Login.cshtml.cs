using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TabFlow.Platform.Services;

namespace TabFlow.Platform.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly PlatformIdentityOptions _identityOptions;

    public LoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        IOptions<PlatformIdentityOptions> identityOptions)
    {
        _signInManager = signInManager;
        _identityOptions = identityOptions.Value;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Error { get; set; }

    public bool ExternalIdentityEnabled => _identityOptions.EnableExternalIdentity;

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_identityOptions.EnableExternalIdentity)
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl
                },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        var result = await _signInManager.PasswordSignInAsync(Email, Password, false, false);
        if (result.Succeeded)
        {
            return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }
}
