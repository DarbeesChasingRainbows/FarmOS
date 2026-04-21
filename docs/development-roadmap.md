# Development Roadmap — FarmOS

> Living plan. Reflects actual build state as of 2026-04-20 and supersedes the original 16-week sovereign plan (which is preserved for context at the bottom).

---

## Where We Actually Are

- **Build**: green on .NET 10 (pinned via `global.json` to `10.0.201`).
- **Backend**: 11 bounded contexts + SharedKernel + auth-only Gateway + Marketplace. See `system-status-and-setup.md` for per-context maturity.
- **Frontend**: 5 Deno Fresh 2.x apps (`hearth-os`, `apiary-os`, `asset-os`, `iot-os`, `flower-os`). Arrow.js `1.0.0-alpha.9` migration completed for apiary-os and in progress for hearth-os (`plans/2026-03-24-apiary-os-arrow-migration.md`, `plans/2026-03-25-hearth-os-arrow-migration.md`).
- **Infra**: `docker-compose.yml` (Caddy + ArangoDB + RabbitMQ + Mosquitto + Home Assistant + 8 APIs + 5 frontends). K3s / Proxmox deployment not yet exercised.
- **Known gaps**: no auth enforcement on any API; `Campus / Codex / Compliance / Counter / Crew / Marketplace` APIs not in docker-compose or Caddy; projection rebuild tooling not yet automated; K3s manifests not yet written.

---

## Milestone A — Stabilise What's Built (Next 2 weeks)

Goal: everything in the solution runs, routes, and is reachable from a frontend.

- [ ] Pin .NET 10 SDK via `global.json` ✅ (done 2026-04-20)
- [ ] Add `Campus`, `Codex`, `Compliance`, `Counter`, `Crew`, `Marketplace` to `docker-compose.yml`
- [ ] Add matching `handle /api/{ctx}/*` blocks to `Caddyfile`
- [ ] Extend `scripts/init-arangodb.sh` to create event + view collections for the 6 new contexts
- [ ] Decide auth posture per ADR `plans/2026-04-20-gateway-and-auth.md` and ship the minimum viable PIN middleware + `.RequireAuthorization()` on mutating endpoints
- [ ] Replace the unsigned Base64 token in `FarmOS.Gateway` with a signed token (HMAC-SHA256 over `{userId}:{role}:{exp}`) — keeps sovereign, adds tamper resistance
- [ ] Projection rebuild CLI: `dotnet run --project tools/FarmOS.ProjectionRebuild -- --context pasture`
- [ ] Remove stale artifacts: `src/build_error*.txt`, `src/test_error*.txt`, `build_output.txt`, `git_log.txt`, `git_status.txt`

**Acceptance**: `docker compose up -d` brings up every context in the solution; a scripted smoke test hits one `GET` and one `POST` per context through Caddy and receives 2xx.

---

## Milestone B — Finish the Polyface Phase 1 Vertical Slices (Weeks 3–6)

Per `plans/2026-03-19-polyface-features-design.md` and `plans/2026-03-19-polyface-features-implementation.md`, Phase 1 added 4 contexts as **Domain-only**. Ship them end-to-end:

- [ ] `Crew`: Application + Infrastructure + API + projections (Worker, Shift, Certification)
- [ ] `Compliance`: Application + Infrastructure + API + projections (Permit, InsurancePolicy)
- [ ] `Codex`: Application + Infrastructure + API + projections (Procedure, Playbook)
- [ ] `Commerce CRM extension`: Customer aggregate + dedup (Layer 1 + Layer 2 only)
- [ ] Integration events wired in SharedKernel: `ShiftCompletedIntegration`, `CertificationExpiringIntegration`, `CustomersMergedIntegration`
- [ ] Ledger subscriber for `ShiftCompletedIntegration` (labor cost allocation)

**Acceptance**: schedule a shift → complete it → Ledger expense auto-appears. Register a permit → 30-day-before expiry alert appears.

---

## Milestone C — Phase 2 Revenue Channels (Weeks 7–10)

