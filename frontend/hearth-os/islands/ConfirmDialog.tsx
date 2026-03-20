import { useSignal } from "@preact/signals";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "danger" | "safe";
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  variant = "safe",
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  if (!open) return null;

  const btnColor = variant === "danger"
    ? "bg-red-600 hover:bg-red-700 text-white"
    : "bg-amber-600 hover:bg-amber-700 text-white";

  return (
    <div
      class="fixed inset-0 z-40 flex items-center justify-center"
      onClick={onCancel}
    >
      {/* Backdrop */}
      <div class="absolute inset-0 bg-black/40 backdrop-blur-sm" />

      {/* Dialog */}
      <div
        class="relative bg-white rounded-xl shadow-2xl border border-stone-200 p-6 max-w-md w-full mx-4 animate-[scaleIn_0.2s_ease-out]"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 class="text-lg font-bold text-stone-800 mb-2">{title}</h3>
        <p class="text-sm text-stone-600 mb-6 leading-relaxed">{message}</p>

        <div class="flex gap-3 justify-end">
          <button
            onClick={onCancel}
            class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            class={`px-4 py-2 text-sm font-semibold rounded-lg transition shadow-sm ${btnColor}`}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
