import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import NavBar from "../../components/NavBar.tsx";
import PostHarvestPanel from "../../islands/PostHarvestPanel.tsx";

export default define.page(function BatchesPage() {
  return (
    <>
      <Head>
        <title>Post-Harvest — Flower OS</title>
      </Head>
      <NavBar current="/batches" />

      <div class="p-8">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              ✂️ Post-Harvest
            </h1>
            <p class="text-stone-500 mt-1">
              Grade, condition, and cool stems. Target 32-35°F for most cut
              flowers.
            </p>
          </div>
        </div>

        <PostHarvestPanel />
      </div>
    </>
  );
});
