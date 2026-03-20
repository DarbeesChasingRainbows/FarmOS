import NavBar from "@/components/NavBar.tsx";
import SensorPanel from "@/islands/SensorPanel.tsx";
import { define } from "../../utils.ts";

export default define.page(function SensorsPage() {
  return (
    <div class="flex min-h-screen bg-stone-950 text-stone-100">
      <NavBar currentPath="/sensors" />
      <main class="flex-1 p-8 overflow-y-auto">
        <SensorPanel />
      </main>
    </div>
  );
});
