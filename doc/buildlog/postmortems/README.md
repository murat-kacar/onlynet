# Postmortems

Append-only record of production and preview incidents.

This subtree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#buildlog--lessons-learned)
and the postmortem skeleton in
[`/doc/buildlog/README.md`](../README.md#format-guide).

## Filename Format

```
YYYY-MM-DD-<short-slug>.md
```

`YYYY-MM-DD` is the date the incident **started**, not the date the
postmortem was written. The slug is a noun phrase describing the
incident (e.g. `2026-05-01-platform-db-outage.md`), all-lowercase,
hyphen-separated.

## Append-Only Rule

Postmortems are **never edited after merge** except for typo fixes,
and **never deleted**. When new evidence supersedes an earlier
finding, publish a follow-up postmortem (or a retrospective in
`/doc/buildlog/retrospectives/`) that links the original. The
original stays.

The contributor who lands the change at the centre of an incident is
the first responder per [`/doc/docs/constitution.md`](/doc/docs/constitution.md)
Section VI.2; the postmortem is blameless and names the failure mode,
not a person, per VI.3.

## What Goes Here

- Production incidents reaching customer or operator surfaces
- Preview-environment incidents that would have reached production
- Release-gate failures that required rollback
- Security incidents (handle disclosure separately, but the
  postmortem still lands here)

## What Does NOT Go Here

- Build-fix or test-flake notes (those belong on the PR or as a
  retrospective in `/doc/buildlog/retrospectives/`)
- Active incident dashboards (incident response uses the team channel
  and the on-call runbook; the postmortem is the *artefact* that
  follows resolution)
- Personal blame, credentials, or customer PII

## Status Today

TabFlow has no incidents to postmortem. This file is a stub README
that activates the moment the first incident closes.

## Cross-References

- Constitution VI.3 — postmortem requirement
- Charter `buildlog/` section — append-only lifecycle
- [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md#breach-notification)
  — KVKK/GDPR breach response writes a postmortem within 5 working
  days
- [`/doc/docs/how-to/configure-branch-protection.md`](/doc/docs/how-to/configure-branch-protection.md#recovering-from-disabled-protection)
  — disabled-protection recovery requires a postmortem
- [`/doc/docs/meta/amendment-template.md`](/doc/docs/meta/amendment-template.md#motivating-experience)
  — amendment PRs cite postmortems as motivating experience
