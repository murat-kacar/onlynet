# TabFlow API Documentation

Authoritative integration reference for **external developers** who
integrate with TabFlow's public contracts.

This tree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#apidocs--external-integration-reference).

## Planned Subtrees

| Subtree | Content | Status |
| --- | --- | --- |
| `rest/` | Public REST API reference (generated from OpenAPI) | Created when the first public REST API exists |
| `websocket/` | Public WebSocket protocol reference | Created when the first public WebSocket exists |
| `webhooks/` | Outbound webhook payloads and signing | Created when the first webhook exists |

## What Goes Here

- Endpoint catalogues
- Request/response schemas
- Authentication flows for external integrations
- Rate-limit semantics
- Webhook signing and replay protection
- Versioning and deprecation policy

## What Does NOT Go Here

- Internal endpoints — those are `/doc/docs/reference/api/internal-api.md`
- Tenant customer endpoints and device WebSocket — those are
  `/doc/docs/reference/api/tenant-api.md`
- Control-plane APIs — those are `/doc/docs/reference/api/internal-api.md`
- Implementation notes — those are `/doc/docs/explanation/`
- The device WebSocket (ESP32 firmware contract) — that is firmware
  reference, not external API; lives at `/doc/docs/reference/firmware.md`

## Status Today

TabFlow v1.0.0 has no external developer API. This tree intentionally
contains only the activation rules for future public integration
contracts. A concrete public API subtree is added when an accepted ADR
defines that contract.

When the tree activates, it MUST follow:

- semantic versioning of the public contract
- a deprecation policy stated in the same ADR
- code samples in at least one language per public surface
