import { html } from "@arrow-js/core";

export interface ArrowTooltipProps {
  text: string | (() => string);
  // deno-lint-ignore no-explicit-any
  children: any;
  position?: "top" | "bottom" | (() => "top" | "bottom");
}

export function ArrowTooltip(props: ArrowTooltipProps) {
  const isTop = () => {
    const pos = typeof props.position === "function"
      ? props.position()
      : props.position || "top";
    return pos === "top";
  };

  return html`
    <span class="relative inline-flex items-center group cursor-help">
      ${props.children}
      <span
        class="${() =>
          isTop()
            ? "absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-3 py-1.5 text-xs text-stone-100 bg-stone-800 rounded-lg shadow-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none z-30"
            : "absolute top-full left-1/2 -translate-x-1/2 mt-2 px-3 py-1.5 text-xs text-stone-100 bg-stone-800 rounded-lg shadow-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none z-30"}"
        style="white-space: normal; width: max-content; max-width: 260px;"
      >
        ${props.text}
        <span
          class="${() =>
            isTop()
              ? "absolute top-full left-1/2 -translate-x-1/2 border-t-stone-800 w-0 h-0 border-4 border-transparent"
              : "absolute bottom-full left-1/2 -translate-x-1/2 border-b-stone-800 w-0 h-0 border-4 border-transparent"}"
        ></span>
      </span>
    </span>
  `;
}

export function ArrowInfoIcon() {
  return html`
    <span
      class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-stone-200 text-stone-500 text-[10px] font-bold ml-1 shrink-0"
    >
      ?
    </span>
  `;
}
