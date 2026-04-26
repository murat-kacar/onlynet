# Internal API Reference

> **Status: stale, owned by [TD-0023](/doc/buildlog/tech-debt-ledger.md#triage-td-0023--internal-apimd-mixes-public-and-staff-tier-surfaces-lists-routes-that-no-longer-ship).**
> The current document mixes customer-tier endpoints with staff-tier
> endpoints, lists `POST /api/orders/submit` (the real route is
> `POST /api/public/orders` per `PublicOrdersController`, PR #6,
> TD-0015 step 3), and omits the staff endpoints that actually ship
> (`/api/orders/{id}`, `/api/orders/session/{sessionId}`,
> `/api/kitchen/*`, `/api/sessions/{sessionId}/close`,
> `/api/tables/*`). Treat the customer-tier sections below as
> superseded by
> [`./tenant-api.md`](./tenant-api.md) and the staff-tier picture as
> incomplete. The shipping route map is in
> [`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md#tenant-host--http-endpoints).
> The rewrite is tracked under **TD-0023**.

This document is the reference for **internal** HTTP endpoints — the
admin and staff API used by the platform admin UI and the tenant admin
console. The public, externally addressable HTTP surface
(`/api/public/*`, `/health/*`, `/ws/*`) lives in
[`./tenant-api.md`](./tenant-api.md).

## Platform API (platform.cafetech.uk)

### Authentication
All endpoints require authentication with cookie auth and appropriate policy:
- `Platform:Read` - Read access
- `Platform:Write` - Write access

### Tenants API

#### GET /api/tenants
**Policy:** `Platform:Read`

**Response:** `IReadOnlyList<TenantDto>`

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

#### GET /api/tenants/{id:guid}
**Policy:** `Platform:Read`

**Response:** `TenantDetailDto`

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

#### POST /api/tenants
**Policy:** `Platform:Write`

**Request:** `CreateTenantRequest`

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

**Response:** `TenantDto` (201 Created)

#### PUT /api/tenants/{id:guid}
**Policy:** `Platform:Write`

**Request:** `UpdateTenantRequest`

```csharp
record UpdateTenantRequest(
    string LanguageCode,
    string CurrencyCode,
    string TimeZone);
```

**Response:** 204 No Content

### Jobs API

#### GET /api/jobs
**Policy:** `Platform:Read`

**Response:** `IReadOnlyList<JobDto>`

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

#### GET /api/jobs/{id:guid}
**Policy:** `Platform:Read`

**Response:** `JobDetailDto`

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
    DateTimeOffset StartedAt); // Note: StartedAt, not CreatedAt
```

## Tenant API (tenant.cafetech.uk)

### Authentication
All endpoints require authentication with cookie auth and appropriate policy:
- `Tenant:Read` - Read access
- `Tenant:Write` - Write access

### Sessions API

#### POST /api/sessions/open
**Policy:** None (public for QR token scanning)

**Request:** `OpenSessionRequest`

```csharp
record OpenSessionRequest(string QrTokenValue);
```

**Response:** `OpenSessionResult`

```csharp
record OpenSessionResult(Guid SessionId, Guid TicketId, string TableLabel);
```

**Notes:**
- Validates QR token against `QrToken.Value`
- Finds active session by table ID
- Issues access ticket using `CustomerSession.IssueTicket()`

#### GET /api/sessions/{ticketId:guid}
**Policy:** None

**Response:** `CustomerSessionState?` (404 if not found)

```csharp
record CustomerSessionState(
    Guid SessionId,
    Guid TicketId,
    string TableLabel,
    IReadOnlyList<CartItemSummary> CartItems);

record CartItemSummary(
    Guid ItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    string? Note);
```

#### POST /api/sessions/{sessionId:guid}/close
**Policy:** None

**Response:** 204 No Content

### Cart API

#### POST /api/cart
**Policy:** None

**Request:** `AddCartItemRequest`

```csharp
record AddCartItemRequest(
    Guid SessionId,
    Guid MenuItemId,
    int Quantity,
    string? Note = null);
```

**Response:** `CartItemDto`

```csharp
record CartItemDto(
    Guid Id,
    Guid MenuItemId,
    string MenuItemName,
    int Quantity,
    decimal UnitPrice,
    string? Note);
```

#### DELETE /api/cart/{id:guid}
**Policy:** None

**Response:** 204 No Content

#### PUT /api/cart/{id:guid}/quantity
**Policy:** None

**Request:** `UpdateQuantityRequest`

```csharp
record UpdateQuantityRequest(int Quantity);
```

**Response:** 204 No Content

#### GET /api/cart/session/{sessionId:guid}
**Policy:** None

**Response:** `IReadOnlyList<CartItemDto>`

### Orders API

#### POST /api/orders/submit
**Policy:** None

**Request:** `SubmitOrderRequest`

```csharp
record SubmitOrderRequest(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string CheckoutProofToken,
    string IdempotencyKey,
    string? Note);
```

**Response:** `SubmitOrderResult`

```csharp
record SubmitOrderResult(Guid OrderId, decimal TotalAmount);
```

**Notes:**
- Validates checkout proof token against `QrToken.Value` and `IsCheckoutProof`
- Validates session is open
- Creates order with items from cart
- Closes session after submission

## WebSocket Endpoints

### Tenant WebSocket

#### WS /ws/tables/{tableNumber:int}
**Purpose:** ESP32 device connection for table

**Protocol:** WebSocket

**Implementation:** `TableWebSocketHandler`

**Notes:**
- Accepts WebSocket connections from ESP32 devices
- Handles incoming messages from devices
- Long-lived connection (timeout: 86400s)
- Currently has placeholder message processing

## Common Patterns

### Blazor HttpClient Usage
```csharp
@inject HttpClient Http

// GET
var items = await Http.GetFromJsonAsync<List<TenantDto>>("/api/tenants");

// POST
var result = await Http.PostAsJsonAsync("/api/cart", request);

// DELETE
await Http.DeleteAsync($"/api/cart/{id}");

// PUT
await Http.PutAsJsonAsync($"/api/cart/{id}/quantity", request);
```

### Authorization in Controllers
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Platform:Read")] // or Platform:Write, Tenant:Read, Tenant:Write
public class TenantsController : ControllerBase
{
    // ...
}
```

### Authorization in Blazor
```razor
@page "/tenants"
@attribute [Authorize(Policy = "Platform:Read")]
```

### Error Handling
Controllers return standard HTTP status codes:
- 200 OK - Success with response body
- 201 Created - Resource created
- 204 No Content - Success without response body
- 400 Bad Request - Invalid input
- 404 Not Found - Resource not found
- 401 Unauthorized - Not authenticated
- 403 Forbidden - Authorized but insufficient permissions
