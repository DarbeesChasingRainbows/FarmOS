# API Reference — Ledger Context

> Farm financial tracking — expenses, revenue, voiding, and enterprise tagging.

**Base URL**: `/api/ledger`
**Gateway**: `http://localhost:5050`

---

## Expenses

### `POST /api/ledger/expenses`
Record a new expense.

**Body**: `RecordExpenseCommand`
```json
{
  "category": "Feed",
  "amount": 285.00,
  "vendor": "Tractor Supply",
  "date": "2026-04-15",
  "description": "Layer feed, 10 bags",
  "boundedContext": "Pasture"
}
```
**Response**: `201 Created` → `{ "id": "guid" }`

**Categories**: `Feed`, `Seed`, `Equipment`, `Fuel`, `Vet`, `Packaging`, `Labor`, `Land`, `Supplies`, `Insurance`, `Utilities`

### `POST /api/ledger/expenses/{id}/void`
Void an expense (soft delete — event-sourced, never truly deleted).

**Body**: `VoidExpenseCommand`
**Response**: `204 No Content`

### `POST /api/ledger/expenses/{id}/tag-enterprise`
Tag an expense to a specific farm enterprise for Schedule F reporting.

**Body**: `TagExpenseEnterpriseCommand`
```json
{ "enterprise": "Hearth", "subEnterprise": "Sourdough" }
```
**Response**: `204 No Content`

---

## Revenue

### `POST /api/ledger/revenue`
Record revenue.

**Body**: `RecordRevenueCommand`
```json
{
  "source": "FarmersMarket",
  "amount": 450.00,
  "date": "2026-04-19",
  "description": "Saturday market — sourdough + honey",
  "boundedContext": "Hearth"
}
```
**Response**: `201 Created` → `{ "id": "guid" }`

**Sources**: `CSA`, `FarmersMarket`, `Wholesale`, `DirectSale`, `Online`, `Grant`

### `POST /api/ledger/revenue/{id}/void`
Void a revenue entry.

**Body**: `VoidRevenueCommand`
**Response**: `204 No Content`

### `POST /api/ledger/revenue/{id}/tag-enterprise`
Tag revenue to a specific enterprise.

**Body**: `TagRevenueEnterpriseCommand`
**Response**: `204 No Content`
