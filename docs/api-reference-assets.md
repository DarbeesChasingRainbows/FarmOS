# API Reference — Assets Context

> Equipment, structures, water sources, compost batches, materials inventory, and Home Assistant sensor bridge.

**Base URL**: `/api/assets`
**Gateway**: `http://localhost:5050`

---

## Equipment

### `GET /api/assets/equipment`
List all equipment.

**Response**: `200 OK` → `EquipmentDto[]`

### `GET /api/assets/equipment/{id}`
Get equipment detail.

**Response**: `200 OK` → `EquipmentDto`, or `404 Not Found`

### `POST /api/assets/equipment`
Register new equipment.

**Body**: `RegisterEquipmentCommand`
```json
{
  "name": "Kubota BX2380",
  "equipmentType": "Tractor",
  "manufacturer": "Kubota",
  "model": "BX2380",
  "serialNumber": "12345",
  "purchaseDate": "2024-06-01",
  "purchasePrice": 18500.00
}
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/assets/equipment/{id}/maintenance`
Record a maintenance event.

**Body**: `RecordEquipmentMaintenanceCommand`
```json
{ "description": "Oil change + filter", "mechanic": "Self", "cost": 45.00, "hoursAtService": 250 }
```
**Response**: `204 No Content`

### `POST /api/assets/equipment/{id}/move`
Update equipment location.

**Body**: `MoveEquipmentCommand`
**Response**: `204 No Content`

### `POST /api/assets/equipment/{id}/retire`
Retire equipment.

**Body**: `RetireEquipmentCommand`
**Response**: `204 No Content`

---

## Structures

### `POST /api/assets/structures`
Register a structure (barn, greenhouse, cooler, etc.).

**Body**: `RegisterStructureCommand`
```json
{ "name": "Walk-in Cooler A", "structureType": "Cooler", "isHACCPZone": true }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/assets/structures/{id}/maintenance`
Record structure maintenance.

**Body**: `RecordStructureMaintenanceCommand`
**Response**: `204 No Content`

---

## Water Sources

### `POST /api/assets/water`
Register a water source.

**Body**: `RegisterWaterSourceCommand`
```json
{ "name": "Main Well", "waterType": "Well", "latitude": 34.87, "longitude": -85.29, "capacityGallons": 500, "flowRateGPM": 8.5 }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/assets/water/{id}/test`
Record a water quality test.

**Body**: `RecordWaterTestCommand`
**Response**: `204 No Content`

---

## Compost Batches

### `GET /api/assets/compost`
List all compost batches.

**Response**: `200 OK` → `CompostBatchSummaryDto[]`

### `GET /api/assets/compost/{id}`
Get compost batch detail (temperature log, turning log, phases).

**Response**: `200 OK` → `CompostBatchDetailDto`, or `404 Not Found`

### `POST /api/assets/compost`
Start a new compost batch.

**Body**: `StartCompostBatchCommand`
```json
{ "name": "Windrow 2026-04", "method": "Windrow", "carbonNitrogenRatio": 30.0 }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/assets/compost/{id}/temp`
Record a temperature reading.

**Body**: `RecordCompostTempCommand` → `{ "temperatureF": 145.0, "moisturePct": 55.0 }`
**Response**: `204 No Content`

### `POST /api/assets/compost/{id}/turn`
Record a turning event.

**Body**: `TurnCompostCommand` → `{ "notes": "Added water" }`
**Response**: `204 No Content`

### `POST /api/assets/compost/{id}/phase`
Change compost phase (Active → Curing → Finished → Applied).

**Body**: `ChangeCompostPhaseCommand` → `{ "newPhase": "Curing" }`
**Response**: `204 No Content`

### `POST /api/assets/compost/{id}/inoculate`
Record inoculation (worm casting, mycorrhizae, etc.).

**Body**: `InoculateCompostCommand`
**Response**: `204 No Content`

### `POST /api/assets/compost/{id}/ph`
Record pH measurement.

**Body**: `MeasureCompostPhCommand`
**Response**: `204 No Content`

### `POST /api/assets/compost/{id}/note`
Add a free-form note.

**Body**: `AddCompostNoteCommand`
**Response**: `204 No Content`

### `POST /api/assets/compost/{id}/complete`
Mark batch complete / ready for application.

**Body**: `CompleteCompostBatchCommand`
**Response**: `204 No Content`

---

## Materials Inventory

### `POST /api/assets/materials`
Register a new material.

**Body**: `RegisterMaterialCommand`
```json
{ "name": "Azomite", "materialType": "Amendment", "quantityValue": 50, "quantityUnit": "lbs", "supplier": "Seven Springs", "isOrganic": true }
```
**Response**: `201 Created` → `{ "id": "guid" }`

### `POST /api/assets/materials/{id}/use`
Record material usage (withdrawal).

**Body**: `UseMaterialCommand` → `{ "quantity": 5, "unit": "lbs", "purpose": "Top-dress Bed A3" }`
**Response**: `204 No Content`

### `POST /api/assets/materials/{id}/restock`
Restock material inventory.

**Body**: `RestockMaterialCommand` → `{ "quantity": 50, "unit": "lbs", "supplier": "Seven Springs", "cost": 35.00 }`
**Response**: `204 No Content`

---

## Home Assistant Sensor Bridge

### `GET /api/assets/ha/status`
Check if Home Assistant is reachable.

**Response**: `200 OK` → `{ "available": true }`

### `GET /api/assets/ha/sensors`
List all sensors from Home Assistant.

**Response**: `200 OK` → sensor summary list

### `GET /api/assets/ha/sensors/{entityId}`
Get sensor detail by Home Assistant entity ID (e.g., `sensor.kitchen_temp`).

**Response**: `200 OK` → sensor detail DTO, or `404 Not Found`

### `GET /api/assets/ha/sensors/{entityId}/history?hours={int}`
Get sensor history (default: last 24 hours).

**Response**: `200 OK` → time-series readings
