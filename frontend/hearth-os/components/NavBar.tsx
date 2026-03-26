interface NavItem {
  href: string;
  label: string;
  icon: string;
}

const navItems: NavItem[] = [
  { href: "/", label: "Dashboard", icon: "🏠" },
  { href: "/batches", label: "Batches", icon: "🍞" },
  { href: "/cultures", label: "Cultures", icon: "🧫" },
  { href: "/mushrooms", label: "Mushrooms", icon: "🍄" },
  { href: "/compliance", label: "Compliance", icon: "📋" },
  { href: "/iot", label: "IoT Devices", icon: "📡" },
];

export default function NavBar({ currentPath }: { currentPath: string }) {
  const isActive = (href: string) => {
    if (href === "/") return currentPath === "/";
    return currentPath.startsWith(href);
  };

  return (
    <nav class="w-64 h-[calc(100vh-2rem)] m-4 flex flex-col bg-stone-900/80 backdrop-blur-xl text-stone-100 rounded-4xl shadow-2xl border border-white/10 overflow-hidden relative z-50">
      {/* Logo */}
      <div class="px-8 py-8">
        <h1 class="text-xl font-bold tracking-tight text-amber-400">
          🔥 Hearth OS
        </h1>
        <p class="text-xs text-stone-500 mt-1 uppercase tracking-widest">
          FarmOS Kitchen
        </p>
      </div>

      {/* Nav Items */}
      <ul class="flex-1 py-4 px-3 space-y-1">
        {navItems.map((item) => (
          <li key={item.href}>
            <a
              href={item.href}
              class={`flex items-center gap-4 px-4 py-3.5 rounded-2xl text-[15px] font-semibold transition-all duration-300 ${
                isActive(item.href)
                  ? "bg-linear-to-r from-amber-500/20 to-orange-500/10 text-amber-400 shadow-[inset_0_1px_0_0_rgba(255,255,255,0.1)] border border-amber-500/20"
                  : "text-stone-400 hover:bg-stone-800/50 hover:text-stone-100 border border-transparent"
              } hover:scale-[1.02]`}
            >
              <span class="text-xl">{item.icon}</span>
              {item.label}
            </a>
          </li>
        ))}
      </ul>

      {/* Settings (separated, per Serial Position Effect) */}
      <div class="px-3 pb-4 border-t border-stone-800/50 pt-3">
        <a
          href="/settings"
          class={`flex items-center gap-4 px-4 py-3.5 rounded-2xl text-[15px] font-semibold transition-all duration-300 ${
            isActive("/settings")
              ? "bg-linear-to-r from-amber-500/20 to-orange-500/10 text-amber-400 shadow-[inset_0_1px_0_0_rgba(255,255,255,0.1)] border border-amber-500/20"
              : "text-stone-400 hover:bg-stone-800/50 hover:text-stone-100 border border-transparent"
          } hover:scale-[1.02]`}
        >
          <span class="text-xl">⚙️</span>
          Settings
        </a>
      </div>

      {/* Footer */}
      <div class="px-8 py-6">
        <div class="w-8 h-1 rounded-full bg-stone-800 mb-3"></div>
        <p class="text-[10px] text-stone-500 font-bold tracking-widest uppercase">Sovereign OS</p>
      </div>
    </nav>
  );
}
