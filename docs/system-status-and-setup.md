# System Status and Setup Guide

This document captures the exact build state of the FarmOS backend and the procedures required to run the local sovereign infrastructure.

---

## 🏗️ Current Build Status (Backend)

The FarmOS backend is composed of **7 distinct Bounded Contexts**, built on .NET 9 using an Event Sourced, CQRS architecture.

| Context | Status | Details |
| :--- | :--- | :--- |
| **SharedKernel** | 🟢 Complete | Contains strictly isolated abstractions for `EventStore`, `CQRS`, `Result<T,E>`, and Sovereign PIN `Auth`. |
| **Gateway** | 🟢 Complete | Caddy reverse proxy running on port `5050`. Routes all traffic based on context base paths (`/api/pasture`, etc.). |
| **Pasture** | 🟢 Complete | Full stack reference implementation: Domain, Application (Commands/Queries/MediatR), Infrastructure (ArangoDB EventStore & Query Projections), API Endpoints. |
| **Flora** | 🟡 Domain Only | Complete Domain entities, value objects, and events. Requires matching Application/API layers. |
| **Hearth** | 🟡 Domain Only | Complete Domain. Requires matching Application/API layers. |
| **Apiary** | 🟡 Domain Only | Complete Domain. Requires matching Application/API layers. |
| **Commerce** | 🟡 Domain Only | Complete Domain. Requires matching Application/API layers. |
| **Assets** | 🟡 Domain Only | Complete Domain. Requires matching Application/API layers. |
| **Ledger** | 🟡 Domain Only | Complete Domain. Requires matching Application/API layers. |

---

## 🚀 Running the Local Dev Environment

FarmOS development requires two backing infrastructure services:
1. **ArangoDB 3.12** (Primary document/graph database)
2. **RabbitMQ 4** (Transient message bus for cross-context events)

### 1. Start Infrastructure
A standard `docker-compose.yml` is provided at the root of the repository.

```bash
docker compose up -d
```
*ArangoDB will be exposed on port `8529`, RabbitMQ on `5672` (and management ui on `15672`).*

### 2. Initialize the Database
The entire schema, collections, geo-indexes, graph edges, and default users are scripted.
Wait for ArangoDB to start, then run the PowerShell setup script (or run equivalent AQL commands from `scripts/setup-collections.js`).

To create the default `farmos` database and all 44 collections:
```powershell
# Assuming the root password from docker compose is 'farmos_dev'
$headers = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("root:farmos_dev")) }

# 1. Create the database
Invoke-RestMethod -Uri "http://localhost:8529/_api/database" -Method POST -Headers $headers -ContentType "application/json" -Body '{"name":"farmos"}'

# (The complete setup details are located in the scripts folder).
```

### 3. Initialize Home Assistant (Optional for Telemetry)

For farm sensor telemetry, Home Assistant needs to be initialized to generate a Long-Lived Access Token.

1. Once the `homeassistant` container has started, run the automated setup script:
   ```bash
   pwsh ./scripts/ha-setup.ps1
   ```
2. Navigate to `http://localhost:8123` and log in with username `admin` and password `farmOS_password123!`.
3. Complete the analytics onboarding wizard.
4. Go to **Profile** (bottom left) -> **Security** -> **Long-Lived Access Tokens**.
5. Create a token and paste it into your `docker-compose.yml` under the `assets-api` service as the `HA_TOKEN` environment variable. Restart the services if running `assets-api` via Docker.

### 4. Start the APIs
Since frontend access relies on the Caddy Gateway for routing and CORS management, you must always run the Gateway. 

1. Start the API services (e.g., Pasture):
   ```bash
   dotnet run --project src/FarmOS.Pasture.API
   ```
   *(Running independently on port `5101`)*

2. Start the Gateway:
   ```bash
   dotnet run --project src/FarmOS.Gateway
   ```
   *(Running on port `5050`)*

---

## 🔑 Authentication

FarmOS utilizes a decentralized **"Sovereign PIN"** model suitable for a family working on a shared LAN. There is no external identity provider (Auth0/Azure).

A user must authenticate against the **Gateway** at `http://localhost:5050/api/auth/login`.

**Credentials** (Seeded by default):
- Steward: PIN `1234`
- Partner: PIN `123`

The gateway returns a stateless Base64-encoded token that must be sent as an `Authorization: Bearer <token>` header to all backend contexts.

---

## 🗺️ Routing Reference

If you are expanding the frontend (`Deno Fresh`), send requests to `localhost:5050`:

*   `/api/auth` ➔ Handled directly by Gateway
*   `/api/pasture/*` ➔ Routed to `FarmOS.Pasture.API`
*   `/api/flora/*` ➔ Routed to `FarmOS.Flora.API` (WIP)
*   `/api/hearth/*` ➔ Routed to `FarmOS.Hearth.API` (WIP)
*   `/api/apiary/*` ➔ Routed to `FarmOS.Apiary.API` (WIP)
*   `/api/commerce/*` ➔ Routed to `FarmOS.Commerce.API` (WIP)
*   `/api/assets/*` ➔ Routed to `FarmOS.Assets.API` (WIP)
*   `/api/ledger/*` ➔ Routed to `FarmOS.Ledger.API` (WIP)

---

## 🗄️ Database Quick Reference

All events are strictly append-only. To assist with debugging read models (projection testing), the collections are divided into three types:

1. **Event Streams:** `pasture_events`, `flora_events`, etc.
2. **Read Models:** `pasture_paddock_view`, `pasture_herd_view`, `commerce_order_view`, etc.
3. **Graph Edges:** `grazes_on`, `descended_from`, `planted_in`, etc.

The named graph inside ArangoDB is **`farmos_graph`**.
