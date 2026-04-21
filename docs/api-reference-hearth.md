# API Reference — Hearth Context

> Sourdough, Kombucha, Living Cultures, Freeze-Dryer, Kitchen Compliance, FSMA 204 Traceability, HACCP, CAPA, and Fermentation Analytics.

**Base URL**: `/api/hearth`
**Gateway**: `http://localhost:5050`

---

## Sourdough Batches

### `POST /api/hearth/sourdough`
Start a new sourdough batch.

**Body**: `StartSourdoughCommand`
```json
{ "batchCode": "SD-2026-042", "starterId": "guid", "ingredients": [...] }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/sourdough/{id}/ccp`
Record a HACCP Critical Control Point reading.

**Body**: `RecordSourdoughCCPCommand`
```json
{ "criticalControlPoint": "Internal Bake Temperature", "temperatureF": 195.0, "pH": null, "withinLimits": true, "correctiveAction": null }
```
**Response**: `204 No Content`

> **Domain Rule**: If `withinLimits: false`, `correctiveAction` is **required**. The backend will reject the command without it.

### `POST /api/hearth/sourdough/{id}/advance`
Advance batch phase (Mixing → BulkFerment → Shaping → Proofing → Baking → Cooling → Complete).

**Body**: `AdvanceSourdoughPhaseCommand` → `{ "newPhase": "Baking" }`
**Response**: `204 No Content`

### `POST /api/hearth/sourdough/{id}/complete`
Mark batch as complete with yield.

**Body**: `CompleteSourdoughCommand` → `{ "yieldValue": 24, "yieldUnit": "loaves" }`
**Response**: `204 No Content`

---

## Kombucha Batches

### `POST /api/hearth/kombucha`
Start a new kombucha batch.

**Body**: `StartKombuchaCommand`
```json
{ "batchCode": "KB-2026-03", "scobyCultureId": "guid", "teaType": "Black", "sugarGrams": 200 }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/kombucha/{id}/ph`
Record a pH reading.

**Body**: `RecordKombuchaPHCommand` → `{ "pH": 3.8 }`
**Response**: `204 No Content`

> **Domain Rule**: If pH > 4.2 after 7 days → batch must be discarded per `KombuchaRules.validatePH`.

### `POST /api/hearth/kombucha/{id}/flavor`
Add secondary flavoring during F2.

**Body**: `AddKombuchaFlavoringCommand` → `{ "ingredient": "Ginger", "amount": 50, "unit": "grams" }`
**Response**: `204 No Content`

### `POST /api/hearth/kombucha/{id}/advance`
Advance phase (Primary → Secondary → Bottled → Complete).

**Body**: `AdvanceKombuchaPhaseCommand` → `{ "newPhase": "Secondary" }`
**Response**: `204 No Content`

### `POST /api/hearth/kombucha/{id}/complete`
Mark batch complete.

**Body**: `CompleteKombuchaCommand`
**Response**: `204 No Content`

---

## Living Cultures

### `POST /api/hearth/cultures`
Register a new living culture (starter or SCOBY).

**Body**: `CreateCultureCommand`
```json
{ "name": "Old Faithful", "type": "SourdoughStarter", "birthDate": "2024-01-15" }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/cultures/{id}/feed`
Record a feeding.

**Body**: `FeedCultureCommand`
```json
{ "flour": "Whole Wheat", "flourAmount": 100, "waterAmount": 100, "pH": 4.1 }
```
**Response**: `204 No Content`

### `POST /api/hearth/cultures/{id}/split`
Split a culture into a new offspring.

**Body**: `SplitCultureCommand` → `{ "newName": "Baby Faithful" }`
**Response**: `200 OK` → `{ "id": "new-guid" }`

---

## Freeze-Dryer (Harvest Right)

### `GET /api/hearth/freeze-dryer`
List all freeze-dryer batches.

**Response**: `200 OK` → `FreezeDryerBatchDto[]`

### `POST /api/hearth/freeze-dryer`
Start a new freeze-dryer batch.

**Body**: `StartFreezeDryerBatchCommand`
```json
{ "batchCode": "FD-2026-04", "productType": "Strawberries", "freshWeightOz": 32.0 }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/freeze-dryer/{id}/readings`
Record vacuum/temperature reading.

**Body**: `RecordFreezeDryerReadingCommand`
```json
{ "shelfTempF": -10.5, "vacuumMTorr": 250, "notes": "Stable" }
```
**Response**: `204 No Content`

### `POST /api/hearth/freeze-dryer/{id}/advance`
Advance phase (Freezing → PrimaryDrying → SecondaryDrying → Complete).

**Body**: `AdvanceFreezeDryerPhaseCommand` → `{ "newPhase": "PrimaryDrying" }`
**Response**: `204 No Content`

### `POST /api/hearth/freeze-dryer/{id}/complete`
Mark batch complete with dry weight.

**Body**: `CompleteFreezeDryerBatchCommand` → `{ "dryWeightOz": 4.5 }`
**Response**: `204 No Content`

### `POST /api/hearth/freeze-dryer/{id}/abort`
Abort a batch.

**Body**: `AbortFreezeDryerBatchCommand` → `{ "reason": "Power failure" }`
**Response**: `204 No Content`

---

## Kitchen Compliance

### `POST /api/hearth/kitchen/temps`
Log an equipment temperature reading.

