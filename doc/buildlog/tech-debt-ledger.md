# Technical Debt Ledger

This is the current ledger of accepted unfinished work in TabFlow. The
constitution requires every temporary compromise to have an identifier,
owner, risk statement, and payoff plan.

## Status Vocabulary

| Status | Meaning |
| --- | --- |
| `[TRIAGE]` | Recorded debt without a named owner yet. It must be claimed, accepted, or abandoned at the next release-gate review. |
| `[OPEN]` | Active debt with an owner and payoff plan. |
| `[CLOSED]` | Debt is paid. The identifier remains reserved for links. |
| `[ACCEPTED]` | Debt is intentionally permanent and cites an ADR. |
| `[ABANDONED]` | A payoff plan was rejected and cites a `buildlog/abandoned/` entry. |

## Cross-Reference Rule

Code or documents that contain a known compromise must link the relevant
ledger entry. The link form is the absolute path with the anchor:
`/doc/buildlog/tech-debt-ledger.md#td-0001`.

---

## Current Queue

<a id="td-0028"></a>
### [TRIAGE] TD-0028 - Customer-facing Razor pages still use Interactive Server

- Owner: TBD
- Symptom: `Cart.razor`, `Menu.razor`, `Order.razor`, and
  `ScanQr.razor` still require interactive handlers even though AD-0004
  assigns customer-facing surfaces to Static SSR.
- Risk if unpaid: customer traffic opens SignalR circuits at diner
  scale, which breaks the capacity model intended for staff surfaces.
- Payoff plan: convert customer actions to Static-SSR-friendly form
  posts/server-rendered confirmation flows, remove the customer
  `@rendermode InteractiveServer` directives, and add a smoke check
  that verifies customer routes render without interactive markers.

<a id="td-0025"></a>
### [TRIAGE] TD-0025 - Test double policy is unresolved

- Owner: TBD
- Symptom: documentation prefers hand-written fakes, while test projects
  reference NSubstitute.
- Risk if unpaid: reviewers apply different standards to new tests.
- Payoff plan: choose one policy, update the test taxonomy and examples,
  and make the repository packages match the policy.

<a id="td-0021"></a>
### [OPEN] TD-0021 - Legacy customer-tier routes remain during public API transition

- Owner: TBD
- Symptom: `/api/public/*` is the canonical customer API, while some
  legacy customer routes remain operational for compatibility.
- Risk if unpaid: staff and customer API boundaries stay harder to
  reason about.
- Payoff plan: keep compatibility routes documented as temporary,
  migrate callers to `/api/public/*`, then remove the legacy routes and
  their route tests.

<a id="td-0020"></a>
### [TRIAGE] TD-0020 - Review-pair operating model needs a standing rule

- Owner: TBD
- Symptom: the constitution requires independent review on risky changes,
  but the single-maintainer operating model needs an explicit rule for
  how that review is obtained.
- Risk if unpaid: release-gate review cannot distinguish intentional
  solo maintenance from missing review.
- Payoff plan: document the standing review model, external-review
  trigger, and release-blocking cases in the review policy.

<a id="td-0019"></a>
### [OPEN] TD-0019 - Repository TODOs need owned closure paths

- Owner: TBD
- Symptom: a small set of TODOs remains in production and test code:
  `src/apps/platform/Middleware/AuditMiddleware.cs`,
  `src/apps/tenant/Components/Pages/TableView.razor`,
  `src/apps/tenant/Services/EventSubscriptionService.cs`, and
  `src/apps/tenant/WebSocket/TableWebSocketHandler.cs`.
- Risk if unpaid: temporary code can blend into normal implementation.
- Payoff plan: convert each TODO into a named debt item or close it with
  code, then add analyzer coverage that rejects untracked TODOs.

<a id="td-0017"></a>
### [OPEN] TD-0017 - Wrong-device order submit integration coverage is missing

- Owner: TBD
- Symptom: device-cookie binding exists on customer tickets, but the
  wrong-device submit path still needs integration coverage.
- Risk if unpaid: regressions in customer device binding can reach a
  release undetected.
- Payoff plan: add an integration test that opens a ticket for one
  device, submits from another, and asserts the expected rejection.

<a id="td-0016"></a>
### [OPEN] TD-0016 - Render-mode release-gate smoke check is missing

- Owner: TBD
- Symptom: render-mode conventions are documented, but the release gate
  does not yet assert them against rendered pages.
- Risk if unpaid: future Razor changes can silently move a route into
  the wrong render mode.
- Payoff plan: add Playwright or equivalent smoke coverage for route
  render modes after the E2E bootstrap is stable.

<a id="td-0015"></a>
### [OPEN] TD-0015 - Tenant API authorization integration coverage is incomplete

- Owner: TBD
- Symptom: authorization attributes and policies exist, but anonymous
  and cross-role integration coverage is incomplete.
- Risk if unpaid: policy regressions can pass unit tests.
- Payoff plan: add integration coverage for anonymous, customer, and
  staff role access on the tenant API.

<a id="td-0014"></a>
### [OPEN] TD-0014 - Analyzer baseline is not restored to strict mode

- Owner: TBD
- Symptom: analyzer rules exist, but the repository still needs the
  final strict-mode baseline expected by the release gate.
- Risk if unpaid: new violations can hide behind tolerated warnings.
- Payoff plan: restore strict analyzer settings and document any
  intentionally suppressed rule.

<a id="td-0013"></a>
### [OPEN] TD-0013 - Worker heartbeat readiness probe is incomplete

- Owner: TBD
- Symptom: platform and tenant health checks exist, but worker heartbeat
  readiness depends on a `worker_heartbeats` schema and writer.
