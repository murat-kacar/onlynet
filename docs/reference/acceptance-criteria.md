# Acceptance Criteria

This document is the flat, testable contract of behaviours that TabFlow
MUST satisfy. Where the [capability matrix][cm] reports *what exists*,
this document states *what must be true*. Each item is deliberately
small enough to map to a single integration or system test.

Anchor identifiers in `AC-XXX` form are stable; new items MUST take a
fresh trailing number rather than reuse a retired one.

[cm]: ./architecture/capability-matrix.md

## Platform Access

- **AC-001** The platform admin console MUST reject unauthenticated
  traffic on every route except `/login`, `/change-password`, and the
  health probes.
- **AC-002** Platform admin sign-in MUST use ASP.NET Core Identity with
  the platform Identity store.
- **AC-003** The platform Identity store MUST NOT federate or merge
  with any tenant Identity store.
- **AC-004** A platform admin marked inactive MUST be refused sign-in
  and MUST have all active sessions terminated within the next
  authentication round trip.

## Tenant Access

- **AC-010** The tenant admin and staff consoles MUST reject
  unauthenticated traffic on every route except `/`, `/g/{token}`,
  `/menu`, `/login`, `/change-password`, and the health probes.
- **AC-011** Tenant sign-in MUST use ASP.NET Core Identity with the
  Identity store inside that tenant's database.
- **AC-012** A tenant user MUST NOT be able to authenticate against a
  tenant other than the one that stores their credentials.
- **AC-013** A station device account MUST only reach `/stations` and
  `/stations/{stationCode}`; it MUST NOT reach `/console*`,
  `/service`, or `/pda`.
- **AC-014** The `Console:ManageUsersBelowOwner` policy MUST deny
  managers from editing `owner` rows on `/console/users`.
- **AC-015** The last active `owner` on a tenant MUST NOT be
  deactivated or demoted.

## Customer Join And Session

- **AC-020** A customer MUST NOT reach a state that shows menu items
  or accepts order submissions without a current, valid customer
  session.
- **AC-021** The only way to open a customer session MUST be a fresh
  scan of the table QR at `/g/{token}`.
- **AC-022** A QR token MUST be single-use. A second successful join
  with the same token value MUST fail with `token_used`.
- **AC-023** A QR token that has expired MUST fail with
  `token_expired`.
- **AC-024** The customer session cookie MUST be `HttpOnly`, `Secure`,
  and `SameSite=Lax` (or stricter).
- **AC-025** The `/menu` route MUST return a locked state referring
  the visitor to the table QR when no valid session exists, without
  leaking catalog, pricing, or availability data.
- **AC-026** A customer-initiated logout endpoint MUST NOT be exposed
  from the public API.

## Order Submission

- **AC-030** `POST /api/public/orders` MUST require a still-open
  customer session on the submitting device.
- **AC-031** `POST /api/public/orders` MUST require a fresh QR
  checkout-proof token produced by a second scan of the current table
  QR at submit time.
- **AC-032** An expired or consumed checkout proof MUST fail with
  `checkout_proof_expired` or `checkout_proof_invalid` respectively.
- **AC-033** Order submission MUST be atomic: cart contents and the
  resulting order and order items MUST commit together or not at all.
- **AC-034** On successful order submission, the in-process event bus
  MUST publish `order.submitted` within the same request so the floor
  workspace and the relevant station boards receive it in real time.
- **AC-035** An empty cart MUST be rejected with `cart_empty`.
- **AC-036** Successful order submission MUST close the originating
  customer session; a second order from the same cookie MUST require a
  fresh QR scan.

## Table And Bill Invariants

- **AC-040** A table MUST have at most one open bill at any time.
- **AC-041** The first order on a table with no open bill MUST
  automatically open a bill.
- **AC-042** Subsequent orders from the same table while a bill is open
  MUST be appended to that bill rather than open a second one.
- **AC-043** Only users with `cashier`, `manager`, or `owner` role MAY
  close a bill.
- **AC-044** Closing a bill MUST invalidate every active customer
  session on that table.
