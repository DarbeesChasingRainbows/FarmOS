# Docker Compose Guide — FarmOS

> Complete service map, networking, environment variables, and operational commands.

---

## Quick Start

```bash
# Start everything
docker compose up -d

# Rebuild after C# code changes
docker compose up -d --build

# Rebuild a specific API
docker compose up -d --build hearth-api

# View logs
docker compose logs -f hearth-api
docker compose logs -f caddy

# Stop everything
docker compose down

# Nuclear reset (destroy volumes)
docker compose down -v
```

---

## Service Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Host Machine                             │
│                                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────┐  ┌─────┐ │
│  │ HearthOS │  │ ApiaryOS │  │ AssetOS  │  │IoT-OS │  │Flwr │ │
│  │  :8000   │  │  :8001   │  │  :8002   │  │ :8003 │  │:8004│ │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └──┬────┘  └──┬──┘ │
│       │              │              │            │          │    │
│       └──────────────┴──────────────┴────────────┴──────────┘   │
│                              │                                   │
│                    ┌─────────▼─────────┐                        │
│                    │    Caddy :5050    │  Reverse Proxy          │
│                    │  /api/{ctx}/* →   │  Routes to backends    │
│                    └─────────┬─────────┘                        │
│                              │                                   │
│  ┌───────┬───────┬───────┬───┴───┬────────┬────────┬──────────┐│
│  │Pasture│ Flora │ Hearth│Apiary │Commerce│ Assets │  Ledger  ││
│  │  API  │  API  │  API  │  API  │  API   │  API   │   API    ││
│  │ :8080 │ :8080 │ :8080 │ :8080 │  :8080 │  :8080 │  :8080   ││
│  └───┬───┴───┬───┴───┬───┴───┬───┴────┬───┴────┬───┴────┬─────┘│
│      └───────┴───────┴───────┴────────┴────────┴────────┘      │
│                              │                                   │
│           ┌──────────────────┼──────────────────┐               │
│           │                  │                  │               │
│   ┌───────▼──────┐  ┌───────▼──────┐  ┌───────▼──────┐        │
│   │  ArangoDB    │  │  RabbitMQ    │  │ Home Assist  │        │
│   │    :8529     │  │ :5672/:15672 │  │    :8123     │        │
│   └──────────────┘  └──────────────┘  └──────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Service Reference

### Infrastructure Services

| Service | Image | Ports | Purpose |
|---------|-------|-------|---------|
| `arangodb` | `arangodb:latest` | `8529` | Event store + projections + graph DB |
| `arangodb-init` | `curlimages/curl` | — | Runs `scripts/init-arangodb.sh` to create collections/indexes |
| `rabbitmq` | `rabbitmq:3-management` | `5672`, `15672` | Cross-context event bus (AMQP + management UI) |
| `mqtt` | `eclipse-mosquitto:latest` | `1883`, `9001` | IoT sensor MQTT broker |
| `homeassistant` | `ghcr.io/.../home-assistant:stable` | `8123` | Sensor integration + automation |
| `caddy` | `ghcr.io/.../caddy-l4:latest` | `5050`, `80`, `25` | API gateway / reverse proxy |

### Backend API Services

All APIs use `Dockerfile.api` with a `PROJECT_NAME` build arg. Each runs on internal port `8080`.

| Service | Project | Caddy Route | Dependencies |
|---------|---------|-------------|--------------|
| `auth-api` | `FarmOS.Gateway` | `/api/auth/*` | ArangoDB |
| `pasture-api` | `FarmOS.Pasture.API` | `/api/pasture/*` | ArangoDB |
| `flora-api` | `FarmOS.Flora.API` | `/api/flora/*` | ArangoDB, RabbitMQ |
| `hearth-api` | `FarmOS.Hearth.API` | `/api/hearth/*`, `/hubs/kitchen*` | ArangoDB, RabbitMQ |
| `apiary-api` | `FarmOS.Apiary.API` | `/api/apiary/*` | ArangoDB |
| `iot-api` | `FarmOS.IoT.API` | `/api/iot/*`, `/hubs/sensors*` | ArangoDB, RabbitMQ, Home Assistant |
| `commerce-api` | `FarmOS.Commerce.API` | `/api/commerce/*` | ArangoDB |
| `assets-api` | `FarmOS.Assets.API` | `/api/assets/*` | ArangoDB, Home Assistant |
| `ledger-api` | `FarmOS.Ledger.API` | `/api/ledger/*` | ArangoDB |

### Frontend Services

| Service | Directory | Host Port | Internal Port |
|---------|-----------|-----------|---------------|
| `hearth-os-ui` | `frontend/hearth-os` | `8000` | `8000` |
| `apiary-os-ui` | `frontend/apiary-os` | `8001` | `8001` |
| `asset-os-ui` | `frontend/asset-os` | `8002` | `8000` |
| `iot-os-ui` | `frontend/iot-os` | `8003` | `8000` |
| `flower-os-ui` | `frontend/flower-os` | `8004` | `8004` |

---

## Environment Variables

### Shared (All APIs)

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | .NET environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Internal listen address |
| `ArangoDB__Url` | `http://arangodb:8529` | ArangoDB connection |
| `ArangoDB__Password` | `farmos_dev` | ArangoDB root password |

### RabbitMQ (Flora, Hearth, IoT APIs)

| Variable | Default | Description |
|----------|---------|-------------|
| `RABBITMQ_HOST` | `rabbitmq` | RabbitMQ hostname |
| `RABBITMQ_PORT` | `5672` | AMQP port |
| `RABBITMQ_USER` | `farmos` | Username |
| `RABBITMQ_PASS` | `farmos_dev` | Password |

### Home Assistant (IoT, Assets APIs)

| Variable | Default | Description |
|----------|---------|-------------|
| `HA_URL` | `http://homeassistant:8123` | Home Assistant URL |
| `HA_TOKEN` | *(empty)* | Long-lived access token |
| `HA_POLL_INTERVAL_SECONDS` | `900` | Sensor poll interval (IoT only) |

### Twilio (IoT API — SMS Alerts)

| Variable | Default | Description |
|----------|---------|-------------|
| `TWILIO_SID` | *(empty)* | Twilio account SID |
| `TWILIO_AUTH_TOKEN` | *(empty)* | Twilio auth token |
| `TWILIO_FROM` | *(empty)* | SMS sender number |
| `ALERT_PHONE` | *(empty)* | Destination phone for alerts |

### Frontend Services

| Variable | Service | Description |
|----------|---------|-------------|
| `GATEWAY_URL` | HearthOS, AssetOS, IoT-OS, FlowerOS | Caddy URL (`http://caddy:5050`) |
| `FARMOS_URL` | ApiaryOS | Caddy URL (`http://caddy:5050`) |
| `PORT` | ApiaryOS | Listen port override |
| `DENO_ENV` | All frontends | `production` in Docker |

---

## Caddy Reverse Proxy

The `Caddyfile` at the project root defines all routing rules:

- **`:5050`** — API gateway
  - `/api/{context}/*` → routes to each backend's internal `:8080`
  - `/hubs/kitchen*` → Hearth API SignalR hub
  - `/hubs/sensors*` → IoT API SignalR hub
  - `/iot-os/*` → IoT frontend
- **`:80`** — Windmill workflow engine UI
- **`:25`** — SMTP relay (layer4 proxy to Windmill)

---

## Volumes

| Volume | Used By | Purpose |
|--------|---------|---------|
| `arangodb-data` | `arangodb` | Persistent event store + projections |
| `rabbitmq-data` | `rabbitmq` | Message queue persistence |
| `caddy-data` | `caddy` | TLS certificates (if configured) |
| `caddy-config` | `caddy` | Caddy auto-generated config |

---

## Dependency Order

Services start in this order based on `depends_on`:

1. **ArangoDB** starts first
2. **arangodb-init** runs init script, then exits
3. **RabbitMQ** starts (parallel with ArangoDB)
4. **All backend APIs** start after arangodb-init completes
5. **Caddy** starts after all APIs are running
6. **All frontends** start after Caddy is running

---

## Common Operations

### Rebuild a single API after code changes

```bash
docker compose up -d --build hearth-api
```

### View ArangoDB UI

Navigate to `http://localhost:8529` — login as `root` / `farmos_dev`, select `farmos` database.

### View RabbitMQ Management UI

Navigate to `http://localhost:15672` — login as `farmos` / `farmos_dev`.

### Run frontend locally (outside Docker)

```bash
cd frontend/hearth-os
deno task dev
# Serves on http://localhost:8000, proxies API to localhost:5050
```

### Check Caddy routing

```bash
# Test a specific API route
curl http://localhost:5050/api/hearth/sourdough
curl http://localhost:5050/api/pasture/paddocks
```
