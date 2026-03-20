const links = [
  { href: "/beds", label: "Beds", icon: "🌱" },
  { href: "/seeds", label: "Seeds", icon: "🌰" },
  { href: "/batches", label: "Post-Harvest", icon: "✂️" },
  { href: "/recipes", label: "Bouquets", icon: "💐" },
  { href: "/plans", label: "Crop Plans", icon: "📋" },
];

export default function NavBar({ current }: { current?: string }) {
  return (
    <nav class="bg-white border-b border-stone-200 px-6 py-3">
      <div class="flex items-center gap-6">
        <a href="/" class="flex items-center gap-2 text-lg font-bold text-emerald-700">
          <span class="text-2xl">🌸</span>
          Flower OS
        </a>
        <div class="flex items-center gap-1 ml-4">
          {links.map(({ href, label, icon }) => (
            <a
              key={href}
              href={href}
              class={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                current === href
                  ? "bg-emerald-50 text-emerald-700"
                  : "text-stone-600 hover:text-stone-900 hover:bg-stone-100"
              }`}
            >
              {icon} {label}
            </a>
          ))}
        </div>
      </div>
    </nav>
  );
}
