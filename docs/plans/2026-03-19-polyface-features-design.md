# Polyface-Style Feature Expansion Design

**Date:** 2026-03-19
**Status:** Approved (amended 2026-04-20 — federation seam added)
**Scope:** 4 new bounded contexts + 2 extensions + 1 cross-cutting context

> **Amendment (2026-04-20):** `Commerce` and `Ledger` now also participate in a **B2B federation seam** with external Quartermaster (QM) peers such as Mustang Coffee. A FarmOS tenant runs its own QM instance as a supplier peer; inbound procurement orders, invoices, and payments are translated by a dedicated `FarmOS.Federation.Quartermaster` adapter and projected into `Commerce` (as `WholesaleAccount` / standing-order records) and `Ledger` (as `RevenueCategory.Wholesale` / `ExpenseCategory.Processing` entries). The in-house decision below is unchanged — federation is *additive*, not a replacement for FarmOS's farm-enterprise accounting. See `docs/plans/2026-04-20-quartermaster-federation-design.md` for the full design.

## Overview

FarmOS has 8 (and growing) bounded contexts covering farm operations (Pasture, Flora, Hearth, Apiary), business (Commerce, Ledger), and infrastructure (Assets, IoT). This design adds 7 feature areas to support a Polyface Farms-style diversified operation with commercial value-added products (Jun, cafe, syrups, sourdough) and eventual agritourism/education programs.

## Decision: Build In-House (Not External CRM)

Evaluated Twenty CRM (Docker) and ERPNext integration. Chose in-house for:
- **Sovereignty** -- no external dependencies, air-gapped compatible but not required
- **Event sourcing** -- complete audit trail for compliance
- **Farm-specific models** -- buying clubs, drop sites, ordering cycles don't map to Salesforce-shaped CRM abstractions
- **Single data store** -- ArangoDB, no PostgreSQL sidecar

## Decision: Authentik Deferred to Phase 4+

Authentik (OIDC/SCIM) identified as the right choice for system user authentication and role-based access. Deferred to avoid complexity -- current implementation proceeds without auth middleware. Integration points documented for future:
- Crew context pushes worker lifecycle to Authentik via SCIM
- All .NET APIs add JWT Bearer validation
- Frontend apps get SSO via OIDC
- Groups map to role policies (farm-admin, employee, apprentice, volunteer)

---

## Architecture

### Bounded Context Map

```
NEW BOUNDED CONTEXTS (4):
  Crew        -- People & labor management
  Campus      -- Agritourism & education
  Compliance  -- Regulatory, permits, insurance
  Counter     -- POS & retail

EXTENDED EXISTING CONTEXTS (2):
  Commerce    -- + CRM, buying clubs, wholesale accounts
  Ledger      -- + Enterprise P&L, cost allocation, COGS

CROSS-CUTTING (1):
  Codex       -- Knowledge & SOPs, referenced by all contexts
```

### Boundary Decisions

| Decision | Reasoning |
|----------|-----------|
| Crew separate from Commerce | Workers are not customers; labor scheduling differs from order processing |
| Campus separate from Commerce | Events have capacity, curricula, waivers -- different from product orders |
| Counter separate from Commerce | POS is real-time walk-up; Commerce is async ordering. Different cadence |
| Codex is its own context | SOPs referenced by every context; avoids duplicating knowledge management |
| CRM extends Commerce | Customer profiles are natural extension of existing CSA/order model |
| Enterprise accounting extends Ledger | P&L and COGS are projections over existing expense/revenue events |

---

## Domain Models

### Context 1: Crew (People & Labor)

**Aggregates:** Worker, Shift, ApprenticeProgram

