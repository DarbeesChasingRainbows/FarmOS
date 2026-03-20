import { Head } from "fresh/runtime";
import { define } from "../utils.ts";

export default define.page(function Dashboard() {
  const stats = [
    {
      icon: "🚜",
      label: "Equipment",
      href: "/equipment",
      gradient: "from-emerald-500 to-emerald-700",
      sub: "Tractors, tools & machines",
    },
    {
      icon: "🏚️",
      label: "Structures",
      href: "/structures",
      gradient: "from-stone-500 to-stone-700",
      sub: "Barns, greenhouses, kitchens",
    },
    {
      icon: "💧",
      label: "Water Sources",
      href: "/water",
      gradient: "from-blue-500 to-blue-700",
      sub: "Wells, ponds & irrigation",
    },
    {
      icon: "♻️",
      label: "Compost",
      href: "/compost",
      gradient: "from-lime-600 to-lime-800",
      sub: "Batches, temps & turns",
    },
    {
      icon: "📡",
      label: "Sensors",
      href: "/sensors",
      gradient: "from-violet-500 to-violet-700",
      sub: "Field sensor network",
    },
    {
      icon: "📦",
      label: "Materials",
      href: "/materials",
      gradient: "from-amber-500 to-amber-700",
      sub: "Inventory with stock guard",
    },
  ];

  return (
    <div class="p-8">
      <Head>
        <title>Dashboard — Asset OS</title>
      </Head>

      <div class="mb-10">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          🌿 Asset Registry
        </h1>
        <p class="text-stone-500 mt-1">
          Manage all farm assets — equipment, structures, sensors, water,
          compost, and materials.
        </p>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {stats.map((s) => (
          <a
            key={s.href}
            href={s.href}
            class="group bg-white rounded-2xl border border-stone-200 shadow-sm p-6 flex items-center gap-5 hover:shadow-lg hover:border-emerald-200 transition-all duration-200 cursor-pointer"
          >
            <div
              class={`w-14 h-14 rounded-xl bg-linear-to-br ${s.gradient} flex items-center justify-center text-2xl shadow-sm group-hover:scale-105 transition-transform duration-200 shrink-0`}
            >
              {s.icon}
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-lg font-bold text-stone-800 leading-tight">
                {s.label}
              </p>
              <p class="text-xs text-stone-500 mt-0.5 truncate">{s.sub}</p>
            </div>
            <span class="text-stone-300 group-hover:text-emerald-500 transition-colors text-xl shrink-0">
              →
            </span>
          </a>
        ))}
      </div>

      <div class="mt-12 bg-emerald-50 rounded-2xl border border-emerald-100 p-6">
        <div class="flex items-center gap-3 mb-3">
          <span class="text-2xl">🚀</span>
          <h2 class="text-lg font-bold text-emerald-900">Getting Started</h2>
        </div>
        <ul class="text-sm text-emerald-800 space-y-1.5">
          <li>
            → Start with{" "}
            <a href="/equipment" class="font-semibold underline">Equipment</a>
            {" "}
            to register your tractors and tools
          </li>
          <li>
            → Add{" "}
            <a href="/structures" class="font-semibold underline">Structures</a>
            {" "}
            like barns, greenhouses, and processing facilities
          </li>
          <li>
            → Register{" "}
            <a href="/sensors" class="font-semibold underline">Sensors</a>{" "}
            to begin streaming real-time field data
          </li>
          <li>
            → Track{" "}
            <a href="/materials" class="font-semibold underline">Materials</a>
            {" "}
            inventory with low-stock alerts
          </li>
        </ul>
      </div>
    </div>
  );
});
