import { html } from "@arrow-js/core";

export interface ArrowProgressRingProps {
  percent: number | (() => number);
  size?: number;
  strokeWidth?: number;
  color?: string;
  label?: string | (() => string);
}

export function ArrowProgressRing(props: ArrowProgressRingProps) {
  const size = props.size || 80;
  const stroke = props.strokeWidth || 6;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const center = size / 2;

  return html`
    <div class="inline-flex flex-col items-center gap-1">
      <svg
        width="${size}"
        height="${size}"
        class="transform -rotate-90"
      >
        <circle
          cx="${center}"
          cy="${center}"
          r="${radius}"
          fill="none"
          stroke="#e7e5e4"
          stroke-width="${stroke}"
        />
        <circle
          cx="${center}"
          cy="${center}"
          r="${radius}"
          fill="none"
          stroke="${props.color || "#f59e0b"}"
          stroke-width="${stroke}"
          stroke-linecap="round"
          stroke-dasharray="${circumference}"
          stroke-dashoffset="${() => {
            const pct = typeof props.percent === "function"
              ? props.percent()
              : props.percent;
            return circumference - (pct / 100) * circumference;
          }}"
          class="transition-all duration-500"
        />
      </svg>
      <span class="text-xs font-bold text-stone-700">${() => {
        const pct = typeof props.percent === "function"
          ? props.percent()
          : props.percent;
        return Math.round(pct) + "%";
      }}</span>
      ${() => {
        const l = typeof props.label === "function"
          ? props.label()
          : props.label;
        return l
          ? html`
            <span class="text-xs text-stone-400">${l}</span>
          `
          : html`

          `;
      }}
    </div>
  `;
}
