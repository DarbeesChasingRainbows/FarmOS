import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import BatchStatusCards from "../islands/BatchStatusCards.tsx";
import IoTLiveFeed from "../islands/IoTLiveFeed.tsx";

export default define.page(function Dashboard() {
  const stats = [
    {
      icon: "🍞",
      label: "Active Batches",
      value: "4",
      gradient: "from-amber-500 to-amber-600",
      href: "/batches",
    },
    {
      icon: "🦫",
      label: "Need Feeding",
      value: "1",
      gradient: "from-red-500 to-red-600",
      href: "/cultures",
    },
    {
      icon: "📋",
      label: "Compliance Tasks",
      value: "3",
      gradient: "from-blue-500 to-blue-600",
      href: "/compliance",
    },
    {
      icon: "🍄",
      label: "Fruiting Blocks",
      value: "2",
      gradient: "from-emerald-600 to-emerald-700",
      href: "/mushrooms",
    },
  ];

  const recentActivity = [
    {
      time: "Today 8:15 AM",
      text: "Fed 'Gertrude' sourdough starter",
      href: "/cultures",
    },
    {
      time: "Yesterday",
      text: "Kombucha KB-MAR-02 moved to Secondary",
      href: "/batches",
    },
    {
      time: "Feb 26",
      text: "Started sourdough batch SD-2024-03-A",
      href: "/batches",
    },
  ];

  return (
    <div class="p-8">
      <Head>
        <title>Dashboard — Hearth OS</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          Good morning, Steward
        </h1>
        <p class="text-stone-500 mt-1">
          Here's what's happening across the Hearth today.
        </p>
      </div>

      {/* Stat Cards — clickable, navigate to area */}
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-10">
        {stats.map((s) => (
          <a
            href={s.href}
            class="bg-white rounded-xl border border-stone-200 shadow-sm p-5 flex items-center gap-4 hover:shadow-md hover:border-amber-200 transition group cursor-pointer"
          >
            <div
              class={`w-12 h-12 rounded-lg bg-linear-to-br ${s.gradient} flex items-center justify-center text-xl shadow-sm group-hover:scale-105 transition`}
            >
              {s.icon}
            </div>
            <div>
              <p class="text-2xl font-bold text-stone-800">{s.value}</p>
              <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                {s.label}
              </p>
            </div>
            <span class="ml-auto text-stone-300 group-hover:text-amber-500 transition text-lg">
              →
            </span>
          </a>
        ))}
      </div>

      {/* Live Batch Status */}
      <section class="mb-10">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-xl font-bold text-stone-800">Active Fermentations</h2>
          <a
            href="/batches"
            class="text-sm text-amber-600 hover:text-amber-800 font-semibold transition"
          >
            View All →
          </a>
        </div>
        <BatchStatusCards />
      </section>

      {/* Bottom row: Recent Activity + IoT Live Feed */}
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <section>
          <h2 class="text-xl font-bold text-stone-800 mb-4">Recent Activity</h2>
          <div class="bg-white rounded-xl border border-stone-200 shadow-sm divide-y divide-stone-100">
            {recentActivity.map((item) => (
              <a
                href={item.href}
                class="px-5 py-3 flex items-center gap-4 hover:bg-stone-50 transition cursor-pointer group"
              >
                <span class="text-xs text-stone-400 w-28 shrink-0 font-medium">
                  {item.time}
                </span>
                <span class="text-sm text-stone-700 flex-1">{item.text}</span>
                <span class="text-stone-300 group-hover:text-amber-500 transition">
                  →
                </span>
              </a>
            ))}
          </div>
        </section>

        <section>
          <h2 class="text-xl font-bold text-stone-800 mb-4">IoT Sensor Feed</h2>
          <IoTLiveFeed />
        </section>
      </div>
    </div>
  );
});
