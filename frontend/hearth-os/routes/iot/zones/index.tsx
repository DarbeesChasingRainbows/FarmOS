import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import ArrowZonesDashboard from "../../../islands/ArrowZonesDashboard.tsx";

export default define.page(function IoTZonesPage() {
  return (
    <div>
      <Head>
        <title>IoT Zones — Hearth OS</title>
      </Head>
      <ArrowZonesDashboard />
    </div>
  );
});
