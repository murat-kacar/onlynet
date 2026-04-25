# Inspect A Provisioning Job

This guide walks through inspecting a `tenant.create` (or other
lifecycle) job that is stuck, failed, or suspicious. Provisioning jobs
live in the platform database and are advanced by the platform worker.

## When To Inspect

- A tenant stays in `pending` or `in_progress` longer than the
  provisioning SLO
  ([`../reference/architecture/slos.md`](../reference/architecture/slos.md))
- The platform admin console Provisioning Jobs surface (`P-06`) shows a job in `failed` state
- A platform worker restart does not resume a `claimed` job
- An operator wants to audit the actual steps a job ran

## First Look — Platform Admin Console

Open the platform admin console and navigate to `P-06` Provisioning
Jobs. Select the job. The detail view shows:

- Job ID
- Tenant code and name
- Current status (`pending`, `claimed`, `in_progress`, `completed`,
  `failed`)
- Worker ID that most recently claimed the job
- Per-step status (database create, migrations, seed, DNS hook, nginx
  reload hook)
- Last error, if any
- Timestamps for every state transition

For most cases this is enough to decide the next action: retry, cancel,
or escalate.

## Second Look — Platform Database

If the console is unreachable or more detail is needed, query the
platform database directly.

```sql
SELECT id, tenant_code, status, claimed_by, last_error,
       created_at, updated_at
FROM provisioning_jobs
WHERE tenant_code = '<tenant-code>'
ORDER BY created_at DESC
LIMIT 5;
```

Then the per-step log for the job:

```sql
SELECT step, status, message, started_at, finished_at
FROM provisioning_job_steps
WHERE job_id = '<job-id>'
ORDER BY started_at;
```

These tables are append-only within a job; rows are not deleted on
retry, a new job is enqueued instead.

## Third Look — Platform Worker Logs

Follow the worker log for the window around the job:

```bash
journalctl -u tabflow-platform-worker --since '15 min ago' -f
```

Every step carries a correlation ID equal to the job ID; filter on that
to reconstruct the exact sequence.

## Common Outcomes

### Job Stuck In `claimed`

The platform worker that claimed the job died before advancing it.
Expire the claim and the job becomes eligible for re-claim:

```sql
UPDATE provisioning_jobs
SET status = 'pending', claimed_by = NULL, claim_expires_at = NULL
WHERE id = '<job-id>' AND status = 'claimed';
```

The platform worker will pick the job up on the next poll.

### Job `failed` With A Transient Error

From the platform admin console, use `Retry job`. This enqueues a new
job targeting the same tenant. The original failed job stays in the
table as an audit trail.

### Job `failed` With A Fatal Error

Examples: DNS hook misconfiguration, missing platform database
permission, schema drift. Fix the underlying cause, then retry. Do not
manually edit `provisioning_jobs` rows to mark them `completed`; the
tenant database has not been created or migrated in that case.

## Related

- [`./provision-tenant.md`](./provision-tenant.md)
- [`./restart-tenant.md`](./restart-tenant.md)
- [`../explanation/concepts/tenant-lifecycle.md`](../explanation/concepts/tenant-lifecycle.md)
- [`../reference/architecture/slos.md`](../reference/architecture/slos.md)
