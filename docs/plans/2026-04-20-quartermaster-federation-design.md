# ADR — FarmOS ↔ Quartermaster Federation

**Date:** 2026-04-20
**Status:** Proposed
**Amends:** `docs/plans/2026-03-19-polyface-features-design.md` (adds federation seam)
**External refs:** `MustangCoffee/docs/plans/2026-04-03-quartermaster-design.md`, `2026-04-10-gateway-qm-integration-design.md`, `2026-04-12-phase1-order-lifecycle-design.md`

---

## Context

Mustang Coffee is building **Quartermaster (QM)** — a standalone .NET 10 service that every participating business runs as a sovereign instance. QM handles:

- Double-entry stock ledger (Odoo-style source/destination locations)
- GL Lite double-entry financial ledger (AR, AP, Revenue, COGS, Tax Payable, Cash)
- Federated B2B catalog, procurement orders, invoicing, and payment (Coinbase Business)
- Regulatory compliance records (Form 8300, 1099-DA data)

The QM design already names **`LDGR-HEARTH-7K4M`** (a FarmOS Hearth instance) as a canonical federation peer example — Mustang's kiosk expects to buy Jun, sourdough, syrups, and honey from a FarmOS instance over the federation protocol.

FarmOS today owns its own `Commerce` (CSA, BuyingClub, WholesaleAccount, StandingOrder, Customer) and `Ledger` (Expense/Revenue per `LedgerContext`) bounded contexts — see `src/FarmOS.Commerce.Domain/Events.cs` and `src/FarmOS.Ledger.Domain/Types.cs`. These remain the farm-facing, enterprise-accounting story. QM adds a **second seam**: federated B2B where a FarmOS farm is the **supplier peer** talking to retailer peers like Mustang Coffee.

### What this ADR decides

- FarmOS runs **its own QM instance per farm tenant**, acting as a **supplier peer** in federation.
- FarmOS bounded contexts (Apiary, Hearth, Flora, Commerce, Ledger, Assets) **listen and speak** to the local QM instance through a dedicated adapter project, **not** via direct inter-context coupling.
- QM is authoritative for B2B invoicing, procurement orders with external retailer peers, and POS-to-ledger financial entries. FarmOS's existing `Commerce` and `Ledger` contexts remain authoritative for CSA, farm-stand retail, buying-club cycles, and farm-wide enterprise accounting.
- MessagePack wire format is interoperable; attribute models are not. We use `MessagePack` 3.1.3 (MessagePack-CSharp, already a SharedKernel dep) on our side and define DTOs that bind by key index to QM's expected shape.

### What this ADR does not decide

- Whether FarmOS eventually **replaces** `FarmOS.Ledger` with QM's GL Lite. Today they coexist; QM is authoritative for federation-touching flows, `FarmOS.Ledger` is authoritative for farm-enterprise cost allocation across `LedgerContext` enterprises.
- Coinbase Business credentials, key custody, or payout-account setup — these are operational concerns tracked separately.

---

## Role & context map

```
                         ┌──────────────────────────────┐
                         │  Farm tenant (one FarmOS)    │
                         │                              │
   [Apiary]  ┐           │  catalog events, stock       │
   [Hearth]  ├─── local ─┼─► QM instance                │◄──── federation (mTLS + SSE)
   [Flora]   ┤   bus     │  LDGR-FARMOS-<rand>          │        │
   [Commerce]┘           │  (supplier peer)             │        │
                         │      │                       │        │
   [Ledger] ◄── project ─┼──────┘   journal + PosDetail │        │
                         │                              │        │
                         └──────────────────────────────┘        │
                                                                 │
                         ┌──────────────────────────────┐        │
                         │  Retailer tenant (Mustang)   │        │
                         │  QM instance                 │◄───────┘
                         │  LDGR-MUSTANG-2F9X           │
                         └──────────────────────────────┘
```

### Which FarmOS context plays which QM role

