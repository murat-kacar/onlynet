# Common Implementation Patterns

## Dependency Injection Patterns

### Service Lifetime Guidelines

**Singleton Services**
- Use for stateless, thread-safe services
- Examples: WebSocket handlers, EventBus
- Do NOT use DbContext in singleton services (create scoped scope instead)

```csharp
builder.Services.AddSingleton<TableWebSocketHandler>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
```

**Scoped Services**
- Use for services that depend on DbContext
- Examples: Business services (CustomerSessionService, CartService, OrderService)
- Created per HTTP request

```csharp
builder.Services.AddScoped<ICustomerSessionService, CustomerSessionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### DbContext in Singleton Services

When a singleton service needs DbContext, create a scoped scope:

```csharp
public async Task SomeMethodAsync(CancellationToken ct)
{
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    
    // Use context...
    await context.SaveChangesAsync(ct);
}
```

## Entity Usage Patterns

### Factory Methods vs Direct Construction

**Always use static factory methods:**
```csharp
// ✅ Correct
var session = CustomerSession.Open(tableId);
var ticket = session.IssueTicket();

// ❌ Wrong
var session = new CustomerSession { ... };
var ticket = CustomerAccessTicket.Create(sessionId); // Internal method
```

### Entity Method Chaining

Some entity methods return void, others return the entity:

```csharp
// Returns void
session.Close();
ticket.Invalidate();

// Returns related entity
var ticket = session.IssueTicket();
```

### Navigation Properties

Navigation properties are read-only. Use entity methods to modify:

```csharp
// ❌ Wrong - won't work
session.CartItems.Add(item);

// ✅ Correct - entity manages collection
// Add items directly to DbContext
context.CartItems.Add(item);
```

## DbContext Patterns

### FindAsync Pattern

```csharp
// Single entity by primary key
var tenant = await context.Tenants.FindAsync(new object[] { id }, ct);
if (tenant == null)
{
    throw new InvalidOperationException($"Tenant {id} not found");
}
```

### Include Pattern for Navigation

```csharp
var job = await context.ProvisioningJobs
    .Include(j => j.Steps)
    .FirstOrDefaultAsync(j => j.Id == id, ct);
```

### Update Pattern

```csharp
// Modify entity
tenant.SetStatus(TenantStatus.Active);

// Mark as modified and save
context.Tenants.Update(tenant);
await context.SaveChangesAsync(ct);
```

### Join Pattern for Related Data

```csharp
var cartItems = await context.CartItems
    .Join(context.MenuItems, ci => ci.ItemId, mi => mi.Id, (ci, mi) => new { ci, mi })
    .Where(x => x.ci.SessionId == sessionId)
    .Select(x => new CartItemDto(x.ci.Id, x.ci.ItemId, x.mi.Name, x.ci.Quantity, x.mi.Price, x.ci.Note))
    .ToListAsync(ct);
```

## Blazor Patterns

### Component Initialization

```csharp
@code {
    private List<TenantDto>? TenantList;

    protected override async Task OnInitializedAsync()
    {
        TenantList = await Http.GetFromJsonAsync<List<TenantDto>>("/api/tenants");
    }
}
```

### Loading States

```razor
@if (TenantList == null)
{
    <p>Loading...</p>
}
else
{
    <!-- Render data -->
}
```

### Event Handlers

```razor
<button @onclick="HandleClick">Click me</button>

@code {
    private async Task HandleClick()
    {
        await Http.PostAsJsonAsync("/api/action", request);
    }
}
```

### Navigation

```razor
@inject NavigationManager NavigationManager

@code {
    private void NavigateToDetail(Guid id)
    {
        NavigationManager.NavigateTo($"/tenants/{id}");
    }
}
```

## API Controller Patterns

### Controller Structure

Controller actions stay thin: parameter binding, `await`, return.
Application services own the data access. The example below uses
`ITenantRegistryService` rather than `PlatformDbContext` directly per
the AD-0003 trade-off ("the internal layer boundary [host →
application service → domain] must remain explicit in code"). New
controllers follow the service-layer shape.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Platform:Read")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRegistryService _service;

    public TenantsController(ITenantRegistryService service)
    {
        _service = service;
    }

    // Methods...
}
```

### Action Return Types

```csharp
// GET - Return data
[HttpGet]
public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetTenants(CancellationToken ct)
{
    var tenants = await ...;
    return Ok(tenants);
}

// GET by ID - Return 404 if not found
[HttpGet("{id:guid}")]
public async Task<ActionResult<TenantDetailDto>> GetTenant(Guid id, CancellationToken ct)
{
    var tenant = await ...;
    if (tenant == null)
    {
        return NotFound();
    }
    return Ok(tenant);
}

// POST - Return 201 with location
[HttpPost]
public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantRequest request, CancellationToken ct)
{
    var tenant = ...;
    return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, dto);
}

// PUT - Return 204
[HttpPut("{id:guid}")]
public async Task<ActionResult> UpdateTenant(Guid id, UpdateTenantRequest request, CancellationToken ct)
{
    // Update...
    return NoContent();
}

// DELETE - Return 204
[HttpDelete("{id:guid}")]
public async Task<ActionResult> DeleteTenant(Guid id, CancellationToken ct)
{
    // Delete...
    return NoContent();
}
```

