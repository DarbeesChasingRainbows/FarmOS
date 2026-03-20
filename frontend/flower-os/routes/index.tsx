import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import NavBar from "../components/NavBar.tsx";

const sections = [
  {
    href: "/beds",
    icon: "🌱",
    title: "Flower Beds",
    description: "Manage beds, succession planting, and track the growing lifecycle from seed to harvest.",
    color: "emerald",
  },
  {
    href: "/seeds",
    icon: "🌰",
    title: "Seed Inventory",
    description: "Track seed lots, germination rates, and organic certification across suppliers.",
    color: "amber",
  },
  {
    href: "/batches",
    icon: "✂️",
    title: "Post-Harvest",
    description: "Grade stems, condition bunches, and manage cooler slots from field to vase.",
    color: "cyan",
  },
  {
    href: "/recipes",
    icon: "💐",
    title: "Bouquet Recipes",
    description: "Design bouquet templates with focal, filler, greenery, and accent stems.",
    color: "rose",
  },
  {
    href: "/plans",
    icon: "📋",
    title: "Crop Plans",
    description: "Plan seasonal production, track yields per linear foot, and analyze profitability.",
    color: "blue",
  },
];

const colorMap: Record<string, string> = {
  emerald: "border-emerald-200 hover:border-emerald-400 hover:bg-emerald-50",
  amber: "border-amber-200 hover:border-amber-400 hover:bg-amber-50",
  cyan: "border-cyan-200 hover:border-cyan-400 hover:bg-cyan-50",
  rose: "border-rose-200 hover:border-rose-400 hover:bg-rose-50",
  blue: "border-blue-200 hover:border-blue-400 hover:bg-blue-50",
};

export default define.page(function Home() {
  return (
    <>
      <Head>
        <title>Flower OS — farmOS</title>
      </Head>
      <NavBar />

      <div class="p-8 max-w-5xl mx-auto">
        <div class="mb-10">
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            🌸 Flower OS
          </h1>
          <p class="text-stone-500 mt-2">
            Cut flower farm management — from succession planting to bouquet delivery.
          </p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {sections.map(({ href, icon, title, description, color }) => (
            <a
              key={href}
              href={href}
              class={`block p-6 bg-white rounded-xl border-2 ${colorMap[color]} transition-all duration-200 hover:shadow-md`}
            >
              <div class="text-3xl mb-3">{icon}</div>
              <h2 class="text-lg font-bold text-stone-800">{title}</h2>
              <p class="text-sm text-stone-500 mt-1">{description}</p>
            </a>
          ))}
        </div>
      </div>
    </>
  );
});
