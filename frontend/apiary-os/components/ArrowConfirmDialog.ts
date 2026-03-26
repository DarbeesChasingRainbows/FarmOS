import { html } from "@arrow-js/core";

export interface ArrowConfirmDialogProps {
  isOpen: () => boolean;
  title: string | (() => string);
  message: string | (() => string);
  onConfirm: () => void;
  onCancel: () => void;
  confirmLabel?: string;
  danger?: boolean;
}

export function ArrowConfirmDialog(props: ArrowConfirmDialogProps) {
  const confirmClass = props.danger
    ? "bg-red-600 text-white hover:bg-red-700"
    : "bg-amber-600 text-white hover:bg-amber-700";

  return html`
    <div
      class="${() =>
        props.isOpen()
          ? "fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50"
          : "hidden"}"
    >
      <div class="bg-white rounded-xl shadow-xl w-full max-w-sm mx-4 p-6">
        <h3 class="text-lg font-bold text-stone-800 mb-2">${props.title}</h3>
        <p class="text-sm text-stone-600 mb-6">${props.message}</p>
        <div class="flex justify-end gap-3">
          <button
            type="button"
            @click="${props.onCancel}"
            class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="${props.onConfirm}"
            class="px-4 py-2 rounded-lg font-semibold ${confirmClass} transition shadow-sm"
          >
            ${props.confirmLabel || "Confirm"}
          </button>
        </div>
      </div>
    </div>
  `;
}
