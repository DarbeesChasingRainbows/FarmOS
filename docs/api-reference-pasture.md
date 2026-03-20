# Pasture API Reference

The Pasture bounded context manages livestock lifecycles, grazing rotations, herds, and paddock metrics (biomass, resting days, soils). It is the reference implementation for FarmOS's CQRS and Event Sourced backend.

All interactions with the Pasture API must route through the **Gateway** (`http://localhost:5050`) and include the Bearer token issued by `POST /api/auth/login`.

---

## 🔒 Authentication
**Header Required:** `Authorization: Bearer <token>`
*(See [Frontend Integration Guide](frontend-integration-guide.md) for details on acquiring this token via PIN auth).*

---

## 🐑 Paddocks

Paddocks are spatial boundaries of land where Herds are rotated. They track rest periods to prevent overgrazing.

### List Paddocks
Retrieves a summary of all paddocks.
**GET** `/api/pasture/paddocks`

**Response:** `200 OK`
```json
[
  {
    "id": "18f91db7-...",
    "name": "North Meadow",
    "acreage": 3.5,
    "landType": "Pasture",
    "status": "Resting",
    "restDaysElapsed": 14,
    "currentHerdId": null
  }
]
```

### Get Paddock Details
Retrieves detailed paddock data, including boundary coordinates and historical metrics.
**GET** `/api/pasture/paddocks/{id}`

**Response:** `200 OK`
*(Returns a `PaddockDetailDto` containing boundary geometry, latest soil tests, and biomass readings).*

### Create Paddock
**POST** `/api/pasture/paddocks`
```json
{
  "name": "South Silvopasture",
  "acreage": 4.2,
  "landType": "Silvopasture"
}
```
**Response:** `200 OK` (Returns the generated `id` structured object).

### Set Boundary
Updates the geospatial boundary of a paddock using GeoJSON.
**PUT** `/api/pasture/paddocks/{id}/boundary`
```json
{
  "geometry": {
    "type": "Polygon",
    "coordinates": [[[-85.1, 34.2], [-85.1, 34.3], [-85.2, 34.3], [-85.1, 34.2]]]
  }
}
```
**Response:** `204 No Content`

### Begin Grazing
Moves a herd into this paddock. **Domain Rule:** The API will return `400 Bad Request` if the paddock has rested for less than 45 days.
**POST** `/api/pasture/paddocks/{id}/begin-grazing`
```json
{
  "herdId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "date": "2026-03-01"
}
```
**Response:** `204 No Content`

### End Grazing
Removes the current herd and starts the paddock's rest period.
**POST** `/api/pasture/paddocks/{id}/end-grazing`
```json
{
  "date": "2026-03-04"
}
```
**Response:** `204 No Content`

### Record Biomass
Records a biomass transit estimate (Tons per Acre).
**POST** `/api/pasture/paddocks/{id}/biomass`
```json
{
  "tonsPerAcre": 1.2,
  "measuredOn": "2026-03-01",
  "method": "VisualEstimate"
}
```

### Record Soil Test
**POST** `/api/pasture/paddocks/{id}/soil-test`
```json
{
  "pH": 6.8,
  "organicMatterPct": 4.5,
  "carbonPct": 2.1,
  "testedOn": "2026-02-15",
  "lab": "Logan Labs"
}
```

---

## 🐄 Animals

Individual livestock tracking from birth/acquisition to butcher.

### List Animals
Returns a summary of all animals. Supports filtering.
**GET** `/api/pasture/animals?species=Cattle&status=Active`

### Get Animal Details
Returns the complete medical, pregnancy, and weight history of the animal.
**GET** `/api/pasture/animals/{id}`

### Register Animal
Registers a new animal onto the farm.
**POST** `/api/pasture/animals`
```json
{
  "tags": [
    { "type": "EarTag", "value": "A123" }
  ],
  "species": "Cattle",
  "breed": "Red Devon",
  "sex": "Female",
  "dateAcquired": "2025-01-10",
  "nickname": "Bessie"
}
```

### Isolate Animal
Moves an animal to sick bay/isolation for observation.
**POST** `/api/pasture/animals/{id}/isolate`
```json
{
  "reason": "Limping on front left",
  "date": "2026-03-01"
}
```

### Record Medical Treatment
**POST** `/api/pasture/animals/{id}/treatment`
```json
{
  "name": "Pinkeye burst",
  "dosage": "5cc",
  "route": "SubQ",
  "date": "2026-03-01",
  "notes": "Administered LA-200",
  "withdrawalPeriodDays": 28
}
```

### Record Weight
**POST** `/api/pasture/animals/{id}/weight`
```json
{
  "weight": {
    "value": 450,
    "unit": "lbs",
    "type": "mass"
  },
  "date": "2026-03-01"
}
```

### Butcher Animal
Ends the animal's lifecycle and transitions it to meat.
**POST** `/api/pasture/animals/{id}/butcher`
```json
{
  "date": "2026-10-15",
  "processor": "Tri-State Meats",
  "hangingWeight": {
    "value": 300,
    "unit": "lbs",
    "type": "mass"
  },
  "cutSheet": "Standard half/half"
}
```

---

## 🐃 Herds

Herds are logical groupings of animals that move through rotational grazing as a single unit (e.g., "The Flerd").

### List Herds
**GET** `/api/pasture/herds`
Returns all herds and their current paddock assignments.

### Get Herd Details
**GET** `/api/pasture/herds/{id}`
Returns herd details along with a fully populated list of `AnimalSummaryDto` members currently in the herd.

### Create Herd
**POST** `/api/pasture/herds`
```json
{
  "name": "Main Cattle Herd",
  "type": 0  
}
```
*(Type: 0=Cattle, 1=Sheep, 2=Pigs, 3=Poultry, 4=Mixed)*

### Move Herd
Moves a herd to an explicitly chosen paddock. *(Equivalent to calling `begin-grazing` on the paddock).*
**POST** `/api/pasture/herds/{id}/move`
```json
{
  "paddockId": "18f91db7-...",
  "date": "2026-03-01"
}
```

### Manage Members
Add or remove individual animals from the herd.
**POST** `/api/pasture/herds/{id}/add-animal`
**POST** `/api/pasture/herds/{id}/remove-animal`
```json
{
  "animalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---
## 🚨 Error Handling

All commands map to pure C# domain logic. If a rule is violated (e.g., selling a dead animal, grazing a resting field), the API intercepts the `Result.Failure` and returns a standard `400 Bad Request`.

**Error Example:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Domain Rule Violation",
  "status": 400,
  "detail": "Paddock requires 45 days of rest. Only 14 days have elapsed.",
  "extensions": {
    "errorType": "Conflict"
  }
}
```
