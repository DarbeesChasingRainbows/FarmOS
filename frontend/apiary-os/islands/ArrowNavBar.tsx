import { useEffect, useRef } from "preact/hooks";
import { html } from "@arrow-js/core";

interface NavItem {
  href: string;
  label: string;
  icon: string;
  group: "manage" | "insights";
}

const navItems: NavItem[] = [
  { href: "/", label: "Dashboard", icon: "\uD83D\uDCCA", group: "manage" },
  { href: "/hives", label: "Hives", icon: "\uD83D\uDC1D", group: "manage" },
  {
    href: "/apiaries",
    label: "Apiaries",
    icon: "\uD83D\uDCCD",
    group: "manage",
  },
  {
    href: "/calendar",
    label: "Calendar",
    icon: "\uD83D\uDCC5",
    group: "manage",
  },
  {
    href: "/reports",
    label: "Reports",
    icon: "\uD83D\uDCC8",
    group: "insights",
  },
  {
    href: "/financials",
    label: "Financials",
    icon: "\uD83D\uDCB0",
    group: "insights",
  },
];

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

    const navLink = (item: NavItem) =>
      html`
        <li>
          <a
            href="${item.href}"
            class="${isActive(item.href)
              ? "bg-amber-600/20 text-amber-300 shadow-sm"
              : "text-stone-400 hover:bg-stone-800 hover:text-stone-200"} flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150"
          >
            <span class="text-lg">${item.icon}</span>
            <span class="hidden lg:inline">${item.label}</span>
          </a>
        </li>
      `;

    const manageItems = navItems.filter((n) => n.group === "manage");
    const insightItems = navItems.filter((n) => n.group === "insights");

    const template = html`
      <nav
        class="w-16 lg:w-60 min-h-screen bg-stone-900 text-stone-100 flex flex-col border-r border-stone-800 shrink-0 transition-all duration-200"
      >
        <div class="px-3 lg:px-6 py-5 border-b border-stone-800">
          <h1
            class="text-xl font-bold tracking-tight text-amber-400 hidden lg:flex items-center gap-2"
          >
            <span>\\uD83D\\uDC1D</span> Apiary OS
          </h1>
          <span class="text-xl lg:hidden block text-center">\\uD83D\\uDC1D</span>
          <p
            class="text-xs text-stone-500 mt-1 uppercase tracking-widest hidden lg:block"
          >
            Colony Management
          </p>
        </div>

        <div class="flex-1 py-4 px-2 lg:px-3">
          <p
            class="text-[10px] text-stone-600 uppercase tracking-widest font-bold px-3 mb-2 hidden lg:block"
          >
            Manage
          </p>
          <ul class="space-y-1 mb-6">
            ${manageItems.map((item) => navLink(item))}
          </ul>
          <p
            class="text-[10px] text-stone-600 uppercase tracking-widest font-bold px-3 mb-2 hidden lg:block"
          >
            Insights
          </p>
          <ul class="space-y-1">
            ${insightItems.map((item) => navLink(item))}
          </ul>
        </div>

        <div class="px-3 lg:px-6 py-4 border-t border-stone-800">
          <p class="text-xs text-stone-600 hidden lg:block">
            Sovereign \\u00B7 Offline-First
          </p>
          <span class="text-xs text-stone-600 lg:hidden block text-center"
          >\\u25CF</span>
        </div>
      </nav>
    `;

    template(containerRef.current);
  }, [currentPath]);

  return <div ref={containerRef}></div>;
}