**Types:**
```
WorkerId(Guid), ShiftId(Guid), ApprenticeProgramId(Guid)

WorkerRole: Employee | Apprentice | Volunteer | Intern
WorkerStatus: Active | OnLeave | Completed | Terminated
CertificationType: FoodHandler | FirstAid | CPR | PesticideApplicator |
                   EquipmentOperator | CDL | OrganicInspector | Custom
Enterprise: Pasture | Flora | Hearth | Apiary | Commerce | Assets | General

Certification(CertificationType, string Name, DateOnly Issued, DateOnly? Expires,
              string? IssuingBody, string? DocumentPath)
EmergencyContact(string Name, string Relationship, string Phone)
WorkerProfile(string Name, string Email, string? Phone, WorkerRole Role,
             EmergencyContact? Emergency, string? HousingAssignment, DateOnly StartDate)
ShiftEntry(WorkerId, Enterprise, DateOnly Date, TimeOnly Start, TimeOnly End,
          string? TaskDescription, string? Notes)
RotationAssignment(Enterprise, DateOnly StartDate, DateOnly EndDate, string? Mentor)
```

**Events:**
- WorkerRegistered, WorkerProfileUpdated, WorkerDeactivated
- CertificationAdded, CertificationExpired
- ShiftScheduled, ShiftStarted, ShiftCompleted, ShiftCancelled
- ApprenticeRotated, ProgramCreated, ProgramCohortStarted, ProgramCompleted
- IncidentReported

**Business Rules:**
- Cannot schedule shift for deactivated worker
- Certification expiry generates alert events (consumed by Compliance)
- Shift completion hours feed into Ledger for labor cost allocation
- Apprentice rotation enforces min/max weeks per enterprise

---

### Context 2: Campus (Agritourism & Education)

**Aggregates:** Event, Booking, Curriculum

**Types:**
```
EventId(Guid), BookingId(Guid), CurriculumId(Guid)

EventType: FarmTour | Workshop | FieldDay | ClassroomSession | PrivateTour | FarmDinner
EventStatus: Draft | Published | Full | InProgress | Completed | Cancelled
BookingStatus: Reserved | Confirmed | CheckedIn | NoShow | Cancelled

EventSchedule(DateOnly Date, TimeOnly Start, TimeOnly End, string Location,
             int Capacity, decimal PricePerPerson, decimal? GroupRate)
WaiverInfo(string SignedBy, DateTimeOffset SignedAt, string? DocumentPath)
AttendeeInfo(string Name, string Email, string? Phone, int PartySize, string? DietaryNotes)
CurriculumModule(string Title, string Description, int DurationMinutes,
                string? MaterialsNeeded, string? InstructorNotes)
```

**Events:**
- EventCreated, EventPublished, EventCancelled, EventCompleted
- BookingCreated, BookingConfirmed, BookingCheckedIn, BookingCancelled, WaiverSigned
- CurriculumCreated, CurriculumModuleAdded, CurriculumArchived

**Business Rules:**
- Cannot book beyond event capacity
- Booking confirmation requires waiver for farm tours (liability)
- Event completion publishes integration event to Ledger (tour revenue)
- Recurring events created from templates

---

### Context 3: Compliance (Regulatory & Permits)

**Aggregates:** Permit, InsurancePolicy, Grant

**Types:**
```
PermitId(Guid), PolicyId(Guid), GrantId(Guid)

PermitType: BusinessLicense | FoodProcessing | RetailFood | SalesTax | ZoningUse |
           OrganicCertification | GAPCertification | CottageFoodExemption |
           HealthDepartment | WeightsAndMeasures | Custom
PermitStatus: Active | PendingRenewal | Expired | Revoked
PolicyType: GeneralLiability | Property | Equipment | WorkersComp |
           ProductLiability | CommercialAuto | UmbrellaPolicy
GrantStatus: Applied | Awarded | Active | Reporting | Closed | Denied

RenewalInfo(DateOnly RenewalDate, decimal? Fee, string? Notes)
CoverageDetail(string CoverageType, decimal Limit, decimal Deductible)
GrantMilestone(string Description, DateOnly DueDate, bool Completed, string? ReportPath)
```

**Events:**
- PermitRegistered, PermitRenewed, PermitExpired, PermitRevoked
- PolicyRegistered, PolicyRenewed, PolicyExpired, PolicyCoverageUpdated
- GrantApplied, GrantAwarded, GrantMilestoneCompleted, GrantReportFiled, GrantClosed
- ComplianceAlertRaised

