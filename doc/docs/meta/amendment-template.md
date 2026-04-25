# Amendment Template

This is the canonical PR shape for amending the constitution
([`../constitution.md`](../constitution.md)) or the documentation
charter ([`./documentation-charter.md`](./documentation-charter.md)).
Both documents require a PR that follows this format.

Copy the body below into the PR description and fill in the bracketed
sections.

---

```markdown
# Amendment: <one-line summary>

**Document affected:** `[constitution.md | documentation-charter.md]`
**Section / rule numbers touched:** `[e.g. II.3, III.5, or "ADR Status Lifecycle"]`
**Type:** `[add | rewrite | remove]`

## Motivating Experience

A concrete event, postmortem, or recurring friction that this
amendment addresses. One paragraph. If a postmortem exists, link it
([`/doc/buildlog/postmortems/...`](/doc/buildlog/)).

Without this section the amendment is rejected. We do not change
fundamental rules on aesthetic grounds.

## Current Wording

Quote the exact lines being changed, with section numbers, so a
reviewer can compare side by side. Use a fenced block.

```text
[paste current text here]
```

## Proposed Wording

The exact replacement text. Use a fenced block.

```text
[paste new text here]
```

## Why This Wording

Two or three sentences explaining why this specific wording is
durable, measurable, and not too narrow / too broad.

## Knock-On Effects

List every file that needs to be updated to keep the rule consistent
across the documentation tree:

- [ ] `path/to/file.md` — [what changes]
- [ ] `path/to/another.md` — [what changes]

Self-consistency check (see
[`./contributing.md`](./contributing.md#self-consistency)):

- [ ] surface IDs unaffected or updated
- [ ] ADR list unaffected or updated
- [ ] AC list unaffected or updated
- [ ] SLI list unaffected or updated
- [ ] capability matrix unaffected or updated
- [ ] glossary unaffected or updated
- [ ] release gate unaffected or updated

## Reviewer Checklist

The amendment merges only when every active maintainer has approved.
The PR description MUST contain one approval line per maintainer:

- [ ] @maintainer-1 — approved
- [ ] @maintainer-2 — approved
- [ ] ...

A maintainer who disagrees records the disagreement in a review
comment, not in code that does the opposite (constitution I.3).
```

---

## When To Use

Use this template for any PR that:

- adds, rewrites, or removes a numbered rule in the constitution
- adds, rewrites, or removes a tree, lifecycle policy, or migration
  rule in the documentation charter
- changes the ADR status taxonomy in the charter
- changes the rules in `docs/meta/contributing.md`,
  `docs/meta/review-policy.md`, or `docs/meta/release-gate.md`

Other documentation edits do not need this template; they follow
[`./contributing.md`](./contributing.md).

## Why The Heavy Format

Constitution and charter changes are one-way doors (constitution I.1).
Once a rule is loosened, the discipline it protected is also loosened.
The heavy format is a deliberate friction that forces the question
"does the experience justify a fundamental change?" to be answered
explicitly, in writing, before the merge button is pressed.