| FarmOS context | QM relationship | What it emits | What it consumes |
|---|---|---|---|
| `Apiary` | Supplier catalog source | `HoneyHarvested`, batch-ready events → Catalog items | — |
| `Hearth` | Supplier catalog source | Batch/ferment/freeze-dryer "available" events → Catalog items | — |
| `Flora` | Supplier catalog source | Cut-flower crop availability → Catalog items | — |
| `Commerce` | Existing farm-retail (CSA, BuyingClub, Wholesale) | — | QM `ProcurementOrderPlaced` from retailer peers creates a `WholesaleAccount` standing-order equivalent record when the counterparty isn't already modeled |
| `Ledger` | Projection target | — | QM journal events (`InvoiceIssued`, `PaymentConfirmed`, `PosSaleRecorded`) project to `RevenueCategory.Wholesale` / `ExpenseCategory.Processing` entries tagged with the peer's Ledger ID |
| `Assets` | Cost-of-goods inputs | `AssetDepreciated`, `ConsumableUsed` → already feeds Ledger today; unchanged | — |
| `IoT` | — | — | — (QM is not in the sensor path) |

### Core principle

FarmOS contexts do **not** import or reference QM types. They publish their existing domain events to the local event bus (RabbitMQ cross-context, same pattern as today), and a new **federation adapter** subscribes, translates, and speaks QM's protocol.

---

## Adapter architecture

Add one new project + one hosted service under `src/`:

```
FarmOS.Federation.Quartermaster/
  Domain/
    QmLedgerId.cs              LDGR-FARMOS-<rand> value object
    FederationPeer.cs          Retailer peer reference (their Ledger ID, friendly name)
    CatalogItemMapping.cs      FarmOS product ↔ QM catalog item link
    ProductIdentifiers.cs      UPC/EAN/GTIN/SKU — matches QM shape
  Contracts/
    QmCatalogItemDto.cs        MessagePack DTOs bound by key index (see "Serialization")
    QmProcurementOrderDto.cs
    QmInvoiceDto.cs
    QmPaymentDto.cs
    QmStockMoveDto.cs
  Outbound/
    IQuartermasterClient.cs    Port: publish catalog, acknowledge procurement, ship, record payment
    HttpQuartermasterClient.cs HTTP + MessagePack + SSE to the local QM
  Inbound/
    QmEventSubscriber.cs       IHostedService that opens SSE to QM, dispatches to MediatR
    QmEventEnvelope.cs         { eventType, payload, occurredAt, peerLedgerId }
  Application/
    Handlers/
      ApiaryBatchToCatalogHandler.cs   IApiaryHoneyHarvested → QM catalog upsert
      HearthBatchToCatalogHandler.cs   Hearth batch Ready → QM catalog upsert
      FloraCropToCatalogHandler.cs     Flora harvest → QM catalog upsert
      QmProcurementOrderReceivedHandler.cs   QM inbound → record WholesaleStandingOrder or one-off order
      QmInvoiceIssuedHandler.cs        QM inbound → Ledger revenue projection
      QmPaymentConfirmedHandler.cs     QM inbound → Ledger receivables settlement
  FarmOS.Federation.Quartermaster.csproj
```

This sits beside the per-context projects (not inside any one of them). It depends on `FarmOS.SharedKernel` and on the Application layer of each supplier context (for event subscriptions) — **never** on Domain or Infrastructure of those contexts.

---

## Event flows

### 1. Catalog publication (FarmOS → QM, internal)

```
Apiary.Domain.HoneyHarvested            ─┐
Hearth.Domain.BatchMarkedReady          ─┼─► RabbitMQ (existing cross-context bus)
Flora.Domain.CropHarvestedForSale       ─┘        │
                                                  ▼
                                    Federation.QuartermasterAdapter
                                         (MediatR handler)
                                                  │
                                                  ▼
                                    POST http://qm:7700/api/v1/catalogs/{id}/items
                                    (MessagePack body)
```

Idempotency key: `{context}-{aggregateId}-{eventVersion}`. Replays are safe — QM `CatalogItem` upserts are idempotent by `productRef`.

### 2. Inbound procurement order (QM → FarmOS, federation peer)

```
Mustang kiosk orders 6 bottles lavender syrup from Hearth peer
            │
            ▼
QM (LDGR-MUSTANG) POST /api/v1/procurement-orders → LDGR-FARMOS federation endpoint
            │
            ▼
Our QM instance emits SSE event: ProcurementPlaced
            │
            ▼
QmEventSubscriber (IHostedService)
            │
            ▼
MediatR: ProcurementOrderReceivedCommand(peerLedgerId, items, referenceId)
            │
            ▼
Handler reconciles: does the peer map to an existing WholesaleAccount?
            │   yes → append a one-off order to that account's cycle
            │   no  → create WholesaleAccount with `businessName = peer.name`
            │         and `metadata.federationPeerLedgerId = peerLedgerId`
            ▼
Acknowledge back to QM: POST /api/v1/procurement-orders/{id}/confirm (or reject)
```

