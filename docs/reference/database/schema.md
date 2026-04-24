# Database Schema Reference

This document is the high-level schema map for TabFlow.

It stays at the shape-and-ownership level. Raw DDL lives in the EF Core
migrations under `src/infra/postgres/migrations/`.

## Boundary

TabFlow uses separate platform and tenant databases.

- The platform database is the control-plane registry.
- Each tenant database is an independent runtime business database.

The platform host connects to the platform database. Each tenant host
connects to one tenant database.

## Naming Convention

Platform database:

- Database: `tabflow_platform`
- Application role: `tabflow_platform_app`

Tenant database (one pair per tenant):

- Database: `tabflow_<tenant-code>`
- Application role: `tabflow_<tenant-code>_app`

`<tenant-code>` matches the tenant-code baseline described in
[`../../explanation/concepts/tenant-lifecycle.md`](../../explanation/concepts/tenant-lifecycle.md):
lowercase `a-z`, `0-9`, and hyphen, 3 to 63 characters, no leading or
trailing hyphen. Hyphens are preserved in both the database and role
name; PostgreSQL quotes them automatically in DDL.

The application role has `LOGIN`, `CONNECT`, and table-level
privileges on its own database only. It MUST NOT have superuser or
cross-database privileges; cross-tenant joins are an architectural
violation ([AD-0001](../architecture/decisions.md#ad-0001-platform-and-tenant-remain-architecturally-separate)).

Backup, restore, and rotation procedures in
[`../../how-to/backup-and-restore.md`](../../how-to/backup-and-restore.md)
and
[`../../how-to/rotate-secrets.md`](../../how-to/rotate-secrets.md)
assume this naming.

## Migration Authority

EF Core is the authoritative migration path. Schema changes are model
changes followed by a generated migration. See
[`../architecture/decisions.md`](../architecture/decisions.md) AD-0008.

Migration trees:

- Platform migrations: `src/infra/postgres/migrations/platform`
- Tenant migrations: `src/infra/postgres/migrations/tenant`

Tenant bootstrap applies the committed tenant migration history during
provisioning. There is no handwritten idempotent `ALTER` fallback.

## Platform Database

Groups:

- Platform identity (ASP.NET Core Identity store)
- Tenant registry and domain assignment
- Provisioning jobs
- Platform audit log
- Secret references and registry metadata

### Platform Identity

Standard ASP.NET Core Identity tables (`AspNetUsers`, `AspNetRoles`,
`AspNetUserRoles`, `AspNetUserLogins`, `AspNetUserClaims`, `AspNetUserTokens`,
`AspNetRoleClaims`).

Roles:

- `owner`
- `admin`
- `viewer`

### Tenant Registry

Owns:

- Tenant identity (`id`, `code`, `display_name`)
- Tenant status (`provisioning`, `active`, `suspended`, `archived`)
- Regional settings (`language_code`, `currency_code`, `time_zone`)
- Primary domain assignment
- Intended initial tenant admin email

### Provisioning Jobs

Owns:

- Job identity, type, status, attempt count, claimed-by
- Job payload
- Job result and failure detail
- Step-level visibility rows for retryable observation

### Platform Audit Log

`platform_audit_log` with the shared audit schema described in
[Audit Baseline](#audit-baseline).

## Tenant Database

Groups:

- Tenant identity (ASP.NET Core Identity store, tenant-scoped)
- Tenant profile
- Catalog
- Floor layout
- Tables and device keys
- Customer session and cart state
- Orders and bills
- Station and kitchen-facing state
- Tenant audit log

### Tenant Identity

Standard ASP.NET Core Identity tables, scoped to this tenant.

Roles:

- `owner`
- `manager`
- `cashier`
- `station_device`

A user holds exactly one role in the baseline. Multi-role membership is a
future extension that can be added through a join table without migrating
existing rows.

### Tenant Profile

Owns:

- Tenant code
- Display name
- Primary domain
- Language, currency, time zone

### Catalog

Owns:

- Categories (`id`, `name`, `sort_order`, `is_active`)
- Menu items (`id`, `name`, `description`, `price`, `category_id`,
  `is_available`)
- Item-to-station routing (`item_id`, `station_id`) — the final routing
  source
- Category-to-station routing (`category_id`, `station_id`) — optional
  default helper

### Floor Layout

Owns:

- Layouts (`id`, `name`, `sort_order`, canvas metadata)
- Zones (`id`, `layout_id`, placement metadata)
- Table placements (`table_id`, `layout_id`, coordinates, size, shape,
  rotation, z-index)
- Fixed objects (`id`, `layout_id`, `kind`, label, placement)

### Tables And Devices

Owns:

- Tables (`id`, `label`, `is_active`)
- Device keys (`table_id`, `device_key_hash`, `last_seen_at`)
- Firmware defaults (`wifi_ssid`, `wifi_password_ref`, pin map, timing
  constants)

### Customer Session And Cart

Owns:

- `customer_sessions` — one row per live table session (one per active
  table check), referencing the table and its state
- `customer_access_tickets` — browser-scoped participants attached to a
  session
- `customer_session_cart_items` — server-side cart bound to the session,
  one row per item with quantity and optional note
- `qr_tokens` — single-use tokens for join and checkout proof

Submit-order flow consumes a `qr_tokens` row marked as checkout proof and
converts the cart into an `orders` + `order_items` pair.

### Orders And Bills

Owns:

- Orders (`id`, `table_id`, `session_id`, `ticket_id`, `submitted_at`,
  `total_amount`)
- Order items (`order_id`, `item_id`, `quantity`, `note`,
  `station_id`, `status`, status timestamps)
- Bills (`id`, `table_id`, `opened_at`, `closed_at`, `total_amount`,
  `payment_method`)

Bill mutation actions (`close`, `move`, `merge`, `split`) are authoritative
over bill state. Each action is auditable.

### Station And Kitchen

Owns:

- Stations (`id`, `name`, `code`, `color`, `type`, `is_active`,
  `is_fallback`, `sort_order`)
- Station operator assignments (scoping an identity to a station)

Order item status progression (`submitted`, `preparing`, `ready`, `served`,
`cancelled`) is owned by the order item row. The station board reads and
mutates status through the application service, which publishes events to
the in-process event bus.

### Tenant Audit Log

`tenant_audit_log` with the shared audit schema.

## Audit Baseline

Both `platform_audit_log` and `tenant_audit_log` share a common shape:

| Column | Type | Note |
| --- | --- | --- |
| `id` | `uuid` | Primary key |
| `actor_id` | `uuid NULL` | Identity user id when the action was performed by a human; null for system actions |
| `actor_email` | `text` | Resolved at write time; stays stable if the user is later deleted |
| `action` | `text` | Fixed snake_case vocabulary, for example `tenant.create`, `bill.close` |
| `resource_type` | `text` | Kind of resource affected |
| `resource_id` | `text` | Identifier of the affected resource |
| `changes` | `jsonb` | Optional before/after or payload snapshot |
| `ip` | `inet` | |
| `user_agent` | `text` | |
| `correlation_id` | `uuid` | Request correlation id |
| `created_at` | `timestamptz` | |

### Logged Actions (minimum set)

- `auth.login.success`, `auth.login.failure`, `auth.password.change`,
  `auth.logout`
- `user.create`, `user.update`, `user.role.change`, `user.deactivate`
- `tenant.create`, `tenant.status.change`, `tenant.regional.update`
  (platform log only)
- `bill.close`, `bill.move`, `bill.merge`, `bill.split`
- `catalog.category.*`, `catalog.item.*`
- `station.create`, `station.update`, `station.delete`,
  `station.device.pair`, `station.device.revoke`
- `provision.job.trigger`

### Not Logged

- Customer menu browsing
- Normal order-state transitions (already captured on the order item row)
- Health and readiness probes

## Bootstrap Direction

Tenant provisioning applies the committed tenant migration history and then
seeds:

- Tenant profile
- Starter tables (`000` and `999`)
- Starter catalog baseline
- Default tenant owner user

The initial owner password is generated at provisioning time and shown once
to the operator who provisioned the tenant. It is not written to any file
after display. First successful login forces a password change.

## Related

- [`../architecture/system-overview.md`](../architecture/system-overview.md)
- [`../architecture/decisions.md`](../architecture/decisions.md)
- [`../../explanation/concepts/tenant-lifecycle.md`](../../explanation/concepts/tenant-lifecycle.md)
- [`../../explanation/concepts/multi-tenancy.md`](../../explanation/concepts/multi-tenancy.md)
- [`../../explanation/concepts/authorization.md`](../../explanation/concepts/authorization.md)
