import { html } from "@arrow-js/core";

export type BadgeVariant =
  | "active"
  | "attention"
  | "resting"
  | "queenless"
  | "healthy"
  | "moderate"
  | "critical";

const variants: Record<BadgeVariant, string> = {
  active: "bg-emerald-100 text-emerald-800 border-emerald-200",
  attention: "bg-amber-100 text-amber-800 border-amber-200",
  resting: "bg-stone-100 text-stone-600 border-stone-200",
  queenless: "bg-red-100 text-red-800 border-red-200",
  healthy: "bg-emerald-100 text-emerald-800 border-emerald-200",
  moderate: "bg-amber-100 text-amber-800 border-amber-200",
  critical: "bg-red-100 text-red-800 border-red-200",
};

export interface ArrowStatusBadgeProps {
  variant: BadgeVariant | (() => BadgeVariant);
  label?: string | (() => string);
}

export function ArrowStatusBadge(props: ArrowStatusBadgeProps) {
  return html`
    <span
      class="${() => {
        const v =
          typeof props.variant === "function"
            ? props.variant()
            : props.variant;
        return (
          "inline-flex items-center gap-1 px-2 py-0.5 text-xs font-semibold uppercase tracking-wider rounded-full border " +
          variants[v]
        );
      }}"
    >
      ${() => {
        const v =
          typeof props.variant === "function"
            ? props.variant()
            : props.variant;
        const l =
          typeof props.label === "function" ? props.label() : props.label;
        return l || v.charAt(0).toUpperCase() + v.slice(1);
      }}
    </span>
  `;
}
