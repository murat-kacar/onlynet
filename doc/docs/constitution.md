# TabFlow Constitution

This is the smallest set of rules every contributor — engineer, operator,
designer, reviewer — agrees to follow when working on TabFlow. It is
deliberately short and operational. Where a rule would be unmeasurable, it
is omitted.

The constitution defines **how we decide, implement, ship, document, verify,
review, operate, and measure**. It does not duplicate the architecture;
technical decisions live in
[`reference/architecture/decisions.md`](./reference/architecture/decisions.md).

Defined terms used below (`spike`, `tracer bullet`, `one-way door`,
`tech debt`, `architectural change`, `DORA metrics`, `stop the line`,
`Done`) live in [`reference/glossary.md`](./reference/glossary.md).

## I. Decisions

1. **Classify every decision as one of two doors.**
   - **One-way door** — reversal is expensive or destructive (schema
     migration on production data, public API contract, persistent file
     layout, cross-tenant boundary, irreversible data deletion). Discuss,
     write an ADR, take a peer review.
   - **Two-way door** — reversal is cheap (an internal helper, an
     un-released UI tweak, a build script). Try it, measure, revert if
     wrong.
2. **When in doubt, treat the decision as one-way.** A one-way door
   discussion is cheap; an unrecoverable rollback is not.
3. **Once a decision is made, everyone implements it.** Disagreement is
   recorded in the ADR, not in code that does the opposite.
4. **An irreversible change MUST be justified explicitly in its ADR.**
   The ADR states why the operation cannot be reversed in one operator
   step and what mitigations are in place.
5. **Default to proven, conventional, and current practice.** When
   choosing a method, pattern, structure, workflow, or wording,
   contributors start from approaches that are broadly proven, current,
   and conventionally accepted for the problem space. Local preference,
   novelty, or cleverness needs an explicit reason. The burden of proof
   is on the deviation, not on the convention.
6. **Check the strongest nearby examples before committing.** A
   decision is not justified only because it works locally.
   Contributors compare against framework guidance, repository
   contracts, and nearby product surfaces, and prefer the simpler,
   clearer, more coherent, or better-proven shape unless there is a
   documented reason not to.
7. **Global fit matters, not just local success.** A change that works
   in isolation but damages product coherence, contract clarity,
   operational consistency, or long-term maintainability is the wrong
   change.

## II. Progress

1. **Unknown territory begins with a time-boxed spike.** A spike has a
   stated question, a budget (hours or days), and an expected artefact
   (an ADR, a tracer-bullet branch, or a documented "no"). Spike
   outcomes that don't become an ADR are recorded in
   [`/doc/buildlog/spikes/`](/doc/buildlog/) so the question and answer are
   preserved.
2. **New capability ships as a tracer bullet first.** A tracer bullet is
   the thinnest end-to-end slice that exercises the real path — real
   request, real database, real event, real test. Hardening (error
   paths, observability, performance, accessibility) follows once the
   slice works end-to-end.
