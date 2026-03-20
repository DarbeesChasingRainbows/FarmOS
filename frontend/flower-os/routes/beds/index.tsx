import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import NavBar from "../../components/NavBar.tsx";
import BedManagementPanel from "../../islands/BedManagementPanel.tsx";

export default define.page(function BedsPage() {
  return (
    <>
      <Head>
        <title>Flower Beds — Flower OS</title>
      </Head>
      <NavBar current="/beds" />

      <div class="p-8">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              🌱 Flower Beds
            </h1>
            <p class="text-stone-500 mt-1">
              Manage beds and succession plantings. Plan 7–14 day intervals for
              continuous harvest.
            </p>
          </div>
        </div>

        <BedManagementPanel />
      </div>
    </>
  );
});
