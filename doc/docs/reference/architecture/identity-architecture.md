# Identity Architecture

This document defines the target identity architecture for TabFlow as of
April 28, 2026.

## Decision

TabFlow standardizes on:

- `Keycloak` as the external identity provider
- `OpenID Connect` for interactive sign-in
- `OAuth 2.0` for token and logout flows
- `ASP.NET Core OpenID Connect + cookies` as the application-side client
  integration

The applications stop acting as primary credential stores for workforce
identities. Authentication moves to Keycloak. Application code keeps
authorization, tenant scoping, and audit.

This direction is recorded in
[`./decisions.md`](./decisions.md) as AD-0016.

## Why Keycloak

Keycloak is the v1 choice because it gives TabFlow a proven IAM control
plane now:

- realms, clients, groups, roles, MFA, and session control are already there
- standards compliance is the primary integration surface
- it keeps the application code out of the password, MFA, and credential
  recovery business
- it is easier to defend in a regulated environment than a growing custom
  auth stack

We integrate through standard OIDC, not Keycloak-specific adapters. This
keeps the app portable if a future move to Duende or another compliant IdP
becomes necessary.

## Trust Boundaries

### Keycloak owns

- user authentication
- password lifecycle
- MFA enrollment and challenge
- login session policy
- account recovery
- identity federation

### TabFlow owns

- tenant registry
- tenant membership and tenant-scoped authorization semantics
- platform-vs-tenant boundary enforcement
- application authorization policies
- audit of business actions
- customer QR/session model
- device authentication for non-human actors

## Host Model

### Platform host

- authenticates against Keycloak
- receives OIDC claims
- maps them into `Platform:*` authorization policies
- keeps a local profile/preferences row only when application data is needed

### Tenant host

- authenticates against Keycloak
- accepts only users assigned to the target tenant
- maps external claims into `Tenant:*` authorization policies
- keeps tenant-local preferences and business metadata only when that data is
  not identity authority

### Customer and device flows

Customer QR access and device pairing do not move into Keycloak in v1.
They stay as dedicated domain contracts because they are not workforce
identity problems.

## Claims Model

Application code should depend on a small internal claim vocabulary, not on
raw Keycloak realm details.

Baseline internal claims:

- `tabflow:subject`
- `tabflow:platform_role`
- `tabflow:tenant_id`
- `tabflow:tenant_role`
- `tabflow:mfa_level`

An application-side claims transformation layer maps Keycloak-issued claims
into these internal claims before policy evaluation.

## Realm And Client Shape

V1 baseline:

- one Keycloak realm for TabFlow workforce identity
- one OIDC confidential client for the platform host
- one OIDC confidential client for tenant hosts
- tenant membership represented through claims/groups/roles, not by creating a
  separate realm per tenant

This keeps workforce identity centralized while leaving tenant business data
isolated in tenant databases.

## Authorization Strategy

Authentication is externalized. Authorization remains in the app.

Platform policies:

- `Platform:Read`
- `Platform:Write`
- `Platform:Security`

Tenant policies:

- `Tenant:Read`
- `Tenant:Write`
- `Tenant:Admin`
- `Tenant:Self`

The source of truth for route-level authorization remains
[`./runtime-surfaces.md`](./runtime-surfaces.md).

## Security Baseline

The target baseline for workforce identity is:

- authorization code flow
- confidential clients
- server-side session cookie in the ASP.NET Core host
- PKCE where supported by the client flow
- MFA required for privileged users
- strict redirect URI registration
- no default passwords
- no shared superuser credential spanning platform and tenant
- no custom password-reset or MFA logic in application code unless there is a
  documented exception

## Migration Plan

Migration happens in ordered slices.

## Current Working Mode

As of April 29, 2026, TabFlow is intentionally running in a temporary
development bridge:

- the target architecture remains Keycloak-backed workforce identity
- platform and tenant hosts may continue using local ASP.NET Core Identity
  for active feature development
- this bridge exists to unblock UI, workflow, and module construction while
  the external identity rollout is still being operationalized

This is a delivery tactic, not a reversal of AD-0016. It is acceptable only
for non-production development environments.

### Phase 1: Prepare the application boundary

- introduce an identity integration seam in both hosts
- move authorization checks fully onto named policies and claims mapping
- stop adding new local-login features
- keep existing local auth only as a temporary bridge

### Phase 2: Stand up Keycloak

- deploy Keycloak
- define realm, clients, redirect URIs, logout URIs, and role model
- configure MFA flows and password policy there

### Phase 3: Platform migration

- switch platform login to OIDC
- map Keycloak roles into `Platform:*` policies
- remove platform local password workflows from the main operator path

### Phase 4: Tenant migration

- switch tenant workforce login to OIDC
- carry tenant membership and tenant role in claims
- enforce tenant-domain and tenant-claim consistency at sign-in time

### Phase 5: Bootstrap redesign

- tenant creation creates tenant metadata plus an invitation workflow
- first admin activation happens through Keycloak-required actions
- the app no longer generates or stores usable bootstrap passwords

### Phase 6: Remove local workforce auth

- remove app-owned workforce password flows
- keep only the local data the applications truly own
- retain customer QR/session auth and device auth as separate domain contracts

## Portability To Duende

A future move from Keycloak to Duende is feasible if these rules hold:

- use standard OIDC/OAuth integration in the apps
- isolate claims mapping in one place
- keep application policies independent from IdP-specific claim names
- do not leak Keycloak realm/group semantics into business code

The hard part of a future move is not protocol wiring. It is user, MFA,
session, and recovery-data migration. This is why portability is an explicit
architecture goal from day one.