**Business Rules:**
- Auto-raise ComplianceAlertRaised 30/60/90 days before expiry
- Organic certification tracks 5-year rolling documentation window
- Cottage food permits enforce state-specific annual revenue caps
- Grant milestones trigger compliance alerts when due dates approach

---

### Context 4: Counter (POS & Retail)

**Aggregates:** Register, Sale, CashDrawer

**Types:**
```
RegisterId(Guid), SaleId(Guid), CashDrawerId(Guid)

RegisterLocation: FarmStore | Cafe | FarmersMarket | PopUp
PaymentMethod: Cash | Card | Check | EBT | Comped
TaxCategory: NonTaxable | StandardFood | PreparedFood | NonFood

SaleLineItem(string ProductName, string? SKU, int Quantity, decimal UnitPrice,
            TaxCategory TaxCat, string? Notes)
PaymentRecord(PaymentMethod Method, decimal Amount, string? Reference)
DrawerCount(decimal Expected, decimal Actual, string? Notes)
```

**Events:**
- RegisterOpened, RegisterClosed
- SaleCompleted, SaleVoided, SaleRefunded
- CashDrawerOpened, CashDrawerCounted, CashDrawerReconciled

**Business Rules:**
- Sale completion publishes integration event to Ledger (retail revenue)
- Tax calculated per line item based on TaxCategory
- Drawer reconciliation flags discrepancy if actual != expected beyond threshold
- EBT payment only valid for qualifying food items (non-prepared)

---

### Context 5: Commerce Extension (CRM + Buying Clubs)

**New Aggregates (added to existing Commerce):** Customer, BuyingClub, WholesaleAccount

**New Types:**
```
CustomerId(Guid), BuyingClubId(Guid), WholesaleAccountId(Guid)

CustomerChannel: CSA | BuyingClub | FarmStore | FarmersMarket | Wholesale | Online | Tour
AccountTier: Standard | Premium | Wholesale
ClubStatus: Active | Paused | Closed
OrderCycleFrequency: Weekly | BiWeekly | Monthly

CustomerProfile(string Name, string Email, string? Phone, string? Address,
               IReadOnlyList<CustomerChannel> Channels, string? Notes, string? DietaryPrefs)
DropSite(string Name, string Address, string ContactPerson, string ContactPhone,
        DayOfWeek DeliveryDay, TimeOnly DeliveryWindow)
StandingOrder(string ProductName, Quantity Qty, decimal UnitPrice, string? Notes)
DeliveryRoute(string Name, IReadOnlyList<string> DropSiteIds, decimal EstimatedMiles)

MatchCandidate(CustomerId ExistingId, string ExistingName,
    string? ExistingEmail, decimal ConfidenceScore, string MatchBasis)
```

**New Events:**
- CustomerCreated, CustomerProfileUpdated, CustomerNoteAdded
- DuplicateSuspected, CustomersMerged, DuplicateDismissed
- BuyingClubCreated, DropSiteAdded, DropSiteRemoved, OrderCycleOpened, OrderCycleClosed
- WholesaleAccountOpened, StandingOrderSet, StandingOrderCancelled, DeliveryRouteAssigned

**Business Rules:**
- Customer is cross-channel identity -- CSA members, buyers, attendees link to one Customer
- Buying club order cycles open/close on schedule (e.g., every 6 weeks)
- Wholesale standing orders auto-generate Draft orders at cycle frequency
- Existing CSAMember gets CustomerId? foreign reference (backward-compatible)

**Customer Dedup Strategy:**
- Layer 1: Deterministic match on email (case-insensitive) -- auto-link at 100% confidence
- Layer 2: Probabilistic match on normalized name + phone -- auto-merge at >= 90%, flag for review at 50-89%
- Layer 3: Manual merge via DuplicateSuspect review UI
- Merge preserves both records (event sourcing) -- absorbed customer marked as merged, not deleted

---

### Context 6: Ledger Extension (Enterprise Accounting)

