# Accessibility

TabFlow treats accessibility as a baseline requirement ("table stakes"),
not as a ranked concern. This document is the baseline.

The normative acceptance criteria corresponding to this document live
in [`../../reference/acceptance-criteria.md`](../../reference/acceptance-criteria.md)
under the **Accessibility** section (AC-110 to AC-116).

## Target Standard

TabFlow targets **[WCAG 2.2 Level AA][wcag22]** conformance across
every HTML surface. Deployments also carry accessibility obligations
under the applicable jurisdiction; those are the operator's
responsibility and are not repeated here.

[wcag22]: https://www.w3.org/TR/WCAG22/

## Baseline Requirements

The following requirements are the non-negotiable baseline. They apply
to every HTML surface TabFlow serves (customer Static SSR, staff
Interactive Server, platform admin console).

### Keyboard

- Every interactive control MUST be reachable and operable with
  keyboard only, without a pointing device.
- Tab order MUST follow visual and logical reading order.
- Focus indication MUST be visible with at least 3:1 contrast against
  the adjacent background.
- Focus MUST NOT be trapped inside a component except for modal
  dialogs that explicitly implement `role="dialog"` with proper focus
  management.

### Colour And Contrast

- Body text MUST meet WCAG 2.2 AA contrast: 4.5:1 for normal text,
  3:1 for large text (≥ 18 pt regular or ≥ 14 pt bold).
- UI components and graphical objects that carry meaning MUST meet
  3:1 contrast against their adjacent backgrounds.
- Colour MUST NOT be the sole carrier of meaning. Station urgency
  bands, order state badges, and error highlights carry a text label
  or icon in addition to colour.

### Screen Readers And Semantic Markup

- Every form control MUST have a programmatically associated label
  (`<label for="...">` or `aria-labelledby`).
- Every page MUST declare its language (`<html lang="...">`) and title
  (`<title>`) appropriate for the surface.
- Landmark regions MUST be used (`<main>`, `<nav>`, `<header>`,
  `<footer>`) so screen readers can navigate by landmark.
- Decorative images MUST use `alt=""` or be marked `aria-hidden="true"`.
- Informative images and icons that stand alone MUST carry a text
  alternative.
- Mermaid and other rendered diagrams MUST be preceded or followed by
  a textual description that conveys the same information; the
  diagram itself is not a substitute.

### Motion And Timing

- Session expiry and customer session end MUST give visible,
  text-based notice; timers MUST NOT silently invalidate work.
- Animations and transitions that are longer than 5 seconds or
  auto-start MUST respect `prefers-reduced-motion`.
- The station board's urgency rendering MUST NOT rely on blinking or
  flashing at a frequency between 2 Hz and 55 Hz (seizure risk).

### Internationalisation

- All user-facing strings MUST be externalised for localisation (ASP.NET
  Core `IStringLocalizer`). Right-to-left locales MUST render the full
  customer journey correctly.
- Numeric formatting (prices, quantities) MUST respect the tenant
  `cultureCode`; date and time MUST respect `timeZone`.

## Staff-Surface Notes

Staff surfaces (T-06..T-16) are used long-form on shared cafe hardware.
In addition to the baseline:

- Touch targets MUST be at least 44 CSS pixels on each side (WCAG 2.2
  Target Size, Level AA).
- The floor and cash workspace MUST NOT require horizontal scrolling
  at a viewport width of 1024 px.
- Station board text MUST stay legible at the distance the board is
  typically mounted (guidance: readable at 2 m without magnification
  on 1080p displays).

## Customer-Surface Notes

Customer surfaces run on personal phones over mobile data:

- Surfaces MUST reflow to a 320 CSS pixel viewport without loss of
  content or function.
- The menu and cart pages MUST function without JavaScript beyond
  Blazor's enhanced navigation and forms; no interactivity-only dead
  ends.
- QR scan errors MUST be communicated in text, not only through icons.

## Verification

The release gate
([`../../meta/release-gate.md`](../../meta/release-gate.md)) requires:

- Automated WCAG 2.2 AA checks against a representative set of
  surfaces: customer menu, floor and cash workspace, one station
  board, platform admin console.
- Manual keyboard walkthrough on the customer menu, login, and order
  submission path.
- Contrast verification on brand colours against the role-accent
  palette of each station.

Any WCAG 2.2 AA failure surfaced by automated checks blocks a release
tag unless explicitly tracked as a deferred finding with a remediation
PR on the roadmap.

## What Is Not In Scope (yet)

- WCAG 2.2 **Level AAA** conformance. It may land per surface over
  time; the baseline commitment is Level AA.
- Full VoiceOver/NVDA/JAWS script-level test coverage beyond the
  automated WCAG 2.2 AA run. Manual screen-reader passes are scheduled
  per-surface, not per-release, until test coverage grows.
- Offline accessibility. TabFlow surfaces are online-first; see AD-0004
  for the render-mode decision that makes this explicit.

## Related

- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
  — the Web Indexing Posture section where accessibility priority lives
- [`../../reference/acceptance-criteria.md`](../../reference/acceptance-criteria.md)
  — AC-110 to AC-116 enumerate the verifiable items
- [`../../meta/release-gate.md`](../../meta/release-gate.md) — release
  gate accessibility section