## Common Pitfalls

### 1. Property Name Confusion
- `QrToken.Value` not `Token`
- `CustomerAccessTicket.IsValid` not `IsInvalid`
- `CustomerAccessTicket.DeviceCookieValue` (per TD-0017; required at issue time)
- `Station.Name` not `Label`
- `CartItem.ItemId` not `MenuItemId`
- `CustomerSession.IsOpen` not `IsActive`
- `ProvisioningJobStep.StartedAt` not `CreatedAt`
- `Order.IdempotencyKey` (per TD-0018; required at create time)

### 2. Internal vs Public Methods
- Use `CustomerSession.IssueTicket(deviceCookieValue)` not `CustomerAccessTicket.Create()` (per TD-0017 the device-binding cookie is mandatory).
- Use `Order.Create(tableId, sessionId, ticketId, idempotencyKey, items, note)`
  with pre-created items. The 4th positional argument is the idempotency key
  per TD-0018; `OrderService.SubmitAsync` returns the original result for a
  duplicate `(SessionId, IdempotencyKey)`, while the unique index on `orders`
  remains the durable guard against racing inserts.
- Don't call internal factory methods directly

### 3. DbContext in Singletons
- Never inject DbContext into singleton
- Create scoped scope when needed

### 4. Null Warnings
- Initialize nullable fields to avoid compiler warnings
```csharp
private List<TenantDto>? TenantList = [];
private Guid? CurrentSessionId = null;
```

### 5. Navigation Manager in Blazor
- Always inject NavigationManager
- Don't use static NavigationManager property

## Configuration Patterns

### Connection Strings

**Platform:**
```json
{
  "ConnectionStrings": {
    "PlatformDb": "Host=localhost;Database=tabflow_platform;Username=tabflow_platform_app;Password=changeme"
  }
}
```

**Tenant:**
```json
{
  "ConnectionStrings": {
    "TenantDb": "Host=localhost;Database=tabflow_<tenant-code>;Username=tabflow_<tenant-code>_app;Password=changeme_tenant"
  }
}
```

Tenant code is the lowercase `a-z`, `0-9`, hyphen identifier described in
[`/doc/docs/reference/database/schema.md`](/doc/docs/reference/database/schema.md#naming-convention).
For local development, see [`/doc/docs/tutorials/local-development.md`](/doc/docs/tutorials/local-development.md).

### OpenTelemetry Setup

**Platform Host:**
```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TabFlow.Platform"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

**Platform Worker:**
```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TabFlow.PlatformWorker"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation());
```

## Testing Patterns

### Unit Testing Services

Unit tests prefer hand-written fakes over a mocking framework, per
[`./test-taxonomy.md`](./test-taxonomy.md#tier-1-unit). The example
below uses NSubstitute (referenced from every test project today) and
is the **subject of [TD-0025](/doc/buildlog/tech-debt-ledger.md#td-0025)**:
the taxonomy doc and the csproj references diverge, and the resolution
(adopt NSubstitute officially or remove it) is open.

```csharp
// Arrange — hand-written fake (preferred until TD-0025 resolves)
var fakeContext = new InMemoryTenantDbContext();
var service = new CartService(fakeContext);

// Act
var result = await service.AddItemAsync(request, ct);

// Assert
Assert.NotNull(result);
```

### Integration Testing Controllers

```csharp
var factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // Replace services with mocks
        });
    });

var client = factory.CreateClient();
var response = await client.GetAsync("/api/tenants");
```

## Migration Patterns

The migrations project is a class library that references `TabFlow.Shared`
(which owns both `PlatformDbContext` and `TenantDbContext`). To make EF Core's
design-time tooling discover the contexts without booting a host, the project
MUST provide an `IDesignTimeDbContextFactory<T>` for each context. See
[`/doc/docs/how-to/setup-migrations.md`](/doc/docs/how-to/setup-migrations.md)
for the required factory shape and project layout.

### Generating Migrations

```bash
# Platform migrations
dotnet ef migrations add MigrationName \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context PlatformDbContext \
  --output-dir Migrations/Platform

# Tenant migrations
dotnet ef migrations add MigrationName \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context TenantDbContext \
  --output-dir Migrations/Tenant
```

### Applying Migrations

```bash
dotnet ef database update \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context PlatformDbContext

dotnet ef database update \
  --project src/infra/postgres/TabFlow.Migrations.csproj \
  --context TenantDbContext \
  --connection "Host=localhost;Database=tabflow_<tenant-code>;Username=tabflow_<tenant-code>_app;Password=..."
```

The tenant `--connection` override is required because the design-time factory
does not know which tenant database to target; tenant migrations apply once
per tenant database, owned by the platform worker during provisioning.

## Deployment Patterns

### Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__PlatformDb=Host=prod-db;Database=tabflow_platform;...
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PlatformDbContext>()
    .AddDbContextCheck<TenantDbContext>();

app.MapHealthChecks("/health");
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```