**Body**: `LogEquipmentTemperatureCommand`
**Response**: `200 OK` → `{ "id": "guid" }`

### `POST /api/hearth/kitchen/temps/{id}/correction`
Append a correction to a temperature log entry.

**Body**: `AppendMonitoringCorrectionCommand`
**Response**: `204 No Content`

### `GET /api/hearth/kitchen/sanitation`
Get recent sanitation records.

**Response**: `200 OK` → sanitation record list

### `POST /api/hearth/kitchen/sanitation`
Log a sanitation event.

**Body**: `RecordSanitationCommand`
```json
{ "surfaceType": "Cutting Board", "cleaningMethod": "Hot Water + Quaternary", "sanitizerType": "Quat", "sanitizerPPM": 200, "notes": "Post-bake cleanup" }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/kitchen/certs`
Record a staff certification. *(Stub — returns `202 Accepted`)*

### `POST /api/hearth/kitchen/deliveries`
Log a delivery receipt. *(Stub — returns `202 Accepted`)*

---

## HACCP Plan Management

### `POST /api/hearth/compliance/haccp/plans`
Create a new HACCP plan.

**Body**: `CreateHACCPPlanCommand`
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/compliance/haccp/plans/{id}/ccps`
Add a CCP definition to a plan.

**Body**: `AddCCPDefinitionCommand`
```json
{ "product": "Sourdough", "ccpName": "Internal Bake Temperature", "criticalLimit": "≥ 190°F", "monitoring": "Probe thermometer", "correctiveAction": "Return to oven" }
```
**Response**: `204 No Content`

### `DELETE /api/hearth/compliance/haccp/plans/{id}/ccps?product=Sourdough&ccpName=...`
Remove a CCP definition.

**Response**: `204 No Content`

---

## CAPA (Corrective & Preventive Actions)

### `POST /api/hearth/compliance/capa`
Open a new CAPA record.

**Body**: `OpenCAPACommand`
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/hearth/compliance/capa/{id}/close`
Close a CAPA with resolution.

**Body**: `CloseCAPACommand`
**Response**: `204 No Content`

---

## FSMA 204 Traceability

### `POST /api/hearth/compliance/traceability/receiving`
Log a receiving Critical Tracking Event.

**Body**: `LogReceivingEventCommand`
**Response**: `200 OK` → `{ "id": "guid" }`

### `POST /api/hearth/compliance/traceability/transformation`
Log a transformation CTE.

**Body**: `LogTransformationEventCommand`
**Response**: `200 OK` → `{ "id": "guid" }`

### `POST /api/hearth/compliance/traceability/shipping`
Log a shipping CTE.

**Body**: `LogShippingEventCommand`
**Response**: `200 OK` → `{ "id": "guid" }`

### `GET /api/hearth/compliance/traceability/audit-report`
Download 24-hour FSMA audit CSV.

**Response**: `200 OK` → `text/csv` file download

---

## Traceability Graph

### `POST /api/hearth/compliance/traceability/graph/lots`
Create a traceability lot vertex.

### `POST /api/hearth/compliance/traceability/graph/batches`
Create a traceability batch vertex.

### `POST /api/hearth/compliance/traceability/graph/customers`
Create a customer vertex.

### `POST /api/hearth/compliance/traceability/graph/suppliers`
Create a supplier vertex.

### `POST /api/hearth/compliance/traceability/graph/lots/{lotId}/used-in/{batchId}`
Link a lot to a batch (edge).

### `POST /api/hearth/compliance/traceability/graph/batches/{batchId}/sold-to/{customerId}`
Link a batch to a customer.

### `POST /api/hearth/compliance/traceability/graph/lots/{lotId}/sourced-from/{supplierId}`
Link a lot to a supplier.

### `GET /api/hearth/compliance/traceability/graph/recall/{lotId}`
Get full recall graph (bidirectional traversal).

### `GET /api/hearth/compliance/traceability/graph/recall/{lotId}/forward`
Trace forward: lot → batches → customers.

### `GET /api/hearth/compliance/traceability/graph/recall/{batchId}/backward`
Trace backward: batch → lots → suppliers.

---

## Fermentation Analytics

### `GET /api/hearth/fermentation/{id}/analytics`
Get pH timeline for a batch.

**Response**: `200 OK` → pH timeline DTO, or `404`

### `GET /api/hearth/fermentation/active-monitoring`
Get all active batch summaries for the monitoring dashboard.

**Response**: `200 OK` → batch summary list

---

## Harvest Right IoT

### `GET /api/hearth/harvest-right/status`
Get MQTT connection status for Harvest Right dryers.

**Response**: `200 OK`
```json
{ "connected": true, "dryers": ["HR-001"], "lastTelemetryAt": "2026-04-20T..." }
```

---

## IoT Sensor Ingest

### `POST /api/hearth/iot/readings`
Ingest a sensor reading from kitchen/mushroom room sensors.

**Body**: `IngestSensorReadingCommand`
**Response**: `200 OK` → alert info (if threshold breached)

---

## SignalR Hub

### `/hubs/kitchen`
Real-time WebSocket hub for kitchen telemetry.

**Events pushed to clients**:
- `ReceiveTelemetry` — live sensor readings (temp, humidity, CO2)
- `ReceiveAlert` — threshold breach alerts
