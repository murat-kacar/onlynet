# Configure Keycloak For The Platform Host

This guide defines the minimum Keycloak setup required for the platform host
to authenticate operators through OpenID Connect.

It is the v1 operational companion to
[`../reference/architecture/identity-architecture.md`](../reference/architecture/identity-architecture.md).

If the team is still in non-production feature-construction mode, this guide
may be deferred temporarily while local development identity remains enabled.
That deferral is a tactical bridge only; it does not replace the target
architecture.

## Goal

After this guide is completed:

- Keycloak is the interactive identity provider for the platform host
- the platform host uses OIDC code flow
- platform operators receive claims that map to `Platform:Read` and
  `Platform:Write`
- local platform passwords are no longer the primary login path

## Inputs

Required:

- public Keycloak URL, for example `https://identity.cafetech.uk`
- realm name, recommended: `tabflow`
- public platform URL, for example `https://platform.cafetech.uk`
- a generated client secret for the platform client

## Realm Baseline

Create one realm:

- `tabflow`

Use one shared workforce realm for the baseline. Do not create one realm per
tenant.

## Platform Client

Create one confidential OIDC client:

- `Client ID`: `tabflow-platform`
- `Client authentication`: enabled
- `Authorization`: disabled
- `Standard flow`: enabled
- `Direct access grants`: disabled
- `Implicit flow`: disabled
- `Service accounts`: disabled

Redirect URIs:

- `https://platform.cafetech.uk/signin-oidc`

Post-logout redirect URIs:

- `https://platform.cafetech.uk/login`

Web origins:

- `https://platform.cafetech.uk`

## Roles

Create realm or client roles that map cleanly into platform authorization.

Recommended baseline:

- `platform_viewer`
- `platform_admin`
- `platform_owner`

The application maps these into:

- `platform_viewer` -> `PlatformRole = Read`
- `platform_admin` -> `PlatformRole = Read`, `PlatformRole = Write`
- `platform_owner` -> `PlatformRole = Read`, `PlatformRole = Write`

## Claims

The platform host can read roles from either:

- realm roles (`realm_access.roles`)
- client roles under `resource_access.tabflow-platform.roles`

For the current implementation, set:

- `RoleClient = tabflow-platform`

and keep role names aligned with the appsettings example.

## Platform Configuration

Set the platform host configuration:

```json
"Identity": {
  "EnableExternalIdentity": true,
  "Authority": "https://identity.cafetech.uk/realms/tabflow",
  "ClientId": "tabflow-platform",
  "ClientSecret": "<generated-secret>",
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc",
  "SignedOutRedirectUri": "/login",
  "RequireHttpsMetadata": true,
  "PlatformReadRole": "platform_viewer",
  "PlatformWriteRole": "platform_admin",
  "PlatformOwnerRole": "platform_owner",
  "RoleClient": "tabflow-platform"
}
```

Production secrets must live in host-owned configuration, not in the source
tree.

## Recommended First Users

Create at least:

- one `platform_owner`
- one `platform_admin`
- optionally one `platform_viewer`

Do not share a single global superuser credential among operators.

## MFA

For platform operators, MFA should be required in Keycloak.

Baseline recommendation:

- require password update only when explicitly needed
- require authenticator-app enrollment for `platform_admin` and
  `platform_owner`
- disable any fallback that weakens privileged access unless there is a
  documented recovery process

## Verification

After configuration:

1. Set `Identity:EnableExternalIdentity=true`
2. restart the platform host
3. open `https://platform.cafetech.uk/login`
4. verify that the page shows the Keycloak sign-in path
5. sign in with a Keycloak user carrying `platform_admin` or
   `platform_owner`
6. verify:
   - `/` loads
   - `/tenants` loads
   - `/jobs` loads
   - `/settings` loads

If login succeeds but protected pages redirect or 403, the claim mapping is
wrong, not the OIDC handshake.

## Failure Hints

- `login loops back to /login`
  - check redirect URIs and callback path
- `authenticated but unauthorized`
  - check role assignment and role-claim mapping
- `logout returns to an error page`
  - check post-logout redirect URI and signed-out callback path

## Follow-Up

Once the platform host is stable on Keycloak:

1. migrate the tenant host to the same OIDC model
2. redesign tenant bootstrap around invitation / required actions
3. remove local workforce password flows from the platform host
