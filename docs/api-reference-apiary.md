# API Reference — Apiary Context

> Hives, inspections, treatments, queen tracking, colony splitting/merging, harvests, apiaries, financials, sensors, weather, and reporting.

**Base URL**: `/api/apiary`
**Gateway**: `http://localhost:5050`

---

## Hives

### `GET /api/apiary/hives`
List all hives with summary data.

**Response**: `200 OK` → `HiveSummaryDto[]`

### `POST /api/apiary/hives`
Create a new hive.

**Body**: `CreateHiveCommand`
```json
{ "name": "Hive Alpha", "type": "Langstroth", "latitude": 34.87, "longitude": -85.29 }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/apiary/hives/{id}/inspect`
Record a hive inspection.

**Body**: `InspectHiveCommand`
```json
{
  "date": "2026-04-20", "queenSeen": true, "broodPattern": "Solid",
  "estimatedFramesOfBees": 8, "temperament": "Calm",
  "miteCount": { "mites": 2, "bees": 300, "method": "AlcoholWash" },
  "observations": ["Good honey stores", "Queen cells on frame 4"],
  "queenCellsSeen": true
}
```
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/harvest`
Record a honey harvest.

**Body**: `HarvestHoneyCommand`
```json
{ "date": "2026-04-20", "extractedValue": 15.5, "extractedUnit": "lbs", "floralSource": "Wildflower", "moisturePct": 17.2 }
```
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/harvest/product`
Record a non-honey product harvest (wax, propolis, pollen).

**Body**: `HarvestProductCommand`
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/treat`
Record a treatment (mite treatment, medication, etc.).

**Body**: `TreatHiveCommand`
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/feed`
Record a feeding (sugar syrup, pollen patty).

**Body**: `FeedHiveCommand`
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/status`
Change hive status (Active, Queenless, Swarmed, Dead, Winterized).

**Body**: `ChangeHiveStatusCommand` → `{ "newStatus": "Winterized" }`
**Response**: `204 No Content`

---

## Queen Tracking

### `POST /api/apiary/hives/{id}/queen`
Introduce a new queen.

**Body**: `IntroduceQueenCommand`
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/queen/lost`
Mark queen as lost/dead.

**Body**: `MarkQueenLostCommand`
**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/queen/replace`
Replace the existing queen.

**Body**: `ReplaceQueenCommand`
**Response**: `204 No Content`

---

## Colony Management

### `POST /api/apiary/hives/{id}/split`
Split a colony into a new hive.

**Body**: `SplitColonyCommand`
**Response**: `201 Created` → `{ "id": "new-hive-guid" }`

### `POST /api/apiary/hives/{id}/merge`
Merge another colony into this (surviving) hive.

**Body**: `MergeColoniesCommand` → `{ "absorbedHiveId": "guid" }`
**Response**: `204 No Content`

---

## Equipment / Supers

### `POST /api/apiary/hives/{id}/super/add`
Add a super to the hive.

**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/super/remove`
Remove a super from the hive.

**Response**: `204 No Content`

### `POST /api/apiary/hives/{id}/configuration`
Update hive configuration (box count, type, etc.).

**Body**: `UpdateHiveConfigurationCommand`
**Response**: `204 No Content`

---

## Apiaries (Yard Locations)

### `GET /api/apiary/apiaries`
List all apiary locations.

**Response**: `200 OK` → `ApiarySummaryDto[]`

### `POST /api/apiary/apiaries`
Create a new apiary yard.

**Body**: `CreateApiaryCommand`
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/apiary/apiaries/{id}/hives`
Move a hive to this apiary.

**Body**: `MoveHiveToApiaryCommand` → `{ "hiveId": "guid" }`
**Response**: `204 No Content`

### `POST /api/apiary/apiaries/{id}/retire`
Retire an apiary location.

**Body**: `RetireApiaryCommand`
**Response**: `204 No Content`

---

## Reports & Analytics

### `GET /api/apiary/reports/mite-trends?hiveId={guid}`
Get mite count trends (optionally filtered to one hive).

### `GET /api/apiary/reports/yield?year={int}`
Get honey yield report (optionally filtered by year).

### `GET /api/apiary/reports/survival`
Get colony survival statistics.

### `GET /api/apiary/reports/weather-correlation`
Get weather-to-hive-activity correlation data.

---

## Seasonal Calendar

### `GET /api/apiary/calendar?month={int}`
Get seasonal beekeeping tasks. If `month` is omitted, returns all tasks.

---

## Financials

### `GET /api/apiary/financials/summary`
Get financial summary (revenue vs. expenses).

### `GET /api/apiary/financials/expenses`
List all apiary expenses.

### `GET /api/apiary/financials/revenue`
List all apiary revenue.

---

## IoT Sensors

### `GET /api/apiary/hives/{id}/sensors`
Get sensor readings for a hive.

### `GET /api/apiary/hives/{id}/sensors/summary`
Get latest sensor summary (temp, humidity, weight).

### `GET /api/apiary/hives/{id}/sensors/weight-trend`
Get weight trend time-series data.

---

## Weather

### `GET /api/apiary/weather/current?lat={double}&lng={double}`
Get current weather at the given coordinates.

**Response**: `200 OK` → weather snapshot, or `204 No Content` if unavailable.
