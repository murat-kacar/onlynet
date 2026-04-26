# Tenant API Reference

The tenant API is the externally visible HTTP surface of a tenant host.

The tenant host runs a Blazor Web App. Most server work happens through
Razor components and dependency-injected application services, not through
an internal HTTP layer. This document covers only the HTTP endpoints in
the external contract: the health probes, the customer-facing public
endpoints, and the ESP32 device WebSocket.

Administrative and staff surfaces do not appear in this reference. They
interact with the domain directly through Blazor components. See
[`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md).

## Base

Each tenant host serves on its own domain. Example:

```text
https://<tenant-domain>/
```

All listed endpoints are relative to that host.

## Health

```http
GET /health
GET /health/live
GET /health/ready
```

- `/health` returns service metadata (host role, build version, commit
  SHA, and tenant code when applicable). It is the catch-all endpoint
  used by humans and service-discovery tooling; it has no
  dependency checks and no readiness semantics.
- `/health/live` returns liveness (the process is running and able to
  handle HTTP). No external dependency check. Used by container and
  systemd probes to decide whether to restart the process.
- `/health/ready` additionally checks tenant database readiness. A
  `503 Service Unavailable` response means the process is live but
  does not yet have a usable database connection. Used by load
  balancers to decide whether to send traffic.

Responses are plain JSON. Probes are unauthenticated.

## Public Endpoints

These endpoints serve the customer-facing flow on top of the Static SSR
menu surface. They are HTTP endpoints because HTTP is the natural
contract for the device-agnostic customer browser.

QR-token join does not appear here. The join flow is owned by the
Static SSR page at `/g/{token}` (route T-03 in
[`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md)):
the page GET validates the token, opens the table session, issues the
access cookie, and redirects to `/menu` in one round trip. There is no
separate HTTP endpoint that duplicates that work.

### Tenant Profile

```http
GET /api/public/profile
```

Returns:

- `code`
- `displayName`
- `primaryDomain`
- `languageCode`
- `currencyCode`
- `timeZone`

Anonymous. Safe to cache per tenant host.

> **Migration status (TD-0021).** The `/api/public/profile` route is
> not yet implemented; a per-tenant profile surface ships only when
> a real consumer requires it. Today the customer-facing Blazor
> components read tenant profile data through the in-process
> services rather than through HTTP.

### Public Catalog

```http
GET /api/public/catalog
```

Returns:

- Tenant summary
- Active categories
- Available menu items

Anonymous. The payload is scoped to customer-relevant fields only.
Internal routing and pricing-construction fields stay server-side.

