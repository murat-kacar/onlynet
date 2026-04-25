# Security Policy

This document describes how to report security vulnerabilities in TabFlow
and what response to expect.

## Supported Versions

Security fixes are produced for the **current major** version on `main`.
Older majors receive fixes only during their stated deprecation window
(see [`/doc/docs/reference/api/README.md`](/doc/docs/reference/api/README.md#governance)).

## Reporting A Vulnerability

**Do not open a public GitHub issue.** Vulnerability reports are handled
privately so users have time to upgrade before details become public.

Send the report to **security@\<tabflow-domain\>** with:

- a clear description of the vulnerability
- the affected component (platform host, tenant host, platform worker,
  firmware, migration project)
- the version or commit SHA where it reproduces
- a minimal proof of concept if one is safe to share
- your preferred contact for follow-up

If GPG is available, the public key for `security@\<tabflow-domain\>`
will be published in the repository's GitHub Security tab.

## What To Expect

| Stage | Target Time | Owner |
| --- | --- | --- |
| Initial acknowledgement | within 2 working days | Maintainer on rotation |
| Severity assessment + reproduction | within 5 working days | Maintainer + reporter |
| Fix scoped, ADR if architectural | within 10 working days for high/critical | Maintainer |
| Fix released | depends on severity (see below) | Maintainer |
| Public disclosure | 90 days after report **or** 7 days after fix release, whichever is sooner | Maintainer + reporter coordination |

## Severity And Release Cadence

| Severity | Examples | Release Track |
| --- | --- | --- |
| Critical | RCE, auth bypass, cross-tenant data leak, payment data exposure | Out-of-band patch; previous major also patched if within deprecation window |
| High | Privilege escalation within a tenant, persistent XSS on staff surfaces, secret leakage via logs | Next scheduled minor release; backport to current major |
| Medium | Local information disclosure, denial-of-service via known-bad input | Next scheduled minor release |
| Low | Theoretical issue without realistic exploit, configuration smell | Next minor release; may bundle into a hardening sprint |

## Coordinated Disclosure

We follow coordinated disclosure. Reporters who follow this policy are
credited in the published advisory (CVE record + repository security
advisory) unless they request anonymity.

We do not pursue legal action against good-faith security researchers
who:

- Make a reasonable effort to avoid privacy violations, destruction of
  data, or interruption of service.
- Only interact with accounts they own or with explicit permission of
  the account holder.
- Do not exploit a vulnerability beyond the minimum necessary to
  confirm its presence.
- Provide reasonable time for the maintainers to fix the issue before
  publishing details.

## Out Of Scope

- Findings from automated scanners without a working exploit.
- Issues requiring privileged access already on the affected host.
- Reports against deprecated and end-of-life versions.
- Social engineering of TabFlow operators.
- Physical attacks against deployed hardware (table devices, station
  printers).

## Related Documents

- [`/doc/docs/meta/review-policy.md`](/doc/docs/meta/review-policy.md) —
  security review trigger for incoming PRs
- [`/doc/docs/constitution.md`](/doc/docs/constitution.md) Section V —
  review rules
- [`/doc/buildlog/postmortems/`](/doc/buildlog/) — postmortems for
  resolved security incidents