The FarmOS operator confirms/rejects from the normal Commerce UI. Confirmation triggers the reverse flow (QM stock-to-transit move on our side, invoice issuance, Coinbase charge, SSE to peer).

### 3. Journal projection into `FarmOS.Ledger` (QM → Ledger, internal)

QM is authoritative for federation-triggered journal entries. `FarmOS.Ledger` subscribes to QM's local SSE feed and projects:

| QM event | `FarmOS.Ledger` projection |
|---|---|
| `InvoiceIssued` (supplier side) | `RevenueRecorded { category = RevenueCategory.Wholesale, enterprise = EnterpriseCode(matching context), reference = invoice.InvoiceNumber }` |
| `PaymentConfirmed` (supplier side) | Mark the prior Revenue record as settled; track gross vs net (Coinbase 1% fee → `ExpenseCategory.Processing`) |
| `PosSaleRecorded` | `RevenueRecorded { category = RevenueCategory.Retail / CafeFood / CafeBeverage per kiosk location }` |
| `CashoutRecorded` (USDC→bank) | No Ledger entry; QM-internal transfer |

Projection handlers live under `FarmOS.Federation.Quartermaster/Application/Handlers/` and emit existing `FarmOS.Ledger.Domain` events through the standard MediatR pipeline so the event store sees them.

**We do not duplicate QM's journal in `FarmOS.Ledger`.** The projection only records what farm-enterprise accounting needs: revenue with a category and enterprise code. QM stays the source of truth for AR, AP, tax payable, and trial balance.

---

## Identity & discovery

- **Ledger ID** generated on first boot of the tenant's QM instance: `LDGR-FARMOS-<4 random chars>`. Stored in QM's config, surfaced via `GET /api/v1/federation/identity`. FarmOS does not mint Ledger IDs — QM owns that.
- **Discovery** per QM design: mDNS (`_quartermaster._tcp.local`) on the LAN; Kubernetes service discovery as fallback; manual URL paste as last resort.
- **Handshake**: standard QM mutual-TLS + signed-challenge flow. The FarmOS operator approves incoming retailer requests from the QM admin UI — FarmOS does not re-implement this.

## Serialization compatibility

QM's plan specifies `Nerdbank.MessagePack`. FarmOS's `SharedKernel` currently references `MessagePack` 3.1.3 (MessagePack-CSharp) at `src/FarmOS.SharedKernel/FarmOS.SharedKernel.csproj:17`. Both libraries write compatible **MessagePack binary** for primitives, arrays, and maps — the over-the-wire bytes interoperate. What differs:

| Concern | MessagePack-CSharp | Nerdbank.MessagePack | Impact |
|---|---|---|---|
| Class attribute | `[MessagePackObject]` + `[Key(n)]` | `[GenerateShape]` (source-gen) | Each side declares its own DTOs |
| Custom formatters | `IMessagePackFormatter<T>` | `MessagePackConverter<T>` | Not used across the boundary |
| Native AOT | Works with 3.x + source gen | First-class | Neither side constrains the other |
| Typeless (`object`) | Supported | Restricted by design | Don't use `object` across the wire |

**Decision:** `FarmOS.Federation.Quartermaster.Contracts` defines DTOs with `[MessagePackObject]` + integer `[Key(n)]` attributes that match QM's shape by **positional index**. Renaming a field on either side breaks the wire — treated as a versioned-contract change, not a refactor. Content negotiation (`Accept: application/x-msgpack` vs `application/json`) follows QM's pattern; JSON is available for debugging.

The existing 2026-04-20 MessagePack ADR (`docs/plans/2026-04-20-msgpack-and-arrow.md`) says the FarmOS HTTP boundary is JSON. That stays true for farm-facing APIs. QM federation is a **different boundary** — binary MessagePack per QM's content-negotiation requirement. Don't confuse the two.

---

## Offline & resilience

