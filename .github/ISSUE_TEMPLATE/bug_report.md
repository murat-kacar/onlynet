---
name: Bug Report
about: Report a defect in TabFlow that does not look like a security issue
title: "[Bug] "
labels: ["bug", "needs-triage"]
assignees: []
---

<!--
  Security issues do NOT belong here. Report them privately per
  /SECURITY.md.
-->

## Summary

One sentence describing what is wrong.

## Working Mode

Primary mode expected after triage:

- [ ] Documentation
- [ ] Implementation
- [ ] Review

Secondary modes, if already known:

- [ ] Documentation
- [ ] Implementation
- [ ] Review

## Affected Surface

- [ ] Platform host
- [ ] Tenant host
- [ ] Platform worker
- [ ] Firmware / device
- [ ] Documentation
- [ ] Build / CI

Surface ID (if known, e.g. `T-13`): 

## Version

- Commit SHA or tag:
- Environment (local / staging / production):

## Steps To Reproduce

1.
2.
3.

## Expected Behaviour

What should happen.

## Actual Behaviour

What actually happens. Include error messages, log snippets, or
screenshots where relevant. Strip any secrets before pasting.

## Logs / Diagnostics

```text
[paste relevant log lines]
```

## Possible Acceptance Criterion

If this bug points at a missing invariant, propose the AC text. The
maintainer adds it to
[`/doc/docs/reference/acceptance-criteria.md`](../../doc/docs/reference/acceptance-criteria.md)
during triage.

## Notes

Anything else useful (workarounds, related issues, hypotheses).
