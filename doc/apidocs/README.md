# TabFlow API Documentation

Authoritative integration reference for **external developers** who
integrate with TabFlow's public contracts.

This tree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#apidocs--external-integration-reference).

## Subtrees

| Subtree | Content | Status |
| --- | --- | --- |
| `rest/` | Public REST API reference (generated from OpenAPI) | Stub |
| `websocket/` | Public WebSocket protocol reference | Stub |
| `webhooks/` | Outbound webhook payloads and signing | Stub |

## What Goes Here

- Endpoint catalogues
- Request/response schemas
- Authentication flows for external integrations
- Rate-limit semantics
- Webhook signing and replay protection
- Versioning and deprecation policy

## What Does NOT Go Here

- Internal endpoints — those are `/doc/docs/reference/api/tenant-api.md`
- Control-plane APIs — those are `/doc/docs/reference/api/`
- Implementation notes — those are `/doc/docs/explanation/`
- The device WebSocket (ESP32 firmware contract) — that is firmware
  reference, not external API; lives at `/doc/docs/reference/firmware.md`

## Status Today

TabFlow has no public API. This tree is a stub. The first content lands
the moment a public endpoint exists and is documented in
`/doc/docs/reference/architecture/decisions.md` as an accepted ADR.

When the tree activates, it MUST follow:

- semantic versioning of the public contract
- a deprecation policy stated in the same ADR
- code samples in at least one language per public surface
