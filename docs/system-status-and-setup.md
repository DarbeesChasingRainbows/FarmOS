# System Status and Setup Guide

This document captures the **actual** build state of the FarmOS backend as verified against `c:\Work\FarmOS\src\` on 2026-04-20, plus the procedures required to run the local sovereign infrastructure.

---

## 🏗️ Current Build Status (Backend)

FarmOS is built on **.NET 10 (GA, pinned via `global.json` to `10.0.201`)** using an Event-Sourced / CQRS architecture. There are **14 backend projects** in the solution: `SharedKernel` + `Gateway` (auth-only) + `Marketplace` + 11 bounded contexts.

| Context | Status | Notes |
| :--- | :--- | :--- |
| **SharedKernel** | 🟢 Complete | `EventStore`, `CQRS`, `Result<T,E>`, Sovereign PIN `Auth` types, MessagePack options. |
| **Gateway (`FarmOS.Gateway`)** | 🟢 Complete | **Auth-only** microservice (despite the name — YARP was removed). Deployed as `auth-api:8080`. Exposes `/api/auth/login` and `/api/auth/whoami`. See ADR `2026-04-20-gateway-and-auth.md`. |
| **Caddy reverse proxy** | 🟢 Complete | The real HTTP gateway on port `:5050`. Routes `/api/{ctx}/*` to each backend container. Configured in `Caddyfile`. |
| **Pasture** | 🟢 Full stack | Reference implementation: Domain, Application, Infrastructure (ArangoEventStore + Projectors), API. |
| **Flora** | 🟢 Full stack | Domain + Application + Infrastructure + API + tests. |
| **Hearth** | 🟢 Full stack | Full CQRS + F# rules project (`FarmOS.Hearth.Rules`). SignalR hub `/hubs/kitchen`. Most mature context. |
| **Apiary** | 🟢 Full stack | Full CQRS + API. |
| **Commerce** | 🟢 Full stack | Customer / CSA / Orders + BuyingClub + WholesaleAccount. |
| **Assets** | 🟢 Full stack | Equipment, structures, water, compost, materials, sensors. |
| **Ledger** | 🟢 Full stack | Expense / Revenue aggregates. |
| **IoT** | 🟢 Full stack | Device registry, zones, telemetry, excursion alerts. SignalR hub `/hubs/sensors`. Publishes `iot.excursion.*` integration events. |
| **Campus** | 🟡 Domain + tests | Events, Bookings, Curricula. Application/Infra/API scaffolded. |
| **Codex** | 🟡 Domain + tests | Procedures, Playbooks, DecisionTrees. |
| **Compliance** | 🟡 Domain + tests | Permits, InsurancePolicies. Grants extension pending. |
| **Counter** | 🟡 Domain + tests | POS: Register, Sale, CashDrawer. |
| **Crew** | 🟡 Domain + tests | Worker, Shift. Apprentice programs pending. |
| **Marketplace (`FarmOS.Marketplace.API`)** | 🟢 Active | Exposes MCP server at `/mcp`, UCP discovery at `/.well-known/ucp`, UCP catalog/checkout endpoints, and Schema.org structured data. Reuses Commerce projections — no own event store. See `plans/2026-03-23-local-discovery-guide.md`. |

> **F# usage**: Only `FarmOS.Hearth.Rules` is in F# today. The planned `*.Domain.FSharp` projects from `implementation-guide.md` were not created for other contexts.

---

## 🔐 Authentication — Current Reality

**There is no auth enforcement on any domain API today.** `/api/auth/login` mints an unsigned Base64 token `"{userId}:{role}:{unix}"` which `/api/auth/whoami` decodes without verification. No `UseAuthentication` / `AddAuthentication` / `RequireAuthorization` calls exist in the solution (only a commented-out stub in `FloraEndpoints.cs:14-22`).

See [`docs/plans/2026-04-20-gateway-and-auth.md`](plans/2026-04-20-gateway-and-auth.md) for the decision record and the minimum viable PIN middleware plan.

Seeded users (in ArangoDB `farmos_users` collection, created by `scripts/init-arangodb.sh`):
- Steward: PIN `1234`
- Partner: PIN `123`

---

## 🚀 Running the Local Dev Environment

Everything runs via Docker Compose. Infra + APIs + frontends all come up together.

```bash
docker compose up -d
docker compose logs -f caddy           # gateway / routing
docker compose logs -f hearth-api      # example backend
```

Ports exposed on the host:

| Service | Port | Purpose |
|---|---|---|
| `caddy` | `5050` | **Primary entry point.** All `/api/*` traffic. |
| `caddy` | `80` | Windmill UI (optional workflow engine) |
| `arangodb` | `8529` | Event store + projections + graph DB |
| `rabbitmq` | `5672`, `15672` | AMQP + management UI |
| `mqtt` | `1883`, `9001` | Eclipse Mosquitto for IoT |
| `homeassistant` | `8123` | Sensor integration |
| `hearth-os-ui` | `8000` | Deno Fresh frontend |
| `apiary-os-ui` | `8001` | Deno Fresh frontend |
| `asset-os-ui` | `8002` | Deno Fresh frontend |
| `iot-os-ui` | `8003` | Deno Fresh frontend |
| `flower-os-ui` | `8004` | Deno Fresh frontend |

Backend API containers (`pasture-api`, `flora-api`, `hearth-api`, `apiary-api`, `commerce-api`, `assets-api`, `ledger-api`, `iot-api`, `auth-api`) listen on port `8080` internally only; access them through Caddy at `http://localhost:5050/api/{ctx}/*`.

> `Marketplace.API`, `Campus.API`, `Codex.API`, `Compliance.API`, `Counter.API`, `Crew.API` are in the solution but **not yet added to `docker-compose.yml` or the Caddyfile**. Add them before their frontends can consume them.

### Database Initialization
`scripts/init-arangodb.sh` runs automatically via the `arangodb-init` service. It creates the `farmos` database, 44 collections, geo-indexes, graph edges, and default users. No manual setup needed.

### Home Assistant (Optional)
1. `pwsh ./scripts/ha-setup.ps1`
2. Browse to `http://localhost:8123`, log in (`admin` / `farmOS_password123!`).
3. Profile → Security → Long-Lived Access Tokens → create one.
4. Paste into `docker-compose.yml` under `iot-api` / `assets-api` `HA_TOKEN`.

---

## 🗺️ Routing Reference (Caddy)

Defined in [`Caddyfile`](../Caddyfile). All requests go through `http://localhost:5050`:

| Path | Target |
|---|---|
| `/api/auth/*` | `auth-api:8080` (the `FarmOS.Gateway` project) |
| `/api/pasture/*` | `pasture-api:8080` |
| `/api/flora/*` | `flora-api:8080` |
| `/api/hearth/*` | `hearth-api:8080` |
| `/hubs/kitchen*` | `hearth-api:8080` (SignalR) |
| `/api/apiary/*` | `apiary-api:8080` |
| `/api/commerce/*` | `commerce-api:8080` |
| `/api/assets/*` | `assets-api:8080` |
| `/api/ledger/*` | `ledger-api:8080` |
| `/api/iot/*` | `iot-api:8080` |
| `/hubs/sensors*` | `iot-api:8080` (SignalR) |
| `/iot-os/*` | `iot-os-ui:8000` |

---

## 🗄️ Database Quick Reference

All events are strictly append-only. Collections are divided into three types:

1. **Event streams**: `pasture_events`, `flora_events`, `hearth_events`, `apiary_events`, `commerce_events`, `assets_events`, `ledger_events`, `iot_events`, `campus_events`, `codex_events`, `compliance_events`, `counter_events`, `crew_events`.
   - Event payloads are **Base64-encoded MessagePack** bytes (see `plans/2026-03-19-messagepack-migration-design.md`). Envelope metadata stays JSON-readable.
2. **Read models (projections)**: `*_view` collections per context.
3. **Graph edges**: `grazes_on`, `descended_from`, `planted_in`, `pollinates`, `located_in`, `monitors`, `applied_to`, `belongs_to`, …

The named graph in ArangoDB is **`farmos_graph`**.

---

## ✅ Verifying a Clean Build

```powershell
dotnet --version                # should print 10.0.201 (pinned by global.json)
dotnet build FarmOS.slnx        # should end with "Build succeeded"
dotnet test FarmOS.slnx -v minimal
```
