# Frontend Rewrite Charter

## Purpose

This document defines how IoTable frontend rewrites happen.

The default posture is radical replacement, not patchwork extension.

## Rewrite Rule

When an operator surface is structurally weak, we replace it.

We do not preserve weak UI just because it already exists.

The target is a believable `v1.0.0` product surface, not a repaired pre-release.

## What We Preserve

The rewrite keeps these layers unless there is a stronger architectural reason to replace them:

- domain model
- application services
- authentication and authorization contracts
- localization infrastructure
- HTTP endpoint contracts
- persistence model and migrations

## What We Replace Freely

The rewrite may replace these layers without hesitation:

- layouts and shell chrome
- page composition
- visual tokens and CSS architecture
- component primitives
- inspector, drawer, and modal behavior
- route-level operator workflows

## Core Product Rule

`Three-Pane Operational Console: inspector-first, overlay-for-actions`

That means:

- operator modules share a fixed top bar, collapsible left navigation,
  center work surface, and collapsible right inspector
- selection-driven work defaults to the right inspector
- task-starting work defaults to a modal or drawer
- route changes are reserved for large, standalone workflows

## Rewrite Quality Bar

An operator screen is not done when it merely works.

It is done when:

- the information hierarchy is obvious at first glance
- the main action is visible without hunting
- the page feels intentionally composed, not accumulated
- the screen matches the shared console shell
- the result feels like the first real release, not a patched draft

## Delivery Shape

Each rewrite slice follows this order:

1. foundation
2. one real module
3. review
4. extend

Foundation means:

- shell
- spacing and typography rhythm
- navigation
- inspector
- drawer and modal primitives
- loading, empty, and error states

## Anti-Patterns

Avoid these:

- patching a broken page until it becomes harder to reason about
- mixing old and new layout systems in the same operator surface
- duplicating navigation inside dashboards
- hiding a started task behind a route jump when an overlay would keep context
- preserving legacy UI just to avoid reimplementation work

## Rewrite Sequence

The default rewrite sequence is:

1. platform shell and operator foundation
2. platform modules
3. tenant shell and operator foundation
4. tenant modules
5. customer-facing public surfaces

## Success Condition

The rewrite succeeds when new screens feel coherent enough that a user cannot tell which module was built first.
