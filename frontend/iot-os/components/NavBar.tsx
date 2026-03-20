/**
 * Rugged tablet navigation — big tap targets, high contrast.
 * Designed for iPads with messy/gloved hands.
 */
export default function NavBar({ currentPath }: { currentPath: string }) {
  const links = [
    { href: "/", label: "🏠 Zones", icon: "📡" },
    { href: "/excursions", label: "🚨 Alerts", icon: "⚠️" },
    { href: "/devices", label: "📟 Devices", icon: "🔧" },
    { href: "/compliance", label: "✅ Compliance", icon: "📋" },
  ];

  return (
    <nav class="bg-stone-950 border-r border-stone-800 w-20 md:w-56 flex flex-col shrink-0">
      {/* Logo */}
      <div class="p-4 md:px-5 md:py-6 border-b border-stone-800">
        <div class="text-center md:text-left">
          <span class="text-2xl">📡</span>
          <span class="hidden md:inline ml-2 text-lg font-black text-amber-400 tracking-tight">
            IoT Grid
          </span>
        </div>
      </div>

      {/* Nav Links — big 64px tap targets */}
      <div class="flex-1 flex flex-col gap-1 p-2 mt-2">
        {links.map((link) => {
          const active = currentPath === link.href ||
            (link.href !== "/" && currentPath.startsWith(link.href));
          return (
            <a
              key={link.href}
              href={link.href}
              class={`flex items-center gap-3 px-3 py-4 rounded-xl text-sm font-bold transition-all min-h-[64px]
                ${active
                  ? "bg-amber-500/20 text-amber-300 border border-amber-500/30"
                  : "text-stone-400 hover:bg-stone-800 hover:text-stone-200 border border-transparent"
                }`}
            >
              <span class="text-xl md:text-base shrink-0">{link.icon}</span>
              <span class="hidden md:inline">{link.label}</span>
            </a>
          );
        })}
      </div>

      {/* Footer */}
      <div class="p-3 border-t border-stone-800">
        <div class="text-center text-xs text-stone-600 hidden md:block">
          FarmOS v2
        </div>
      </div>
    </nav>
  );
}
