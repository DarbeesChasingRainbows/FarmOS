import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import NavBar from "../../components/NavBar.tsx";
import SeedInventoryPanel from "../../islands/SeedInventoryPanel.tsx";

export default define.page(function SeedsPage() {
  return (
    <>
      <Head>
        <title>Seed Inventory — Flower OS</title>
      </Head>
      <NavBar current="/seeds" />

      <div class="p-8">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              🌱 Seed Inventory
            </h1>
            <p class="text-stone-500 mt-1">
              Track seed lots, germination rates, and stock levels across
              suppliers.
            </p>
          </div>
        </div>

        <SeedInventoryPanel />
      </div>
    </>
  );
});
