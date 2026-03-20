interface StatusBadgeProps {
  variant: "active" | "maintenance" | "retired" | "attention" | "idle" | "ok";
  label: string;
}

const styles: Record<string, string> = {
  active: "bg-emerald-100 text-emerald-800 border border-emerald-200",
  maintenance: "bg-amber-100 text-amber-800 border border-amber-200",
  retired: "bg-stone-200 text-stone-600 border border-stone-300",
  attention: "bg-red-100 text-red-800 border border-red-200",
  idle: "bg-amber-100 text-amber-800 border border-amber-200",
  ok: "bg-emerald-100 text-emerald-800 border border-emerald-200",
};

export default function StatusBadge({ variant, label }: StatusBadgeProps) {
  return (
    <span
      class={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${
        styles[variant] ?? styles.active
      }`}
    >
      {label}
    </span>
  );
}
