import { useEffect, useRef } from "preact/hooks";
import { html } from "@arrow-js/core";

interface NavItem {
  href: string;
  label: string;
  icon: string;
}

interface NavGroup {
  title: string;
  items: NavItem[];
}

const navGroups: NavGroup[] = [
  {
    title: "Fermentation",
    items: [
      { href: "/", label: "Dashboard", icon: "\uD83C\uDFE0" },
      { href: "/batches", label: "Batches", icon: "\uD83C\uDF5E" },
      { href: "/cultures", label: "Cultures", icon: "\uD83E\uDDEB" },
      { href: "/kombucha", label: "Kombucha", icon: "\uD83E\uDED6" },
    ],
  },
  {
    title: "Production",
    items: [
      { href: "/freeze-dryer", label: "Freeze Dryer", icon: "\u2744\uFE0F" },
    ],
  },
  {
    title: "Operations",
    items: [
      { href: "/iot", label: "IoT Devices", icon: "\uD83D\uDCE1" },
      { href: "/compliance", label: "Compliance", icon: "\uD83D\uDCCB" },
    ],
  },
];

const settingsItem: NavItem = {
  href: "/settings",
  label: "Settings",
  icon: "\u2699\uFE0F",
};

export default function ArrowNavBar(
  { currentPath }: { currentPath: string },
) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const isActive = (href: string) => {
      if (href === "/") return currentPath === "/";
      return currentPath.startsWith(href);
    };

    const navLink = (item: NavItem) => html`
      <li>
        <a
          href="${item.href}"
          class="${isActive(item.href)
      ? "bg-orange-600/20 text-orange-300 shadow-sm"
      : "text-stone-400 hover:bg-stone-800 hover:text-stone-200"} flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150"
        >
          <span class="text-lg">${item.icon}</span>
          <span class="hidden lg:inline">${item.label}</span>
        </a>
      </li>
    `;

    const navGroupTemplate = (group: NavGroup) => html`
      <div class="mb-6">
        <p
          class="text-[10px] text-stone-600 uppercase tracking-widest font-bold px-3 mb-2 hidden lg:block"
        >
          ${group.title}
        </p>
        <ul class="space-y-1">
          ${group.items.map((item) => navLink(item))}
        </ul>
      </div>
    `;

    const template = html`
      <nav
        class="w-16 lg:w-60 min-h-screen bg-stone-900 text-stone-100 flex flex-col border-r border-stone-800 shrink-0 transition-all duration-200"
      >
        <div class="px-3 lg:px-6 py-5 border-b border-stone-800">
          <h1
            class="text-xl font-bold tracking-tight text-orange-400 hidden lg:flex items-center gap-2"
          >
            <span>\uD83D\uDD25</span> Hearth OS
          </h1>
          <span class="text-xl lg:hidden block text-center"
            >\uD83D\uDD25</span
          >
          <p
            class="text-xs text-stone-500 mt-1 uppercase tracking-widest hidden lg:block"
          >
            FarmOS Kitchen
          </p>
        </div>

        <div class="flex-1 py-4 px-2 lg:px-3">
          ${navGroups.map((group) => navGroupTemplate(group))}
        </div>

        <div class="px-2 lg:px-3 pb-2 border-t border-stone-800 pt-3">
          <ul class="space-y-1">
            ${navLink(settingsItem)}
          </ul>
        </div>

        <div class="px-3 lg:px-6 py-4 border-t border-stone-800">
          <p class="text-xs text-stone-600 hidden lg:block">
            Sovereign \u00B7 Offline-First
          </p>
          <span class="text-xs text-stone-600 lg:hidden block text-center"
            >\u25CF</span
          >
        </div>
      </nav>
    `;

    template(containerRef.current);
  }, [currentPath]);

  return <div ref={containerRef}></div>;
}