**No new aggregates.** Extends existing Expense/Revenue with enterprise tagging and adds read-side projections.

**Type Changes:**
```
// Extend LedgerContext enum:
LedgerContext: ... | Crew | Campus | Counter | Compliance

// Extend ExpenseCategory enum:
ExpenseCategory: ... | Permits | Certification | GrantMatch | Tour | Wages | Stipend

// Extend RevenueCategory enum:
RevenueCategory: ... | Tours | Workshops | BuyingClub | Wholesale | CafeFood | CafeBeverage | Retail

// New types:
EnterpriseCode(LedgerContext Context, string? SubEnterprise)
CostAllocationRule(EnterpriseCode From, EnterpriseCode To, decimal Percentage, string Basis)
```

**New Events:**
- ExpenseEnterpriseTagged, RevenueEnterpriseTagged, CostAllocationRuleSet

**New Projections (read-side only):**
- EnterpriseProfitLossProjection -- P&L per enterprise with shared cost allocation
- COGSProjection -- cost of goods sold per product
- BudgetVarianceProjection -- budget vs actual by enterprise and month
- TaxCategoryProjection -- Schedule F line item mapping

---

### Context 7: Codex (Knowledge & SOPs)

**Aggregates:** Procedure, Playbook, DecisionTree

**Types:**
```
ProcedureId(Guid), PlaybookId(Guid), DecisionTreeId(Guid)

ProcedureCategory: Pasture | Flora | Hearth | Apiary | Commerce | Assets |
                   Safety | Compliance | Onboarding | General
ProcedureStatus: Draft | Published | Archived
AudienceRole: Everyone | Employee | Apprentice | Manager

ProcedureStep(int Order, string Title, string Instructions, string? ImagePath,
             string? WarningNote, int? EstimatedMinutes)
PlaybookTask(int Month, string Title, string Description, ProcedureCategory Category,
            string? LinkedProcedureId, string Priority)
DecisionNode(string Id, string Question, string? YesNodeId, string? NoNodeId,
            string? ActionIfTerminal, string? Notes)
```

**Events:**
- ProcedureCreated, ProcedureStepAdded, ProcedurePublished, ProcedureRevised, ProcedureArchived
- PlaybookCreated, PlaybookTaskAdded, PlaybookTaskRemoved
- DecisionTreeCreated, DecisionNodeAdded, DecisionNodeUpdated

**Business Rules:**
- Publishing new revision auto-archives previous version
- Procedures linked to Compliance permits
- Playbook tasks can reference Procedures by ID
- Onboarding checklists are Playbooks with AudienceRole = Apprentice

---

## Cross-Context Integration

### Integration Events (via RabbitMQ)

| Source | Event | Consumer | Purpose |
|--------|-------|----------|---------|
| Crew | ShiftCompletedIntegration | Ledger | Labor cost allocation |
| Crew | CertificationExpiringIntegration | Compliance | Cert expiry tracking |
| Campus | EventRevenueIntegration | Ledger | Tour/workshop revenue |
| Campus | AttendeeCustomerLinkIntegration | Commerce | Attendee -> Customer linkage |
| Counter | RetailSaleIntegration | Ledger | Retail revenue recording |
| Counter | RetailPurchaseIntegration | Commerce | Customer purchase history |
| Commerce | CustomersMergedIntegration | Campus, Counter | Update read models |
| Compliance | ComplianceAlertIntegration | Any subscriber | Renewal/expiry alerts |

### Data Ownership

| Data | Owner (writes) | Readers |
|------|---------------|---------|
| Worker profiles, certs, shifts | Crew | Ledger, Compliance |
| Events, bookings, waivers | Campus | Ledger, Commerce |
| POS sales, cash drawers | Counter | Ledger, Commerce |
| Customer profiles, dedup | Commerce | Campus, Counter |
| Buying clubs, wholesale | Commerce | Ledger |
| Permits, insurance, grants | Compliance | All (alerts) |
| SOPs, playbooks, decision trees | Codex | All (reference links) |
| Enterprise P&L, COGS | Ledger | All (dashboards) |

