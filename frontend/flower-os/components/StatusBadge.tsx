const styles: Record<string, string> = {
  active: "bg-emerald-100 text-emerald-700",
  planned: "bg-blue-100 text-blue-700",
  seeded: "bg-amber-100 text-amber-700",
  growing: "bg-lime-100 text-lime-700",
  harvesting: "bg-rose-100 text-rose-700",
  conditioned: "bg-cyan-100 text-cyan-700",
  cooler: "bg-sky-100 text-sky-700",
  premium: "bg-emerald-100 text-emerald-700",
  standard: "bg-blue-100 text-blue-700",
  seconds: "bg-amber-100 text-amber-700",
  cull: "bg-red-100 text-red-700",
  default: "bg-stone-100 text-stone-600",
};

export default function StatusBadge(
  { status, label }: { status: string; label?: string },
) {
  const style = styles[status.toLowerCase()] || styles.default;
  return (
    <span
      class={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${style}`}
    >
      {label || status}
    </span>
  );
}
