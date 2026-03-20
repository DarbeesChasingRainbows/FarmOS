# Flora Context API Reference

The Flora Bounded Context API manages everything related to plants, including **Orchard Guilds** (permanent polycultures), **Flower Beds** (annual cultivation), and **Seed Inventory**. The Gateway safely routes traffic based on the `/api/flora` path prefix.

> **Gateway URL:** `http://localhost:5050/api/flora`
> **Internal Service:** `FarmOS.Flora.API`

---

## 🌳 Orchard Guilds
Guilds represent permanent or semi-permanent, multi-species plantings focused around a primary anchor crop (e.g., an Apple tree with comfrey, yarrow, and daffodils).

### **Create Guild**
**`POST /guilds`**
Registers a new orchard guild at a given geographic position.

**Request Body:**
```json
{
  "name": "N.A.P. Apple Core",
  "type": 0,                // 0: NAP, 1: Trio, 2: Custom
  "position": {
    "latitude": 34.256,
    "longitude": -85.163
  },
  "planted": "2023-11-15"
}
```
**Response:** `201 Created`
```json
{ "id": "uuid-here" }
```

### **Add Guild Member**
**`POST /guilds/{id}/members`**
Assigns a secondary plant (e.g., dynamic accumulator) into the established guild.

**Request Body:**
```json
{
  "member": {
    "plantId": { "value": "uuid-here" },
    "species": "Symphytum officinale",
    "cultivar": "Bocking 14",
    "role": 3               // 3: DynamicAccumulator
  }
}
```
**Response:** `204 No Content`

---

## 🌻 Flower Beds
Beds represent managed growing areas, typically for intensive high-margin production (like cut flowers), and support crop successions over a season.

### **Create Flower Bed**
**`POST /beds`**
Initializes a new bed within a growing block.

**Request Body:**
```json
{
  "name": "Bed A1",
  "block": "South Terrace",
  "dimensions": {
    "lengthFeet": 50,
    "widthFeet": 4
  }
}
```
**Response:** `201 Created`

### **Plan Succession**
**`POST /beds/{id}/successions`**
Schedules a crop succession (seeding, transplanting, harvesting window).

**Request Body:**
```json
{
  "variety": {
    "species": "Zinnia elegans",
    "cultivar": "Benary's Giant",
    "daysToMaturity": 75,
    "color": "Salmon Rose"
  },
  "sowDate": "2024-03-01",
  "transplantDate": "2024-04-15",
  "harvestStart": "2024-06-01"
}
```
**Response:** `200 OK`
```json
{ "id": "succ-uuid-here" }
```

---

## 🧄 Seed Inventory
Track seed stock quantities, suppliers, and germination rates natively integrated to bed sowings.

### **Register Seed Lot**
**`POST /seeds`**
Adds seed packets or bulk seed purchases to inventory.

**Request Body:**
```json
{
  "variety": { "species": "Allium sativum", "cultivar": "Chesnok Red", "daysToMaturity": 240 },
  "supplier": "Johnny's Selected Seeds",
  "quantity": { "value": 100, "unit": "cloves", "type": "count" },
  "germinationPct": 0.95,
  "harvestYear": 2023,
  "isOrganic": true
}
```
**Response:** `201 Created`

### **Withdraw Seed**
**`POST /seeds/{id}/withdraw`**
Deducts seed inventory for planting a specific flower bed. Returns an error if attempting to overdraw.

**Request Body:**
```json
{
  "quantity": { "value": 25, "unit": "cloves", "type": "count" },
  "destinationBedId": "bed-uuid-here"
}
```
**Response:** `204 No Content`
