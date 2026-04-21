# API Reference — Compliance Context

> Permits, insurance policies, and grant tracking for farm regulatory compliance.

**Base URL**: `/api/compliance`
**Gateway**: `http://localhost:5050`

> **Note**: This context handles farm-level regulatory compliance (permits, insurance, grants). Kitchen/food-safety compliance (HACCP, sanitation, CAPA, FSMA 204) is in the [Hearth API](api-reference-hearth.md).

---

## Permits

### `POST /api/compliance/permits`
Register a new permit.

**Body**: `RegisterPermitCommand`
```json
{
  "name": "Food Sales Establishment License",
  "issuingAuthority": "Georgia Department of Agriculture",
  "permitNumber": "FSE-2026-1234",
  "issuedDate": "2026-01-15",
  "expirationDate": "2027-01-15",
  "category": "FoodSafety"
}
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/compliance/permits/{id}/renew`
Renew a permit with new expiration.

**Body**: `RenewPermitCommand`
```json
{ "newExpirationDate": "2028-01-15", "renewalCost": 250.00 }
```
**Response**: `204 No Content`

### `POST /api/compliance/permits/{id}/revoke`
Revoke a permit.

**Body**: `RevokePermitCommand` → `{ "reason": "Voluntary surrender" }`
**Response**: `204 No Content`

### `POST /api/compliance/permits/{id}/expire`
Mark a permit as expired.

**Response**: `204 No Content`

---

## Insurance Policies

### `POST /api/compliance/policies`
Register an insurance policy.

**Body**: `RegisterPolicyCommand`
```json
{
  "name": "Farm Liability",
  "provider": "Farm Bureau",
  "policyNumber": "FB-2026-5678",
  "startDate": "2026-03-01",
  "endDate": "2027-03-01",
  "annualPremium": 1200.00,
  "coverages": ["GeneralLiability", "ProductLiability", "PropertyDamage"]
}
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/compliance/policies/{id}/renew`
Renew a policy.

**Body**: `RenewPolicyCommand`
**Response**: `204 No Content`

### `POST /api/compliance/policies/{id}/expire`
Mark a policy as expired.

**Response**: `204 No Content`

### `PUT /api/compliance/policies/{id}/coverages`
Update policy coverage list.

**Body**: `UpdateCoveragesCommand`
```json
{ "coverages": ["GeneralLiability", "ProductLiability", "PropertyDamage", "WorkersComp"] }
```
**Response**: `204 No Content`

---

## Grants

### `POST /api/compliance/grants`
Apply for a grant.

**Body**: `ApplyForGrantCommand`
```json
{
  "name": "NRCS EQIP Conservation Grant",
  "grantingOrganization": "USDA NRCS",
  "applicationDate": "2026-02-01",
  "requestedAmount": 15000.00,
  "purpose": "Silvopasture establishment"
}
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/compliance/grants/{id}/award`
Mark a grant as awarded.

**Body**: `AwardGrantCommand`
```json
{ "awardedAmount": 12000.00, "awardDate": "2026-04-01" }
```
**Response**: `204 No Content`

### `POST /api/compliance/grants/{id}/deny`
Mark a grant application as denied.

**Body**: `DenyGrantCommand` → `{ "reason": "Funding exhausted" }`
**Response**: `204 No Content`

### `POST /api/compliance/grants/{id}/milestones`
Add a milestone to a grant.

**Body**: `AddGrantMilestoneCommand`
```json
{ "description": "Plant 200 trees", "dueDate": "2026-10-01" }
```
**Response**: `204 No Content`

### `POST /api/compliance/grants/{id}/milestones/{description}/complete`
Complete a grant milestone.

**Body**: `CompleteGrantMilestoneCommand`
**Response**: `204 No Content`

### `POST /api/compliance/grants/{id}/close`
Close a grant (all milestones completed, final report submitted).

**Response**: `204 No Content`
