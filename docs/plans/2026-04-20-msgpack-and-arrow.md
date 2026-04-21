# ADR — Narrow MessagePack Scope & Arrow.js Risk Plan

**Date:** 2026-04-20
**Status:** Part 1 Proposed; **Part 2 Superseded** (Arrow.js 1.0 shipped; see amendment note below)
**Amends:** `docs/plans/2026-03-19-messagepack-migration-design.md`, `docs/arrow-js-island-pattern.md`

> **Amendment (2026-04-20, same day):** Part 2's "freeze at alpha.9 + vendor fallback" plan is superseded.
> `@arrow-js/core@1.0.6` shipped as stable on npm. `hearth-os` and `apiary-os` have been bumped to `^1.0.6`
> in a single PR; the reactive-state call sites were updated to match 1.0.6's stricter `Reactive<T>` typings
> (removed `as Record<string, unknown>` casts, added `?? []` guards on nullable API results). The vendor
> bundle (`frontend/shared/vendor/arrow-js-1.0.0-alpha.9.js`) and adapter-layer steps in the Implementation
> Checklist below are no longer required — a stable 1.x removes the alpha-yank risk that motivated them.

---

## Part 1 — MessagePack: Narrow the Blast Radius

### Context

The March 2026 migration replaced JSON with MessagePack + LZ4 across **seven layers** (event store, event bus, 8 domain event stores, projections, Minimal API HTTP boundary, frontend fetch client, excluding only 3rd-party APIs).

Two concrete pain points have shown up since:

1. **ArangoDB Web UI is now opaque for event payloads.** `EventEnvelope.Payload` is Base64-encoded MsgPack bytes. Operators can no longer read event content in the Arango UI during projection debugging. The design doc acknowledges this and lists "Custom Event Viewer" as "Future TODO" — meaning debugging is worse today than before the migration.
2. **Frontend integration is fragile.** `git_log.txt:1-34` shows two sequential commits (`2eba17f` and `9bd36e5`) spent fixing Vite's resolution of `@msgpack/msgpack` for the shared client — first via `resolve.modules` (wrong, silently ignored by Vite), then via `resolve.alias`. These errors caused blank pages on island-based routes across three frontends. `asset-os` and `iot-os` don't even use the shared client and had the dep removed entirely.

The **size/perf win** matters at two boundaries:
- **Event store**: writes are append-only and read during replay; smaller payloads → faster replay + lower disk.
- **RabbitMQ**: high-volume telemetry + cross-context events; binary is natural.

The win does **not** matter at:
- **HTTP boundary**: requests are low-volume (farm LAN), content negotiation adds complexity, and browsers/curl/Postman all handle JSON natively.

### Decision

**Keep MsgPack only where it earns its keep. Return to JSON on the HTTP boundary.**

| Layer | Decision |
|---|---|
| Event store `EventEnvelope.Payload` in ArangoDB | **MsgPack + Base64** (keep) — smaller, and envelope metadata stays JSON-readable |
| RabbitMQ cross-context events | **MsgPack binary** (keep) — `application/x-msgpack` |
| ASP.NET Core Minimal API request/response | **JSON via System.Text.Json** (revert) — remove `MessagePackMiddleware` from the pipeline |
| Frontend `farmos-client.ts` | **JSON** (revert) — drop `@msgpack/msgpack` from every `deno.json` |
| 3rd-party APIs (Home Assistant, Harvest Right) | **JSON** (unchanged) |

Also: build the promised **event viewer** as a tiny tool, *now*, not "future TODO":
- `tools/FarmOS.EventViewer/` — CLI that takes a collection + aggregateId and prints decoded events to stdout as JSON.
- ~100 lines. Uses existing `MsgPackOptions` + `EventTypeMap` from each context.

### Consequences

- Two layers of revert work: remove `UseMiddleware<MessagePackMiddleware>()` from every `Program.cs`; remove encode/decode from `farmos-client.ts`.
- `MessagePackMiddleware` and `MsgPackOptions` stay — just their wiring at the HTTP boundary is removed.
- Frontend Vite configs can drop the `resolve.alias` workaround.
- Debugging improves immediately. No more base64 roundtrip to read a POST body.
- The ~60-70% payload size savings at the HTTP boundary are forfeited — acceptable on a LAN.

