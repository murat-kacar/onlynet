# Test Taxonomy

This document defines the kinds of automated tests TabFlow uses, what
each is for, and what is **not** appropriate for each tier. It is the
single source of truth for test placement.

The taxonomy exists because constitution Section IV.1 says "tested" is
part of the definition of *Done*. Without a shared meaning of "tested"
contributors and reviewers disagree about whether a change is
finished. This document provides that shared meaning.

## The Four Tiers

TabFlow tests fit into one of four tiers. Every test belongs to exactly
one tier; a test that does not fit a tier is a sign the tier
definitions are wrong, not that the tier should be relaxed.

| Tier | Scope | Speed | Where It Runs |
| --- | --- | --- | --- |
| **Unit** | One class or pure function in isolation | `< 50 ms` per test | Every PR, every push, every editor save |
| **Integration** | A small composition of classes against a real dependency (database, in-process event bus) | `< 2 s` per test | Every PR, every push |
| **End-to-end (E2E)** | A full request flow through a deployed host | `< 30 s` per test | Every PR (smoke subset), every release branch (full) |
| **Smoke** | A handful of critical-path checks against a deployed environment | `< 60 s` total | After every deploy; release-gate verification |

## Tier 1: Unit

A unit test exercises one class (or one pure function) in isolation.
Its purpose is to lock in the **logic** of that unit so that future
edits cannot regress it.

**Belongs in unit:**

- pure functions (calculators, validators, formatters);
- a class's behaviour when its dependencies are replaced with fakes
  written specifically for the test;
- branch coverage of conditional logic;
- error-path coverage for thrown exceptions and returned `Result`
  values.

**Does NOT belong in unit:**

- anything that touches a real database, file system, network, or
  clock;
- anything that requires more than one production class to run;
- any test whose assertion crosses a process boundary;
- any test whose execution time exceeds the budget above.

Project layout: `tests/<Project>.UnitTests/`. One test class per
production class; test method names read as English sentences:
`Submit_RejectsOrder_WhenCartIsEmpty`.

Test doubles are written by hand. We do not use a mocking framework;
hand-written fakes are easier to read in failures and survive
refactoring better.

## Tier 2: Integration

An integration test exercises a small composition of classes against a
**real** instance of one external dependency. Its purpose is to verify
that our wiring against that dependency works the way we think it
does.

**Belongs in integration:**

- repository tests against a real PostgreSQL database (provisioned
  per-test or per-fixture, never shared between unrelated tests);
- EF Core query shape verification (does this LINQ generate the SQL we
  expect, does it use the index);
- in-process event bus tests with real `Channel<T>` plumbing;
- HTTP route handler tests using `WebApplicationFactory<T>` against an
  in-memory test server;
- migration tests that run a migration up, then down, and assert the
  schema is reachable both ways.

**Does NOT belong in integration:**

- tests that require a deployed host, a reverse proxy, or a real
  network — those go to E2E;
- tests that exercise behaviour the unit tier already covers;
- tests longer than the 2 s budget — split or restructure.

Project layout: `tests/<Project>.IntegrationTests/`. Tests use a real
PostgreSQL via the per-developer dev database (matches AD-0007 and
AD-0008) or via Testcontainers when the dev DB is not assumed.

## Tier 3: End-To-End

An E2E test exercises a full request through a deployed host (or a
host launched by the test harness). Its purpose is to verify that the
**system as composed** produces the expected user-visible outcome.

**Belongs in E2E:**

- a customer scanning a QR token, joining a session, adding to cart,
  and submitting an order, all through the public HTTP surface;
- a staff user logging in, updating menu state, and observing the
  customer surface refresh in real time;
- a tenant provisioning flow from `tenant.create` job submission to
  `active` status;
- a device WebSocket lifecycle: connect, push token, receive
  acknowledgement, drop, reconnect.

**Does NOT belong in E2E:**

- coverage of conditional branches inside a single class — that goes
  to unit;
- coverage of EF Core query shape — that goes to integration;
- coverage of every error message — pick one E2E happy path per
  capability and rely on lower tiers for the variants.

Project layout: `tests/E2E/`. Browser flows use [Playwright for
.NET](https://playwright.dev/dotnet/); HTTP-only flows use
`HttpClient` against the test host. The release branch runs the full
E2E suite; PR builds run only the **smoke E2E subset** marked with the
`[Trait("Category", "Smoke")]` attribute.

## Tier 4: Smoke

A smoke test is a real-environment check that runs **after a deploy**
and as part of the release gate. It is not exhaustive; it is the
minimum sanity proof that the deploy did not break the basics.

**Belongs in smoke:**

- platform `/health/ready` returns `200`;
- a representative tenant `/health/ready` returns `200`;
- the customer ordering page renders without server error against a
  test tenant;
- the staff console login page renders without server error;
- an authenticated admin can list tenants;
- the in-process event bus accepts a synthetic event without rejection.

**Does NOT belong in smoke:**

- anything that creates persistent data in production-like
  environments;
- anything that takes longer than the per-test or total budget;
- any check that depends on traffic patterns smoke cannot reproduce.

Smoke is implemented as a small `tests/Smoke/` project executed by the
deploy pipeline against the freshly deployed environment. The
[release-gate document](../../meta/release-gate.md) lists the exact
checks that must pass.

## Cross-Tier Rules

- **Every PR runs unit + integration + smoke E2E.** A PR that touches
  a host runs the full E2E suite for that host before merge.
- **A failing test does not become a TODO.** A failure on `main`
  triggers stop-the-line per constitution VI.1. A failure on a feature
  branch is fixed before merge.
- **No test is skipped.** A `[Skip]` attribute on a merged test is a
  bug; the test either describes a real invariant (in which case fix
  it) or it does not (in which case delete it).
- **Flakes are bugs.** A test that passes 9 of 10 runs is broken. The
  fix is not "rerun the suite"; it is to identify and remove the
  source of non-determinism.
- **Coverage is a side effect.** We do not chase a coverage number.
  We add tests because they describe behaviour we care about; the
  number on the dashboard is a lagging indicator, not a target.

## Test Naming

Tests use English sentence form. The pattern is:

```text
<MethodOrSubject>_<Outcome>_<Condition>
```

Examples:

```csharp
[Fact]
public void Submit_RejectsOrder_WhenCartIsEmpty() { ... }

[Fact]
public void TenantCreate_ReturnsConflict_WhenTenantCodeAlreadyExists() { ... }

[Fact]
public async Task QrJoin_PersistsLanguageCode_WhenAcceptLanguageHeaderIsTurkish() { ... }
```

Per AD-0015, test names — like all identifiers — are English.

## Property-Based Tests

Property-based tests (using FsCheck or Hedgehog) describe an invariant
that holds across many randomly generated inputs. They live with the
unit tier when they exercise pure logic, with the integration tier
when they exercise a database round-trip.

A property-based test is preferred over a parameterised test when the
input space is large enough that hand-picked examples leave gaps. It
is **not** a substitute for explicit edge-case tests; both coexist.

## Mutation Tests

Mutation testing (e.g. via Stryker.NET) is run quarterly, not on every
PR. Its purpose is to catch tests that pass but do not actually verify
the behaviour they claim to verify. Mutation-test findings open
tech-debt ledger entries when they reveal a class with weak coverage.

## Related

- [`../../constitution.md`](../../constitution.md) Section IV.1 —
  "tested" as part of *Done*
- [`../../meta/release-gate.md`](../../meta/release-gate.md) — release
  gate verification
- [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
  AD-0014 — coding standards (analyzer rules apply to test projects)
- [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
  AD-0015 — English-first identifiers (test names included)
