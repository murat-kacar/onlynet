---
name: Tech Debt
about: Record a known compromise so it lands in the ledger
title: "[Tech Debt] "
labels: ["tech-debt", "needs-triage"]
assignees: []
---

<!--
  This issue produces a row in /doc/buildlog/tech-debt-ledger.md.
  Triage assigns the next free TD-NNNN identifier and either claims
  it (becomes [OPEN]) or leaves it for the next release-gate triage
  ([TRIAGE]) per the constitution Section VII.3.
-->

## Origin

How was this debt discovered? (postmortem, code review, spike, audit,
pair session...). One paragraph.

## Symptom

What is the visible problem today? Include file paths or surface IDs
where relevant.

## Risk If Unpaid

What goes wrong if we never fix this? Be specific: a violated AC, a
missed SLO, a security exposure, a maintenance multiplier.

## Suggested Payoff Plan

How would this be fixed? Outline only — the named owner refines this
when the entry transitions to `[OPEN]`.

## Affected ADRs / ACs / Surfaces

- ADR(s):
- AC(s):
- Surface ID(s):

## Proposed Owner

If you intend to claim this debt, write your handle. Otherwise leave
`TBD` and triage assigns it at the next release gate.