### Customer Identity

Customer aggregate in Commerce is the cross-channel identity hub. Linked by:
- CSAMember.CustomerId (Commerce)
- Booking.AttendeeEmail -> Customer match (Campus -> Commerce via integration event)
- Sale.CustomerName -> Customer match (Counter -> Commerce via integration event)
- BuyingClub.MemberIds (Commerce)
- WholesaleAccount.CustomerId (Commerce)

### Name Normalization (SharedKernel utility)

For dedup fuzzy matching:
- Lowercase, trim, collapse whitespace
- Strip punctuation
- Handle "Last, First" -> "First Last"
- Levenshtein distance for similarity scoring

---

## Frontend Apps

### New Apps

| App | Context | Purpose |
|-----|---------|---------|
| Crew-OS | Crew | Worker profiles, shift scheduling, apprentice rotations, certifications |
| Campus-OS | Campus | Event management, booking, curricula, attendance |
| Counter-OS | Counter | POS terminal, sale flow, cash drawer, daily reconciliation |
| Commerce-OS | Commerce + Ledger | CRM, buying clubs, wholesale, customer dedup, enterprise P&L |
| Codex-OS | Codex | SOP editor, playbook browser, decision tree viewer |

### Extended Apps

| App | Extension | Purpose |
|-----|-----------|---------|
| Asset-OS | + Compliance | Permits, insurance, grants alongside equipment/structures |

### API Surface

#### Crew: /api/crew/
- POST /workers, PUT /workers/{id}/profile, POST /workers/{id}/deactivate
- POST /workers/{id}/certifications
- POST /shifts, POST /shifts/{id}/start, POST /shifts/{id}/complete, POST /shifts/{id}/cancel
- POST /programs, POST /programs/{id}/rotate
- POST /incidents
- GET /workers, GET /workers/{id}, GET /shifts, GET /programs/{id}, GET /reports/labor-hours

#### Campus: /api/campus/
- POST /events, POST /events/{id}/publish, POST /events/{id}/cancel, POST /events/{id}/complete
- POST /bookings, POST /bookings/{id}/confirm, POST /bookings/{id}/checkin, POST /bookings/{id}/cancel
- POST /bookings/{id}/waiver
- POST /curricula, POST /curricula/{id}/modules
- GET /events, GET /events/{id}, GET /curricula, GET /reports/attendance

#### Compliance: /api/compliance/
- POST /permits, POST /permits/{id}/renew
- POST /policies, POST /policies/{id}/renew
- POST /grants, POST /grants/{id}/award, POST /grants/{id}/milestone, POST /grants/{id}/report
- GET /permits, GET /policies, GET /grants, GET /alerts, GET /dashboard

#### Counter: /api/counter/
- POST /registers/{id}/open, POST /registers/{id}/close
- POST /sales, POST /sales/{id}/void, POST /sales/{id}/refund
- POST /drawers/{id}/count, POST /drawers/{id}/reconcile
- GET /registers, GET /sales, GET /drawers/{id}, GET /reports/daily

#### Commerce Extensions: /api/commerce/
- POST /customers, PUT /customers/{id}/profile, POST /customers/{id}/notes
- POST /customers/merge, POST /customers/dismiss-duplicate
- POST /buying-clubs, POST /buying-clubs/{id}/drop-sites
- POST /buying-clubs/{id}/cycle/open, POST /buying-clubs/{id}/cycle/close
- POST /wholesale, POST /wholesale/{id}/standing-order, POST /wholesale/{id}/route
- GET /customers, GET /customers/{id}, GET /buying-clubs, GET /wholesale
- GET /reports/customer-value

#### Ledger Extensions: /api/ledger/
- POST /expenses/{id}/tag-enterprise, POST /revenue/{id}/tag-enterprise
- POST /cost-allocation
- GET /reports/enterprise-pnl, GET /reports/cogs, GET /reports/budget-variance
- GET /reports/tax-categories