- Risk if unpaid: a dead worker can be missed by readiness automation.
- Payoff plan: add the heartbeat table/writer, wire the health probe,
  and include it in release-gate smoke coverage.

<a id="td-0012"></a>
### [OPEN] TD-0012 - Serilog sink verification is missing

- Owner: TBD
- Symptom: logging is configured, but deployed file/sink delivery is not
  verified by an operational check.
- Risk if unpaid: production incidents may lack expected log evidence.
- Payoff plan: add a deploy-time or smoke-time assertion that the
  configured sink receives a known event.

<a id="td-0011"></a>
### [OPEN] TD-0011 - Tenant localization sweep is incomplete

- Owner: TBD
- Symptom: platform localization exists, but tenant staff and customer
  surfaces still need the same preference-backed localization model and
  missing-key checks.
- Risk if unpaid: tenant users see mixed-language UI and untracked
  localization gaps.
- Payoff plan: add tenant resource files, wire preferences, and enforce
  missing-key checks in CI.

<a id="td-0010"></a>
### [OPEN] TD-0010 - Integration fixture and smoke tier bootstrap are incomplete

- Owner: TBD
- Symptom: test categories exist, but the hermetic integration fixture
  and release smoke tier are not complete.
- Risk if unpaid: infrastructure and end-to-end regressions can escape
  fast-path tests.
- Payoff plan: finish transactional integration bootstrapping, compose
  the smoke tier, and make CI run the intended test split.

<a id="td-0009"></a>
### [OPEN] TD-0009 - Analyzer coverage expansion remains

- Owner: TBD
- Symptom: the English-first analyzer exists, while broader analyzer
  coverage for repository rules remains incomplete.
- Risk if unpaid: style and safety rules rely on review discipline
  rather than repeatable automation.
- Payoff plan: add the remaining analyzer rules, analyzer release
  metadata, and regression tests.

<a id="td-0008"></a>
### [OPEN] TD-0008 - Retention sweep jobs are not implemented

- Owner: TBD
- Symptom: retention schedules are documented, but worker jobs do not yet
  enforce them.
- Risk if unpaid: personal data can outlive the documented retention
  period.
- Payoff plan: implement scheduled sweep jobs, audit their deletions, and
  test the retention boundaries.

<a id="td-0007"></a>
### [OPEN] TD-0007 - Personal-data classification sweep is incomplete

- Owner: TBD
- Symptom: classification primitives exist, but every personal-data
  property still needs classification coverage and release-gate checks.
- Risk if unpaid: sensitive data can be added without explicit storage
  and retention semantics.
- Payoff plan: complete entity annotations and add a release check that
  rejects unclassified sensitive/restricted data.

<a id="td-0006"></a>
### [OPEN] TD-0006 - Branch protection is not configured

- Owner: TBD
- Symptom: branch-protection policy is documented but not applied to the
  repository host.
- Risk if unpaid: main can accept changes without the required checks and
  review controls.
- Payoff plan: configure branch protection and capture the configuration
  evidence in the release gate.

<a id="td-0005"></a>
### [OPEN] TD-0005 - CI workflows need a real PR/tag validation pass

- Owner: TBD
- Symptom: workflow files exist, but their complete behavior has not been
  validated by the repository's hosted CI lifecycle.
- Risk if unpaid: release automation may fail only when a release is
  attempted.
- Payoff plan: run the PR and tag workflows in the hosted environment
  and fix any discovered gaps.

<a id="td-0004"></a>
### [OPEN] TD-0004 - Encrypted off-site backup pipeline is not implemented

- Owner: TBD
- Symptom: backup and restore procedures are documented, but automated
  encrypted off-site backup is not wired.
- Risk if unpaid: disaster recovery relies on operator discipline rather
  than scheduled evidence.
- Payoff plan: implement encrypted backups, off-site copy, restore
  verification, and recovery-drill evidence.

<a id="td-0003"></a>
### [OPEN] TD-0003 - Tenant migration application path is incomplete

- Owner: TBD
- Symptom: EF Core migrations exist, but tenant provisioning still needs
  the production path that applies and verifies tenant schema migration.
- Risk if unpaid: tenant schema can drift from the model at provisioning
  time.
- Payoff plan: wire `MigrateAsync()` into the provisioning flow and add
  verification around migration history.

<a id="td-0002"></a>
### [OPEN] TD-0002 - Platform bootstrap admin run remains an operator action

- Owner: TBD
- Symptom: the bootstrap command exists, but first-deployment execution
  and evidence capture remain operational work.
- Risk if unpaid: platform startup can stall without a controlled owner
  account.
- Payoff plan: run the command during bootstrap, verify password-change
  enforcement, and capture release-gate evidence.

<a id="td-0001"></a>
### [OPEN] TD-0001 - Platform migration application verification remains

- Owner: TBD
- Symptom: platform migrations exist, but production bootstrap still
  needs verified migration application evidence.
- Risk if unpaid: platform schema drift can go undetected.
- Payoff plan: apply migrations through the documented operator path and
  verify migration history before release.

## Reserved Closed Identifiers

These identifiers are paid in the v1.0.0 baseline and remain reserved so
older code comments or links do not get reused accidentally.

<a id="td-0018"></a>
### [CLOSED] TD-0018 - Customer order idempotency

<a id="td-0022"></a>
### [CLOSED] TD-0022 - Tenant controller application-service seams

<a id="td-0023"></a>
### [CLOSED] TD-0023 - Razor route inventory alignment

<a id="td-0024"></a>
### [CLOSED] TD-0024 - Customer-session data classification primitives

<a id="td-0026"></a>
### [CLOSED] TD-0026 - systemd lifetime registration

<a id="td-0027"></a>
### [CLOSED] TD-0027 - Blazor Web App host baseline
