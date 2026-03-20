import type { ComponentChildren } from "preact";

interface TooltipProps {
  text: string;
  children: ComponentChildren;
  position?: "top" | "bottom";
}

export default function Tooltip(
  { text, children, position = "top" }: TooltipProps,
) {
  const pos = position === "top"
    ? "bottom-full left-1/2 -translate-x-1/2 mb-2"
    : "top-full left-1/2 -translate-x-1/2 mt-2";

  const arrow = position === "top"
    ? "top-full left-1/2 -translate-x-1/2 border-t-stone-800"
    : "bottom-full left-1/2 -translate-x-1/2 border-b-stone-800";

  return (
    <span class="relative inline-flex items-center group cursor-help">
      {children}
      <span
        class={`absolute ${pos} px-3 py-1.5 text-xs text-stone-100 bg-stone-800 rounded-lg shadow-lg whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none z-30 max-w-xs`}
        style={{
          whiteSpace: "normal",
          width: "max-content",
          maxWidth: "260px",
        }}
      >
        {text}
        <span class={`absolute ${arrow} w-0 h-0 border-4 border-transparent`} />
      </span>
    </span>
  );
}

/** Small info icon you can wrap with <Tooltip> */
export function InfoIcon() {
  return (
    <span class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-stone-200 text-stone-500 text-[10px] font-bold ml-1 shrink-0">
      ?
    </span>
  );
}
