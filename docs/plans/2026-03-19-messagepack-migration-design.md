# MessagePack Migration Design

**Date:** 2026-03-19
**Status:** Approved
**Scope:** Replace all JSON serialization with MessagePack + LZ4 compression

## Decision

Migrate all serialization from System.Text.Json to MessagePack-CSharp using
`ContractlessStandardResolver` with `Lz4BlockArray` compression. No domain type
annotations required — serialization remains an infrastructure concern.

## Approach: Contractless + LZ4

- **ContractlessStandardResolver** — serializes by property name, zero attributes needed
- **Lz4BlockArray compression** — ~60-70% smaller than JSON payloads
- **UntrustedData security** — hardened deserialization for defense-in-depth
- **No existing data** — clean cut, no backward compatibility or migration scripts

## Architecture

### Shared Options (Single Source of Truth)

```
FarmOS.SharedKernel/Infrastructure/MsgPackOptions.cs
  → ContractlessStandardResolver + Lz4BlockArray + UntrustedData
```

All layers reference this one static options instance.

### Layer 1 — Event Store (ArangoDB)

EventEnvelope.Payload stays `string` but now holds Base64-encoded MessagePack bytes.
ArangoDB stores documents as JSON natively, so the envelope metadata (AggregateId,
EventType, Version, etc.) remains JSON-readable in the ArangoDB web UI. Only the
domain event payload is opaque binary.

Serialize: `event → MessagePack bytes → Base64 string → ArangoDB`
Deserialize: `ArangoDB → Base64 string → MessagePack bytes → event`

### Layer 2 — RabbitMQ Event Bus

Raw MessagePack bytes on the wire. ContentType: `application/x-msgpack`.
No Base64 encoding — RabbitMQ handles binary natively.

### Layer 3 — Domain Event Stores (8 Bounded Contexts)

Each `*EventStore.cs` swaps `JsonSerializer.Serialize/Deserialize` to
`MessagePackSerializer.Serialize/Deserialize` with Base64 wrapping.
Identical mechanical change across all 8 contexts.

### Layer 4 — Projections

Same Base64 → MessagePack decode pattern for event replay.

### Layer 5 — ASP.NET Core Minimal APIs

Custom middleware for MessagePack content negotiation:
- Reads `application/x-msgpack` request bodies
- Writes `application/x-msgpack` responses when `Accept` header matches
- Each API Program.cs registers the middleware

### Layer 6 — Frontend

Add `@msgpack/msgpack` to each Deno Fresh app. Shared `farmos-client.ts` encodes
request bodies with `encode()` and decodes responses with `decode()` from the
msgpack library. Content-Type switches to `application/x-msgpack`.

### Layer 7 — External APIs (EXCLUDED)

Home Assistant and Harvest Right integrations keep JSON — those are third-party
APIs that dictate their own wire format. These files are not touched.

### Layer 8 — Custom Event Viewer (Future TODO)

A standalone debugging tool to browse ArangoDB event payloads:
decode Base64 → deserialize MessagePack → render as readable output.

## Files Changed

| Layer | Files | Change |
|-------|-------|--------|
| SharedKernel | .csproj, new MsgPackOptions.cs, ArangoEventStore.cs, RabbitMqEventBus.cs, IEventStore.cs | Add MessagePack pkg, shared options, Base64 payload |
| Event Stores | 8x *EventStore.cs | Swap JSON to MsgPack+Base64 |
| Projections | ~10 projection files | Swap JSON deserialize to MsgPack+Base64 |
| APIs | 8x Program.cs + new middleware | MessagePack content negotiation |
| Frontend | shared/farmos-client.ts + deno.json files | Add @msgpack/msgpack, binary encode/decode |
| External APIs | No change | HA and Harvest Right keep JSON |
| Tests | API test files | Switch to MessagePack request/response |