- **AC-045** Bill split, merge, and reassign operations MUST preserve
  the one-open-bill-per-table invariant for every table involved.

## Station Board

- **AC-050** A station board MUST display order items whose preparation
  is routed to that station and no others.
- **AC-051** A new `order.submitted` event MUST be reflected on the
  relevant station boards within the SLO defined in
  [`./architecture/slos.md`](./architecture/slos.md) for
  `tenant_event_push_p95_latency_ms`.
- **AC-052** State transitions on a station ticket MUST be idempotent
  under repeated clicks or network retries.

## Device Channel

- **AC-060** The ESP32 device WebSocket MUST authenticate with a
  constant-time comparison against the stored `device_key_hash`.
- **AC-061** Only one WebSocket connection MUST be accepted per table
  at a time. A new accepted connection MUST evict the previous one.
- **AC-062** The server MUST push the current QR payload on handshake
  and on every token rotation, carrying the backend-produced bit
  matrix.
- **AC-063** The firmware MUST NOT generate QR codes locally.

## Auditability

- **AC-070** Every admin mutation on tenant data MUST write to the
  tenant audit log before responding success.
- **AC-071** Every admin mutation on platform data MUST write to the
  platform audit log before responding success.
- **AC-072** Audit log entries MUST NOT contain credentials, QR token
  values, or other secrets.

## Data Residency

- **AC-080** The platform database MUST NOT hold any tenant business
  data (menu items, orders, bills, tenant users, tenant audit).
- **AC-081** A tenant database MUST NOT hold control-plane data
  (platform user rows, cross-tenant registry rows, provisioning jobs).
- **AC-082** Every tenant database migration MUST be applied via
  committed EF Core migrations, not ad-hoc SQL.

## Web Posture

- **AC-090** Customer, staff, and admin HTML surfaces MUST serve
  `X-Robots-Tag: noindex, nofollow, noarchive`.
- **AC-091** `GET /robots.txt` MUST return a fully-disallowed policy
  for every tenant and the platform host.
- **AC-092** A sitemap containing application routes MUST NOT be served.

## Observability

- **AC-100** Every request MUST carry an ASP.NET Core `Activity`
  trace identifier in the response problem-details body
  (`traceId` field) on error responses.
- **AC-101** Every tenant MUST serve `/health`, `/health/live`, and
  `/health/ready`.
- **AC-102** `/health/ready` MUST fail when the tenant database is
  unreachable.

## Accessibility

The baseline is WCAG 2.2 Level AA across every HTML surface. See
[`../explanation/concepts/accessibility.md`](../explanation/concepts/accessibility.md).

- **AC-110** Every interactive control on every HTML surface MUST be
  reachable and operable with keyboard only (Tab, Shift-Tab, arrow
  keys, Enter, Space).
- **AC-111** Body text MUST meet WCAG 2.2 AA contrast (4.5:1 for
  normal text, 3:1 for large text). UI components and graphical
  objects that carry meaning MUST meet 3:1 contrast against their
  adjacent background.
- **AC-112** Colour MUST NOT be the sole carrier of meaning on
  station urgency bands, order-state badges, or error indicators.
- **AC-113** Every form control MUST have a programmatically
  associated label; every page MUST declare `<html lang>` and a
  `<title>` appropriate for the surface.
- **AC-114** Staff touch targets MUST be at least 44 CSS pixels on
  each side (WCAG 2.2 Target Size, Level AA).
- **AC-115** Customer surfaces MUST reflow to a 320 CSS pixel
  viewport without loss of content or function.
- **AC-116** Animations longer than 5 seconds or auto-starting MUST
  respect `prefers-reduced-motion`; the station board MUST NOT
  flash between 2 Hz and 55 Hz.

## Related

- [`./architecture/runtime-surfaces.md`](./architecture/runtime-surfaces.md)
- [`./architecture/slos.md`](./architecture/slos.md)
- [`./api/tenant-api.md`](./api/tenant-api.md)
- [`./api/error-codes.md`](./api/error-codes.md)
- [`../meta/release-gate.md`](../meta/release-gate.md)
