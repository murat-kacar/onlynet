# Authorization

This document describes the current authorization direction for TabFlow.

It covers:

- where authentication authority lives
- what application code still owns
- how platform and tenant policies are evaluated
- what stays outside workforce identity

The governing architecture decisions are
[`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
AD-0001, AD-0003, and AD-0016.

## Identity Authority

TabFlow is moving to an external identity-provider model for workforce users.

- `Keycloak` is the v1 identity authority
- platform and tenant hosts are OIDC clients
- application code is not the long-term source of truth for passwords, MFA,
  recovery, or login policy

This is a deliberate shift away from app-owned workforce authentication.

## Boundaries

### Keycloak owns

- interactive login
- password rules
- MFA enrollment and challenge
- account recovery
- workforce session initiation

### TabFlow owns

- authorization policies
- tenant scoping
- business audit
- local operator preferences
- customer QR/session access
- device access contracts

## Platform And Tenant Separation

Identity is centralized, but authorization boundaries remain separate.

- a platform operator is not implicitly a tenant operator
- a tenant operator is not implicitly a platform operator
- a token or session must carry explicit claims for the surface being entered

The application must reject any login that does not satisfy both:

1. the identity-provider-level sign-in succeeded
2. the application-level surface and tenant checks succeeded

## Policy Model

TabFlow continues to use named ASP.NET Core authorization policies.

Platform baseline:

- `Platform:Read`
- `Platform:Write`
- `Platform:Security`

Tenant baseline:

- `Tenant:Read`
- `Tenant:Write`
- `Tenant:Admin`
- `Tenant:Self`

Route-to-policy mapping remains the responsibility of
[`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md).

## Claims Mapping

Application code should not depend directly on raw Keycloak realm/group names.

Instead, each host maps provider claims into a small internal vocabulary
before policy evaluation. The internal vocabulary is documented in
[`../../reference/architecture/identity-architecture.md`](../../reference/architecture/identity-architecture.md).

This keeps business code stable if the external IdP changes later.

## Tenant Scoping

Tenant authorization is not satisfied by “user has a tenant role” alone.

The tenant host must also verify that:

- the requested tenant domain matches the tenant in the mapped claim set
- the role is valid for that tenant
- no cross-tenant claim bleed is accepted

This check happens before any business mutation is allowed.

## Bootstrap Direction

The secure baseline for workforce bootstrap is:

- no default password
- invitation or required-action flow
- first login completes password set and MFA enrollment
- high-privilege access is blocked until the enrollment baseline is satisfied

Bootstrap convenience is never allowed to weaken the security model.

## What Stays Outside Workforce Auth

These flows do not move into Keycloak in v1:

- customer QR/session access
- table access tickets
- device pairing and device credentials

They remain dedicated domain contracts because they are not workforce
identity problems and should not inherit enterprise workforce UX by default.

## Audit

Authentication and business actions remain auditable, but they come from
different layers.

- identity-provider events: login, MFA, session lifecycle
- application events: tenant changes, menu edits, billing, order state, and
  similar business actions

The application continues to own the business audit trail even after login
is externalized.

## Migration Note

Local ASP.NET Core Identity rows may exist temporarily during migration.
They are a bridge, not the target state. New product work should align with
the external-identity direction, not deepen app-owned workforce auth.

## Related

- [`../../reference/architecture/identity-architecture.md`](../../reference/architecture/identity-architecture.md)
- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
- [`./customer-session-model.md`](./customer-session-model.md)
- [`./threat-model.md`](./threat-model.md)
