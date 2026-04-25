# Release Gate

Last-stop verification before a release tag moves. Every item below
MUST hold on the exact commit that would ship. A single unchecked
item blocks the release. The specific tooling used to check each item
is the operator's choice.

Run the gate in two passes:

1. **Local pass** — against the commit on the release branch.
2. **Staging pass** — against a deployed staging tenant at the same
   commit. Items marked *staging* apply only here.

## Documentation

- [ ] [`reference/architecture/decisions.md`](../reference/architecture/decisions.md)
  reflects every architecture decision that landed in this release.
- [ ] No ADR ships with `Status: Proposed`. Every accepted change
  has its ADR in `Status: Accepted`; rejected proposals are recorded
  as `Status: Rejected`.
- [ ] [`reference/architecture/runtime-surfaces.md`](../reference/architecture/runtime-surfaces.md),
  [`render-modes.md`](../reference/architecture/render-modes.md),
  [`capability-matrix.md`](../reference/architecture/capability-matrix.md),
  [`tenant-api.md`](../reference/api/tenant-api.md),
  [`api/internal-api.md`](../reference/api/internal-api.md),
  [`schema.md`](../reference/database/schema.md),
  [`architecture/health-checks.md`](../reference/architecture/health-checks.md),
  [`slos.md`](../reference/architecture/slos.md),
  [`explanation/concepts/threat-model.md`](../explanation/concepts/threat-model.md),
  [`explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md),
  [`explanation/concepts/internationalization.md`](../explanation/concepts/internationalization.md),
  and [`explanation/concepts/test-taxonomy.md`](../explanation/concepts/test-taxonomy.md)
  match the shipped behaviour.
- [ ] [`reference/api/error-codes.md`](../reference/api/error-codes.md)
  covers every code returned by the shipped HTTP surfaces.
- [ ] [`reference/acceptance-criteria.md`](../reference/acceptance-criteria.md)
  has an item for every new invariant introduced in this release.
- [ ] [`../constitution.md`](../constitution.md) and
  [`./documentation-charter.md`](./documentation-charter.md) are
  either unchanged or amended via a PR that follows their amendment
  rule.

## Checks

- [ ] The test suite passes across platform, tenant, and firmware
  code.
- [ ] Formatting and static analysis are clean across the solution.
- [ ] No pending model-to-schema mismatches in either the platform or
  tenant database context.
- [ ] Markdown lint passes on every documentation tree under
  [`/doc/`](/doc/).
- [ ] Semantic cross-reference integrity is verified — surface IDs,
  ADR IDs, AC IDs, SLI names, TD IDs, and tree paths all resolve.
- [ ] Dead-link checker passes on every documentation tree under
  [`/doc/`](/doc/).
- [ ] No orphan `TD-NNNN` reference: every `TD-` mention in code or
  docs resolves to an entry in
  [`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md).
- [ ] SAST and dependency audit pipelines are green.

## Acceptance

Every statement in
[`reference/acceptance-criteria.md`](../reference/acceptance-criteria.md)
MUST hold on the build under test. If any item cannot be verified by
an automated test, the gate MUST include a recorded manual pass with
a ticket link.

- [ ] Platform access items (AC-001 to AC-006) verified.
- [ ] Tenant access items (AC-010 to AC-015) verified.
- [ ] Customer join and session items (AC-020 to AC-026) verified.
- [ ] Order submission items (AC-030 to AC-036) verified.
- [ ] Table and bill invariants (AC-040 to AC-045) verified.
- [ ] Station board items (AC-050 to AC-052) verified.
- [ ] Device channel items (AC-060 to AC-063) verified.
- [ ] Auditability items (AC-070 to AC-072) verified.
- [ ] Data residency items (AC-080 to AC-082) verified.
- [ ] Web posture items (AC-090 to AC-092) verified.
- [ ] Observability items (AC-100 to AC-102) verified.
- [ ] Accessibility items (AC-110 to AC-116) verified. Automated WCAG
  2.2 AA checks against the customer menu, the floor and cash
  workspace, one station board, and the platform admin console
  report zero violations; any remaining finding has a tracked
  remediation PR on the roadmap.
- [ ] Internationalization items (AC-117 to AC-121) verified.
- [ ] Data-protection items (AC-122 to AC-126) verified.
- [ ] Recovery items (AC-127 to AC-130) verified.
- [ ] Test items (AC-131 to AC-134) verified.

## Smoke Scenarios (*staging*)

Run each of these end-to-end on a staging tenant provisioned from the
release commit. The scenario MUST complete without manual recovery.

- [ ] Customer join and order: scan QR at `/g/{token}` → `/menu`
  opens with the cart empty → add two items → submit with a fresh
  second scan → customer session closes → order appears on the
  relevant station board within the SLO latency budget.
