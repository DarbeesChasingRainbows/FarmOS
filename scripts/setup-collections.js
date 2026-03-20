// FarmOS ArangoDB Collection Setup Script
// Run this in the ArangoDB web UI (http://localhost:8529) → farmos database → Queries tab
// Or via arangosh: arangosh --server.database farmos < scripts/setup-collections.js

// ─── Create Database ─────────────────────────────────────────────────
// db._createDatabase('farmos');  // Run once at root level, not inside this script

// ─── Event Store Collections (append-only) ──────────────────────────
const eventCollections = [
    'pasture_events',
    'flora_events',
    'hearth_events',
    'apiary_events',
    'commerce_events',
    'assets_events',
    'ledger_events'
];

eventCollections.forEach(name => {
    if (!db._collection(name)) {
        db._createDocumentCollection(name);
        // Compound index for loading events by aggregate
        db._collection(name).ensureIndex({
            type: 'persistent',
            fields: ['AggregateId', 'Version'],
            unique: true,
            name: `idx_${name}_aggregate_version`
        });
        // Index for projecting events by type
        db._collection(name).ensureIndex({
            type: 'persistent',
            fields: ['EventType', 'OccurredAt'],
            name: `idx_${name}_type_time`
        });
        // Tenant index (for future multi-tenancy)
        db._collection(name).ensureIndex({
            type: 'persistent',
            fields: ['TenantId'],
            name: `idx_${name}_tenant`
        });
        print(`Created event collection: ${name}`);
    }
});

// ─── Read Model (Projection) Collections ────────────────────────────
const viewCollections = [
    // Pasture
    'pasture_paddock_view',
    'pasture_animal_view',
    'pasture_herd_view',
    // Flora
    'flora_guild_view',
    'flora_flowerbed_view',
    'flora_seedlot_view',
    // Hearth
    'hearth_sourdough_view',
    'hearth_kombucha_view',
    'hearth_culture_view',
    // Apiary
    'apiary_hive_view',
    // Commerce
    'commerce_season_view',
    'commerce_member_view',
    'commerce_order_view',
    // Assets
    'assets_equipment_view',
    'assets_structure_view',
    'assets_watersource_view',
    'assets_compost_view',
    'assets_sensor_view',
    'assets_material_view',
    // Ledger
    'ledger_expense_view',
    'ledger_revenue_view',
    // Auth
    'farm_users'
];

viewCollections.forEach(name => {
    if (!db._collection(name)) {
        db._createDocumentCollection(name);
        print(`Created view collection: ${name}`);
    }
});

// ─── Geo Indexes ─────────────────────────────────────────────────────
const geoCollections = {
    'pasture_paddock_view': 'Boundary',
    'flora_guild_view': 'Position',
    'apiary_hive_view': 'Position',
    'assets_equipment_view': 'CurrentLocation',
    'assets_structure_view': 'Footprint',
    'assets_watersource_view': 'Position',
    'assets_sensor_view': 'Position',
    'assets_compost_view': 'Location'
};

Object.entries(geoCollections).forEach(([col, field]) => {
    try {
        db._collection(col).ensureIndex({
            type: 'geo',
            fields: [field],
            geoJson: true,
            name: `idx_${col}_geo`
        });
        print(`Created geo index on ${col}.${field}`);
    } catch(e) {
        print(`Note: geo index on ${col}.${field} - ${e.message}`);
    }
});

// ─── Edge Collections (Graph) ────────────────────────────────────────
const edgeCollections = [
    'belongs_to',       // Animal → Herd
    'grazes_on',        // Herd → Paddock
    'planted_in',       // Guild/Succession → FlowerBed
    'pollinates',       // Hive → Guild
    'uses_starter',     // SourdoughBatch → LivingCulture
    'uses_scoby',       // KombuchaBatch → LivingCulture
    'descended_from',   // LivingCulture → LivingCulture (parent)
    'member_of_season', // CSAMember → CSASeason
    'located_at',       // Asset → Paddock/Structure
    'expense_for',      // Expense → (any context entity)
    'revenue_from',     // Revenue → (any context entity)
    'offspring_of',     // Animal → Animal (dam/sire)
    'seed_from',        // FlowerBed → SeedLot
    'monitors',         // Sensor → Paddock/Structure
    'maintained_by'     // Equipment → MaintenanceRecord
];

edgeCollections.forEach(name => {
    if (!db._collection(name)) {
        db._createEdgeCollection(name);
        print(`Created edge collection: ${name}`);
    }
});

// ─── Named Graph ─────────────────────────────────────────────────────
const graphName = 'farmos_graph';
const graphModule = require('@arangodb/general-graph');

if (!graphModule._exists(graphName)) {
    graphModule._create(graphName, [
        graphModule._relation('belongs_to',     ['pasture_animal_view'], ['pasture_herd_view']),
        graphModule._relation('grazes_on',      ['pasture_herd_view'],   ['pasture_paddock_view']),
        graphModule._relation('planted_in',     ['flora_guild_view'],    ['flora_flowerbed_view']),
        graphModule._relation('pollinates',     ['apiary_hive_view'],    ['flora_guild_view']),
        graphModule._relation('uses_starter',   ['hearth_sourdough_view'], ['hearth_culture_view']),
        graphModule._relation('uses_scoby',     ['hearth_kombucha_view'], ['hearth_culture_view']),
        graphModule._relation('descended_from', ['hearth_culture_view'], ['hearth_culture_view']),
        graphModule._relation('member_of_season', ['commerce_member_view'], ['commerce_season_view']),
        graphModule._relation('offspring_of',   ['pasture_animal_view'], ['pasture_animal_view']),
        graphModule._relation('seed_from',      ['flora_flowerbed_view'], ['flora_seedlot_view']),
    ]);
    print(`Created named graph: ${graphName}`);
}

// ─── Seed Default Admin User (PIN: 1234) ─────────────────────────────
if (db.farm_users.count() === 0) {
    db.farm_users.save({
        _key: 'steward',
        Name: 'Farm Steward',
        Role: 'steward',
        PinHash: '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4' // SHA-256 of "1234"
    });
    db.farm_users.save({
        _key: 'partner',
        Name: 'Farm Partner',
        Role: 'partner',
        PinHash: 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3' // SHA-256 of "123"
    });
    print('Seeded default users (steward PIN: 1234, partner PIN: 123)');
}

print('\\n✅ FarmOS database setup complete!');
