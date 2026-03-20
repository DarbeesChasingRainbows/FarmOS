import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import NavBar from "../../components/NavBar.tsx";
import CropPlanDashboard from "../../islands/CropPlanDashboard.tsx";

export default define.page(function PlansPage() {
  return (
    <>
      <Head>
        <title>Crop Plans — Flower OS</title>
      </Head>
      <NavBar current="/plans" />

      <div class="p-8">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              📋 Crop Plans
            </h1>
            <p class="text-stone-500 mt-1">
              Plan seasonal production, track stems per linear foot, and analyze
              profitability by sales channel.
            </p>
          </div>
        </div>

        <CropPlanDashboard />
      </div>
    </>
  );
});
