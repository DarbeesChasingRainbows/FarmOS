import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import {
  type Toast,
  toastSignal,
  type ToastType,
} from "../utils/toastState.ts";

const iconMap: Record<ToastType, string> = {
  success: "✓",
  error: "✕",
  info: "ℹ",
  warning: "⚠",
};

const colorMap: Record<
  ToastType,
  { bg: string; border: string; icon: string }
> = {
  success: {
    bg: "bg-emerald-50",
    border: "border-emerald-300",
    icon: "text-emerald-600 bg-emerald-100",
  },
  error: {
    bg: "bg-red-50",
    border: "border-red-300",
    icon: "text-red-600 bg-red-100",
  },
  info: {
    bg: "bg-blue-50",
    border: "border-blue-300",
    icon: "text-blue-600 bg-blue-100",
  },
  warning: {
    bg: "bg-amber-50",
    border: "border-amber-300",
    icon: "text-amber-600 bg-amber-100",
  },
};

export default function ArrowToastProvider() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state: any = reactive({
      toasts: [],
    });

    const interval = setInterval(() => {
      if (JSON.stringify(state.toasts) !== JSON.stringify(toastSignal.value)) {
        state.toasts = [...toastSignal.value];
      }
    }, 100);

    const removeToast = (id: number) => {
      toastSignal.value = toastSignal.value.filter((t) => t.id !== id);
      state.toasts = state.toasts.filter((t: { id: number }) => t.id !== id);
    };

    const toastTemplate = (toast: Toast) => {
      const c = colorMap[toast.type];
      return html`
        <div
          class="pointer-events-auto ${c.bg} ${c
            .border} border rounded-xl p-4 shadow-lg flex items-start gap-3 animate-[slideIn_0.3s_ease-out]"
        >
          <span
            class="${c
              .icon} w-7 h-7 rounded-full flex items-center justify-center text-sm font-bold shrink-0"
          >
            ${iconMap[toast.type]}
          </span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-stone-800">${toast.title}</p>
            ${() =>
              toast.message
                ? html`
                  <p class="text-xs text-stone-600 mt-0.5">${toast.message}</p>
                `
                : ""}
          </div>
          <button
            @click="${() => removeToast(toast.id)}"
            class="text-stone-400 hover:text-stone-600 text-xs shrink-0"
          >
            ✕
          </button>
        </div>
      `;
    };

    const template = html`
      <div
        class="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none"
        style="max-width: 380px;"
      >
        ${() => state.toasts.map(toastTemplate)}
      </div>
    `;

    template(containerRef.current);

    return () => clearInterval(interval);
  }, []);

  return <div ref={containerRef}></div>;
}
