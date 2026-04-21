#!/bin/sh
set -e

echo "Waiting for ArangoDB..."
until curl -s --user root:farmos_dev http://arangodb:8529/_api/version > /dev/null 2>&1; do
  sleep 2
done
echo "ArangoDB is up."

curl -s -X POST http://arangodb:8529/_api/database \
  --user root:farmos_dev \
  -H "Content-Type: application/json" \
  -d '{"name":"farmos"}' || true

echo "Database 'farmos' ensured."

for col in pasture_events flora_events hearth_events apiary_events commerce_events assets_events ledger_events iot_events \
           campus_events codex_events compliance_events counter_events crew_events; do
  curl -s -X POST http://arangodb:8529/_db/farmos/_api/collection \
    --user root:farmos_dev \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"$col\"}" > /dev/null 2>&1 || true
done

for col in pasture_paddock_view pasture_animal_view pasture_herd_view \
           flora_guild_view flora_flowerbed_view flora_seedlot_view \
           hearth_sourdough_view hearth_kombucha_view hearth_culture_view \
           apiary_hive_view \
           commerce_season_view commerce_member_view commerce_order_view \
           assets_equipment_view assets_structure_view assets_watersource_view \
           assets_compost_view assets_sensor_view assets_material_view \
           ledger_expense_view ledger_revenue_view \
           iot_devices iot_zones \
           iot_telemetry_view iot_excursion_view iot_apothecary_climate_view \
           traceability_lots traceability_batches traceability_customers traceability_suppliers \
           farm_users; do
  curl -s -X POST http://arangodb:8529/_db/farmos/_api/collection \
    --user root:farmos_dev \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"$col\"}" > /dev/null 2>&1 || true
done

for col in belongs_to grazes_on planted_in pollinates uses_starter uses_scoby \
           descended_from member_of_season located_at expense_for revenue_from \
           offspring_of seed_from monitors maintained_by \
           used_in sold_to sourced_from; do
  curl -s -X POST http://arangodb:8529/_db/farmos/_api/collection \
    --user root:farmos_dev \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"$col\",\"type\":3}" > /dev/null 2>&1 || true
done

curl -s -X POST http://arangodb:8529/_db/farmos/_api/document/farm_users \
  --user root:farmos_dev \
  -H "Content-Type: application/json" \
  -d '{"_key":"steward","Name":"Farm Steward","Role":"steward","PinHash":"03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4"}' > /dev/null 2>&1 || true

# ── Named Graph: traceability_graph ──────────────────────────────────────
curl -s -X POST http://arangodb:8529/_db/farmos/_api/gharial \
  --user root:farmos_dev \
  -H "Content-Type: application/json" \
  -d '{
    "name": "traceability_graph",
    "edgeDefinitions": [
      {
        "collection": "used_in",
        "from": ["traceability_lots"],
        "to": ["traceability_batches"]
      },
      {
        "collection": "sold_to",
        "from": ["traceability_batches"],
        "to": ["traceability_customers"]
      },
      {
        "collection": "sourced_from",
        "from": ["traceability_lots"],
        "to": ["traceability_suppliers"]
      }
    ]
  }' > /dev/null 2>&1 || true

echo "Traceability graph ensured."
echo "FarmOS database setup complete!"
