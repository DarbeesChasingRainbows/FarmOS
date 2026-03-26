import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowIoTDashboard from "../../islands/ArrowIoTDashboard.tsx";

export default define.page(function IoTPage() {
  return (
    <div>
      <Head>
        <title>IoT Devices — Hearth OS</title>
      </Head>
      <ArrowIoTDashboard />
    </div>
  );
});
