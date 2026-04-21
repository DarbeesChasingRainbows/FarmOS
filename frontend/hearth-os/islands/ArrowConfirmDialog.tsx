import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";

export interface ArrowConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "danger" | "safe";
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ArrowConfirmDialog(props: ArrowConfirmDialogProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    // Convert props to reactive state so we can track changes
    const state = reactive({
      open: props.open,
    });

    // Update state when props change
    state.open = props.open;

    const template = html`
      ${() => {
        if (!state.open) return "";

        const btnColor = props.variant === "danger"
          ? "bg-red-600 hover:bg-red-700 text-white"
          : "bg-amber-600 hover:bg-amber-700 text-white";

        return html`
          <div
            class="fixed inset-0 z-40 flex items-center justify-center"
            @click="${props.onCancel}"
          >
            <div class="absolute inset-0 bg-black/40 backdrop-blur-sm"></div>

            <div
              class="relative bg-white rounded-xl shadow-2xl border border-stone-200 p-6 max-w-md w-full mx-4 animate-[scaleIn_0.2s_ease-out]"
              @click="${(e: Event) => e.stopPropagation()}"
            >
              <h3 class="text-lg font-bold text-stone-800 mb-2">${props
                .title}</h3>
              <p class="text-sm text-stone-600 mb-6 leading-relaxed">${props
                .message}</p>

              <div class="flex gap-3 justify-end">
                <button
                  @click="${props.onCancel}"
                  class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                >
                  ${props.cancelLabel || "Cancel"}
                </button>
                <button
                  @click="${props.onConfirm}"
                  class="px-4 py-2 text-sm font-semibold rounded-lg transition shadow-sm ${btnColor}"
                >
                  ${props.confirmLabel || "Confirm"}
                </button>
              </div>
            </div>
          </div>
        `;
      }}
    `;

    template(containerRef.current);
  }, [
    props.open,
    props.title,
    props.message,
    props.confirmLabel,
    props.cancelLabel,
    props.variant,
  ]);

  return <div ref={containerRef}></div>;
}
