# Design System

## Purpose

This UI is an operational surface, not a marketing page.

The interface should help operators scan, compare, and act quickly.

## Layout Rules

- sidebar for primary navigation
- top header for current workspace and primary action
- cards for summary
- tables for control
- detail views split into summary plus related activity

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
