# Design System

## Purpose

This UI is an operational surface, not a marketing page.

The interface should help operators scan, compare, and act quickly.

The default operator-shell pattern is the `Three-Pane Operational
Console`:

- left rail for navigation
- fixed top bar for workspace title and primary actions
- center pane for the active work surface
- right inspector for contextual detail, selection state, and secondary actions

The default interaction rule inside that shell is `inspector-first, overlay-for-actions`.

## Canonical Pattern

### Name

`Three-Pane Operational Console`

### Definition

The shell has four fixed regions:

| Region | Purpose | Collapse behavior |
| --- | --- | --- |
| Top bar | Current workspace title, context breadcrumbs, primary actions, user/session controls | Stays visible. It does not collapse. |
| Left navigation panel | Product navigation and workspace switching | Collapses to icon rail or hidden mobile drawer. |
| Center work surface | The active list, board, form-light workspace, or operational queue | Always remains the primary reading and action area. |
| Right inspector panel | Details for the selected row/card/table/order/job/device | Collapses without losing selection; reopens to the same selected context when possible. |

The shell is selection-driven. The center pane answers "what am I
working on?" The right inspector answers "what did I select?" Modals and
drawers answer "what task did I start?"

### Why This Pattern Fits TabFlow

TabFlow is an operational product. Operators scan, compare, select, and
act under time pressure. A three-pane console keeps those actions in one
place:

- navigation is stable and predictable
- the active work surface stays visible during review
- selected detail appears without a route jump
- create/edit/confirm actions can interrupt deliberately through
  modals or drawers
- the page title and primary action remain visible while panes scroll

This pattern applies to platform console, tenant setup console, floor
and cash workspace, and station manager views. Customer phone surfaces
do not use this shell; they use a focused mobile Static SSR flow.

## Proven Reference Patterns

The TabFlow shell is a synthesis of proven current product patterns, not
a custom novelty.

| Reference | Pattern to learn from | What TabFlow adopts | What TabFlow does not copy |
| --- | --- | --- | --- |
| Figma editor | Left layers/navigation panel, central canvas, right properties panel, top toolbar | Selection-driven right inspector; collapsible side panels; center surface stays primary | Freeform creative canvas and designer-specific tool density |
| Notion workspace | Persistent collapsible left sidebar and page-centered work | Calm workspace framing, collapsible navigation, low-friction page switching | Document-editor looseness for operational tasks |
| PatternFly page and primary-detail patterns | Masthead + sidebar + page sections; primary list/detail side-by-side | Enterprise shell vocabulary, accessible page regions, primary/detail layout for tables and lists | PatternFly's visual styling wholesale |
| GitLab navigation sidebar | Context-specific left navigation with stable header area | Navigation changes by product context while preserving predictable shell position | Repository-specific information architecture |
| Shopify Polaris admin patterns | Consistent page actions, modals for task interruption, disciplined destructive actions | Modal/drawer action discipline and careful confirmation for risky operations | Commerce-specific component vocabulary |
| Stripe Dashboard | Dense operational data with calm hierarchy | Scannable metrics, tables, status, and restrained visual tone | Payment-specific navigation and branding |
| Linear | Fast issue/list workflows with command-like task starts | Speed, keyboard-friendly task initiation, dense list ergonomics | Ambiguous or inconsistent side-panel behavior |

Reference links:

- Figma Help: [layers/sidebar](https://help.figma.com/hc/en-us/articles/360039831974-View-layers-and-assets-in-the-Layers-Panel) and [properties/right sidebar](https://help.figma.com/hc/en-us/articles/360039832014-Design-Prototype-and-view-Code-in-the-Properties-Panel)
- PatternFly: [Page](https://www.patternfly.org/components/page) and [Primary-detail](https://staging-v6.patternfly.org/patterns/primary-detail/design-guidelines/)
- GitLab Docs: [navigation sidebar](https://docs.gitlab.com/development/navigation_sidebar/)
- Shopify Polaris: [Modal](https://polaris-react.shopify.com/components/internal-only/modal) and [common actions](https://polaris-react.shopify.com/patterns/common-actions)

## TabFlow Synthesis

TabFlow's synthesis is:

`Figma-style inspector + PatternFly-style app shell + Polaris-style
action discipline + Notion-style calm navigation`.

The synthesis rules:

- Use a fixed top bar for page identity, context, and the main action.
- Use a collapsible left navigation panel for product areas, not for
  selected-record detail.
- Use the center pane for the durable work surface: tables, queues,
  boards, dashboards, and focused forms.
- Use the right inspector for selected object detail, secondary actions,
  status, and small edits.
- Use drawers for create/edit workflows that need more room than the
  inspector but should preserve page context.
- Use modals for confirmation, destructive actions, and short
  interruption tasks.
- Use full route changes only for standalone workflows, auth pages,
  customer phone flows, and pages that must be linkable as a primary
  destination.
- Keep the same shell across operator modules so a user can learn one
  interaction model.

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
- drawers are preferred for multi-field create/edit flows
- modals are preferred for confirmations, destructive actions, and
  compact task interruption
- confirmations use compact confirmation dialogs
- overlays should make the started task feel visible and in progress
- route changes are reserved for large, multi-step, or standalone workflows

## Inspector Rules

- the inspector shows the currently selected record, its status, and its next useful actions
- inspectors should support quick review and small edits without losing table context
- inspectors are not full forms by default; when a task expands, hand off to a drawer or modal
- dense admin registries should prefer `table + inspector` over `table + route jump`
- an empty inspector explains what to select and may show workspace-level
  hints; it must not duplicate dashboard content
- multiple selection shows a bulk-action state, not the first selected
  record by accident

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

- `Figma` for inspector-first detail handling
- `PatternFly` for app shell and primary-detail layout vocabulary
- `Shopify Polaris` for modal/action discipline
- `Notion` for collapsible navigation and calm workspace framing
- `GitLab` for context-specific left navigation
- `Stripe Dashboard` for data density and operational calm
- `Linear` for fast list workflows and command-like task initiation