- Retailer peer unreachable: catalog upserts queue in a local outbox table; drain on reconnect. FarmOS operations are unaffected.
- Local QM unreachable: FarmOS contexts keep emitting domain events to RabbitMQ. The adapter accumulates; replays when QM returns (events are idempotent by `{context}-{aggregateId}-{eventVersion}`).
- FarmOS restart: `QmEventSubscriber` records the last SSE `Last-Event-ID` per stream; reconnects with `Last-Event-ID` header to resume without loss.

---

## Infrastructure impact

- **New docker-compose service**: `quartermaster-api` running QM's `Quartermaster.Api` image. Default port 7700 (matches QM plan). Follows the `update-infrastructure` Windsurf skill: add to root `docker-compose.yml`, add `handle /api/qm/*` reverse-proxy to Caddyfile, add to `caddy.depends_on`. Separate ArangoDB collection namespace or separate ArangoDB container per QM's recommendation.
- **Config keys**: `QM_BASE_URL` (internal, default `http://quartermaster-api:7700`), `QM_LEDGER_ID` (cached after first QM response), `COINBASE_BUSINESS_API_KEY` (lives in QM's env, not FarmOS's).
- **Solution file**: register `src/FarmOS.Federation.Quartermaster/FarmOS.Federation.Quartermaster.csproj` in `FarmOS.slnx`. Reference from each domain API's composition root that needs inbound handlers (`FarmOS.Commerce.API`, `FarmOS.Ledger.API`).

---

## Explicit non-goals

- **No replacement of `FarmOS.Ledger`** in this phase. The two ledgers coexist. Farm enterprise accounting (cost allocation across `LedgerContext` enterprises, CSA revenue, CSA-share refunds, grant matching) stays in `FarmOS.Ledger`.
- **No coupling of `FarmOS.Commerce`'s `CSASeason` / `BuyingClub` to QM catalogs.** CSA share selection is farm-to-member, not B2B. If a CSA pickup ever transacts through QM, that's a separate design.
- **No direct Coinbase integration in FarmOS.** QM owns the payment gateway port and webhook. FarmOS only sees QM's `PaymentConfirmed` projection.
- **No IoT or sensor data to QM.** QM is not in that path.

---

## Amendment 2026-04-20: Validation & Coordination Model

### Validation findings from QM source code

On 2026-04-20, the Mustang Coffee Quartermaster source code was validated against this ADR's assumptions. Key findings:

| ADR assumption | QM source reality | Impact |
|---|---|---|
| **Serialization**: Nerdbank.MessagePack | **JSON** (System.Text.Json with `JsonSerializerDefaults.Web`) | ADR §"Serialization compatibility" needs revision. FarmOS should use `Quartermaster.Contracts` DTOs with JSON, not MessagePack-CSharp. |
| **SSE library**: unspecified | Lib.AspNetCore.ServerSentEvents | Matches ADR SSE semantics; no change needed. |
| **IFederationGateway port**: `DiscoverPeerAsync`, `PerformHandshakeAsync`, `SendEventAsync` | **Confirmed** — exists in `Quartermaster.Domain.Ports.IFederationGateway` | Port shape matches ADR. |
| **Federation endpoints**: `/api/v1/federation/*` exist | **Not implemented** — no endpoints in `Quartermaster.Api/Endpoints/` | Draft DTOs exist in `Quartermaster.Contracts.Drafts` (marked `[Obsolete]`). FarmOS adapter must wait for promotion. |
| **Procurement endpoints**: `/api/v1/procurement-orders` exist | **Not implemented** — `ProcurementOrder` entity exists but no HTTP surface | Draft DTOs exist in `Drafts/Procurement.cs`. FarmOS adapter must wait. |
| **Product-link endpoints**: `/api/v1/product-links` exist | **Not implemented** — domain entities exist but no HTTP surface | Draft DTOs exist in `Drafts/ProductLinks.cs`. FarmOS adapter must wait. |
| **F# Domain project**: referenced by C# | **Unreferenced** — `Quartermaster.Domain.FSharp` is Mustang-specific, not consumed by C# layer | No impact on federation. See "F# disposition" below. |

### Contract sharing model

