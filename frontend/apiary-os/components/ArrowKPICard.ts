import { html } from "@arrow-js/core";

export interface KPICardProps {
  label: string;
  value: string | (() => string);
  trend?: string | (() => string);
  trendDirection?: "up" | "down" | "flat" | (() => "up" | "down" | "flat");
  icon: string;
  color?: "amber" | "emerald" | "red" | "violet" | "sky" | "stone";
}

const colorStyles: Record<
  string,
  { bg: string; iconBg: string; trend: string }
> = {
  amber: {
    bg: "bg-white",
    iconBg: "bg-amber-50 text-amber-600",
    trend: "text-amber-600",
  },
  emerald: {
    bg: "bg-white",
    iconBg: "bg-emerald-50 text-emerald-600",
    trend: "text-emerald-600",
  },
  red: {
    bg: "bg-white",
    iconBg: "bg-red-50 text-red-600",
    trend: "text-red-600",
  },
  violet: {
    bg: "bg-white",
    iconBg: "bg-violet-50 text-violet-600",
    trend: "text-violet-600",
  },
  sky: {
    bg: "bg-white",
    iconBg: "bg-sky-50 text-sky-600",
    trend: "text-sky-600",
  },
  stone: {
    bg: "bg-white",
    iconBg: "bg-stone-100 text-stone-600",
    trend: "text-stone-600",
  },
};

export function ArrowKPICard(props: KPICardProps) {
  const c = colorStyles[props.color || "amber"];

  const trendArrow = () => {
    const dir =
      typeof props.trendDirection === "function"
        ? props.trendDirection()
        : props.trendDirection;
    if (dir === "up") return "\u2197";
    if (dir === "down") return "\u2198";
    return "\u2192";
  };

  const trendColor = () => {
    const dir =
      typeof props.trendDirection === "function"
        ? props.trendDirection()
        : props.trendDirection;
    if (dir === "up") return "text-emerald-600 bg-emerald-50";
    if (dir === "down") return "text-red-600 bg-red-50";
    return "text-stone-500 bg-stone-50";
  };

  return html`
    <div
      class="${c.bg} rounded-2xl border border-stone-200/60 shadow-sm p-5 hover:shadow-md transition-shadow"
    >
      <div class="flex items-center justify-between mb-3">
        <span
          class="w-10 h-10 rounded-xl ${c.iconBg} flex items-center justify-center text-lg"
          >${props.icon}</span
        >
        ${() => {
          const trend =
            typeof props.trend === "function" ? props.trend() : props.trend;
          if (!trend) return html``;
          return html`
            <span
              class="${() =>
                trendColor()} text-xs font-bold px-2 py-0.5 rounded-full flex items-center gap-0.5"
            >
              ${() => trendArrow()}
              ${() =>
                typeof props.trend === "function"
                  ? props.trend()
                  : props.trend}
            </span>
          `;
        }}
      </div>
      <p class="text-2xl font-extrabold text-stone-800 tracking-tight">
        ${props.value}
      </p>
      <p class="text-xs text-stone-400 mt-1 uppercase tracking-wider font-medium">
        ${props.label}
      </p>
    </div>
  `;
}
