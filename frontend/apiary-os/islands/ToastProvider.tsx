import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
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

export default function ToastProvider() {
  const toasts = useSignal<Toast[]>([]);

  useEffect(() => {
    // Poll the global signal (lightweight, no event system needed)
    const interval = setInterval(() => {
      if (JSON.stringify(toasts.value) !== JSON.stringify(toastSignal.value)) {
        toasts.value = [...toastSignal.value];
      }
    }, 100);
    return () => clearInterval(interval);
  }, []);

  if (toasts.value.length === 0) return null;

  return (
    <div
      class="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none"
      style={{ maxWidth: "380px" }}
    >
      {toasts.value.map((toast) => {
        const c = colorMap[toast.type];
        return (
          <div
            key={toast.id}
            class={`pointer-events-auto ${c.bg} ${c.border} border rounded-xl p-4 shadow-lg flex items-start gap-3 animate-[slideIn_0.3s_ease-out]`}
          >
            <span
              class={`${c.icon} w-7 h-7 rounded-full flex items-center justify-center text-sm font-bold shrink-0`}
            >
              {iconMap[toast.type]}
            </span>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-stone-800">{toast.title}</p>
              {toast.message && (
                <p class="text-xs text-stone-600 mt-0.5">{toast.message}</p>
              )}
            </div>
            <button
              onClick={() => {
                toastSignal.value = toastSignal.value.filter((t) =>
                  t.id !== toast.id
                );
                toasts.value = toasts.value.filter((t) => t.id !== toast.id);
              }}
              class="text-stone-400 hover:text-stone-600 text-xs shrink-0"
            >
              ✕
            </button>
          </div>
        );
      })}
    </div>
  );
}
