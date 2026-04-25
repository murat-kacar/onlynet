# Rotate Secrets

This guide rotates each class of TabFlow runtime secret safely. Every
rotation path below avoids service interruption when possible and
records the rotation in the appropriate audit log.

## Secret Classes

TabFlow uses these secret classes:

- Platform admin password
- Tenant user password (`owner`, `manager`, `cashier`, `station_device`)
- Tenant device key (ESP32 table device)
- Tenant host data-protection keys (ASP.NET Core Identity cookie
  signing and encryption keys)
- Platform host data-protection keys
- Database connection credentials

The procedures below assume ASP.NET Core Identity is active (see
[AD-0005](../reference/architecture/decisions.md#ad-0005-aspnet-core-identity-as-the-single-authentication-model)
in [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)).

## Platform Admin Password

From the platform admin console, user detail view, `Reset password`.
The new password is generated server-side and displayed once. It is
written to the platform audit log as a `platform_admin.password_reset`
event. Clients with an open session are not forcibly logged out; their
sessions expire naturally with the cookie lifetime.

For forced re-auth, combine with a data-protection key rotation
(below).

## Tenant User Password

From the tenant admin console, the owner or a manager resets a user
password. The new password is displayed once and recorded in the
tenant audit log. Owner rotation is owner-only; managers cannot
rotate owner credentials (see `Console:ManageUsersBelowOwner` in
[`../explanation/concepts/authorization.md`](../explanation/concepts/authorization.md)).

## Tenant Device Key

From the tenant admin console, tables detail, `Rotate device key`.
The new device key is generated server-side, the stored
`device_key_hash` is replaced, and the event is recorded in the tenant
audit log. The tenant host keeps honoring the current WebSocket
connection until it drops; the next reconnect attempt with the old key
will be rejected and the device must be reflashed with the new key.

Reflashing workflow:

1. Generate the per-table firmware sketch with the new device key.
2. Physically reflash the ESP32.
3. Verify the device reconnects on the device WebSocket and renders
   the current QR matrix.

## Data-Protection Keys

ASP.NET Core Identity cookies are signed and encrypted with the
data-protection keyring. By default the keyring auto-rotates (90 days),
but forced rotation is occasionally necessary (for example after a
suspected key compromise).

For a forced rotation, on the host whose keys are being rotated:

1. Revoke all keys: remove the persisted keyring directory content, or
   use the admin keyring tool to revoke.
2. Restart the host; a fresh keyring is generated on startup.
3. All existing Identity cookies are invalidated. Active users must log
   in again.

Rotating the keyring for a tenant host forcibly logs out all staff on
that tenant. Rotating the platform keyring forcibly logs out all
platform admins.

## Database Connection Credentials

1. Create a new PostgreSQL role with the required permissions.
2. Update the connection string in the affected host's
   `appsettings.Production.json`.
3. Restart the host (see [`./restart-tenant.md`](./restart-tenant.md)
   for tenants; platform uses its own unit).
4. Drop or disable the old role once all hosts are verified.

Connection-string rotations never happen in place; always add the new
role first, flip configuration, then retire the old role.

## Audit Expectations

Every rotation listed here writes to the appropriate audit log
(platform or tenant). Audit rows carry actor, action, resource, and
timestamp. Absence of an expected audit row after a rotation is a
signal to investigate.

## Related

- [`./backup-and-restore.md`](./backup-and-restore.md)
- [`./restart-tenant.md`](./restart-tenant.md)
- [`../explanation/concepts/authorization.md`](../explanation/concepts/authorization.md)
- [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)
