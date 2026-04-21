import { html } from "@arrow-js/core";

export type BadgeVariant =
  | "active"
  | "fermenting"
  | "resting"
  | "complete"
  | "attention"
  | "idle";

const variants: Record<BadgeVariant, string> = {
  active: "bg-emerald-100 text-emerald-800 border-emerald-200",
  fermenting: "bg-amber-100 text-amber-800 border-amber-200",
  resting: "bg-blue-100 text-blue-800 border-blue-200",
  complete: "bg-stone-100 text-stone-600 border-stone-200",
  attention: "bg-red-100 text-red-800 border-red-200",
  idle: "bg-stone-50 text-stone-500 border-stone-200",
};

const icons: Record<BadgeVariant, string> = {
  active: "🟢",
  fermenting: "🟡",
  resting: "🔵",
  complete: "✅",
  attention: "🔴",
  idle: "⚪",
};

export interface ArrowStatusBadgeProps {
  variant: BadgeVariant | (() => BadgeVariant);
  label?: string | (() => string);
}

export function ArrowStatusBadge(props: ArrowStatusBadgeProps) {
  return html`
    <span
      class="${() => {
        const v = typeof props.variant === "function"
          ? props.variant()
          : props.variant;
        return `inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold uppercase tracking-wider rounded-full border ${
          variants[v]
        }`;
      }}"
    >
      <span class="text-[10px]">
        ${() => {
          const v = typeof props.variant === "function"
            ? props.variant()
            : props.variant;
          return icons[v];
        }}
      </span>
      ${() => {
        const v = typeof props.variant === "function"
          ? props.variant()
          : props.variant;
        const l = typeof props.label === "function"
          ? props.label()
          : props.label;
        return l || v.charAt(0).toUpperCase() + v.slice(1);
      }}
    </span>
  `;
}
