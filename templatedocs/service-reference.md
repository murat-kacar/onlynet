# Service Interface Reference

## Platform Services

### IPlatformAuditService
**Location:** `TabFlow.Platform.Services.PlatformAuditService`

**Methods:**
- `LogAsync(actorEmail, action, resourceType, resourceId, changes, ip, userAgent)` → Task

**Usage:**
```csharp
await _auditService.LogAsync(
    user.Email,
    "TenantCreated",
    "Tenant",
    tenant.Id.ToString(),
    changes,
    HttpContext.Connection.RemoteIpAddress?.ToString(),
    Request.Headers["User-Agent"]);
```

## Tenant Services

### ICustomerSessionService
**Location:** `TabFlow.Tenant.Services.CustomerSessionService`

**Methods:**
- `OpenSessionAsync(qrTokenValue)` → Task<OpenSessionResult>
- `GetSessionStateAsync(ticketId)` → Task<CustomerSessionState?>
- `CloseSessionAsync(sessionId)` → Task

**DTOs:**
```csharp
record OpenSessionResult(Guid SessionId, Guid TicketId, string TableLabel);
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

**Important Notes:**
- `OpenSessionAsync` validates QR token against `QrToken.Value` (not `Token`)
- Finds active session by `TableId` matching QR token's `TableId`
- Uses `CustomerSession.IssueTicket()` (not `CustomerAccessTicket.Create`)
- Checks `CustomerSession.IsOpen` (not `IsActive`)
- `GetSessionStateAsync` returns null if ticket is invalid or session not found
- Joins `CartItem` with `MenuItem` to get `MenuItemName` and `UnitPrice`

### ICartService
**Location:** `TabFlow.Tenant.Services.CartService`

**Methods:**
- `AddItemAsync(request)` → Task<CartItemDto>
- `RemoveItemAsync(cartItemId)` → Task
- `UpdateItemQuantityAsync(cartItemId, quantity)` → Task
- `GetCartItemsAsync(sessionId)` → Task<IReadOnlyList<CartItemDto>>

**DTOs:**
```csharp
record AddCartItemRequest(
    Guid SessionId,
    Guid MenuItemId,
    int Quantity,
    string? Note = null);
record CartItemDto(
    Guid Id,
    Guid MenuItemId,
    string MenuItemName,
    int Quantity,
    decimal UnitPrice,
    string? Note);
```

**Important Notes:**
- `CartItem.ItemId` maps to `MenuItemId` in DTO
- Joins with `MenuItem` to get name and price
- `UpdateQuantity` calls `CartItem.UpdateQuantity()`

### IOrderService
**Location:** `TabFlow.Tenant.Services.OrderService`

**Methods:**
- `SubmitAsync(request)` → Task<SubmitOrderResult>

**DTOs:**
```csharp
record SubmitOrderRequest(
    Guid SessionId,
    Guid TicketId,
    Guid TableId,
    string CheckoutProofToken,
    string IdempotencyKey,
    string? Note);
record SubmitOrderResult(Guid OrderId, decimal TotalAmount);
```

**Important Notes:**
- Validates checkout proof token against `QrToken.Value` and `IsCheckoutProof`
- Validates session is open (`CustomerSession.IsOpen`)
- Creates `OrderItem` with `StationId` parameter (required)
- Uses `Order.Create(tableId, sessionId, ticketId, items, note)` signature
- Closes session after order submission
- Joins `CartItem` with `MenuItem` to get order item details

## EventBus

### IEventBus
**Location:** `TabFlow.Shared.Application.EventBus`

**Methods:**
- `PublishAsync(event)` → Task
- `SubscribeAsync(handler)` → Task

**Implementation:** `InProcessEventBus` using System.Threading.Channels

**Usage:**
```csharp
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
```

## Important Implementation Patterns

### 1. Service Registration
```csharp
// Singleton services (stateless, thread-safe)
builder.Services.AddSingleton<TableWebSocketHandler>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();

// Scoped services (per request, DbContext dependent)
builder.Services.AddScoped<ICustomerSessionService, CustomerSessionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### 2. DbContext Usage in Services
```csharp
public async Task SomeMethodAsync(CancellationToken ct)
{
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    
    // Use context...
    await context.SaveChangesAsync(ct);
}
```

### 3. Entity Method Calls
- Use parent entity methods, not internal factory methods
- Example: Use `CustomerSession.IssueTicket()` not `CustomerAccessTicket.Create()`
- Example: Use `Order.Create(tableId, sessionId, ticketId, items, note)` with pre-created items

### 4. Property Name Conventions
- `Value` not `Token` for QrToken
- `IsValid` not `IsInvalid` for CustomerAccessTicket
- `Name` not `Label` for Station
- `ItemId` not `MenuItemId` for CartItem
- `IsOpen` not `IsActive` for CustomerSession
- `StartedAt` not `CreatedAt` for ProvisioningJobStep
