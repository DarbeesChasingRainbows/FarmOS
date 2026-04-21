# API Reference — IoT Context

> Device management, zone management, telemetry ingestion, climate logs, compliance reports, and excursion tracking.

**Base URL**: `/api/iot`
**Gateway**: `http://localhost:5050`

---

## Devices

### `GET /api/iot/devices`
List all registered IoT devices.

**Response**: `200 OK` → `DeviceSummaryDto[]`

### `GET /api/iot/devices/{id}`
Get device detail (including zone assignment, recent readings).

**Response**: `200 OK` → `DeviceDetailDto`, or `404 Not Found`

### `POST /api/iot/devices`
Register a new device.

**Body**: `RegisterDeviceCommand`
```json
{ "name": "Kitchen Temp Probe", "deviceType": "Temperature", "manufacturer": "ThermoWorks", "externalId": "HA-sensor.kitchen_temp" }
```
**Response**: `200 OK` → `{ "id": "guid" }`

### `PUT /api/iot/devices/{id}`
Update device metadata.

**Body**: `UpdateDeviceCommand` (must include `deviceId` matching path)
**Response**: `204 No Content`

### `POST /api/iot/devices/{id}/decommission`
Decommission a device (soft delete).

**Body**: `DecommissionDeviceCommand` (must include `deviceId` matching path)
**Response**: `204 No Content`

---

## Device Assignments

### `PUT /api/iot/devices/{id}/zone`
Assign a device to a monitoring zone.

**Body**: `AssignDeviceToZoneCommand`
```json
{ "deviceId": "guid", "zoneId": "guid" }
```
**Response**: `204 No Content`

### `DELETE /api/iot/devices/{id}/zone`
Unassign a device from its zone.

**Response**: `204 No Content`

### `PUT /api/iot/devices/{id}/asset`
Assign a device to a domain asset (e.g., a hive, paddock, structure).

**Body**: `AssignDeviceToAssetCommand`
```json
{ "deviceId": "guid", "context": "Apiary", "assetType": "Hive", "assetId": "guid" }
```
**Response**: `204 No Content`

### `DELETE /api/iot/devices/{id}/asset/{context}/{assetType}/{assetId}`
Unassign a device from a specific domain asset.

**Response**: `204 No Content`

---

## Zones

### `GET /api/iot/zones`
List all monitoring zones.

**Response**: `200 OK` → `ZoneSummaryDto[]`

### `GET /api/iot/zones/{id}`
Get zone detail (including devices, thresholds, latest readings).

**Response**: `200 OK` → `ZoneDetailDto`, or `404 Not Found`

### `POST /api/iot/zones`
Create a new monitoring zone.

**Body**: `CreateZoneCommand`
```json
{ "name": "Kitchen", "type": "Indoor", "thresholds": { "tempMinF": 35, "tempMaxF": 90, "humidityMaxPct": 80 } }
```
**Response**: `200 OK` → `{ "id": "guid" }`

### `PUT /api/iot/zones/{id}`
Update zone configuration (name, thresholds).

**Body**: `UpdateZoneCommand` (must include `zoneId` matching path)
**Response**: `204 No Content`

### `POST /api/iot/zones/{id}/archive`
Archive a zone (soft delete).

**Body**: `ArchiveZoneCommand` (must include `zoneId` matching path)
**Response**: `204 No Content`

---

## Telemetry

### `POST /api/iot/telemetry/readings`
Ingest a sensor reading (used by HA polling worker or direct POST).

**Body**: `RecordTelemetryReadingCommand`
```json
{ "deviceId": "guid", "metric": "temperature", "value": 72.5, "unit": "°F", "timestamp": "2026-04-20T14:30:00Z" }
```
**Response**: `202 Accepted`

### `GET /api/iot/telemetry/zones/{zoneId}/climate-log?from={ISO}&to={ISO}`
Get time-series climate readings for a zone.

**Response**: `200 OK` → climate log DTO, or `404 Not Found`

### `GET /api/iot/telemetry/zones/{zoneId}/compliance-report?from={ISO}&to={ISO}`
Get compliance report for a zone (out-of-range events, excursions).

**Response**: `200 OK` → compliance report DTO, or `404 Not Found`

### `GET /api/iot/telemetry/excursions/active`
Get all currently active threshold excursions.

**Response**: `200 OK` → `ExcursionDto[]`

---

## SignalR Hub

### `/hubs/sensors`
Real-time WebSocket hub for sensor telemetry.

**Events pushed to clients**:
- `ReceiveReading` — live sensor readings
- `ReceiveExcursion` — threshold breach alerts
