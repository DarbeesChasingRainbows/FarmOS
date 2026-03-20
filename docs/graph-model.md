# ArangoDB Graph Model — FarmOS

> The ecological knowledge graph that maps the biological relationships on the farm.

---

## Why a Graph

Relational databases model tables. Your farm is not a table — it is a **web of relationships**:

- An animal **grazes** a paddock, which **follows in rotation** after another paddock
- An orchard guild **contains** a nitrogen fixer that **fixes nitrogen for** a fruit tree
- A SCOBY **descended from** a mother culture that was **split** in March 2026
- A hive **pollinates** an orchard guild that **produces** fruit for a CSA box

ArangoDB's native graph engine lets you traverse these relationships with single AQL queries instead of painful recursive CTEs.

---

## Graph: `farm_graph`

### Vertex Collections

| Collection | Description | Example Document |
|-----------|-------------|-----------------|
| `paddocks` | Physical land units | `{ _key: "p-001", name: "North Meadow", acreage: 2.5, zone: "pasture" }` |
| `animals` | Individual livestock | `{ _key: "a-cattle-047", tag: "047", species: "cattle", sex: "cow" }` |
| `herds` | Logical animal groups | `{ _key: "h-broiler-1", name: "Broiler Tractor 1", type: "BroilerTractor" }` |
| `plants` | Individual trees/perennials | `{ _key: "pl-apple-honeycrisp-01", species: "Malus domestica", cultivar: "Honeycrisp" }` |
| `guilds` | Orchard guild groupings | `{ _key: "g-nap-01", name: "North NAP Trio 1", type: "NAP" }` |
| `flower_beds` | Cut flower beds/rows | `{ _key: "fb-a3-r2", name: "Bed A3 Row 2", block: "A" }` |
| `hives` | Beehives | `{ _key: "hv-alpha", name: "Hive Alpha", type: "Langstroth" }` |
| `cultures` | Living cultures (starters, SCOBYs) | `{ _key: "c-old-faithful", name: "Old Faithful", type: "SourdoughStarter" }` |
| `products` | Sellable products | `{ _key: "pr-sourdough-loaf", name: "Country Sourdough", category: "bakery" }` |

### Edge Collections

| Collection | From → To | Meaning |
|-----------|-----------|---------|
| `grazed` | `herds` → `paddocks` | Herd grazed this paddock (with dates, duration, cow-days) |
| `follows_in_rotation` | `paddocks` → `paddocks` | Paddock A is followed by Paddock B in rotation sequence |
| `member_of_herd` | `animals` → `herds` | Animal belongs to this herd |
| `born_from` | `animals` → `animals` | Offspring → Dam lineage |
| `sired_by` | `animals` → `animals` | Offspring → Sire lineage |
| `member_of_guild` | `plants` → `guilds` | Plant is part of this orchard guild |
| `fixes_nitrogen_for` | `plants` → `plants` | Nitrogen fixer benefits this neighbor |
| `pollinates` | `hives` → `guilds` | Hive pollinates plants in this guild |
| `planted_in` | `plants` → `flower_beds` | Crop succession planted in this bed |
| `descended_from` | `cultures` → `cultures` | Culture lineage (starter splits, SCOBY offspring) |
| `produced_from` | `products` → `cultures` | Product was made from this culture/batch |
| `harvested_from` | `products` → `hives` | Honey harvested from this hive |
| `located_in` | `hives` → `paddocks` | Hive is physically in this area |

### Example Edge Documents

```json
// Grazing event edge
{
  "_from": "herds/h-cattle-main",
  "_to": "paddocks/p-001",
  "startDate": "2026-03-01",
  "endDate": "2026-03-03",
  "daysGrazed": 2,
  "animalCount": 12,
  "cowDaysPerAcre": 9.6
}

// Culture lineage edge
{
  "_from": "cultures/c-jun-scoby-002",
  "_to": "cultures/c-jun-scoby-mother",
  "splitDate": "2026-02-15",
  "reason": "New batch vessel"
}
```

---

## Example AQL Queries

### 1. Full rotation history for a paddock (last 12 months)

```aql
FOR v, e IN 1..1 INBOUND 'paddocks/p-001' grazed
  FILTER e.startDate >= '2025-03-01'
  SORT e.startDate DESC
  RETURN { herd: v.name, start: e.startDate, end: e.endDate, cowDays: e.cowDaysPerAcre }
```

### 2. Which paddock should be grazed next? (longest rested, meeting 45-day minimum)

```aql
LET today = DATE_ISO8601(DATE_NOW())
FOR p IN paddocks
  FILTER p.zone == 'pasture'
  LET lastGrazing = FIRST(
    FOR v, e IN 1..1 INBOUND p grazed
      SORT e.endDate DESC LIMIT 1
      RETURN e.endDate
  )
  LET restDays = DATE_DIFF(lastGrazing, today, 'day')
  FILTER restDays >= 45
  SORT restDays DESC
  RETURN { paddock: p.name, restDays, lastGrazed: lastGrazing }
```

### 3. Full orchard guild composition with ecological roles

```aql
FOR plant, edge IN 1..1 INBOUND 'guilds/g-nap-01' member_of_guild
  RETURN { species: plant.species, cultivar: plant.cultivar, role: edge.role }
```

### 4. SCOBY lineage tree (3 generations deep)

```aql
FOR v, e, p IN 1..3 OUTBOUND 'cultures/c-jun-scoby-002' descended_from
  RETURN { culture: v.name, type: v.type, splitDate: e.splitDate, depth: LENGTH(p.edges) }
```

### 5. Which hives pollinate which fruit trees?

```aql
FOR hive IN hives
  FOR guild, pollinationEdge IN 1..1 OUTBOUND hive pollinates
    FOR plant, memberEdge IN 1..1 INBOUND guild member_of_guild
      FILTER memberEdge.role IN ['PrimaryFruit', 'SecondaryFruit']
      RETURN { hive: hive.name, guild: guild.name, fruitTree: plant.cultivar }
```

---

## Graph Indexing Strategy

```javascript
// Persistent indexes for common access patterns
db.grazed.ensureIndex({ type: "persistent", fields: ["startDate"] });
db.grazed.ensureIndex({ type: "persistent", fields: ["endDate"] });
db.paddocks.ensureIndex({ type: "persistent", fields: ["zone"] });
db.animals.ensureIndex({ type: "persistent", fields: ["species", "status"] });
db.cultures.ensureIndex({ type: "persistent", fields: ["type", "health"] });
```
