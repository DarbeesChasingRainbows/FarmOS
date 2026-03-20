interface NavItem {
  href: string;
  label: string;
  icon: string;
}

const navItems: NavItem[] = [
  { href: "/", label: "Dashboard", icon: "🏡" },
  { href: "/equipment", label: "Equipment", icon: "🚜" },
  { href: "/structures", label: "Structures", icon: "🏚️" },
  { href: "/water", label: "Water Sources", icon: "💧" },
  { href: "/compost", label: "Compost", icon: "♻️" },
  { href: "/sensors", label: "Sensors", icon: "📡" },
  { href: "/materials", label: "Materials", icon: "📦" },
];

export default function NavBar({ currentPath }: { currentPath: string }) {
  const isActive = (href: string) => {
    if (href === "/") return currentPath === "/";
    return currentPath.startsWith(href);
  };

  return (
    <nav class="w-64 min-h-screen bg-stone-900 text-stone-100 flex flex-col border-r border-stone-800">
      {/* Logo */}
      <div class="px-6 py-5 border-b border-stone-800">
        <h1 class="text-xl font-bold tracking-tight text-emerald-400">
          🌿 Asset OS
        </h1>
        <p class="text-xs text-stone-500 mt-1 uppercase tracking-widest">
          Farm Asset Registry
        </p>
      </div>

      {/* Nav */}
      <ul class="flex-1 py-4 px-3 space-y-1">
        {navItems.map((item) => (
          <li key={item.href}>
            <a
              href={item.href}
              class={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150 ${
                isActive(item.href)
                  ? "bg-emerald-600/20 text-emerald-300 shadow-sm"
                  : "text-stone-400 hover:bg-stone-800 hover:text-stone-200"
              }`}
            >
              <span class="text-lg">{item.icon}</span>
              {item.label}
            </a>
          </li>
        ))}
      </ul>

      {/* Footer */}
      <div class="px-6 py-4 border-t border-stone-800">
        <p class="text-xs text-stone-600">Sovereign • Offline-First</p>
      </div>
    </nav>
  );
}