**Current phase (nested-now)**:
- `Quartermaster.Contracts` is a local project inside the Mustang Coffee repo at `src/Quartermaster.Contracts/`.
- FarmOS consumes it via `<ProjectReference>` to the local path `c:\Work\MustangCoffee\Quartermaster\src\Quartermaster.Contracts\Quartermaster.Contracts.csproj`.
- Coordination overhead: shared folder + git sync between repos.

**Future phase (after QM extraction)**:
- Quartermaster will be extracted to its own independent repo.
- `Quartermaster.Contracts` will be published as a NuGet package.
- FarmOS will reference the package version matching the QM server version it federates with.
- Semver rules apply: MAJOR/MINOR/PATCH changes trigger version bumps and CHANGELOG entries.

### F# domain project disposition

`Quartermaster.Domain.FSharp` contains Mustang-specific domain logic (units of measure, inventory discriminated unions, farm product pipeline with Mustang data). It is **not referenced** by the C# layer and is **not part of the federation contract surface**.

**Recommendation**: Keep the F# project as Mustang Coffee's internal domain model. Do not expose F# types through `Quartermaster.Contracts`. If federation eventually needs the F# logic, port the relevant concepts to C# DTOs in the contracts project.

### Coordination scaffolding completed

The following artifacts were created in the Mustang Coffee Quartermaster repo to support cross-repo coordination:

1. **`src/Quartermaster.Contracts/`** — Contracts project with:
   - Stable DTOs for all existing QM endpoints (Stock, BOM, Locations, Products, Catalogs, Invoices, Payments, Ledger, Tax, POS, Compliance).
   - SSE event constants and payload DTOs (`SseEventTypes`, `LedgerIdFormat`, `StockChangeEvent`, etc.).
   - Draft federation DTOs in `Drafts/` namespace (marked `[Obsolete]`): Federation handshake, procurement orders, product links.

2. **`tests/Quartermaster.Contracts.Tests/`** — Round-trip serialization tests for all stable DTOs. Tests verify JSON wire-format stability.

3. **`docs/federation-spec/`** — Protocol discipline documentation:
   - `README.md` — Overview, contract sharing model, integration checklist.
   - `endpoints.md` — HTTP routes, verbs, request/response shapes for all stable endpoints.
   - `events.md` — SSE event types, payload shapes, delivery guarantees.
   - `CHANGELOG.md` — Versioned contract changes (initial 0.1.0).
   - `open-questions.md` — Unresolved design decisions (security, lifecycle, cross-stream ordering).

### Updated implementation checklist

The original checklist remains valid with the following adjustments:

- [x] ~~Implement `HttpQuartermasterClient` with MessagePack + JSON content negotiation~~ → **Use JSON only** (System.Text.Json), per QM's actual wire format.
- [ ] Reference `Quartermaster.Contracts` via local `<ProjectReference>` (nested-now phase).
- [ ] After QM extraction, update to NuGet package reference.
- [ ] Do not implement federation/procurement/product-link endpoints until QM promotes draft DTOs to stable.
- [ ] Do not depend on `Quartermaster.Contracts.Drafts` namespace types.

---

## Original implementation checklist (tracking)

- [ ] Add `quartermaster-api` + ArangoDB bind to root `docker-compose.yml`
- [ ] Add Caddy route `/api/qm/*` → `quartermaster-api:7700`
- [ ] Scaffold `src/FarmOS.Federation.Quartermaster/` project (Domain/Contracts/Outbound/Inbound/Application), register in `FarmOS.slnx`
- [ ] Implement `HttpQuartermasterClient` with MessagePack + JSON content negotiation
- [ ] Implement `QmEventSubscriber` IHostedService with SSE + `Last-Event-ID` resume
- [ ] Add MediatR handlers: Apiary/Hearth/Flora `*Ready` → QM catalog upsert
- [ ] Add inbound handlers: `ProcurementOrderReceived`, `InvoiceIssued`, `PaymentConfirmed` → `Commerce` / `Ledger` projections
- [ ] Add outbox table + idempotency key per event (`{context}-{aggregateId}-{eventVersion}`)
- [ ] Create `docs/api-reference-federation.md` covering `/api/qm/*` proxied routes (per the `sync-api-docs` rule, if any endpoints are exposed from FarmOS itself)
- [ ] Amend `docs/plans/2026-03-19-polyface-features-design.md` to reference this ADR under the Commerce + Ledger context descriptions
