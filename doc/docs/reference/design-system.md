# Design System

## Purpose

This UI is an operational surface, not a marketing page.

The interface should help operators scan, compare, and act quickly.

The default operator-shell pattern is a `Three-Pane Console Shell`:

- left rail for navigation
- fixed top bar for workspace title and primary actions
- center pane for the active work surface
- right inspector for contextual detail, selection state, and secondary actions

The default interaction rule inside that shell is `inspector-first, overlay-for-actions`.

## Layout Rules

- sidebar for primary navigation
- top header for current workspace and primary action
- right inspector for details, selection state, and secondary actions
- cards for summary
- tables for control
- detail views split into summary plus related activity
- nav can collapse; the header stays fixed
- the inspector can collapse without removing the active workspace
- routes should reuse the shared shell rather than invent page-specific chrome

## Interaction Rules

- selection-driven work defaults to the right inspector
- task-starting work defaults to an overlay
- full-route transitions are reserved for large, multi-step, or standalone work
- dashboards expose health, attention, and summary; they do not repeat navigation
- a button should make the started task visible immediately

## Hierarchy Rules

- `eyebrow` for section context
- `h1` for page intent
- metrics before prose
- status before explanation

## Copy Rules

- short labels
- short helper text
- no decorative paragraphs
- empty states should explain the next useful step

## Status Rules

- `success` for healthy, active, completed
- `warning` for in-progress, claimed, attention-needed
- `danger` for failed or blocked
- `neutral` for informational state only

## Table Rules

- first column carries identity
- status must be scannable without reading the full row
- actions belong at the far right or in the page header
- row selection should populate the inspector when possible
- long-form detail should move out of the main table and into the inspector or an overlay

## Overlay Rules

- create and edit flows default to modal or drawer presentation
- confirmations use compact confirmation dialogs
- overlays should make the started task feel visible and in progress
- route changes are reserved for large, multi-step, or standalone workflows

## Inspector Rules

- the inspector shows the currently selected record, its status, and its next useful actions
- inspectors should support quick review and small edits without losing table context
- inspectors are not full forms by default; when a task expands, hand off to a drawer or modal
- dense admin registries should prefer `table + inspector` over `table + route jump`

## Motion Rules

- subtle entrance only
- no decorative looping motion
- motion must reinforce hierarchy, not distract from it

## Token Seeds

- radius: `8px`
- control height: `40px`
- compact control height: `32px`
- panel shadow: soft, low contrast
- spacing rhythm: `8 / 12 / 16 / 24 / 32`

## Reference Patterns

- `Stripe Dashboard` for data density and operational calm
- `Figma` for inspector-first detail handling
- `Notion` for collapsible navigation and workspace framing
