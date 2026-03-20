import { useSignal } from "@preact/signals";

/**
 * Culture Lineage Tree island.
 *
 * Renders a visual tree of culture splits using the `descended_from`
 * graph edges from ArangoDB. Nodes display health status with color indicators.
 *
 * Props are passed from the culture detail route handler which queries:
 *   FOR v, e, p IN 1..5 OUTBOUND 'cultures/{id}' descended_from
 *     RETURN { culture: v, edge: e, depth: LENGTH(p.edges) }
 */

interface CultureNode {
  id: string;
  name: string;
  type: string; // SourdoughStarter | JunSCOBY | StandardSCOBY
  health: "Thriving" | "NeedsFeed" | "Dormant" | "Retired";
  birthDate: string;
  children?: CultureNode[];
}

interface Props {
  rootCulture: CultureNode;
}

const healthStyles: Record<
  string,
  { dot: string; text: string; label: string }
> = {
  Thriving: {
    dot: "bg-emerald-500",
    text: "text-emerald-700",
    label: "Thriving",
  },
  NeedsFeed: {
    dot: "bg-amber-500",
    text: "text-amber-700",
    label: "Needs Feed",
  },
  Dormant: { dot: "bg-stone-400", text: "text-stone-500", label: "Dormant" },
  Retired: {
    dot: "bg-stone-300",
    text: "text-stone-400 line-through",
    label: "Retired",
  },
};

const cultureIcons: Record<string, string> = {
  SourdoughStarter: "🍞",
  JunSCOBY: "🫖",
  StandardSCOBY: "🫖",
};

function CultureNodeComponent(
  { node, depth = 0 }: { node: CultureNode; depth?: number },
) {
  const expanded = useSignal(depth < 2); // Auto-expand first 2 levels
  const style = healthStyles[node.health] || healthStyles.Dormant;
  const icon = cultureIcons[node.type] || "🧫";
  const hasChildren = node.children && node.children.length > 0;

  return (
    <div class={`${depth > 0 ? "ml-6 border-l-2 border-stone-200 pl-4" : ""}`}>
      <div
        class={`flex items-center gap-3 py-2 px-3 rounded-lg hover:bg-stone-50 transition group ${
          depth === 0 ? "bg-stone-50 border border-stone-200" : ""
        }`}
      >
        {/* Expand toggle */}
        {hasChildren
          ? (
            <button
              type="button"
              onClick={() => (expanded.value = !expanded.value)}
              class="w-5 h-5 flex items-center justify-center text-stone-400 hover:text-stone-600 transition text-xs shrink-0"
              aria-label={expanded.value ? "Collapse" : "Expand"}
            >
              {expanded.value ? "▼" : "▶"}
            </button>
          )
          : <span class="w-5 h-5 shrink-0" />}

        {/* Health dot */}
        <span class={`w-2.5 h-2.5 rounded-full ${style.dot} shrink-0`} />

        {/* Icon + Name */}
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2">
            <span class="text-sm">{icon}</span>
            <span
              class={`text-sm font-semibold ${
                node.health === "Retired"
                  ? "line-through text-stone-400"
                  : "text-stone-800"
              }`}
            >
              {node.name}
            </span>
            {depth === 0 && (
              <span class="text-[10px] bg-stone-200 text-stone-600 px-1.5 py-0.5 rounded font-medium">
                ROOT
              </span>
            )}
          </div>
          <div class="flex items-center gap-2 mt-0.5">
            <span class={`text-[10px] font-medium ${style.text}`}>
              {style.label}
            </span>
            <span class="text-[10px] text-stone-400">
              Born {new Date(node.birthDate).toLocaleDateString()}
            </span>
            {hasChildren && (
              <span class="text-[10px] text-stone-400">
                · {node.children!.length} offspring
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Children */}
      {expanded.value && hasChildren && (
        <div class="mt-1">
          {node.children!.map((child) => (
            <CultureNodeComponent
              key={child.id}
              node={child}
              depth={depth + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
}

export default function CultureLineageTree(props: Props) {
  const { rootCulture } = props;

  return (
    <div class="bg-white rounded-xl border border-stone-200 shadow-sm overflow-hidden">
      {/* Header */}
      <div class="px-5 py-4 border-b border-stone-100 bg-stone-50">
        <h3 class="text-base font-bold text-stone-800">
          🌳 Culture Lineage
        </h3>
        <p class="text-xs text-stone-500 mt-0.5">
          Split history from{" "}
          <code class="bg-stone-200 px-1 rounded text-stone-700">
            descended_from
          </code>{" "}
          graph edges
        </p>
      </div>

      {/* Tree */}
      <div class="p-4">
        <CultureNodeComponent node={rootCulture} />
      </div>

      {/* Legend */}
      <div class="px-5 py-3 bg-stone-50 border-t border-stone-100 flex gap-4 text-[10px] text-stone-500">
        {Object.entries(healthStyles).map(([key, style]) => (
          <span key={key} class="flex items-center gap-1">
            <span class={`w-2 h-2 rounded-full ${style.dot}`} />
            {style.label}
          </span>
        ))}
      </div>
    </div>
  );
}