---

## Part 2 — Arrow.js 1.0.0-alpha.9: Risk Plan

### Context

Frontend strategy (`docs/arrow-js-island-pattern.md`) and two large migration plans (`plans/2026-03-24-apiary-os-arrow-migration.md` — 36 KB, `plans/2026-03-25-hearth-os-arrow-migration.md` — 80 KB) commit the entire frontend estate to Arrow.js **`1.0.0-alpha.9`** — a pre-1.0 library with no public stability guarantee. The Polyface plan queues 5 more frontends (`crew-os`, `campus-os`, `counter-os`, `commerce-os`, `codex-os`) on the same foundation.

Self-reported friction already present in the docs:
- `docs/arrow-js-island-pattern.md:274-283` — persistent TypeScript warnings about `ReactiveProxy` reassignment that the codebase has chosen to "acknowledge and accept".
- `:105` — a common-sense guard: "You must **replace** arrays rather than mutating them in-place. `state.items.push(x)` will NOT trigger reactivity." Easy to get wrong, hard to grep for.
- `:132` — "The falsy branch must return an Arrow template, not `null` or `""`." More footguns.

Migrating away later costs 5 app-rewrites. Staying committed to an alpha costs whatever future alpha versions break.

### Decision

**Freeze, don't flee.** The apiary-os/hearth-os migrations are too far along to revert. Protect them:

1. **Pin exactly** in every `deno.json`: `"@arrow-js/core": "npm:@arrow-js/core@1.0.0-alpha.9"` (no `^`, no `~`). Do the same across all 5 existing frontends in one commit.
2. **Vendor as a fallback**: copy the minified dist into `frontend/shared/vendor/arrow-js-1.0.0-alpha.9.js` and import through an alias. If npm/JSR yanks the package or the API changes in alpha.10, apps still build.
3. **Wrap in a small adapter layer** (`frontend/shared/arrow-adapter.ts`) that re-exports `html`, `reactive`. Every new island imports from the adapter, not directly from `@arrow-js/core`. If we ever swap frameworks, the adapter is the single replacement point.
4. **Gate new frontends on a pilot**: do *not* start `crew-os`/`campus-os`/`counter-os`/`commerce-os`/`codex-os` until Hearth-OS migration is 100% complete and running in prod for ≥ 2 weeks with no Arrow-related bug tickets.
5. **Document escape hatch**: if alpha.10 breaks us or the project goes stale, the adapter layer makes it feasible to swap to:
   - **Preact Signals** (what we migrated away from) — fine-grained, stable 1.x, native to the Preact ecosystem Fresh already uses.
   - **SolidJS** inside islands — similar fine-grained reactivity, stable, production track record.

### Consequences

- Adds one adapter file and one vendored JS bundle. Low cost.
- Freezes the alpha at a known-working version so upstream churn can't break us unasked.
- Buys optionality without committing to a rewrite today.
- New-frontend timeline slips by the length of the Hearth-OS stabilization window — acceptable given 5 apps are queued.

---

## Implementation Checklist

### MsgPack narrowing
- [ ] Remove `app.UseMiddleware<MessagePackMiddleware>()` from every `*.API/Program.cs`
- [ ] Delete MsgPack encode/decode calls in `frontend/shared/farmos-client.ts`; use `JSON.stringify` / `res.json()`
- [ ] Remove `@msgpack/msgpack` from `frontend/*/deno.json` and the Vite `resolve.alias` workarounds
- [ ] Keep `MsgPackOptions`, `ArangoEventStore` payload roundtrip, and `RabbitMqEventBus` wire format as-is
- [ ] Create `tools/FarmOS.EventViewer/` CLI

### Arrow.js freeze
- [ ] Pin exact version in every `frontend/*/deno.json`
- [ ] Add `frontend/shared/vendor/arrow-js-1.0.0-alpha.9.js`
- [ ] Add `frontend/shared/arrow-adapter.ts` and migrate imports progressively (non-blocking)
- [ ] Block creation of 5 new Polyface frontends until Hearth-OS migration is verified stable
