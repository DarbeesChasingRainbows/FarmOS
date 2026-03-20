import { signal } from "@preact/signals";

export interface Toast {
  id: number;
  type: "success" | "error" | "warning" | "info";
  title: string;
  message: string;
}

let nextId = 0;
export const toasts = signal<Toast[]>([]);

export function showToast(type: Toast["type"], title: string, message: string) {
  const id = ++nextId;
  toasts.value = [...toasts.value, { id, type, title, message }];
  setTimeout(() => {
    toasts.value = toasts.value.filter((t) => t.id !== id);
  }, 5000);
}
