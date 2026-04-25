# Entity Layer Reference

## Platform Entities

### TenantRegistration
**Properties:**
- `Id` (Guid)
- `Code` (string, max 63, unique)
- `DisplayName` (string, max 200)
- `Status` (TenantStatus enum)
- `PrimaryDomain` (string, max 253, unique)
- `LanguageCode` (string, max 10)
- `CurrencyCode` (string, max 3)
- `TimeZone` (string, max 100)
- `IntendedOwnerEmail` (string, max 320)
- `DatabaseName` (string?, max 63)
- `DatabaseUser` (string?, max 63)
- `DatabasePassword` (string?, max 100)
- `CreatedAt` (DateTimeOffset)
- `UpdatedAt` (DateTimeOffset)

**Methods:**
- `Create(code, displayName, primaryDomain, languageCode, currencyCode, timeZone, intendedOwnerEmail)` → TenantRegistration
- `SetStatus(status)` → void
- `UpdateRegionalSettings(languageCode, currencyCode, timeZone)` → void
- `SetDatabaseConnection(dbName, dbUser, dbPassword)` → void

### ProvisioningJob
**Properties:**
- `Id` (Guid)
- `TenantId` (Guid)
- `Type` (string, max 50)
- `Status` (ProvisioningJobStatus enum)
- `AttemptCount` (int)
- `ClaimedBy` (string?, max 100)
- `Payload` (string?, jsonb)
- `Result` (string?, jsonb)
- `FailureDetail` (string?)
- `CreatedAt` (DateTimeOffset)
- `UpdatedAt` (DateTimeOffset)
- `Steps` (IReadOnlyList<ProvisioningJobStep>)

**Methods:**
- `CreateTenantCreate(tenantId, payload)` → ProvisioningJob
- `Claim(workerId)` → void (sets Status=Claimed, ClaimedBy, increments AttemptCount)
- `MarkRunning()` → void
- `MarkSucceeded(result)` → void
- `MarkFailed(detail)` → void
- `AddStep(name)` → ProvisioningJobStep

### ProvisioningJobStep
**Properties:**
- `Id` (Guid)
- `JobId` (Guid)
- `Name` (string, max 100)
- `Status` (string, max 20)
- `Detail` (string?)
- `StartedAt` (DateTimeOffset) — **Not CreatedAt**
- `CompletedAt` (DateTimeOffset?)

**Methods:**
- `Create(jobId, name)` → ProvisioningJobStep (internal)
- `Complete(detail)` → void
- `Fail(detail)` → void

### PlatformAuditEntry
**Properties:**
- `Id` (Guid)
- `ActorEmail` (string, max 320)
- `Action` (string, max 100)
- `ResourceType` (string, max 100)
- `ResourceId` (string, max 200)
- `Changes` (string?, jsonb)
- `Ip` (string?, max 45)
- `UserAgent` (string?, max 500)
- `CreatedAt` (DateTimeOffset)

## Tenant Entities

### CustomerSession
**Properties:**
- `Id` (Guid)
- `TableId` (Guid)
- `IsOpen` (bool) — **Not IsActive**
- `OpenedAt` (DateTimeOffset)
- `ClosedAt` (DateTimeOffset?)
- `AccessTickets` (IReadOnlyList<CustomerAccessTicket>)
- `CartItems` (IReadOnlyList<CartItem>)

**Methods:**
- `Open(tableId)` → CustomerSession
- `IssueTicket()` → CustomerAccessTicket (use this instead of CustomerAccessTicket.Create)
- `Close()` → void (sets IsOpen=false, ClosedAt, invalidates all tickets)

### QrToken
**Properties:**
- `Id` (Guid)
- `Value` (string) — **Not Token**
- `TableId` (Guid)
- `IsCheckoutProof` (bool)
- `IsConsumed` (bool)
- `ExpiresAt` (DateTimeOffset)
- `CreatedAt` (DateTimeOffset)
- `IsExpired` (bool, computed)

**Methods:**
- `CreateJoinToken(tableId, value, expiresAt)` → QrToken
- `CreateCheckoutProof(tableId, value, expiresAt)` → QrToken
- `Consume()` → void

### CustomerAccessTicket
**Properties:**
- `Id` (Guid)
- `SessionId` (Guid)
- `IsValid` (bool) — **Not IsInvalid**
- `CreatedAt` (DateTimeOffset)

**Methods:**
- `Create(sessionId)` → CustomerAccessTicket (internal, use CustomerSession.IssueTicket instead)
- `Invalidate()` → void (sets IsValid=false)

### Station
**Properties:**
- `Id` (Guid)
- `Name` (string) — **Not Label**
- `Code` (string)
- `Color` (string)
- `Type` (string)
- `IsActive` (bool)
- `IsFallback` (bool)
- `SortOrder` (int)

**Methods:**
- `Create(name, code, color, type, sortOrder)` → Station
- `Update(name, color, type, isActive, sortOrder)` → void
- `SetFallback(isFallback)` → void

### CartItem
**Properties:**
- `Id` (Guid)
- `SessionId` (Guid)
- `ItemId` (Guid) — **Not MenuItemId**
- `Quantity` (int)
- `Note` (string?)

**Methods:**
- `Create(sessionId, itemId, quantity, note)` → CartItem
- `UpdateQuantity(quantity)` → void
- `UpdateNote(note)` → void

### MenuItem
**Properties:**
- `Id` (Guid)
- `Name` (string)
- `Price` (decimal)
- `Description` (string?)
- `CategoryId` (Guid)
- `IsActive` (bool)

### Order
**Properties:**
- `Id` (Guid)
- `TableId` (Guid)
- `SessionId` (Guid)
- `TicketId` (Guid)
- `BillId` (Guid?)
- `TotalAmount` (decimal)
- `SubmittedAt` (DateTimeOffset)
- `Note` (string?)
- `Items` (IReadOnlyList<OrderItem>)

**Methods:**
- `Create(tableId, sessionId, ticketId, items, note)` → Order (items must be pre-created)
- `AssignToBill(billId)` → void

### OrderItem
**Properties:**
- `Id` (Guid)
- `OrderId` (Guid)
- `ItemId` (Guid)
- `ItemName` (string)
- `Quantity` (int)
- `UnitPrice` (decimal)
- `Note` (string?)
- `StationId` (Guid)
- `Status` (OrderItemStatus enum)
- `PreparingAt` (DateTimeOffset?)
- `ReadyAt` (DateTimeOffset?)
- `ServedAt` (DateTimeOffset?)
- `CancelledAt` (DateTimeOffset?)

**Methods:**
- `Create(itemId, itemName, quantity, unitPrice, stationId, note)` → OrderItem
- `SetOrderId(orderId)` → void (internal)
- `StartPreparing()` → void
- `MarkReady()` → void
- `MarkServed()` → void
- `Cancel()` → void

## Important Notes

1. **Internal vs Public Methods**: Some factory methods (like `CustomerAccessTicket.Create`, `OrderItem.SetOrderId`) are internal. Use the parent entity's methods instead (e.g., `CustomerSession.IssueTicket`, `Order.Create` with items).

2. **Property Naming Conventions**:
   - `Value` not `Token` for QrToken
   - `IsValid` not `IsInvalid` for CustomerAccessTicket
   - `Name` not `Label` for Station
   - `ItemId` not `MenuItemId` for CartItem
   - `IsOpen` not `IsActive` for CustomerSession
   - `StartedAt` not `CreatedAt` for ProvisioningJobStep

3. **Navigation Properties**: Navigation properties (like `Steps`, `Items`) are read-only collections. Use the entity's methods to modify them.
