# Read Model Projectors Architecture

## Understanding CQRS and Eventual Consistency in FarmOS

FarmOS uses a strict **Command Query Responsibility Segregation (CQRS)** pattern. When a user or system issues a command (e.g., "Begin Grazing" or "Register Animal"), the command is validated against the domain model and appended as an immutable event to the ArangoDB Document Store (`pasture_events` collection).

However, the UI and frontends rarely want to query an event stream directly. They require rich, optimized data representations (e.g., "Give me a list of all active paddocks with their current herd assignments").

To achieve this, FarmOS uses **Read Model Projectors**: background workers that listen to the append-only event stream and project (transform) those events into flattened, optimized read-model documents.

---

## 🏗️ The Projector Worker Pipeline

The Projectors run as `IHostedService` (BackgroundService) singletons inside their respective API processes. 

### Worker Execution Cycle
1. **Poll Event Stream**: The projector periodically polls its bounded context's event collection (e.g., `pasture_events`).
2. **Sort by Time (Idempotency)**: Events are fetched sorting by `StoredAt ASC, Version ASC` to guarantee chronological consistency.
3. **High-Water Mark (Cursor)**: The service tracks the `lastProcessed` timestamp in memory (which will be upgraded to a persistent ArangoDB cursor collection in distributed scenarios) to ensure events are not processed repeatedly unless specifically replaying.
4. **Translate and Upsert (AQL)**: The worker deserializes the event payload, translates the business event into a view-oriented update, and runs an `UPSERT/UPDATE` AQL query against the projection collections.

---

## 🐑 Reference Example: Pasture Projector

**Source:** `src/FarmOS.Pasture.Infrastructure/Projectors/PastureProjectorWorker.cs`

### Projection Collections
The Pasture context maintains three primary read-model collections:
- `pasture_paddock_view`
- `pasture_animal_view`
- `pasture_herd_view`

### Event-to-View Translation (AQL Examples)

#### 1. Entity Creation 
When a `PaddockCreated` event is detected, the projector builds the foundational document. Notice the use of `UPSERT`—this makes the projection **idempotent**, meaning the event stream can be replayed from the beginning of time without violating unique constraints.

```aql
UPSERT { _key: @aggregateId }
INSERT { 
    _key: @aggregateId, 
    Name: @name, 
    Acreage: @acreage, 
    LandType: @landType, 
    Status: 'Resting', 
    RestDaysElapsed: 0 
}
UPDATE { Name: @name }
IN pasture_paddock_view
```

#### 2. Cross-Entity Updates (Grazing)
When a `GrazingStarted` event is detected, the paddock's status is mutated. No deep joins exist in the read model—instead, the current state and relationships are explicitly set.

```aql
UPDATE @aggregateId
WITH { 
    Status: 'BeingGrazed', 
    CurrentHerdId: @herdId, 
    RestDaysElapsed: 0 
}
IN pasture_paddock_view
OPTIONS { ignoreErrors: true }
```

#### 3. Real-Time Graph Construction
As side-effects, projectors in FarmOS actively build the `farmos_graph`. When an `AnimalAddedToHerd` event occurs, the projector not only updates document properties but explicitly manages the `belongs_to` edge relationships.

```aql
// Update animal document reference
UPDATE @animalId WITH { CurrentHerdId: @herdId } IN pasture_animal_view

// Explicitly Upsert Graph Edge
UPSERT { _from: CONCAT('pasture_animal_view/', @animalId), _to: CONCAT('pasture_herd_view/', @herdId) }
INSERT { _from: CONCAT('pasture_animal_view/', @animalId), _to: CONCAT('pasture_herd_view/', @herdId) }
UPDATE {}
IN belongs_to
```

---

## ⚡ Reading the Projections
With projectors actively managing these views in the background (typically executing within `<50ms` of the command completing), the API read paths become dramatically simplified.

Query Handlers (e.g., `GetPaddocksQuery`) no longer execute rules logic or map deeply nested joins. They simply execute:

```aql
FOR p IN pasture_paddock_view
    SORT p.Name ASC
    RETURN p
```

This guarantees O(1) or O(log N) lookup speeds tailored explicitly for frontend rendering, effectively shifting the computational cost from read-time to write-time.
