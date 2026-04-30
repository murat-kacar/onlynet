# TabFlow User Documentation

Step-by-step help for the people who **use** TabFlow.

This tree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#userdocs--end-user-help).

## Personas

Each persona will have its own subfolder when that persona's first stable
walkthrough ships. Content is written in the language of the persona, not
engineering language.

| Persona | Folder | Status |
| --- | --- | --- |
| Cafe owner | `owner/` | Stub |
| Manager | `manager/` | Stub |
| Cashier | `cashier/` | Stub |
| Station operator | `station/` | Stub |
| Customer (diner) | `customer/` | Stub |

## What Goes Here

- "How do I close a bill?"
- "How do I add a menu item?"
- "Why is my QR code not working?"
- Per-persona walkthroughs with screenshots
- Per-persona FAQs

## What Does NOT Go Here

- Database schema, API specs, deployment procedures (those go to `/doc/docs/`)
- Internal hostnames, operator contact info, implementation noise
- Engineering language ("the platform host", "the EF Core context")

## Status Today

TabFlow v1.0.0 has no end-user walkthrough set yet. This tree
intentionally contains only the persona structure and activation rules;
each persona folder is added when that persona's stable workflow is
ready to document.
