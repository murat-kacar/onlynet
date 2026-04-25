# Authorization

This document describes the authorization model for TabFlow.

It covers:

- how identity is stored per host
- which roles exist
- which routes those roles can reach
- which authorization primitives ASP.NET Core exposes and how TabFlow uses
  them
- the status of the station-device access decision

The underlying decision is recorded in
[`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
AD-0005.

## Identity Boundaries

Each host owns its own identity store.

- The platform host stores platform identities in the platform database.
- Each tenant host stores its tenant identities in its own tenant database.

There is no cross-host identity federation. A platform admin is not a tenant
user. A tenant manager is not a platform admin. These stores never share
rows.

Customer access is not a user in the Identity sense. Customer access runs
through the QR-seeded access-ticket model, covered separately in
[`./customer-session-model.md`](./customer-session-model.md).

The ESP32 device endpoint authenticates with a device key at the WebSocket
handshake. It is not an Identity user. This is a deliberate carve-out and is
covered in the firmware reference.

## Roles

### Platform Host

| Role | Description |
| --- | --- |
| `owner` | Platform owner. Full control, including role assignment. |
| `admin` | Tenant lifecycle, audit, provisioning. Cannot edit owners. |
| `viewer` | Read-only dashboards and audit. |

Role capability is recorded by policy, not by hard-coded role checks inside
components. See [Authorization Primitives](#authorization-primitives).

### Tenant Host

| Role | Description |
| --- | --- |
| `owner` | Tenant owner. Full control, including role assignment. |
| `manager` | Tenant administrator for menu, floor layout, stations, staff users below owner, and reports. |
| `cashier` | Service floor and cashier surfaces. Orders, bills, table operations. |
| `station_device` | A single station terminal scoped to one station's fulfillment board. |

Users hold exactly one role in the baseline schema. A future m-to-n table
can be introduced without moving existing data if multi-role or custom roles
become required.

## Authentication Flow

Both hosts use ASP.NET Core Identity with the cookie scheme.

### Platform Host

1. User opens `/login`.
2. Identity validates the email and password against the platform store.
3. Identity issues a cookie with the `owner`, `admin`, or `viewer` role
   claim.
4. Subsequent requests carry the cookie; authorization policies decide
   access per route.

### Tenant Host

1. User opens `/login` on the tenant domain.
2. Identity validates the email and password against that tenant's store.
3. Identity issues a cookie scoped to the tenant host, carrying the
   `owner`, `manager`, `cashier`, or `station_device` role claim.
4. Role-based redirect sends the user to the appropriate default surface:
   - `owner`, `manager` → `/console`
   - `cashier` → `/service`
   - `station_device` → `/stations`

### Password Lifecycle

- Initial credentials generated during provisioning are shown once and must
  be changed on first successful login.
- Password hashing uses the default Identity hasher with the platform
  PBKDF2 parameters currently in use. Parameter upgrades are a normal
  Identity concern handled by the framework.
- Password change happens at `/change-password` on both hosts.

### Lockout And Rate Limiting

- Identity's lockout policy protects against brute force on all interactive
  login surfaces.
- Public customer endpoints (`/api/public/**`) are protected by ASP.NET Core
  rate limiting rather than by Identity lockout, because they do not carry a
  user context.

## Authorization Primitives

TabFlow uses two ASP.NET Core Authorization primitives:

- `[Authorize(Roles = "...")]` for route-level role checks
- Named authorization policies for composed rules

Policy naming follows the form `Surface:Action`, for example
`Console:WriteCatalog`, `Console:ManageUsersBelowOwner`,
`Service:CloseBill`, `Station:MarkReady`.

Platform-side read/write split uses `Platform:Read` (owner, admin,
viewer), `Platform:Write` (owner, admin), and `Platform:Self` (any
authenticated platform user — `/change-password` only). The full map
is in
[`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md).

`Console:ManageUsersBelowOwner` is the policy that gates the `/console/users`
surface. It allows `owner` and `manager` to enter the surface and enforces
the "managers cannot edit owner rows" rule at the application service, so
a manager can reach the page but cannot promote or demote an owner.

Policies are registered at host startup. Route attributes, Razor
`@attribute`, and `AuthorizeView` all resolve to the same policy set.

## Route Authorization Baseline

The full route-to-role map lives in
[`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md).
The authorization baseline is:

- Every authenticated route has an explicit policy or role check. There is
  no implicit default-allow behind authentication.
- Every anonymous route is listed explicitly in the surface map and carries
  no sensitive payload beyond what the QR handoff and the customer session
  model already define.
- Every mutating application service verifies the caller's role at the
  application boundary, not just in the component. Component-level checks
  are an ergonomics layer, not the security boundary.

## Audit

Every authenticated action that changes system state is recorded in the
appropriate audit log:

- Platform actions go to `platform_audit_log`
- Tenant actions go to `tenant_audit_log`

The audit baseline lives in
[`../../reference/database/schema.md`](../../reference/database/schema.md)
and lists the minimum action set that must be logged (login success and
failure, password change, role change, tenant status change, bill mutation,
catalog change, station device pair or revoke, provisioning job trigger).

## Station-Device Access

The station-device role exists in the baseline schema and guards the
`/stations` and `/stations/{stationCode}` routes. The concrete authentication
flow for that role is still open and depends on the station hardware
decision.

### What Is Decided

- The role exists as a first-class tenant identity role.
- Its routes are protected by a dedicated authorization policy.
- The rest of the stack (event bus subscription, board actions, audit) is
  written against the abstraction, so changing the authentication mechanism
  later does not ripple through the runtime surfaces.

### What Is Deferred

The authentication mechanism itself depends on the station hardware choice.
The options under consideration, ordered by how hardware-independent they
are:

1. **Pairing code plus device cookie** — a manager issues a code on
   `/console/stations/{id}/devices`, the station terminal enters the code
   once, and the host issues a long-lived, revokable cookie bound to that
   device. This works on any device with a browser and a cookie store,
   which covers every plausible station terminal class, and is the safe
   default if the project has to land something before the hardware is
   chosen.
2. **Identity user per station** — reuse ASP.NET Core Identity with a
   synthetic user row per station terminal. This reuses the existing
   Identity infrastructure but requires a human-friendly way to sign in
   on a shared terminal.
3. **Scoped URL token** — a URL that encodes a revokable token, removing
   the login step entirely. This is the lowest-friction option for
   kiosk-style terminals but requires the URL itself to be treated as a
   secret.

The final decision will land when the station hardware class is chosen.
Until then, the `/stations` route carries an `AuthorizationPolicy` named
`StationDevice` whose implementation is a placeholder. The placeholder is
a single clearly commented seam so the final mechanism can slot in
without touching the rest of the surface.

### Threat Notes

- Station board actions are mutating. A purely public URL would let any
  reachable actor transition order state. Whatever mechanism lands must be
  revokable per device.
- The station-device role must not be able to reach console, service, or
  PDA surfaces. That is enforced at the policy layer and is independent of
  the authentication mechanism choice.

## Related

- [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
  — AD-0005 and the station-device deferral
- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
- [`./customer-session-model.md`](./customer-session-model.md)
- [`../../reference/database/schema.md`](../../reference/database/schema.md)