- [ ] `Campus`: full stack — Event, Booking, Curriculum, waiver handling
- [ ] `Counter`: full stack — Register, Sale, CashDrawer with tax + EBT rules
- [ ] `Commerce` buying-clubs + wholesale aggregates (`plans/2026-03-19-polyface-features-implementation.md` Task 15)
- [ ] Cross-context integration events: `RetailSaleIntegration` → Ledger; `EventRevenueIntegration` → Ledger; `AttendeeCustomerLinkIntegration` → Commerce

**Acceptance**: cash sale in Counter → Ledger revenue appears. Booking confirmed in Campus → Commerce Customer record linked.

---

## Milestone D — Phase 3 Financial Intelligence (Weeks 11–13)

- [ ] `Ledger` enterprise accounting: `EnterpriseCode` tagging, `CostAllocationRule`, `EnterpriseProfitLossProjection`, `COGSProjection`, `BudgetVarianceProjection`
- [ ] `Compliance` Grants aggregate + milestones
- [ ] `Codex` DecisionTree aggregate
- [ ] `Crew` ApprenticeProgram aggregate + rotation rules
- [ ] Dashboard endpoints: `/api/ledger/reports/enterprise-pnl`, `/api/ledger/reports/cogs`, `/api/ledger/reports/budget-variance`

**Acceptance**: P&L per enterprise queryable from a single endpoint, populated from production events.

---

## Milestone E — Frontend Consolidation (parallel with B/C/D)

- [ ] Finish Hearth-OS Arrow migration (`plans/2026-03-25-hearth-os-arrow-migration.md`)
- [ ] Decide on Arrow.js alpha risk per ADR `plans/2026-04-20-msgpack-and-arrow.md` before starting 5 new frontends
- [ ] Replace MsgPack on the HTTP boundary with JSON (same ADR); keep MsgPack in event store + RabbitMQ only
- [ ] New frontends (one per quarter, not all at once): `crew-os` → `campus-os` → `counter-os` → `commerce-os` → `codex-os`

---

## Milestone F — Edge Appliance / Deploy (Weeks 14–16)

- [ ] K3s single-node deployment on Proxmox VM (manifests in `deploy/k3s/`)
- [ ] ArangoDB in LXC with `arangodump` cron backup
- [ ] FluxCD (or Rancher Fleet) pull-based OTA updates per `edge-appliance.md`
- [ ] Cosign-signed container images + Kyverno verification policy
- [ ] Offline-resilience test: pull the WAN plug for 72 hours, verify all internal operations continue

---

## Milestone G — Strategic Fork (Decision Point)

At the end of Milestone F, choose **one**:

- **G1. Cooperative Cloud**: pursue `edge-appliance.md` vision — multi-farm cloud hub + B2B Guild context + EdgePortal marketplace + Excommunication Protocol.
- **G2. Enterprise SaaS**: pursue `enterprise-saas-evolution.md` — Keycloak, per-tenant isolation, Stripe billing, PWA mobile.
- **G3. Agentic Commerce first**: double down on `FarmOS.Marketplace.API` — MCP server expansion, UCP catalog depth, AI-assisted order placement, Google/Anthropic merchant onboarding (`plans/2026-03-23-local-discovery-guide.md`).

These are partly overlapping but compete for effort. Picking one commits the next 6 months of roadmap shape.

---

## Cross-Cutting Items (Ongoing)

- **Tests**: xUnit + FluentAssertions + NSubstitute per `*.Domain.Tests` project. Aim for ≥1 happy-path + ≥1 rule-violation test per aggregate method.
- **F# rules**: currently only Hearth. Decide (per Milestone B) whether to port Pasture `GrazingRules` and Flora `SuccessionRules` to F#, or retire the F# plan and keep rules in C# everywhere.
- **Docs**: every new feature updates `system-status-and-setup.md` status table and adds an ADR in `docs/plans/YYYY-MM-DD-*.md` when it changes architecture.

---

## Appendix — Original Sovereign 16-Week Plan (for reference)

The original phased plan targeted 5 contexts (Pasture, Flora, Hearth, Apiary, Commerce) plus AI integration and sensor telemetry. Most of that plan is either absorbed into Milestones A–F above, or was delivered ahead of schedule (all 5 original contexts are now full-stack), or was replaced by the Polyface feature expansion. See git history and `docs/plans/` for the evolution.
