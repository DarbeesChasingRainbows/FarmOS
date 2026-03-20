export type ToastType = "success" | "error" | "info" | "warning";

export interface Toast {
  id: number;
  type: ToastType;
  title: string;
  message?: string;
}

let _nextId = 0;

// Exporting a standard object to hold the value so we don't have to put signals in a non-tsx file
export const toastSignal = { value: [] as Toast[] };

export function showToast(type: ToastType, title: string, message?: string) {
  const id = ++_nextId;
  const toast: Toast = { id, type, title, message };
  toastSignal.value = [...toastSignal.value, toast];

  setTimeout(() => {
    toastSignal.value = toastSignal.value.filter((t) => t.id !== id);
  }, 4000);
}
