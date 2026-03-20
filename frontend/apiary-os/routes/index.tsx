import { Head } from "fresh/runtime";
import { define } from "../utils.ts";

export default define.page(function Home() {
  return (
    <div class="px-8 py-10 max-w-5xl mx-auto">
      <Head>
        <title>Apiary OS — farmOS</title>
      </Head>

      <header class="mb-12">
        <h1 class="text-4xl font-extrabold text-stone-800 tracking-tight flex items-center gap-3">
          <span>🐝</span> Apiary OS
        </h1>
        <p class="text-stone-500 mt-2 text-lg">
          Manage colonies, track queen vitals, and log physical inspections.
        </p>
      </header>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
        <a
          href="/hives"
          class="group bg-white rounded-2xl border border-stone-200 shadow-sm p-6 hover:shadow-md hover:border-amber-300 transition block text-left"
        >
          <div class="w-12 h-12 rounded-full bg-amber-50 text-amber-600 flex items-center justify-center text-2xl mb-4 group-hover:scale-110 transition-transform">
            🍯
          </div>
          <h2 class="text-xl font-bold text-stone-800 mb-1">
            Colonies & Hives
          </h2>
          <p class="text-stone-500 text-sm">
            View all active hives, varroa mite counts, and temperament alerts.
          </p>
          <div class="mt-4 text-amber-600 text-sm font-semibold flex items-center gap-1">
            Manage Apiary{" "}
            <span class="group-hover:translate-x-1 transition-transform">
              →
            </span>
          </div>
        </a>

        <a
          href="/apiaries"
          class="group bg-white rounded-2xl border border-stone-200 shadow-sm p-6 hover:shadow-md hover:border-teal-300 transition block text-left"
        >
          <div class="w-12 h-12 rounded-full bg-teal-50 text-teal-600 flex items-center justify-center text-2xl mb-4 group-hover:scale-110 transition-transform">
            📍
          </div>
          <h2 class="text-xl font-bold text-stone-800 mb-1">
            Apiary Locations
          </h2>
          <p class="text-stone-500 text-sm">
            Group hives by yard — home, orchard, outyard — and track capacity.
          </p>
          <div class="mt-4 text-teal-600 text-sm font-semibold flex items-center gap-1">
            Manage Locations{" "}
            <span class="group-hover:translate-x-1 transition-transform">
              →
            </span>
          </div>
        </a>

        <a
          href="/reports"
          class="group bg-white rounded-2xl border border-stone-200 shadow-sm p-6 hover:shadow-md hover:border-violet-300 transition block text-left"
        >
          <div class="w-12 h-12 rounded-full bg-violet-50 text-violet-600 flex items-center justify-center text-2xl mb-4 group-hover:scale-110 transition-transform">
            📊
          </div>
          <h2 class="text-xl font-bold text-stone-800 mb-1">
            Reports & Analytics
          </h2>
          <p class="text-stone-500 text-sm">
            Mite trends, yield reports, colony survival, and weather correlations.
          </p>
          <div class="mt-4 text-violet-600 text-sm font-semibold flex items-center gap-1">
            View Reports{" "}
            <span class="group-hover:translate-x-1 transition-transform">
              →
            </span>
          </div>
        </a>

        <a
          href="/calendar"
          class="group bg-white rounded-2xl border border-stone-200 shadow-sm p-6 hover:shadow-md hover:border-sky-300 transition block text-left"
        >
          <div class="w-12 h-12 rounded-full bg-sky-50 text-sky-600 flex items-center justify-center text-2xl mb-4 group-hover:scale-110 transition-transform">
            📅
          </div>
          <h2 class="text-xl font-bold text-stone-800 mb-1">
            Seasonal Calendar
          </h2>
          <p class="text-stone-500 text-sm">
            Monthly task templates — inspections, treatments, harvest windows.
          </p>
          <div class="mt-4 text-sky-600 text-sm font-semibold flex items-center gap-1">
            View Calendar{" "}
            <span class="group-hover:translate-x-1 transition-transform">
              →
            </span>
          </div>
        </a>

        <a
          href="/financials"
          class="group bg-white rounded-2xl border border-stone-200 shadow-sm p-6 hover:shadow-md hover:border-emerald-300 transition block text-left"
        >
          <div class="w-12 h-12 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center text-2xl mb-4 group-hover:scale-110 transition-transform">
            💰
          </div>
          <h2 class="text-xl font-bold text-stone-800 mb-1">
            Financials
          </h2>
          <p class="text-stone-500 text-sm">
            Track expenses, revenue, and profitability for your apiary operations.
          </p>
          <div class="mt-4 text-emerald-600 text-sm font-semibold flex items-center gap-1">
            View Financials{" "}
            <span class="group-hover:translate-x-1 transition-transform">
              →
            </span>
          </div>
        </a>
      </div>
    </div>
  );
});
