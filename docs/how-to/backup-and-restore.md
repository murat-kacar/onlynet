# Backup And Restore

This guide describes how to back up and restore TabFlow databases. It
covers the platform database and every tenant database.

## Scope

Backups cover:

- platform database
- every tenant database
- platform `appsettings.Production.json` (for connection strings and
  non-secret operational configuration)

Backups deliberately do not cover:

- generated firmware sketches under `runtime/generated/`; these are
  rebuildable from database state
- local logs beyond the retention window; logs are for operational
  triage, not long-term memory

## Backup Cadence

- Full logical dump of every database once per day
- Hourly continuous WAL archiving into a retained location
- Retention: 30 days of daily dumps, 7 days of hourly WAL

## Daily Full Dump

Run on the database host, not on the application host:

```bash
for db in $(psql -At -U postgres \
              -c "SELECT datname FROM pg_database WHERE datname LIKE 'tabflow_%';"); do
  pg_dump --format=custom --compress=9 \
          --file="/var/backups/tabflow/$(date +%F)-${db}.dump" \
          "${db}"
done
```

Each database is dumped independently so individual tenants can be
restored without touching other tenants.

## Continuous WAL Archiving

Configure PostgreSQL's `archive_command` to push WAL segments into the
retained archive location. Verify archive health:

```bash
psql -U postgres -c "SELECT pg_switch_wal();"
ls -la /var/archives/tabflow/wal/ | tail -5
```

A healthy archive gains a new segment per configured WAL write
interval.

## Restore — Single Tenant

Restoring a single tenant from a logical dump:

1. Stop the tenant host:
   `systemctl stop tabflow-tenant@<tenant-code>.service`
2. Drop the target database if replacing:
   `dropdb -U postgres tabflow_<tenant-code>`
3. Create an empty target:
   `createdb -U postgres tabflow_<tenant-code>`
4. Restore from the dump:
   `pg_restore --dbname=tabflow_<tenant-code> --clean --if-exists \
       /var/backups/tabflow/<date>-tabflow_<tenant-code>.dump`
5. Start the tenant host:
   `systemctl start tabflow-tenant@<tenant-code>.service`
6. Verify: `curl -fsS https://<tenant-domain>/health/ready`

## Restore — Point In Time

For point-in-time recovery, combine the latest base backup with the WAL
archive up to the target timestamp. This procedure rebuilds the entire
PostgreSQL cluster; it is a last-resort operation.

Outline:

1. Stop the PostgreSQL service.
2. Move the existing data directory aside (keep it; do not delete
   until recovery is confirmed).
3. Restore the base backup into the data directory.
4. Configure `recovery.signal` and `restore_command` against the WAL
   archive, targeting the desired timestamp.
5. Start PostgreSQL; it will replay WAL until the target, then open the
   cluster in normal mode.
6. Verify platform and tenant `/health/ready` on every affected host.

## Restore Verification

After any restore, verify:

- `/health/ready` returns `200` on platform and the restored tenant
  hosts
- Schema migrations pointer (EF Core `__EFMigrationsHistory`) matches
  the expected head
- A representative device WebSocket reconnects
- The tenant audit log tail is consistent with pre-restore state

## Related

- [`./restart-tenant.md`](./restart-tenant.md)
- [`./rotate-secrets.md`](./rotate-secrets.md)
- [`../reference/database/schema.md`](../reference/database/schema.md)