#### Codex: /api/codex/
- POST /procedures, POST /procedures/{id}/steps, POST /procedures/{id}/publish
- POST /procedures/{id}/revise, POST /procedures/{id}/archive
- POST /playbooks, POST /playbooks/{id}/tasks
- POST /decision-trees, POST /decision-trees/{id}/nodes
- GET /procedures, GET /procedures/{id}, GET /playbooks, GET /playbooks/{id}
- GET /decision-trees/{id}, GET /search

---

## Implementation Phasing

### Phase 1: Foundation (parallel)
- **Crew** -- workers, shifts, certifications (no apprentice programs yet)
- **Compliance** -- permits, insurance policies (no grants yet)
- **Codex** -- procedures and playbooks (no decision trees yet)
- **Commerce CRM** -- Customer aggregate, dedup, customer profiles

### Phase 2: Revenue Channels (depends on Phase 1)
- **Campus** -- events, bookings, waivers, curricula
- **Counter** -- registers, sales, cash drawers
- **Commerce Buying Clubs** -- buying clubs, drop sites, order cycles
- **Commerce Wholesale** -- wholesale accounts, standing orders, delivery routes

### Phase 3: Financial Intelligence (depends on Phase 1+2)
- **Ledger Enterprise Accounting** -- enterprise tagging, P&L, COGS, budget variance
- **Crew Apprentice Programs** -- full rotation management
- **Compliance Grants** -- grant tracking, milestones, reporting
- **Codex Decision Trees** -- diagnostic flowcharts

### Phase 4: Infrastructure (future)
- **Authentik** -- OIDC/SCIM for system authentication
- **Task & Workflow Engine** -- cross-context task management
- **Mapping & Spatial** -- GIS farm map
- **Weather Integration** -- automated weather data
- **Mobile & Offline** -- offline-first data entry

---

## Dependency Graph

```
Phase 1 (parallel):
  Crew ────────────────┐
  Compliance ──────────┤── no dependencies between these
  Codex ───────────────┤
  Commerce CRM ────────┘

Phase 2 (soft dep on Phase 1):
  Campus ──────── needs Commerce.Customer for attendee linkage
  Counter ─────── needs Commerce.Customer for purchase history
  Buying Clubs ── needs Commerce.Customer
  Wholesale ───── needs Commerce.Customer

Phase 3 (needs Phase 1+2 events):
  Ledger Ext ──── needs all contexts publishing revenue/expense events
  Apprentice ──── needs Crew base
  Grants ──────── needs Compliance base
  Decision Trees ─ needs Codex base

Phase 4 (infrastructure):
  Authentik ───── needs Crew (SCIM sync source)
  Tasks ─────────── needs all contexts (cross-context assignment)
```

## Critical Files (Most Modified)

**New projects (4 new bounded contexts):**
- src/FarmOS.Crew.Domain/, .Application/, .Infrastructure/, .API/
- src/FarmOS.Campus.Domain/, .Application/, .Infrastructure/, .API/
- src/FarmOS.Counter.Domain/, .Application/, .Infrastructure/, .API/
- src/FarmOS.Codex.Domain/, .Application/, .Infrastructure/, .API/

**Extended projects:**
- src/FarmOS.Commerce.Domain/Types.cs -- new customer/club/wholesale types
- src/FarmOS.Commerce.Domain/Events.cs -- 12+ new events
- src/FarmOS.Commerce.Domain/Aggregates/ -- 3 new aggregates
- src/FarmOS.Ledger.Domain/Types.cs -- extended enums + EnterpriseCode
- src/FarmOS.Ledger.Domain/Events.cs -- 3 new events
- src/FarmOS.SharedKernel/IntegrationEvents.cs -- 7 new integration events
- src/FarmOS.SharedKernel/StringNormalization.cs -- dedup utility (new)

**New frontend apps:**
- frontend/crew-os/
- frontend/campus-os/
- frontend/counter-os/
- frontend/commerce-os/
- frontend/codex-os/

**Extended frontend:**
- frontend/asset-os/ -- add Compliance views
