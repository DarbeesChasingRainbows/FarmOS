# FarmOS Frontend Integration Guide (Deno Fresh)

This guide is designed for developing the Deno Fresh frontends against the FarmOS Sovereign C# Backend. The backend uses a decentralized, sovereign-first architecture, heavily leveraging Event Sourcing via ArangoDB, CQRS, and an API Gateway (Caddy).

## 1. Network & Architecture Overview

All frontend interactions MUST go through the **FarmOS Gateway**. You will not communicate directly with the backend microservices (Pasture, Flora, etc.).

*   **Gateway Address (Local Dev):** `http://localhost:5050`
*   **Gateway Address (Production/Proxmox):** This will be the ingress IP mapping to the `farmos-gateway` container in your K3s cluster.

### Contexts & Routing
The Gateway routes traffic based on the first path segment after `/api/`:

| Context | Base URL | What it manages |
| :--- | :--- | :--- |
| **Auth** | `/api/auth` | PIN-based local login |
| **Pasture** | `/api/pasture` | Livestock, paddocks, herds, grazing, meat |
| **Flora** | `/api/flora` | Market garden, orchards, successions, seeds |
| **Hearth** | `/api/hearth` | Sourdough, Kombucha, living cultures, HACCP |
| **Apiary** | `/api/apiary` | Beehives, inspections, honey harvests |
| **Commerce** | `/api/commerce` | CSA management, orders, members |
| **Assets** | `/api/assets` | Equipment, structures, water, sensors, materials |
| **Ledger** | `/api/ledger` | Farm expenses, revenue tracking |

---

## 2. Authentication (Sovereign PIN)

FarmOS operates on a family-farm trust model. Authentication inside the LAN uses a 4-digit PIN.

**Endpoint:** `POST /api/auth/login`
**Payload:**
```json
{
  "pin": "1234"
}
```

**Response (200 OK):**
```json
{
  "id": "steward",
  "name": "Farm Steward",
  "role": "steward",
  "token": "c3Rld2FyZDpzdGV3YXJkOjE3MTA0MzE2NjM=" 
}
```

### The Token
The backend issues a lightweight, stateless token (Base64 encoded `<UserId>:<Role>:<Timestamp>`). 
For *every* subsequent API request to any Context, you must pass this token in the `Authorization` header:
```http
Authorization: Bearer c3Rld2FyZDpzdGV3YXJkOjE3MTA0MzE2NjM=
```

---

## 3. CQRS Design (How to interact with the API)

Because FarmOS uses CQRS (Command Query Responsibility Segregation) and Event Sourcing, the API behaves in two distinct ways depending on whether you are reading data (Queries) or writing data (Commands).

### A. Queries (Read Models)
Queries use `GET` requests. They pull data from optimized, "flat" read-models projection collections in ArangoDB.
- **Fast and cacheable:** They don't run heavy logic.
- **Flat DTOs:** No nested domain logic leaks through.

**Example: Get all paddocks**
```http
GET /api/pasture/paddocks
```
**Response:**
```json
[
  {
    "id": "0764c246-5b92-4e9e-86f6-1e4881ae9491",
    "name": "North Meadow",
    "acreage": 3.5,
    "landType": "Pasture",
    "status": "Resting",
    "restDaysElapsed": 12,
    "currentHerdId": null
  }
]
```

### B. Commands (State Changes)
Commands use `POST` or `PUT` requests. They encode a *business intent* (e.g., "Begin Grazing" rather than "Update Paddock Status to Active").