> **Migration status (TD-0021, PR #30).** The customer-tier catalog
> surface now ships at `/api/public/catalog` and
> `/api/public/catalog/category/{categoryId}` per
> [`PublicCatalogController`](/src/apps/tenant/Controllers/Api/PublicCatalogController.cs);
> `Menu.razor` calls the new prefix. The legacy `/api/menu` and
> `/api/menu/category/{categoryId}` routes on
> [`MenuController`](/src/apps/tenant/Controllers/Api/MenuController.cs)
> stay operational during the deprecation window TD-0021 step 3
> declares (one minor release per AD-0011) and are removed in a
> follow-up PR that returns HTTP 410 from the legacy routes.

### Customer Session

```http
GET /api/public/session
```

Returns the current session state for the cookie-bearing browser,
including the active table label and the current cart summary.

A customer-initiated logout endpoint is intentionally not exposed.
Access tickets become invalid automatically when the parent table
session closes; a browser-side logout would not add operational value.

> **Migration status (TD-0021, PR #30).** The customer-tier session
> surface now ships at `/api/public/session/open` (POST, opens a
> customer session from a fresh QR token) and
> `/api/public/session/{ticketId}` (GET, returns the session state
> for the cookie-bearing browser) per
> [`PublicSessionController`](/src/apps/tenant/Controllers/Api/PublicSessionController.cs);
> `ScanQr.razor` calls the new prefix. The legacy
> `/api/sessions/open` and `/api/sessions/{ticketId}` routes on
> [`SessionsController`](/src/apps/tenant/Controllers/Api/SessionsController.cs)
> stay operational during the deprecation window TD-0021 step 3
> declares; the staff-tier `POST /api/sessions/{sessionId}/close`
> action is unaffected and stays under `Tenant:Write`.

### Customer Order Submission

```http
POST /api/public/orders
```

Body includes the order items assembled from the server-side cart, a
fresh QR checkout-proof token produced by a second scan of the current
table QR at submit time, and a per-submission idempotency key.

The body's `idempotencyKey` field MUST carry a unique token (UUIDv4
recommended) generated by the client for this submission attempt.
The key is enforced server-side by a unique index over
`(SessionId, IdempotencyKey)` on the `orders` table per
[TD-0018](/doc/buildlog/tech-debt-ledger.md#td-0018);
a duplicate submit attempt within the session inserts onto the unique
constraint, the service catches the conflict, and the original
result is returned. The key is read from the request body, **not from
an `Idempotency-Key` HTTP header** — the body field shape is the
shipping contract per
[`PublicOrdersController.SubmitOrder`](/src/apps/tenant/Controllers/Api/PublicOrdersController.cs).

Behavior:

- Verifies that the access ticket belongs to the still-open table
  session and that the device-binding cookie matches the ticket's
  `DeviceCookieValue` per
  [TD-0017](/doc/buildlog/tech-debt-ledger.md#td-0017).
- Verifies and consumes the fresh QR checkout proof from the request
  body.
- Checks the body's `idempotencyKey` against the
  `(SessionId, IdempotencyKey)` unique index; returns the prior
  result if the key is a replay.
- Atomically converts the cart into an order and order items.
- Publishes `order.submitted` on the in-process event bus so the floor
  and cash workspace and the relevant station boards react immediately.

Checkout-proof verification is inlined into this endpoint rather than
split into a separate `verify` call. Submission is the only place a
checkout proof is meaningful, so colocating the check keeps the protocol
single-round-trip and avoids duplicated verify plumbing.

## Device WebSocket

```text
wss://<tenant-domain>/ws/tables/{tableNumber}?deviceKey={deviceKey}
```

Authentication:

- `{deviceKey}` is compared with the stored `device_key_hash` for the
  table using a constant-time comparison.
- Only one device connection is accepted per table.

Message flow:

- Server sends `auth_ok` on successful handshake.
- Server sends `new_token` payloads when the current QR token rotates.
- Server sends `refresh` when operational state invalidates the current
  QR.
- Client sends `ping`; server replies with `pong`.

Token payload fields:

- `url`
- `ttl_seconds`
- `expires_at`
- `qr_side`
- `qr_bits_hex`

The payload is backend-produced. Firmware does not generate QR codes
locally. Details live in [`../firmware.md`](../firmware.md).

## Error Model

All error responses use [RFC 7807 Problem Details for HTTP APIs][rfc7807]
with the `application/problem+json` media type. TabFlow extends the
baseline Problem Details object with two additional members that every
error response MUST carry:

- `code` — a short, machine-stable identifier drawn from the enumerated
  vocabulary in [`./error-codes.md`](./error-codes.md).
- `traceId` — the current trace identifier (ASP.NET Core
  `Activity.TraceId`) for correlating server logs.

[rfc7807]: https://www.rfc-editor.org/rfc/rfc7807

Example error body:

```json
{
  "type": "https://example.com/tabflow/errors/token-used",
  "title": "QR token already consumed",
  "status": 409,
  "detail": "The submitted QR token has already opened another session.",
  "code": "token_used",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

The `type` URI uses the IANA-reserved `example.com` domain per
[RFC 2606](https://www.rfc-editor.org/rfc/rfc2606). Deployments
MAY substitute a real tenant- or vendor-owned domain; `type` is a
stable identifier, not a resolvable link.

Status code policy:

- `400 Bad Request` — request schema invalid or business precondition
  failed at input validation.
- `401 Unauthorized` — the caller has no valid session or access
  ticket.
- `403 Forbidden` — the caller is authenticated but the requested
  resource is outside their role scope.
- `404 Not Found` — the addressed resource does not exist in the
  tenant scope.
- `409 Conflict` — the request targets a state that has already
  progressed (consumed token, reassigned bill, closed session).
- `410 Gone` — the addressed resource existed and is now retired
  (expired token, closed bill that was also archived).
- `429 Too Many Requests` — the caller exceeded the rate budget for
  the endpoint.
- `5xx` — infrastructure failure; the response MUST still carry the
  Problem Details shape and a generic `code`.

The `code` vocabulary is the normative contract for error handling.
Clients key their behaviour off `code`, never off `title`. See
[`./error-codes.md`](./error-codes.md).

Per-endpoint error codes:

- `POST /api/public/orders`:
  `invalid_request`, `session_expired`, `checkout_proof_missing`,
  `checkout_proof_invalid`, `checkout_proof_expired`, `cart_empty`,
  `catalog_stale`, `order_duplicate`, `rate_limited`.
- `GET /api/public/session`: `session_expired`.
- `GET /api/public/catalog`: `rate_limited` on abusive probes.
- `GET /api/public/profile`: `rate_limited` on abusive probes.
- `GET /ws/tables/{tableNumber}`: handshake failure surfaces a close
  code with `code` in the close reason; see the WebSocket family in
  [`./error-codes.md`](./error-codes.md).

Provisioning or runtime infrastructure problems surface through
`/health` and provisioning job state, not through customer-facing
endpoints.

## Absent Surfaces

The following endpoint families are not part of the external tenant API:

- `/api/admin/**` is not exposed. Admin and staff surfaces interact with
  the domain through Blazor components, not through an internal HTTP
  layer.
- `/api/public/token/verify` is not a separate endpoint. Token join runs
  inside the `/g/{token}` Static SSR page; checkout proof is inlined into
  `POST /api/public/orders`.
- `/api/public/session/logout` is not exposed; see
  [Customer Session](#customer-session) for the rationale.
- Tenant-side audit log export is not an external endpoint today. Audit
  review runs inside the tenant admin console.

## Versioning

Public customer endpoints stay unversioned on the current major.
Additive, non-breaking changes are allowed. A breaking change introduces
a new major surface in parallel, for example `/api/v2/public/**`. See
[`./README.md`](./README.md).

## Related

- [`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md)
- [`../architecture/system-overview.md`](../architecture/system-overview.md)
- [`../firmware.md`](../firmware.md)
- [`../../explanation/concepts/customer-session-model.md`](../../explanation/concepts/customer-session-model.md)
