# Changelog

All notable changes to TabFlow are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Each release tag on `main` corresponds to one section below. Unreleased
work accumulates under `[Unreleased]` and is moved into a versioned
section when a release tag is cut.

## Versioning Rule

- **MAJOR** — a breaking change to a stable contract: HTTP route, event
  payload shape, DB column, config key, or accepted ADR with
  `Status: Superseded`.
- **MINOR** — additive, non-breaking changes: new capability, new
  endpoint, new optional field.
- **PATCH** — bug fixes, internal refactors, doc updates that do not
  touch a contract.

Pre-1.0.0 the public surface is considered unstable; minor bumps may
carry breaking changes if explicitly noted under a `### Breaking`
heading.

Detailed rules live in
[`doc/docs/reference/architecture/decisions.md`](./doc/docs/reference/architecture/decisions.md)
AD-0011.

## [Unreleased]

### Added

- Project constitution
  ([`doc/docs/constitution.md`](./doc/docs/constitution.md)) and
  documentation charter
  ([`doc/docs/meta/documentation-charter.md`](./doc/docs/meta/documentation-charter.md)).
- Tech debt ledger
  ([`doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md))
  with `[TRIAGE] [OPEN] [CLOSED] [ACCEPTED] [ABANDONED]` status flow,
  seeded with TD-0001…TD-0009 covering schema rebuild, bootstrap CLI,
  tenant migrations, backup encryption, CI validation, branch
  protection, data classification, retention sweeps, and English-first
  lint.
- Pull-request review policy
  ([`doc/docs/meta/review-policy.md`](./doc/docs/meta/review-policy.md))
  and an expanded release gate
  ([`doc/docs/meta/release-gate.md`](./doc/docs/meta/release-gate.md))
  with disaster-recovery, data-protection, test-coverage, DORA, and
  tech-debt-triage sections.
- Apache 2.0 license, NOTICE file, Contributor Covenant Code of Conduct,
  and security disclosure policy at the repository root.
- Internal API reference
  ([`doc/docs/reference/api/internal-api.md`](./doc/docs/reference/api/internal-api.md))
  and health-check endpoint reference
  ([`doc/docs/reference/architecture/health-checks.md`](./doc/docs/reference/architecture/health-checks.md)).
- ADRs AD-0011 (semantic versioning), AD-0012 (Apache 2.0 licence),
  AD-0013 (GitHub Actions CI), AD-0014 (`.editorconfig` +
  `Directory.Build.props` for coding standards), and AD-0015
  (English-first internal contracts).
- Concept explainers for internationalization
  ([`doc/docs/explanation/concepts/internationalization.md`](./doc/docs/explanation/concepts/internationalization.md)),
  data protection — KVKK + GDPR
  ([`doc/docs/explanation/concepts/data-protection.md`](./doc/docs/explanation/concepts/data-protection.md)),
  and the four-tier test taxonomy
  ([`doc/docs/explanation/concepts/test-taxonomy.md`](./doc/docs/explanation/concepts/test-taxonomy.md)).
- Recovery objectives (RTO/RPO) section in
  [`doc/docs/reference/architecture/slos.md`](./doc/docs/reference/architecture/slos.md)
  and an encryption + off-site + quarterly drill section in
  [`doc/docs/how-to/backup-and-restore.md`](./doc/docs/how-to/backup-and-restore.md).
- Acceptance criteria AC-117…AC-134 covering internationalization,
  data protection, recovery, and tests.
- GitHub repository configuration: `CODEOWNERS`,
  `pull_request_template.md`, three issue templates (bug, feature,
  tech-debt), and CI workflows (`pr.yml`, `main.yml`, `release.yml`).
- `.editorconfig` and `Directory.Build.props` enforcing the AD-0014
  coding-standards baseline (analyzer level, nullable, warnings as
  errors, treat `IDE`/`CA` rules as build-breaking).

### Changed

- Documentation tree consolidated under a single `doc/` umbrella with
  four trees (`docs/`, `userdocs/`, `apidocs/`, `buildlog/`); the
  earlier `templatedocs/` tree was retired and its content migrated.
- ADR status taxonomy formalised: `Proposed`, `Accepted`, `Rejected`,
  `Deprecated`, `Superseded` (per the documentation charter).
- Capability matrix `Implemented` definition tightened to require all
  three of *tested*, *observable*, and *documented* per constitution
  II.4. New capability rows for encrypted backup, recovery drill,
  data classification, retention sweeps, i18n, English-first lint,
  GitHub Actions, and branch protection.
- Glossary expanded with KVKK, GDPR, RTO, RPO, Recovery Drill, Data
  Class, Data Controller, Data Processor, Test Tier, and English-First
  entries.

### Documentation

- `/doc/buildlog/code-audit-2026-04-25.md` extended with **Section 11
  (Re-Review Findings)**. The re-review treated the original audit as
  an artefact under inspection and recorded:
  - Three structural defects in the audit itself (incomplete
    inventory, wrong ADR topics in Section 6, no build / test had been
    run).
  - Two new critical findings (RR-C2: tenant API controllers
    anonymous; RR-C3: tenant `InitialCreate` `Up` empty).
  - Two new high findings (RR-H1: AD-0004 not exercised; RR-H2:
    `/api/public/orders` mis-routed inside `OrdersController`).
  - Two new medium findings (RR-M1: tenant seed data carries Turkish
    strings; RR-M2: capability matrix Health row stale).
  - A corrected ADR conformance table that supersedes Section 6
    conceptually (Section 6 stays in place per the append-only rule).
  - Recommended audit hygiene for the next pass.
- `/doc/buildlog/tech-debt-ledger.md`:
  - Opened **TD-0015** — tenant API controllers expose every endpoint
    anonymously (highest-priority security gap currently in the
    ledger).
  - Opened **TD-0016** — AD-0004 mixed render modes never exercised.
  - Rewrote **TD-0003** payoff plan with the empty-`Up` finding and
    the Turkish-seed flag.
- `/doc/docs/reference/architecture/capability-matrix.md` — Health
  check endpoints row promoted from `Target` to `Implemented` and
  evidence list expanded.

### Added

- `/health`, `/health/live`, and `/health/ready` endpoints on both
  the platform host and the tenant host (AC-101). `/health/ready`
  fails (HTTP `503`, `status: "fail"`) when the host's DbContext
  cannot reach its PostgreSQL database (AC-102), via the
  `platform-db:ping` and `tenant-db:ping` probes registered with
  `AddDbContextCheck<T>(...)`. All three endpoints are
  `[AllowAnonymous]`, return `application/health+json` per the IETF
  draft, and serialise via the new shared
  `TabFlow.Shared.Infrastructure.Diagnostics.HealthJsonWriter`.
- `TabFlow.Shared.Infrastructure.Diagnostics.HealthJsonWriter` —
  shared response writer that renders `Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport`
  in the IETF `health+json` shape so both hosts produce
  byte-equivalent bodies.
- 8 unit tests in `Shared.Tests/Infrastructure/Diagnostics/HealthJsonWriterTests.cs`
  covering status-string mapping, content-type, component-type
  classification, observed duration, and version/release-id presence.
  This is the first passing test in the `Shared.Tests` project.

### Fixed

- `Serilog.Enrichers.Environment` `WithEnvironment()` call in
  `src/apps/{platform,tenant}/Program.cs`. The method does not exist in
  the 3.x package; the call site had never compiled. Removed so the
  Serilog configuration now compiles. `WithMachineName()` provides
  equivalent host context. Runtime log emission is still untested
  (TD-0012).
- `Directory.Build.props` analyzer baseline calibrated against the
  existing code. `AnalysisMode` lowered from `All` to `Recommended`,
  `EnforceCodeStyleInBuild` set to `false`, and `NoWarn` extended with
  the named CA / CS rules listed in the file. AD-0014's strict
  baseline is restored by TD-0014's seven-step ratchet plan.
- `Directory.Packages.props` extended with the previously-missing
  `PackageVersion` rows for `Serilog.AspNetCore`, `Serilog.Sinks.Console`,
  `Serilog.Sinks.File`, `Serilog.Enrichers.Environment`, and
  `Microsoft.CodeAnalysis.Analyzers`. Central Package Management is now
  consistent: `dotnet build TabFlow.sln` reports 0 warnings, 0 errors
  across 9 projects.

### Removed

- `templatedocs/` tree and all its files (content migrated to `docs/`
  or absorbed into the tech debt ledger).
- Migration `Migrations/Platform/20260425143828_InitialAdminUser.cs`
  (and its Designer + the orphan `PlatformDbContextModelSnapshot.cs`).
  The migration ran a raw SQL `INSERT INTO "AspNetUsers"` with a
  hard-coded placeholder hash, violating AC-005 and AD-0010. The
  platform now has no seeded admin until the `bootstrap-admin` CLI
  lands (TD-0002).
- The parallel lowercase `src/infra/postgres/migrations/` tree
  (`InitialCreate` and `AddTenantDatabaseConnection` for
  `PlatformDbContext`). Two case-different migration directories were
  compiled into the same project; the lowercase tree was the older of
  the two and lacked a model snapshot. Platform schema now has zero
  migrations in source and will be rebuilt by step 3 of TD-0001's
  payoff plan on the next bootstrap window.

## Release Procedure

1. Ensure `[Unreleased]` accurately reflects every change since the
   previous release tag.
2. Run the release gate
   ([`doc/docs/meta/release-gate.md`](./doc/docs/meta/release-gate.md)).
3. Rename the `[Unreleased]` heading to `[<MAJOR>.<MINOR>.<PATCH>] -
   <YYYY-MM-DD>` and open a fresh empty `[Unreleased]` block above it.
4. Tag the release commit with `v<MAJOR>.<MINOR>.<PATCH>`.
5. Publish the release notes by copying the new section into the GitHub
   release page.
