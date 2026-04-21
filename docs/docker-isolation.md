# Running FarmOS Alongside Other Docker Projects

> How to run FarmOS's Caddy + API + Arango + RabbitMQ stack on the same host as other projects (including other gateway-based stacks) without interference — in either direction.

---

## How Compose Isolates Projects (and Where It Doesn't)

Docker Compose derives four namespaces from a single **project name**:

| Namespace | Default pattern | Collision possible? |
|---|---|---|
| Container names | `<project>-<service>-<idx>` | Only if you set `container_name:` explicitly (don't) |
| Default network | `<project>_default` | No — each project gets its own bridge |
| Named volumes | `<project>_<volume>` | No |
| **Host port bindings** | Literal values in `ports:` | **YES — this is the only real collision point** |

Reference: [Docker docs — *Specify a project name*](https://docs.docker.com/compose/how-tos/project-name/). Precedence (high→low):
1. `docker compose -p <name> ...` flag
2. `COMPOSE_PROJECT_NAME` env var
3. Top-level `name:` in the compose file
4. Base directory name (fallback)

FarmOS now sets `name: farmos` at the top of `docker-compose.yml`. That pins the namespace deterministically regardless of where you clone the repo or what env var is set elsewhere.

---

## FarmOS Host Ports (the collision surface)

These are the ports FarmOS publishes on the host today:

| Port | Service | Can I change it? |
|---|---|---|
| `5050` | Caddy (main API gateway) | Yes — edit `caddy.ports` in `docker-compose.yml` |
| `80` | Caddy (Windmill UI) | **Very likely to collide with other projects.** Strongly recommend changing or binding to `127.0.0.1`. |
| `25` | Caddy (Windmill SMTP proxy) | Keep if you use Windmill mail, else remove |
| `8529` | ArangoDB | Only needed on host for Arango Web UI / tooling |
| `5672` | RabbitMQ AMQP | Only if external consumers need it |
| `15672` | RabbitMQ management UI | Nice-to-have, bind to loopback |
| `1883` | Mosquitto MQTT | Only if external sensors publish here |
| `9001` | Mosquitto WebSocket | " |
| `8123` | Home Assistant | External access to HA UI |
| `8000`–`8004` | 5 Deno Fresh frontends | Common ports — easy to clash |

Everything else (the 14 API containers) listens on `:8080` **inside** each container only — no host port — so they never collide with anything.

---

## Three Isolation Patterns (pick one)

### Pattern A — Loopback-only binding (recommended default)

Only expose ports to `127.0.0.1` so another project on the same machine can't collide on `0.0.0.0`.

**A1 — Network-wide (cleanest, one change):** Set the bridge driver option `com.docker.network.bridge.host_binding_ipv4` on the default network. Every published port on that network then binds to `127.0.0.1` by default, with no per-service edits:

```yaml
name: farmos

services:
  caddy:
    ports:
      - "5050:5050"   # ← binds to 127.0.0.1:5050 automatically
      - "8080:80"     # ← binds to 127.0.0.1:8080 automatically

networks:
  default:
    driver: bridge
    driver_opts:
      com.docker.network.bridge.host_binding_ipv4: "127.0.0.1"
```

Source: Docker Engine docs — *[Port publishing](https://docs.docker.com/engine/network/port-publishing/)*, *[Compose services reference — networks.driver_opts](https://docs.docker.com/reference/compose-file/services/)*.

**A2 — Per-port (explicit):** Change every `"X:Y"` to `"127.0.0.1:X:Y"`. Useful when you want *most* ports public and *some* loopback.

Pros: zero conflicts with anything on `0.0.0.0`, easy to verify with `netstat -ano -p tcp`.
Cons: LAN clients can't reach FarmOS. Fix: put your LAN-facing reverse proxy (Tailscale, host Caddy, Traefik) in front, or override only the ports you need public.

### Pattern B — Env-var-driven ports

Make every host port a variable so each machine / each checkout can pick its own:

```yaml
caddy:
  ports:
    - "${FARMOS_GATEWAY_PORT:-5050}:5050"
    - "${FARMOS_WINDMILL_PORT:-8080}:80"
```

Then in a project-local `.env` (NOT the repo — already gitignored):

```env
FARMOS_GATEWAY_PORT=5150
FARMOS_WINDMILL_PORT=8088
```

Other projects do the same with different defaults. Nothing in any compose file collides.

### Pattern C — External shared network (only if you want cross-project traffic)

If another project's containers must actually talk to FarmOS containers (e.g. a separate analytics stack consuming RabbitMQ), create a user-defined network *outside* compose:

```bash
docker network create farmos-shared
```

Then in both projects' compose files:

```yaml
services:
  rabbitmq:
    networks:
      - default
      - farmos-shared

networks:
  farmos-shared:
    name: farmos-shared
    external: true
```

Services reach each other by name over `farmos-shared`. The same `external: true` pattern works for shared volumes — see [volumes.md — external](https://docs.docker.com/reference/compose-file/volumes/). This is overkill for most "also running another dev stack" cases — prefer A or B.

### Bonus — Profiles to disable parts of FarmOS while testing

If you want to run only a subset of FarmOS (e.g. skip IoT + Home Assistant while focused on Hearth), tag optional services with `profiles:`. Services without a profile always start; services with a profile only start when activated.

```yaml
  homeassistant:
    # ...
    profiles: ["iot"]

  iot-api:
    # ...
    profiles: ["iot"]
```

Then `docker compose up` runs the core stack; `docker compose --profile iot up` includes IoT. Frees host ports + RAM without editing the file. Source: [Compose services reference — profiles](https://docs.docker.com/reference/compose-file/services/).

---

## Preventing *Other* Projects From Breaking FarmOS

The `name: farmos` top-level key plus Pattern A or B also protects FarmOS *from* the other direction:

- Another project can't overwrite FarmOS containers because container names are namespaced by project.
- Another project can't mutate FarmOS volumes because volumes are namespaced by project.
- Another project can't knock FarmOS off the network because networks are namespaced by project.
- The only thing another project can do is **take a host port first** — Pattern A/B sidesteps this by either moving to loopback or parameterising.

If another project crashes and leaves dangling containers named `<theirproject>-caddy-1`, it does not matter to FarmOS — different namespace.

---

## Running Two Gateway-Based Stacks Concurrently — Worked Example

Suppose you have Project X with its own Caddy on `:5050` and `:80`.

Quick fix for FarmOS side (recommended):

```yaml
# docker-compose.yml
name: farmos

services:
  caddy:
    ports:
      - "127.0.0.1:5150:5050"   # FarmOS API gateway moved
      - "127.0.0.1:8088:80"     # Windmill UI moved
```

Verify:

```powershell
docker compose -p farmos up -d
curl http://localhost:5150/api/hearth/...   # FarmOS works on 5150
curl http://localhost:5050/...              # Project X untouched on 5050
```

Both stacks run side-by-side, each on its own port, each in its own network/volume namespace. Frontends (`hearth-os-ui` et al.) keep talking to Caddy internally at `caddy:5050` because that's on the *internal* bridge network — internal ports never change.

---

## Useful Commands

```powershell
# See every Compose project on this host and their services
docker compose ls --all

# Stop just FarmOS (even if you're cwd somewhere else)
docker compose -p farmos down

# See which host ports are in use
netstat -ano -p tcp | findstr LISTENING

# Show each container's port bindings
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

---

## Summary

1. `name: farmos` is now pinned in `docker-compose.yml` — namespace is deterministic. ([docs](https://docs.docker.com/reference/compose-file/version-and-name/))
2. Host-port collisions are the only real risk — fix network-wide with the `com.docker.network.bridge.host_binding_ipv4: "127.0.0.1"` driver option (Pattern A1), per-port (A2), or env-var-driven ports (Pattern B).
3. Never use `container_name:` — it breaks namespacing.
4. Never share volumes or networks by default — only via explicit `external: true` when you *want* cross-project traffic. ([networks](https://docs.docker.com/reference/compose-file/networks/), [volumes](https://docs.docker.com/reference/compose-file/volumes/))
5. Internal container-to-container traffic (`caddy:5050`, `arangodb:8529`, etc.) is unaffected by any of the above and never collides.
6. Use `profiles:` to run only the services you're actively testing. ([docs](https://docs.docker.com/reference/compose-file/services/))

## Sources

All guidance in this document is cross-checked against the official Docker docs via context7 (library id `/docker/docs`):

- [Specify a project name](https://docs.docker.com/compose/how-tos/project-name/)
- [Networking in Compose](https://docs.docker.com/compose/how-tos/networking/)
- [Port publishing](https://docs.docker.com/engine/network/port-publishing/)
- [Compose file — version and name](https://docs.docker.com/reference/compose-file/version-and-name/)
- [Compose file — networks](https://docs.docker.com/reference/compose-file/networks/)
- [Compose file — volumes](https://docs.docker.com/reference/compose-file/volumes/)
- [Compose file — services (profiles, driver_opts)](https://docs.docker.com/reference/compose-file/services/)
- [FAQ — Running multiple copies of a project](https://docs.docker.com/compose/support-and-feedback/faq/)
