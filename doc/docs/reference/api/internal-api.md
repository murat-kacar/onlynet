# Internal API Reference

This document is the reference for the **internal**, staff-tier
HTTP surface used by the platform admin console and the tenant
staff console. The publicly addressable HTTP surface
(`/api/public/*`, `/health/*`, `/ws/*`) lives in
[`./tenant-api.md`](./tenant-api.md). The shipping route map is
in
[`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md#tenant-host--http-endpoints).

The error model is the RFC 7807 Problem Details shape documented in
[`./error-codes.md`](./error-codes.md); per-code semantics are
specified there.

## Conventions

- **Authentication.** Every internal endpoint authenticates via the
  ASP.NET Core Identity cookie issued by `/login`. API callers that
  forget the cookie receive `401 Unauthorized`; callers that lack
  the action's policy receive `403 Forbidden`. The cookie handler
  short-circuits the default 302 redirect for any path under
  `/api/`, so HTTP API clients see status codes, not HTML
  redirects.
- **Authorisation.** Each action carries a policy on the
  controller class (default-restrictive ordering): the controller
  is `[Authorize(Policy = "...")]` and individual actions either
  inherit that policy or escalate via an explicit
  `[Authorize(Policy = "...:Write")]`. The four policies are
  `Platform:Read`, `Platform:Write`, `Tenant:Read`, and
  `Tenant:Write`; their definitions live in each host's
  `Program.cs` `AddAuthorization(...)` block.
- **Status codes.**
  - `200 OK` — success with a response body.
  - `201 Created` — resource created; `Location` header points at
    the read endpoint.
  - `204 No Content` — success without a response body.
  - `400 Bad Request` — validation failure, RFC 7807 body.
  - `401 Unauthorized` — no valid Identity cookie.
  - `403 Forbidden` — authenticated but the policy gate failed.
  - `404 Not Found` — resource missing or invisible to this
    caller.
- **Content type.** JSON request and response bodies are
  `application/json`. Problem Details bodies are
  `application/problem+json`.
- **Internal vs public.** None of the routes below ship under the
  `/api/public/*` prefix; that prefix is reserved for the
  customer-tier surface in `tenant-api.md` (PR #6, TD-0015 step 3
  for the orders shim; TD-0021 plans the rest of the migration).

## Platform Host (`platform.cafetech.uk`)

Two controllers, both under
`src/apps/platform/Controllers/Api/`.

### Tenants — `[Route("api/[controller]")]`, default `[Authorize(Policy = "Platform:Read")]`

Source:
[`/src/apps/platform/Controllers/Api/TenantsController.cs`](/src/apps/platform/Controllers/Api/TenantsController.cs).
Tracked under TD-0022 step 1 for the move to a service-layer
caller that owns the EF Core context; the route table below is
stable across that refactor.

#### `GET /api/tenants`

Lists every tenant the caller can read.

- **Policy.** `Platform:Read`.
- **Response.** `200 OK`, body
  `IReadOnlyList<TenantDto>`.

```csharp
record TenantDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);
```

#### `GET /api/tenants/{id:guid}`

Reads a single tenant.

- **Policy.** `Platform:Read`.
- **Responses.** `200 OK` with `TenantDetailDto`, or `404` if
  no tenant has the supplied id.

```csharp
record TenantDetailDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone,
    string IntendedOwnerEmail,
    string? DatabaseName,
    string? DatabaseUser);
```

#### `POST /api/tenants`

Creates a new tenant.

- **Policy.** `Platform:Write`.
- **Request.** `CreateTenantRequest`.
- **Responses.** `201 Created` with `TenantDto` and a
  `Location` header pointing at `GET /api/tenants/{id}`.

```csharp
record CreateTenantRequest(
    string Code,
    string DisplayName,
    string PrimaryDomain,
    string LanguageCode,
    string CurrencyCode,
    string TimeZone,
    string IntendedOwnerEmail);
```

#### `PUT /api/tenants/{id:guid}`

Updates the locale-shaped fields on a tenant. The
`Code`, `DisplayName`, and `IntendedOwnerEmail` are immutable from
this surface and require a separate admin escalation that does
not yet ship.

- **Policy.** `Platform:Write`.
- **Request.** `UpdateTenantRequest`.
- **Response.** `204 No Content`.

```csharp
record UpdateTenantRequest(
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);
```

### Jobs — `[Route("api/[controller]")]`, default `[Authorize(Policy = "Platform:Read")]`

Source:
[`/src/apps/platform/Controllers/Api/JobsController.cs`](/src/apps/platform/Controllers/Api/JobsController.cs).
This controller is read-only today; the create path lands as
part of the platform worker bring-up (`tenant.create` jobs are
written by `TenantsController.POST`, not by direct caller).

#### `GET /api/jobs`

Lists every job. The list is unsorted; the admin console sorts
client-side. Pagination is not yet wired; the platform job table
is small (one row per provisioning attempt) so the unsorted
`IReadOnlyList` is sufficient until volume forces a sort key.

- **Policy.** `Platform:Read`.
- **Response.** `200 OK`, body `IReadOnlyList<JobDto>`.

```csharp
record JobDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string Status,
    string? ClaimedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

#### `GET /api/jobs/{id:guid}`

Reads a single job and the step list that records its progress.

- **Policy.** `Platform:Read`.
- **Responses.** `200 OK` with `JobDetailDto`, or `404` if
  no job has the supplied id.

```csharp
record JobDetailDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string Status,
    string? ClaimedBy,
    string? Payload,
    string? Result,
    string? FailureDetail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<JobStepDto> Steps);

record JobStepDto(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset StartedAt);
```

## Tenant Host (`*.cafetech.uk` / per-tenant subdomain)

Five staff-tier controllers under
`src/apps/tenant/Controllers/Api/`. Customer-tier
(`PublicOrdersController`, customer-anonymous actions on
`SessionsController` and `CartController`, anonymous reads on
`MenuController`) is documented in
[`./tenant-api.md`](./tenant-api.md) and is **not** repeated here.

### Orders — `[Route("api/[controller]")]`, default `[Authorize(Policy = "Tenant:Read")]`

Source:
[`/src/apps/tenant/Controllers/Api/OrdersController.cs`](/src/apps/tenant/Controllers/Api/OrdersController.cs).
The customer-facing submit action is **not** here; it lives at
`POST /api/public/orders` on `PublicOrdersController` (PR #6,
TD-0015 step 3). This controller carries the staff read paths
only.

#### `GET /api/orders/{id:guid}`

Reads a single order plus its line items.

- **Policy.** `Tenant:Read`.
- **Responses.** `200 OK` with `OrderDetailDto`, or `404` if
  no order has the supplied id.

The DTO shape follows `Order` and `OrderItem`; consumers should
prefer the `[generated]` DTOs in
`src/apps/tenant/Models/Api/Orders.cs` rather than
hand-rolled equivalents.

#### `GET /api/orders/session/{sessionId:guid}`

Lists every order placed under a customer session — used by the
staff console's per-table history.

- **Policy.** `Tenant:Read`.
- **Response.** `200 OK`, body `IReadOnlyList<OrderSummaryDto>`.

### Kitchen — `[Route("api/[controller]")]`, no class-level policy

Source:
[`/src/apps/tenant/Controllers/Api/KitchenController.cs`](/src/apps/tenant/Controllers/Api/KitchenController.cs).
Each action carries its own policy (`Tenant:Read` for the read
path, `Tenant:Write` for the status-change path) so the route
table cannot accidentally inherit the wrong default.

#### `GET /api/kitchen/orders`

Lists every order that the station displays should currently see —
typically `Submitted` and `Preparing`, ordered by submission
time.

- **Policy.** `Tenant:Read`.
- **Response.** `200 OK`, body `IReadOnlyList<KitchenOrderDto>`.

#### `PUT /api/kitchen/items/{itemId:guid}/status`

Advances an order item along the station state machine
(`Submitted` → `Preparing` → `Ready` → `Served`).

- **Policy.** `Tenant:Write`.
- **Request.** `UpdateItemStatusRequest`.
- **Response.** `204 No Content` on success; `400` (RFC 7807)
  with `code = "order_item_state_invalid"` if the requested
  transition is not allowed from the item's current state.

```csharp
record UpdateItemStatusRequest(string Status);
```

### Tables — `[Route("api/[controller]")]`, default `[Authorize(Policy = "Tenant:Read")]`

Source:
[`/src/apps/tenant/Controllers/Api/TablesController.cs`](/src/apps/tenant/Controllers/Api/TablesController.cs).
Read-only; table writes (rename, status flip, remove) ship later
as part of the staff console catalog work.

#### `GET /api/tables`

Lists every table with its current customer-session attachment.

- **Policy.** `Tenant:Read`.
- **Response.** `200 OK`, body `IReadOnlyList<TableDto>`.

#### `GET /api/tables/{id:guid}`

Reads a single table.

- **Policy.** `Tenant:Read`.
- **Responses.** `200 OK` with `TableDetailDto`, or `404` if
  no table has the supplied id.

### Sessions — staff-tier slice

Source:
[`/src/apps/tenant/Controllers/Api/SessionsController.cs`](/src/apps/tenant/Controllers/Api/SessionsController.cs).
This controller hosts both customer-tier and staff-tier actions on
the same route. The customer-tier actions are documented in
[`./tenant-api.md`](./tenant-api.md) (and are slated to move to a
`/api/public/*` shim under TD-0021 step 2). The staff-tier slice
is the close action below.

#### `POST /api/sessions/{sessionId:guid}/close`

Closes a customer session that the staff console wants to mark as
done — typically after the bill is settled.

- **Policy.** `Tenant:Write`.
- **Response.** `204 No Content` on success; `404` if the
  session id does not exist or the session is already closed.

The class-level `[Authorize(Policy = "Tenant:Read")]` plus the
action-level `[Authorize(Policy = "Tenant:Write")]` rely on the
default-restrictive ordering in `Program.cs`'s
`AddAuthorization(...)` (per AC-043). The action policy escalates
the requirement; without that ordering, the analyser warning
ASP0026 surfaces and the build breaks.

## Migration Notes

- **TD-0021 (open).** Four customer-tier surfaces still ship from
  staff-tier controllers under `/api/menu`, `/api/cart`,
  `/api/sessions/open`, and `/api/sessions/{ticketId}`. Per
  AD-0003, the public surface belongs under `/api/public/*`. The
  payoff plan in
  [TD-0021](/doc/buildlog/tech-debt-ledger.md#td-0021) introduces shim
  controllers at `/api/public/*` that delegate into the existing
  service layer; once the shims ship, this document and
  `tenant-api.md` are the only references that change.
- **TD-0022 (open).** Six controllers
  (`TenantsController`, `JobsController`, `KitchenController`,
  `OrdersController`, `TablesController`, `SessionsController`)
  still hold an EF Core `DbContext` directly. The payoff plan
  introduces a thin service per controller that owns the
  context; the route table here is stable across that refactor.
- **TD-0015 step 6 (open).** The class-level vs action-level
  policy ordering on `SessionsController` and (post-TD-0021) on
  the customer-tier shims is the point that an integration test
  must exercise: `401` on a missing cookie, `403` on a present
  cookie that lacks the policy. The fixture for that test depends
  on TD-0010 step 5.