- [ ] Floor and cash: cashier opens `/service`, sees the just-closed
  session's bill as open on the table, closes the bill → table
  returns to empty state and remaining customer sessions on the table
  are invalidated.
- [ ] Bill split: two tables, one with an open bill, split one line
  item to the second table → both tables respect the one-open-bill
  invariant.
- [ ] Device rotation: a table's ESP32 device shows the rotated QR
  within the token TTL; a stale QR at `/g/{token}` is rejected with
  `token_used` or `token_expired`.
- [ ] Provisioning: provision a fresh tenant from the platform
  console; the new tenant boots with seed roles (`owner`, `manager`,
  `cashier`, `station_device`) and the one-time owner credentials
  are delivered through the provisioning job payload.

## Observability (*staging*)

- [ ] A `HEAD /menu` request returns `X-Robots-Tag: noindex, nofollow,
  noarchive`.
- [ ] `GET /robots.txt` returns a fully-disallowed policy.
- [ ] `HEAD /health/ready` returns `200` when the tenant database is
  reachable.
- [ ] `POST /api/public/orders` with an invalid body returns an
  `application/problem+json` response carrying both `code` and
  `traceId`.
- [ ] SLO dashboards show no error-budget burn from the previous
  release window.

## Rollback Plan

- [ ] The previous release tag is reachable and deployable without
  schema rollback.
- [ ] Any migration included in this release has a documented
  rollback path, or is declared forward-only with a noted reason.
- [ ] Feature flags introduced in this release default to the
  pre-release behaviour on fresh boot.

## Disaster Recovery Verification

Per [`../reference/architecture/slos.md`](../reference/architecture/slos.md#recovery-objectives)
and [`../how-to/backup-and-restore.md`](../how-to/backup-and-restore.md#quarterly-recovery-drill).

- [ ] A recovery drill has been performed within the previous 90 days.
- [ ] The drill recorded measured RTO and RPO that meet the targets
  for every scope (platform host, tenant host, tenant database,
  multi-tenant outage).
- [ ] The drill record is present in [`/doc/buildlog/`](/doc/buildlog/)
  with the drill date, measured numbers, and any procedure
  corrections.
- [ ] Backup encryption verification (LUKS volume + age-encrypted
  dumps) passes on the production database host.
- [ ] Off-site backup copy is reachable and append-only credentials
  are in force.

## Data Protection Verification

Per [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md).

- [ ] Every new column with personal data carries a `[DataClass]`
  attribute and a corresponding schema comment.
- [ ] Every new processing activity introduced in this release has a
  row in the lawful-basis table.
- [ ] Retention sweep jobs for any new data class are wired into the
  platform worker schedule.
- [ ] Any new sub-processor (third party processing personal data on
  TabFlow's behalf) is recorded in the sub-processor table and
  notified to tenants.

## Test Coverage Verification

Per [`../explanation/concepts/test-taxonomy.md`](../explanation/concepts/test-taxonomy.md).

- [ ] Every new capability ships with at least one test in the
  appropriate tier (unit, integration, E2E, or smoke).
- [ ] No `[Skip]` attribute is added during this release.
- [ ] Flaky-test rate over the release window is `< 1%` (a flake is a
  test that re-passed on the same commit without a code change).

## DORA Metrics Review

Per [`../constitution.md`](../constitution.md) Section VII.1.

- [ ] Deployment frequency, lead time from commit to production,
  change failure rate, and mean time to recovery are reported for the
  release window since the last gate.
- [ ] A trend in the wrong direction is discussed and either accepted
  with a recorded rationale or blocked from sign-off.

## Tech Debt Ledger Triage

Per [`../constitution.md`](../constitution.md) Section VII.3.

- [ ] Every `[TRIAGE]` entry in
  [`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md)
  is reviewed and transitioned to `[OPEN]` (with a named owner),
  `[ACCEPTED]` (with an ADR citation), or `[ABANDONED]` (with a
  `buildlog/abandoned/` reference).
- [ ] Every `[OPEN]` entry has an owner who is not `TBD`.
- [ ] `[OPEN]` entries older than 90 days are either claimed by a new
  named owner with a refreshed payoff plan or escalated.
- [ ] Any debt closed in this release has a `[CLOSED]` block with a
  resolution note.

## Sign-Off

- [ ] Release owner has recorded the gate pass on the release ticket
  with a link to the commit.
- [ ] At least one additional reviewer has co-signed the gate per
  [`./review-policy.md`](./review-policy.md).
- [ ] Every security-sensitive PR merged since the previous release
  carries a `security: reviewed` note per
  [`./review-policy.md`](./review-policy.md).
