namespace TabFlow.Platform.Services;

public sealed class PlatformIdentityOptions
{
    public const string SectionName = "Identity";

    public bool EnableExternalIdentity { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
    public bool RequireHttpsMetadata { get; set; } = true;
    public string SignedOutRedirectUri { get; set; } = "/login";
    public string? PlatformReadRole { get; set; } = "platform_viewer";
    public string? PlatformWriteRole { get; set; } = "platform_admin";
    public string? PlatformOwnerRole { get; set; } = "platform_owner";
    public string? RoleClient { get; set; }
}