*   Command requests return `200 OK`, `201 Created`, or `204 No Content` on success.
*   If a domain rule is violated (e.g., trying to graze a paddock that hasn't rested for 45 days), the API returns `400 Bad Request` with a standardized `DomainError` JSON structure.

**Example: Registering a new animal**
```http
POST /api/pasture/animals
Content-Type: application/json

{
  "tags": [{ "type": "EarTag", "value": "A123" }],
  "species": "Cattle",
  "breed": "Red Devon",
  "sex": "Female",
  "dateAcquired": "2024-03-14",
  "nickname": "Bessie"
}
```
**Response:** (Returns the ID of the new entity)
```json
{
  "id": "d0f1b213-4abc-4c56-9a2f-1e9d8f7b5a3c"
}
```

---

## 4. Pasture API Endpoints Reference

The `Pasture` context is fully implemented. Here is the contract mapping for your Deno frontend.

### Paddocks
*   `GET /api/pasture/paddocks` - List all paddocks summaries.
*   `GET /api/pasture/paddocks/{id}` - Get explicit details (including boundary geo-coordinates, biomass history, soil tests).
*   `POST /api/pasture/paddocks` - Create a paddock. `{"name": "string", "acreage": number, "landType": "string"}`.
*   `PUT /api/pasture/paddocks/{id}/boundary` - Set boundary. `{"geometry": { "type": "Polygon", "coordinates": [...] }}`.
*   `POST /api/pasture/paddocks/{id}/begin-grazing` - `{"herdId": "guid", "date": "YYYY-MM-DD"}`. *Will 400 if rest period < 45 days.*
*   `POST /api/pasture/paddocks/{id}/end-grazing` - `{ "date": "YYYY-MM-DD" }`.
*   `POST /api/pasture/paddocks/{id}/biomass` - `{ "tonsPerAcre": number, "measuredOn": "YYYY-MM-DD", "method": "string" }`.
*   `POST /api/pasture/paddocks/{id}/soil-test` - `{ "pH": number, "organicMatterPct": number, "carbonPct": number, "testedOn": "YYYY-MM-DD", "lab": "string" }`.

### Animals
*   `GET /api/pasture/animals?species={species}&status={status}` - List animals (supports query string filtering).
*   `GET /api/pasture/animals/{id}` - Get full animal history (medical, weight, pregnancy).
*   `POST /api/pasture/animals` - Register incoming animal.
*   `POST /api/pasture/animals/{id}/isolate` - Move to sick bay/isolation. `{"reason": "string", "date": "YYYY-MM-DD"}`.
*   `POST /api/pasture/animals/{id}/treatment` - Record medical. `{"name": "...", "dosage": "...", "route": "...", "date": "...", "notes": "...", "withdrawalPeriodDays": "..."}`.
*   `POST /api/pasture/animals/{id}/weight` - Record weight. `{"weight": {"value": num, "unit": "lbs", "type": "mass"}, "date": "..."}`.
*   `POST /api/pasture/animals/{id}/butcher` - Process an animal. `{"date": "...", "processor": "...", "hangingWeight": {"value": num...}, "cutSheet": "..."}`.

### Herds
*   `GET /api/pasture/herds` - List herds and member counts.
*   `GET /api/pasture/herds/{id}` - Details and full list of mapped animal members.
*   `POST /api/pasture/herds` - Create herd. `{"name": "string", "type": 0}` (Type: 0=Cattle, 1=Sheep, 2=Pigs, etc).
*   `POST /api/pasture/herds/{id}/move` - `{"paddockId": "guid", "date": "YYYY-MM-DD"}`.
*   `POST /api/pasture/herds/{id}/add-animal` - `{"animalId": "guid"}`.
*   `POST /api/pasture/herds/{id}/remove-animal` - `{"animalId": "guid"}`.

---

## 5. Deno Fresh Integration Pattern

Here's an example of how you should structure your Deno API client classes to interface with this backend. Use native `fetch` and wrap it to inject the auth token.

**Example `utils/farmos-client.ts`:**
```ts
const GATEWAY_URL = Deno.env.get("FARMOS_GATEWAY_URL") || "http://localhost:5050";

export async function fetchFarmOS(path: string, options: RequestInit = {}) {
  const token = localStorage.getItem("farmos_token") || ""; // Or read from cookies if SSR in Fresh
  
  const headers = new Headers(options.headers);
  headers.set("Authorization", `Bearer ${token}`);
  if (!headers.has("Content-Type") && options.method !== "GET") {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${GATEWAY_URL}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    // 400 Bad Request indicates a Domain Rule Violation from the C# backend
    if (response.status === 400) {
      const errProps = await response.json();
      throw new Error(`Domain Error: ${errProps.message}`);
    }
    throw new Error(`HTTP Error: ${response.status}`);
  }

  // 204 No Content support
  if (response.status === 204) return null;
  return response.json();
}

// Wrapping Pasture commands
export const PastureAPI = {
  getPaddocks: () => fetchFarmOS("/api/pasture/paddocks"),
  
  beginGrazing: (paddockId: string, herdId: string, date: string) => 
    fetchFarmOS(`/api/pasture/paddocks/${paddockId}/begin-grazing`, {
      method: "POST",
      body: JSON.stringify({ herdId, date })
    })
};
```