3. **Temporary work is technical debt and is visible.** Anything labelled
   "temporary" is recorded in
   [`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md)
   with an owner, a payoff plan, and a `TD-NNNN` identifier. The phrase
   "we'll fix it later" without a ledger entry is forbidden.
4. **"Done" means tested, observable, and documented.** All three. None
   is optional. Capability status in
   [`reference/architecture/capability-matrix.md`](./reference/architecture/capability-matrix.md)
   honours the same definition.

## III. Documentation

The shape, audience, and lifecycle of every documentation tree are
defined by [`./meta/documentation-charter.md`](./meta/documentation-charter.md).
This section states the rules that apply across **all** trees.

1. **Documentation reflects reality.** When code and docs disagree, treat
   it as a documentation bug first; investigate before changing the code.
2. **Architectural change lands in docs first or alongside the code,
   never after.** *Architectural change* means any change to a public
   contract (HTTP route, event payload, DB column, config key, runtime
   surface, accepted ADR). A PR that changes a contract without
   updating the tree-of-record is incomplete.
3. **One fact, one place.** Each fact lives in exactly one tree (the
   charter names which one). Other trees link to it; they do not
   duplicate it.
4. **Stable contracts have a deprecation path.** Breaking a stable
   reference (API route, DB column, config key, public guide) requires
   a deprecation window stated in the same change that introduces the
   replacement.
5. **Decision rationale is not historical narrative.** "Why we chose X"
   is a current contract and stays in `docs/`. "We used to do Y" is
   biography and is deleted. A `Status: Superseded` ADR is the only
   acceptable form of preserved decision evolution.
6. **The charter governs tree boundaries.** Adding, removing, or
   redefining a tree is a charter amendment. See
   [`./meta/documentation-charter.md`](./meta/documentation-charter.md#amendment).
7. **Documentation prefers the clearest proven form for its audience.**
   Documentation does not optimise for novelty, personality, or private
   structure. It optimises for conventional form, current best
   practice, cross-document consistency, recognisable sectioning, and
   fast comprehension by the intended reader.
8. **A correct document with the wrong shape is still low quality.**
   If a simpler, more standard, more recognisable structure would serve
   the audience better, that structure wins.

## IV. Quality

1. **Every invariant has a test.** If a behaviour is not exercised by an
   automated test, it is not an invariant — it is a wish.
2. **Every capability is observable in production.** Without a metric, a
   log, or a trace, you cannot tell whether the capability is working.
3. **Implementation follows proven framework and engineering practice.**
   ASP.NET Core, EF Core, Blazor, PostgreSQL, and adjacent tooling have
   idiomatic patterns; we use them. Contributors prefer solutions that
   are clean, cohesive, well-factored, proportionate to the problem,
   and aligned with stronger nearby examples. Unnecessary duplication,
   indirection, hidden coupling, speculative abstraction, and avoidable
   complexity are quality failures even when the code works.
4. **Browser-side custom logic is TypeScript-first.** When TabFlow
   needs custom browser code beyond framework-provided scripts,
   contributors write it in TypeScript by default. Node-based frontend
   tooling targets the current Node 24 LTS line and uses `pnpm` as the
   package manager by default. Small, trivial snippets may remain
   JavaScript, but new non-trivial browser logic does not start as ad
   hoc plain JS.
5. **Local correctness is insufficient without system fit.** A change
   is not complete just because it works. It must also agree with the
   surrounding contracts, naming, runtime surfaces, architecture
   decisions, and operational model.

## V. Review

1. **Every change lands via pull request.** Direct pushes to `main` are
   prohibited.
2. **Every PR has at least one reviewer who is not the author.** Solo
   merges are reserved for build-fix and stop-the-line situations
   declared in the PR body.
3. **One-way door PRs require an ADR review.** A reviewer who is
   unfamiliar with the surrounding decision MUST request the ADR before
   approving.
4. **Security-sensitive changes require a security review.** This
   includes changes to authentication, authorization, secrets handling,
   data-protection (PII, payment, audit), and the public HTTP surface.
   The reviewer notes "security: reviewed" in the PR.
5. **Review policy details (what reviewers check, escalation paths)
   live in** [`./meta/review-policy.md`](./meta/review-policy.md).
6. **Review checks method quality, not only outcome correctness.**
   Review asks whether the chosen approach is proven, current,
   conventional, maintainable, and globally well-matched — not merely
   whether it can be made to pass today.

## VI. Operations

1. **Stop the line.** When `main` is broken — build red, smoke test red,
   release-gate red, or a production incident is open — no contributor
   starts new work; the team converges on the fix until `main` is green
   again.
2. **You build it, you run it.** The contributor who lands a change is
   the first responder for the resulting incident, until ownership is
   formally transferred.
3. **Every incident produces a blameless postmortem** in
   [`/doc/buildlog/postmortems/`](/doc/buildlog/). The postmortem names the
   failure mode, not a person, and produces follow-up work tracked to
   completion (often as a tech-debt-ledger entry).
4. **Dependencies are costed.** Every external package, framework, or
   service is a maintenance liability. New dependencies need an ADR
   sentence: *what does this give us that we cannot do in three days*?
5. **Production access is minimal.** Read access is the default; write
   access is logged and reviewed.

## VII. Measurement

1. **DORA metrics are the heartbeat.**
   - Deployment frequency
   - Lead time from commit to production
   - Change failure rate
   - Mean time to recovery

   These are reported at every release-gate review (see
   [`./meta/release-gate.md`](./meta/release-gate.md)) and revisited at
   every postmortem. A trend in the wrong direction is a release-gate
   concern, not a vibe.
2. **SLO breach is a release-gate failure.** SLOs live in
   [`reference/architecture/slos.md`](./reference/architecture/slos.md).
   A regression there blocks release, the same way a failing test does.
3. **The tech debt ledger is reviewed at the same cadence as the
   release gate.** New entries are triaged, stale `[OPEN]` entries are
   either claimed or escalated. The ledger lives at
   [`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md).

## VIII. Working Modes

1. **Every work item declares its working mode.** The primary mode is one of
   `documentation`, `implementation`, or `review`. Secondary modes are listed
   when the same work item intentionally crosses modes. A work item can change
   modes, but the handoff is explicit in the issue, PR, ADR, or tech-debt
   ledger entry.
2. **Documentation mode starts with the charter's decision test and the
   conventional-fit test.** The contributor identifies the audience,
   tree-of-record, lifecycle, and the clearest proven form for that
   document before writing. If a simpler, more standard, more
   recognisable structure would communicate better, that structure
   wins.
3. **Implementation mode starts from the governing contract and the
   best-fit implementation shape.** The contributor checks the relevant
   ADR, reference document, acceptance criterion, capability-matrix
   row, tech-debt entry, framework guidance, and nearby repository
   examples before changing code. If a simpler or more conventional
   shape exists, it is preferred unless the deviation is justified
   explicitly.
4. **Review mode starts with risk and method fit.** The reviewer leads
   with correctness, security, contract drift, missing tests, missing
   observability, missing documentation, untracked technical debt,
   unnecessary novelty, avoidable complexity, and mismatch with proven
   or conventional practice before style or preference.
5. **Mode output is reviewable.** Documentation mode produces a source-of-truth
   document or a documented deletion. Implementation mode produces code plus
   the tests, observability, and documentation required by `Done`. Review mode
   produces actionable findings or an explicit approval against the review
   policy.

## Scope

This constitution governs **process and culture**. It does not govern:

- Specific architecture choices — see
  [`reference/architecture/decisions.md`](./reference/architecture/decisions.md).
- Specific runtime contracts — see `reference/`.
- Specific operational procedures — see `how-to/`.
- Specific review checks — see [`./meta/review-policy.md`](./meta/review-policy.md).
- Specific documentation tree boundaries — see
  [`./meta/documentation-charter.md`](./meta/documentation-charter.md).

When this document conflicts with a more specific document, the more
specific document wins, and a follow-up PR reconciles the constitution.

## Amendment

The constitution is amended by a PR that:

1. Adds, removes, or rewrites a numbered rule.
2. Cites the concrete experience or postmortem that motivated the change.
3. Is reviewed by every active maintainer.

Amendment is the only way the constitution changes. Implicit drift is not
accepted; if a rule is being routinely ignored, that is a constitution
bug, fixed by amendment, not by silence.
