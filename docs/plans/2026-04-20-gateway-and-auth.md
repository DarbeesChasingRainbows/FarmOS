# ADR — Gateway Topology and Sovereign PIN Auth

**Date:** 2026-04-20
**Status:** Proposed
**Supersedes:** ambiguous statements in `docs/implementation-guide.md` (Step 5) and the original `docs/system-status-and-setup.md`.

---

## Context

The repo contains both a `Caddyfile` and a `src/FarmOS.Gateway/` .NET project. `docs/implementation-guide.md:390` described a Caddy reverse proxy on port 5000. `docs/system-status-and-setup.md` (pre-update) described `FarmOS.Gateway` running on port 5050. On inspection:

- `Caddyfile:15` — **Caddy is the real HTTP gateway**, listening on `:5050`. It routes `/api/{ctx}/*` and SignalR hubs to each backend container.
- `src/FarmOS.Gateway/Program.cs:6` — comment "YARP removed — routing handled by Caddy". The project now only exposes `/api/auth/login` and `/api/auth/whoami`.
- `docker-compose.yml:85-99` — the project is deployed as the service `auth-api`.

Auth on domain APIs:

- `grep UseAuthentication|AddAuthentication|RequireAuthorization src/` returns **zero enforcement**. The only hit is three commented-out `.RequireAuthorization()` calls in `FarmOS.Flora.API/FloraEndpoints.cs:14-22`.
- `FarmOS.Gateway/Program.cs:44-46` mints an **unsigned Base64** token of the form `"{userId}:{role}:{unix}"`. Anyone can forge one; `/api/auth/whoami` decodes without verification.
- The PIN middleware described in `docs/implementation-guide.md:298` ("reads `X-Farm-Pin` header or cookie, resolves to `FarmUser`") does not exist in the codebase.

---

## Decision

### Gateway Topology (locked in)

- **Caddy is the HTTP gateway.** Exposed host ports: `5050` (API), `80` (Windmill UI), `25` (Windmill SMTP proxy).
- **`FarmOS.Gateway` is renamed conceptually to the "Auth API"**. The project name stays (to avoid churn), but every doc refers to the container as `auth-api`. No YARP, no reverse-proxy responsibilities.
- All new context APIs (Campus, Codex, Compliance, Counter, Crew, Marketplace) are added as:
  - service in `docker-compose.yml`, listening on `:8080` internally
  - `handle /api/{ctx}/* { reverse_proxy {ctx}-api:8080 }` block in `Caddyfile`

### Auth Posture

**Minimum viable, sovereign-appropriate, applied to all contexts**:

1. **Signed token**. Replace the Base64 token with HMAC-SHA256 over `{userId}:{role}:{exp}` using a secret from `FARMOS_AUTH_SECRET` env var (mounted via docker-compose `.env`). Still stateless, still fully local, no external IdP. Exp of 12 hours.
2. **`FarmOS.SharedKernel.Auth.PinAuthMiddleware`** — single middleware registered in every API's `Program.cs`:
   - Reads `Authorization: Bearer <token>`
   - Verifies HMAC + expiry
   - Sets `HttpContext.User` with claims `sub`, `role`
   - On failure: returns `401` only if the endpoint is inside a group marked `.RequireAuthorization()`; otherwise passes through (so health probes and SSR pages keep working)
3. **Endpoint groups**:
   - `GET /api/{ctx}/*` — anonymous (for SSR reads on the LAN).
   - `POST|PUT|DELETE /api/{ctx}/*` — `.RequireAuthorization()` with role `steward | partner` by default.
   - Special cases: `Counter` sales require any authenticated user; `Compliance` and `Ledger` mutations require `steward`.
4. **Frontend**: the existing `Authorization: Bearer` header plumbing in `frontend/shared/farmos-client.ts` is already the right shape. No frontend change needed once the server honors the header.

### Scope Explicitly Excluded

- No Authentik / Keycloak / OIDC in this ADR — that is deferred per `plans/2026-03-19-polyface-features-design.md:20` and is only relevant for Milestone G2 (SaaS fork).
- No RBAC policy engine — role claim check is enough for a single family.
- No refresh tokens — a 12-hour token and re-login is acceptable on a LAN.

---

## Consequences

### Positive
- Stops shipping new contexts with zero auth.
- Forging a token now requires the shared HMAC secret, not just base64 encoding.
- Same middleware across all 14 APIs → one place to harden later (rate limits, lockout).
- Compatible with the future move to Authentik (swap the middleware, keep endpoint groups).

### Negative / Cost
- Every API `Program.cs` gets a two-line change. ~14 files.
- Frontends must ensure they call `/api/auth/login` and store the token — most already do for the mock flow.
- The seeded PIN hashes in `scripts/init-arangodb.sh` need to be double-checked.

### Risk
- If `FARMOS_AUTH_SECRET` is not set, middleware must fail closed (refuse to start). This is a standard config validation check.

---

## Implementation Checklist

- [ ] Add `FarmOS.SharedKernel.Auth.PinAuthMiddleware` + `HmacTokenService`
- [ ] Update `FarmOS.Gateway/Program.cs` to use `HmacTokenService` for issuance
- [ ] Register middleware in every `*.API/Program.cs` (Pasture, Flora, Hearth, Apiary, Commerce, Assets, Ledger, IoT, Campus, Codex, Compliance, Counter, Crew, Marketplace — 14 files)
- [ ] Add `.RequireAuthorization()` to mutating endpoint groups (remove the commented-out Flora stubs)
- [ ] Add `FARMOS_AUTH_SECRET` to `docker-compose.yml` for every API service
- [ ] Unit tests: token issuance, verification, expiry, tamper detection
- [ ] Integration test: POST without token → 401; POST with valid token → 2xx

---

## References
- `Caddyfile`
- `src/FarmOS.Gateway/Program.cs`
- `src/FarmOS.SharedKernel/Auth/AuthTypes.cs`
- `src/FarmOS.SharedKernel/Infrastructure/ArangoAuthService.cs`
- `docs/system-status-and-setup.md` (Authentication section)
