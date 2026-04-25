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

- Full logical dump of every database once per day.
- Continuous WAL archiving with at most 15 minutes of in-flight gap
  (matches the RPO target in
  [`../reference/architecture/slos.md`](../reference/architecture/slos.md#recovery-objectives)).
- Retention: 30 days of daily dumps; 30 days of WAL.

## Encryption

Backups contain personal data (`Sensitive` and `Restricted` classes
per [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md)).
They MUST be encrypted at rest.

The deployed configuration:

- Filesystem holding `/var/backups/tabflow/` is on a LUKS-encrypted
  volume; the volume key is stored in the operator's secret manager,
  not on the host.
- Each dump is additionally re-encrypted with `age` before leaving the
  source host. The recipient public key for off-site copies is held by
  the operator; the corresponding private key never lives on a
  TabFlow host.
- WAL segments are encrypted in transit by the `archive_command`
  pipeline before they reach the off-site location.

Verify encryption is active:

```bash
# LUKS volume mounted as encrypted
findmnt -no FSTYPE,SOURCE /var/backups/tabflow | grep -q crypt
# Sample dump file is age-encrypted (starts with the age header)
head -c 16 /var/backups/tabflow/$(date +%F)-tabflow_platform.dump.age \
  | grep -q '^age-encryption.org/'
```

A failure of either check stops backup activity and triggers an
incident per [`../constitution.md`](../constitution.md) Section VI.

## Off-Site Storage

A single host is not a backup. Every encrypted dump and every archived
WAL segment is replicated to a location that does not share a failure
domain with the source database:

- different physical site,
- different cloud account or storage credential,
- different operator responsible for access.

The off-site copy is **append-only** from the source host: the source
credential can write but cannot delete or overwrite. Pruning of expired
backups happens through a separate retention process with its own
credential.

If the off-site copy is unreachable for more than one cadence interval,
the operator is paged.

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

## Quarterly Recovery Drill

The release gate
([`../meta/release-gate.md`](../meta/release-gate.md)) requires that a
recovery drill has been performed within the previous 90 days.

A drill is a real restore against a non-production target:

1. Pick a representative tenant database from the latest backup set.
2. Restore it onto a clean host (single-tenant restore, above).
3. Optionally, perform a point-in-time recovery to a timestamp 30
   minutes before the latest backup to exercise WAL replay.
4. Run the restore-verification checklist.
5. Record:
   - drill date,
   - measured wall-clock RTO (from "incident declared" to
     `/health/ready` `200`),
   - measured RPO (gap between target timestamp and latest replayed
     transaction),
   - any deviation from the documented procedure.
6. If the measured RTO or RPO misses the targets in
   [`../reference/architecture/slos.md`](../reference/architecture/slos.md#recovery-objectives),
   open a tech-debt ledger entry with a payoff plan.
7. If the documented procedure failed to match reality, fix the
   procedure in the same PR that records the drill.

The drill record lives in
[`/doc/buildlog/`](/doc/buildlog/) — drills are part of the lessons
record.

## Related

- [`./restart-tenant.md`](./restart-tenant.md)
- [`./rotate-secrets.md`](./rotate-secrets.md)
- [`../reference/database/schema.md`](../reference/database/schema.md)
- [`../reference/architecture/slos.md`](../reference/architecture/slos.md#recovery-objectives)
  — RTO / RPO targets the cadence and procedure pair to
- [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md)
  — retention windows backups must satisfy
- [`../meta/release-gate.md`](../meta/release-gate.md) — drill
  verification at release
