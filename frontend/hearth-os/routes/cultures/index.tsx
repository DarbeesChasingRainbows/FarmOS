import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowCultureDetailPanel from "../../islands/ArrowCultureDetailPanel.tsx";
import ArrowCreateCultureForm from "../../islands/ArrowCreateCultureForm.tsx";

export default define.page(function CulturesPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Cultures — Hearth OS</title>
      </Head>
      <header class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Living Cultures
          </h1>
          <p class="text-stone-500 mt-1">
            Click any culture to view details, feed, or split it.
          </p>
        </div>
        <ArrowCreateCultureForm />
      </header>
      <ArrowCultureDetailPanel />
    </div>
  );
});
