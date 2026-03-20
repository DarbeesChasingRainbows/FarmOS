/** @jsxImportSource preact */
import { useEffect, useState } from "preact/hooks";
import type {
  CompostBatchDetail,
  CompostBatchSummary,
  CompostMethod,
  CompostPhase,
  KnfInputType,
  NoteCategory,
  PhStatus,
  TempZone,
} from "../utils/assets-client.ts";
import { CompostAPI } from "../utils/assets-client.ts";

// ─── Constants & display helpers ────────────────────────────────────────────

const METHOD_META: Record<
  CompostMethod,
  { icon: string; label: string; color: string }
> = {
  HotAerobic: { icon: "🔥", label: "Hot Aerobic", color: "var(--amber)" },
  ColdPassive: { icon: "❄️", label: "Cold Passive", color: "var(--sky)" },
  Permaculture: { icon: "🌱", label: "Permaculture", color: "var(--emerald)" },
  KoreanNaturalFarming: { icon: "🧪", label: "KNF", color: "var(--violet)" },
  Bokashi: { icon: "🥫", label: "Bokashi", color: "var(--rose)" },
  Vermicompost: { icon: "🪱", label: "Vermicompost", color: "var(--lime)" },
};

const PHASE_COLOR: Record<CompostPhase, string> = {
  Active: "#22c55e",
  Turning: "#f59e0b",
  Fermentation: "#8b5cf6",
  Inoculation: "#06b6d4",
  Curing: "#64748b",
  Finished: "#16a34a",
  Abandoned: "#ef4444",
};

const TEMP_ZONE_META: Record<
  string,
  { icon: string; color: string; label: string }
> = {
  Optimal: { icon: "🟢", color: "#22c55e", label: "Optimal" },
  TooHot: { icon: "🔴", color: "#ef4444", label: "Too Hot" },
  TooLow: { icon: "🟡", color: "#f59e0b", label: "Too Low" },
  Fermentation: { icon: "🟣", color: "#8b5cf6", label: "Fermenting" },
  Ambient: { icon: "⚪", color: "#94a3b8", label: "Ambient" },
};

const PH_STATUS_META: Record<string, { color: string; label: string }> = {
  Optimal: { color: "#22c55e", label: "✅ Optimal" },
  TooHigh: { color: "#f59e0b", label: "⬆ Too High" },
  TooLow: { color: "#ef4444", label: "⬇ Too Low" },
  Neutral: { color: "#64748b", label: "Neutral" },
  Acidic: { color: "#8b5cf6", label: "Acidic" },
  Alkaline: { color: "#0ea5e9", label: "Alkaline" },
};

const NOTE_CATEGORY_COLOR: Record<NoteCategory, string> = {
  Observation: "#64748b",
  Amendment: "#22c55e",
  Issue: "#ef4444",
  Milestone: "#f59e0b",
  Harvest: "#16a34a",
};

const KNF_INPUT_TYPES: KnfInputType[] = [
  "IMO1",
  "IMO2",
  "IMO3",
  "IMO4",
  "LAB",
  "FPJ",
  "FAA",
  "WSCA",
  "OHN",
];
const NOTE_CATEGORIES: NoteCategory[] = [
  "Observation",
  "Amendment",
  "Issue",
  "Milestone",
  "Harvest",
];

const COMPOST_METHODS: CompostMethod[] = [
  "HotAerobic",
  "ColdPassive",
  "Permaculture",
  "KoreanNaturalFarming",
  "Bokashi",
  "Vermicompost",
];

function fToC(f: number) {
  return ((f - 32) * 5 / 9).toFixed(1);
}
function today() {
  return new Date().toISOString().substring(0, 10);
}

// ─── Mini sparkline for temperature log ─────────────────────────────────────

function TempSparkline(
  { readings, method }: {
    readings: { temperatureF: number; zone: TempZone }[];
    method: CompostMethod;
  },
) {
  if (readings.length < 2) {
    return (
      <span style={{ color: "#64748b", fontSize: "0.75rem" }}>
        Not enough data
      </span>
    );
  }
  const values = readings.map((r) => r.temperatureF);
  const min = Math.min(...values) - 5;
  const max = Math.max(...values) + 5;
  const h = 60, w = 240;
  const pts = readings.map((r, i) => {
    const x = (i / (readings.length - 1)) * w;
    const y = h - ((r.temperatureF - min) / (max - min)) * h;
    return `${x},${y}`;
  }).join(" ");

  // Optimal zone lines
  const optLow = method === "Vermicompost" ? 65 : 131;
  const optHigh = method === "Vermicompost" ? 95 : 149;
  const yLow = h - ((optLow - min) / (max - min)) * h;
  const yHigh = h - ((optHigh - min) / (max - min)) * h;

  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      style={{ width: "100%", height: "60px", overflow: "visible" }}
    >
      {/* Optimal zone band */}
      <rect
        x={0}
        y={Math.min(yLow, yHigh)}
        width={w}
        height={Math.abs(yHigh - yLow)}
        fill="rgba(34,197,94,0.12)"
      />
      <line
        x1={0}
        y1={yHigh}
        x2={w}
        y2={yHigh}
        stroke="#22c55e"
        strokeWidth={1}
        strokeDasharray="4 2"
      />
      <line
        x1={0}
        y1={yLow}
        x2={w}
        y2={yLow}
        stroke="#22c55e"
        strokeWidth={1}
        strokeDasharray="4 2"
      />
      <polyline
        points={pts}
        fill="none"
        stroke="#f59e0b"
        strokeWidth={2}
        strokeLinejoin="round"
      />
      {/* Last point dot */}
      {readings.length > 0 && (() => {
        const last = readings[readings.length - 1];
        const lx = w;
        const ly = h - ((last.temperatureF - min) / (max - min)) * h;
        return (
          <circle
            cx={lx}
            cy={ly}
            r={3}
            fill={TEMP_ZONE_META[last.zone]?.color ?? "#f59e0b"}
          />
        );
      })()}
    </svg>
  );
}

// ─── Main CompostPanel island ────────────────────────────────────────────────

export default function CompostPanel() {
  const [batches, setBatches] = useState<CompostBatchSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<CompostBatchDetail | null>(null);
  const [sidebarLoading, setSidebarLoading] = useState(false);

  // Modal state
  type Modal =
    | "start"
    | "temp"
    | "turn"
    | "phase"
    | "inoculate"
    | "ph"
    | "note"
    | "complete"
    | null;
  const [modal, setModal] = useState<Modal>(null);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const load = async () => {
    try {
      setLoading(true);
      const data = await CompostAPI.list();
      setBatches(data);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load batches");
    } finally {
      setLoading(false);
    }
  };

  const openSidebar = async (id: string) => {
    setSidebarLoading(true);
    try {
      const detail = await CompostAPI.detail(id);
      setSelected(detail);
    } finally {
      setSidebarLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const openModal = (m: Modal, id: string) => {
    setModal(m);
    setActiveId(id);
  };
  const closeModal = () => {
    setModal(null);
    setActiveId(null);
  };

  const after = async () => {
    closeModal();
    await load();
    if (selected) await openSidebar(selected.id);
  };

  return (
    <div style={{ display: "flex", height: "100%", gap: "1.5rem" }}>
      {/* ── Left: batch grid ── */}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: "1.5rem",
          }}
        >
          <div>
            <h1 style={{ fontSize: "1.75rem", fontWeight: 700, margin: 0 }}>
              🌿 Compost
            </h1>
            <p
              style={{
                color: "#64748b",
                margin: "0.25rem 0 0 0",
                fontSize: "0.9rem",
              }}
            >
              Track batches across 6 methods: Hot Aerobic, Cold Passive,
              Permaculture, KNF, Bokashi & Vermicompost
            </p>
          </div>
          <button
            id="btn-start-compost"
            onClick={() => setModal("start")}
            style={{
              background: "linear-gradient(135deg,#16a34a,#22c55e)",
              color: "#fff",
              border: "none",
              borderRadius: "0.5rem",
              padding: "0.6rem 1.2rem",
              fontWeight: 600,
              cursor: "pointer",
              boxShadow: "0 2px 8px rgba(34,197,94,0.4)",
            }}
          >
            + Start Batch
          </button>
        </div>

        {loading && <p style={{ color: "#64748b" }}>Loading batches…</p>}
        {error && <p style={{ color: "#ef4444" }}>{error}</p>}

        {!loading && batches.length === 0 && (
          <div
            style={{
              textAlign: "center",
              padding: "4rem 2rem",
              color: "#94a3b8",
            }}
          >
            <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>🌿</div>
            <p style={{ fontWeight: 600, marginBottom: "0.5rem" }}>
              No compost batches yet
            </p>
            <p style={{ fontSize: "0.875rem" }}>
              Start your first batch — choose from Hot Aerobic, KNF, Bokashi,
              and more.
            </p>
          </div>
        )}

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill,minmax(300px,1fr))",
            gap: "1rem",
          }}
        >
          {batches.map((b) => (
            <BatchCard
              key={b.id}
              batch={b}
              onClick={() => openSidebar(b.id)}
              onAction={openModal}
            />
          ))}
        </div>
      </div>

      {/* ── Right: detail sidebar ── */}
      {(selected || sidebarLoading) && (
        <DetailSidebar
          detail={selected}
          loading={sidebarLoading}
          onClose={() => setSelected(null)}
          onAction={openModal}
        />
      )}

      {/* ── Modals ── */}
      {modal === "start" && <StartModal onClose={closeModal} onDone={after} />}
      {modal === "temp" && activeId && (
        <TempModal id={activeId} onClose={closeModal} onDone={after} />
      )}
      {modal === "turn" && activeId && (
        <TurnModal id={activeId} onClose={closeModal} onDone={after} />
      )}
      {modal === "phase" && activeId && (
        <PhaseModal id={activeId} onClose={closeModal} onDone={after} />
      )}
      {modal === "inoculate" && activeId && (
        <InoculateModal id={activeId} onClose={closeModal} onDone={after} />
      )}
      {modal === "ph" && activeId && (
        <PHModal id={activeId} onClose={closeModal} onDone={after} />
      )}
      {modal === "note" && activeId && (
        <NoteModal id={activeId} onClose={closeModal} onDone={after} />
      )}
      {modal === "complete" && activeId && (
        <CompleteModal id={activeId} onClose={closeModal} onDone={after} />
      )}
    </div>
  );
}

// ─── Batch Card ──────────────────────────────────────────────────────────────

function BatchCard({ batch, onClick, onAction }: {
  batch: CompostBatchSummary;
  onClick: () => void;
  onAction: (
    m: "temp" | "turn" | "phase" | "inoculate" | "ph" | "note" | "complete",
    id: string,
  ) => void;
}) {
  const meta = METHOD_META[batch.method];
  const zone = batch.tempZone ? TEMP_ZONE_META[batch.tempZone] : null;

  const cnColor = (() => {
    if (!batch.cnRatioDisplay) return "#94a3b8";
    const ratio = batch.carbonRatio && batch.nitrogenRatio
      ? batch.carbonRatio / batch.nitrogenRatio
      : null;
    if (!ratio) return "#94a3b8";
    return ratio >= 20 && ratio <= 35
      ? "#22c55e"
      : ratio < 15 || ratio > 50
      ? "#ef4444"
      : "#f59e0b";
  })();

  return (
    <div
      onClick={onClick}
      style={{
        background: "#1e293b",
        borderRadius: "0.75rem",
        padding: "1.25rem",
        cursor: "pointer",
        border: "2px solid transparent",
        transition: "border-color 0.2s, transform 0.15s, box-shadow 0.2s",
        boxShadow: "0 2px 8px rgba(0,0,0,0.25)",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLDivElement).style.borderColor = meta.color;
        (e.currentTarget as HTMLDivElement).style.transform =
          "translateY(-2px)";
        (e.currentTarget as HTMLDivElement).style.boxShadow =
          `0 8px 24px rgba(0,0,0,0.3)`;
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLDivElement).style.borderColor = "transparent";
        (e.currentTarget as HTMLDivElement).style.transform = "";
        (e.currentTarget as HTMLDivElement).style.boxShadow =
          "0 2px 8px rgba(0,0,0,0.25)";
      }}
    >
      {/* Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          marginBottom: "0.75rem",
        }}
      >
        <div>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.5rem",
              marginBottom: "0.25rem",
            }}
          >
            <span
              style={{
                background: meta.color + "22",
                color: meta.color,
                borderRadius: "0.375rem",
                padding: "0.2rem 0.5rem",
                fontSize: "0.75rem",
                fontWeight: 600,
              }}
            >
              {meta.icon} {meta.label}
            </span>
          </div>
          <h3 style={{ margin: 0, fontSize: "1.1rem", fontWeight: 700 }}>
            {batch.batchCode}
          </h3>
          <span style={{ fontSize: "0.8rem", color: "#94a3b8" }}>
            {batch.daysElapsed} days old
          </span>
        </div>
        <span
          style={{
            background: PHASE_COLOR[batch.phase] + "22",
            color: PHASE_COLOR[batch.phase],
            padding: "0.25rem 0.6rem",
            borderRadius: "9999px",
            fontSize: "0.75rem",
            fontWeight: 600,
          }}
        >
          {batch.phase}
        </span>
      </div>

      {/* Stats row */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          gap: "0.5rem",
          marginBottom: "0.75rem",
        }}
      >
        {/* Temperature */}
        {batch.lastTempF !== undefined && (
          <div
            style={{
              background: "#0f172a",
              borderRadius: "0.5rem",
              padding: "0.5rem",
            }}
          >
            <div
              style={{
                fontSize: "0.7rem",
                color: "#64748b",
                marginBottom: "0.2rem",
              }}
            >
              TEMP
            </div>
            <div
              style={{ display: "flex", alignItems: "center", gap: "0.3rem" }}
            >
              <span style={{ fontWeight: 700, fontSize: "1rem" }}>
                {batch.lastTempF}°F
              </span>
              <span style={{ fontSize: "0.7rem", color: "#94a3b8" }}>
                ({fToC(batch.lastTempF)}°C)
              </span>
              {zone && (
                <span title={zone.label} style={{ marginLeft: "auto" }}>
                  {zone.icon}
                </span>
              )}
            </div>
          </div>
        )}
        {/* C:N Ratio */}
        {batch.cnRatioDisplay && (
          <div
            style={{
              background: "#0f172a",
              borderRadius: "0.5rem",
              padding: "0.5rem",
            }}
          >
            <div
              style={{
                fontSize: "0.7rem",
                color: "#64748b",
                marginBottom: "0.2rem",
              }}
            >
              C:N RATIO
            </div>
            <div style={{ fontWeight: 700, fontSize: "1rem", color: cnColor }}>
              {batch.cnRatioDisplay}
            </div>
          </div>
        )}
        {/* Latest pH (Bokashi) */}
        {batch.latestPH !== undefined && (
          <div
            style={{
              background: "#0f172a",
              borderRadius: "0.5rem",
              padding: "0.5rem",
            }}
          >
            <div
              style={{
                fontSize: "0.7rem",
                color: "#64748b",
                marginBottom: "0.2rem",
              }}
            >
              pH
            </div>
            <div style={{ fontWeight: 700, fontSize: "1rem" }}>
              {batch.latestPH}
            </div>
          </div>
        )}
        {/* Turns */}
        <div
          style={{
            background: "#0f172a",
            borderRadius: "0.5rem",
            padding: "0.5rem",
          }}
        >
          <div
            style={{
              fontSize: "0.7rem",
              color: "#64748b",
              marginBottom: "0.2rem",
            }}
          >
            TURNS
          </div>
          <div style={{ fontWeight: 700, fontSize: "1rem" }}>
            🔄 {batch.turnCount}
          </div>
        </div>
      </div>

      {/* Quick actions */}
      <div
        style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}
        onClick={(e) => e.stopPropagation()}
      >
        <ActionBtn
          label="🌡 Log Temp"
          onClick={() => onAction("temp", batch.id)}
        />
        <ActionBtn label="🔄 Turn" onClick={() => onAction("turn", batch.id)} />
        {(batch.method === "KoreanNaturalFarming") && (
          <ActionBtn
            label="🧪 Inoculate"
            onClick={() => onAction("inoculate", batch.id)}
          />
        )}
        {(batch.method === "Bokashi") && (
          <ActionBtn
            label="📊 Log pH"
            onClick={() => onAction("ph", batch.id)}
          />
        )}
        <ActionBtn label="📝 Note" onClick={() => onAction("note", batch.id)} />
        {batch.phase !== "Finished" && batch.phase !== "Abandoned" && (
          <ActionBtn
            label="✅ Complete"
            color="#22c55e"
            onClick={() => onAction("complete", batch.id)}
          />
        )}
      </div>
    </div>
  );
}

function ActionBtn(
  { label, onClick, color }: {
    label: string;
    onClick: () => void;
    color?: string;
  },
) {
  return (
    <button
      onClick={onClick}
      style={{
        background: color ? color + "22" : "#334155",
        color: color ?? "#cbd5e1",
        border: color ? `1px solid ${color}55` : "1px solid #475569",
        borderRadius: "0.375rem",
        padding: "0.25rem 0.6rem",
        fontSize: "0.72rem",
        cursor: "pointer",
        fontWeight: 500,
        transition: "background 0.15s",
      }}
    >
      {label}
    </button>
  );
}

// ─── Detail Sidebar ──────────────────────────────────────────────────────────

function DetailSidebar({ detail, loading, onClose, onAction }: {
  detail: CompostBatchDetail | null;
  loading: boolean;
  onClose: () => void;
  onAction: (
    m: "temp" | "turn" | "phase" | "inoculate" | "ph" | "note" | "complete",
    id: string,
  ) => void;
}) {
  const meta = detail ? METHOD_META[detail.method] : null;

  return (
    <div
      style={{
        width: "380px",
        flexShrink: 0,
        background: "#1e293b",
        borderRadius: "0.75rem",
        padding: "1.5rem",
        overflowY: "auto",
        maxHeight: "calc(100vh - 8rem)",
        border: "1px solid #334155",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          marginBottom: "1rem",
        }}
      >
        <div>
          {meta && (
            <span
              style={{
                background: meta.color + "22",
                color: meta.color,
                borderRadius: "0.375rem",
                padding: "0.2rem 0.5rem",
                fontSize: "0.8rem",
                fontWeight: 600,
              }}
            >
              {meta.icon} {meta.label}
            </span>
          )}
          <h2
            style={{
              margin: "0.5rem 0 0 0",
              fontSize: "1.25rem",
              fontWeight: 700,
            }}
          >
            {detail?.batchCode ?? "Loading…"}
          </h2>
        </div>
        <button
          onClick={onClose}
          style={{
            background: "none",
            border: "none",
            color: "#94a3b8",
            fontSize: "1.5rem",
            cursor: "pointer",
            lineHeight: 1,
          }}
        >
          ×
        </button>
      </div>

      {loading && <p style={{ color: "#64748b" }}>Loading detail…</p>}

      {detail && (
        <>
          {/* Phase + stats */}
          <div
            style={{
              display: "flex",
              gap: "0.5rem",
              marginBottom: "1rem",
              flexWrap: "wrap",
            }}
          >
            <Chip
              label={`Phase: ${detail.phase}`}
              color={PHASE_COLOR[detail.phase]}
            />
            <Chip label={`${detail.daysElapsed} days`} color="#64748b" />
            {detail.cnRatioDisplay && (
              <Chip label={`C:N ${detail.cnRatioDisplay}`} color="#22c55e" />
            )}
            {detail.yieldCuYd && (
              <Chip label={`Yield: ${detail.yieldCuYd}`} color="#16a34a" />
            )}
          </div>

          {/* Action buttons */}
          <div
            style={{
              display: "flex",
              gap: "0.4rem",
              flexWrap: "wrap",
              marginBottom: "1.25rem",
            }}
          >
            <ActionBtn
              label="🌡 Log Temp"
              onClick={() => onAction("temp", detail.id)}
            />
            <ActionBtn
              label="🔄 Turn"
              onClick={() => onAction("turn", detail.id)}
            />
            <ActionBtn
              label="📊 Phase"
              onClick={() => onAction("phase", detail.id)}
            />
            {detail.method === "KoreanNaturalFarming" && (
              <ActionBtn
                label="🧪 Inoculate"
                onClick={() => onAction("inoculate", detail.id)}
              />
            )}
            {detail.method === "Bokashi" && (
              <ActionBtn
                label="📊 Log pH"
                onClick={() => onAction("ph", detail.id)}
              />
            )}
            <ActionBtn
              label="📝 Note"
              onClick={() => onAction("note", detail.id)}
            />
            {detail.phase !== "Finished" && detail.phase !== "Abandoned" && (
              <ActionBtn
                label="✅ Complete"
                color="#22c55e"
                onClick={() => onAction("complete", detail.id)}
              />
            )}
          </div>

          {/* Inputs */}
          <Section title="🪣 Inputs">
            {detail.inputs.length === 0 ? <Empty /> : (
              <table
                style={{
                  width: "100%",
                  fontSize: "0.8rem",
                  borderCollapse: "collapse",
                }}
              >
                <thead>
                  <tr style={{ color: "#64748b" }}>
                    <th
                      style={{ textAlign: "left", paddingBottom: "0.25rem" }}
                    >
                      Material
                    </th>
                    <th style={{ textAlign: "right" }}>Amount</th>
                    <th style={{ textAlign: "right" }}>C:N</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.inputs.map((inp, i) => (
                    <tr key={i} style={{ borderTop: "1px solid #334155" }}>
                      <td style={{ padding: "0.3rem 0" }}>
                        <span
                          style={{
                            fontSize: "0.7rem",
                            color: inp.type === "Browns"
                              ? "#f59e0b"
                              : "#22c55e",
                            marginRight: "0.3rem",
                          }}
                        >
                          {inp.type === "Browns" ? "🟤" : "🟢"}
                        </span>
                        {inp.material}
                      </td>
                      <td style={{ textAlign: "right" }}>
                        {inp.amount} {inp.unit}
                      </td>
                      <td style={{ textAlign: "right", color: "#94a3b8" }}>
                        {inp.cnRatio ? `${inp.cnRatio}:1` : "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Section>

          {/* Temperature chart */}
          <Section title="🌡 Temperature Log">
            {detail.temperatureLog.length === 0
              ? <Empty text="No temperature readings yet" />
              : (
                <>
                  <TempSparkline
                    readings={detail.temperatureLog}
                    method={detail.method}
                  />
                  <div
                    style={{
                      fontSize: "0.75rem",
                      color: "#64748b",
                      marginTop: "0.25rem",
                    }}
                  >
                    Latest:{" "}
                    <strong style={{ color: "#fff" }}>
                      {detail.temperatureLog[detail.temperatureLog.length - 1]
                        .temperatureF}°F
                    </strong>{" "}
                    ({fToC(
                      detail.temperatureLog[detail.temperatureLog.length - 1]
                        .temperatureF,
                    )}°C)
                    {" — "}
                    {TEMP_ZONE_META[
                      detail.temperatureLog[detail.temperatureLog.length - 1]
                        .zone
                    ]?.icon} {TEMP_ZONE_META[
                      detail.temperatureLog[detail.temperatureLog.length - 1]
                        .zone
                    ]?.label}
                  </div>
                </>
              )}
          </Section>

          {/* Turn log */}
          <Section title={`🔄 Turn Log (${detail.turnLog.length})`}>
            {detail.turnLog.length === 0
              ? <Empty text="Pile not yet turned" />
              : (
                detail.turnLog.map((t, i) => (
                  <div
                    key={i}
                    style={{
                      borderTop: "1px solid #334155",
                      padding: "0.4rem 0",
                      fontSize: "0.8rem",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                      }}
                    >
                      <span>{t.date}</span>
                      <span style={{ color: "#94a3b8" }}>
                        +{t.daysSincePrev} days
                      </span>
                    </div>
                    {t.notes && (
                      <div style={{ color: "#94a3b8", fontSize: "0.75rem" }}>
                        {t.notes}
                      </div>
                    )}
                  </div>
                ))
              )}
          </Section>

          {/* KNF Inoculation log */}
          {detail.method === "KoreanNaturalFarming" && (
            <Section title={`🧪 Inoculations (${detail.inoculations.length})`}>
              {detail.inoculations.length === 0
                ? <Empty text="No inoculations recorded" />
                : (
                  detail.inoculations.map((inp, i) => (
                    <div
                      key={i}
                      style={{
                        borderTop: "1px solid #334155",
                        padding: "0.4rem 0",
                        fontSize: "0.8rem",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                        }}
                      >
                        <span style={{ fontWeight: 600, color: "#8b5cf6" }}>
                          {inp.inputType}
                        </span>
                        <span style={{ color: "#94a3b8" }}>
                          {inp.preparedDate}
                        </span>
                      </div>
                      <div style={{ color: "#cbd5e1" }}>{inp.description}</div>
                      <div style={{ color: "#64748b", fontSize: "0.72rem" }}>
                        {inp.amount} {inp.unit}
                      </div>
                    </div>
                  ))
                )}
            </Section>
          )}

          {/* pH log (Bokashi + general) */}
          {(detail.method === "Bokashi" || detail.phLog.length > 0) && (
            <Section title={`📊 pH Log (${detail.phLog.length})`}>
              {detail.phLog.length === 0
                ? <Empty text="No pH measurements yet" />
                : (
                  detail.phLog.map((p, i) => (
                    <div
                      key={i}
                      style={{
                        borderTop: "1px solid #334155",
                        padding: "0.4rem 0",
                        fontSize: "0.8rem",
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                      }}
                    >
                      <span>{p.date}</span>
                      <span style={{ fontWeight: 700, fontSize: "1rem" }}>
                        {p.pH}
                      </span>
                      <span
                        style={{
                          color: PH_STATUS_META[p.status]?.color ?? "#fff",
                          fontSize: "0.72rem",
                        }}
                      >
                        {PH_STATUS_META[p.status]?.label}
                      </span>
                    </div>
                  ))
                )}
            </Section>
          )}

          {/* Notes */}
          <Section title={`📝 Notes (${detail.notes.length})`}>
            {detail.notes.length === 0 ? <Empty text="No notes yet" /> : (
              detail.notes.map((n, i) => (
                <div
                  key={i}
                  style={{
                    borderTop: "1px solid #334155",
                    padding: "0.5rem 0",
                    fontSize: "0.8rem",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      gap: "0.5rem",
                      marginBottom: "0.25rem",
                    }}
                  >
                    <span
                      style={{
                        background: NOTE_CATEGORY_COLOR[n.category] + "22",
                        color: NOTE_CATEGORY_COLOR[n.category],
                        borderRadius: "0.25rem",
                        padding: "0.1rem 0.4rem",
                        fontSize: "0.7rem",
                      }}
                    >
                      {n.category}
                    </span>
                    <span style={{ color: "#64748b", fontSize: "0.72rem" }}>
                      {n.date}
                    </span>
                  </div>
                  <div style={{ color: "#cbd5e1" }}>{n.body}</div>
                </div>
              ))
            )}
          </Section>
        </>
      )}
    </div>
  );
}

function Section(
  { title, children }: { title: string; children: preact.ComponentChildren },
) {
  return (
    <div style={{ marginBottom: "1.25rem" }}>
      <h4
        style={{
          margin: "0 0 0.5rem 0",
          fontSize: "0.8rem",
          fontWeight: 700,
          color: "#94a3b8",
          letterSpacing: "0.05em",
          textTransform: "uppercase",
        }}
      >
        {title}
      </h4>
      {children}
    </div>
  );
}

function Chip({ label, color }: { label: string; color: string }) {
  return (
    <span
      style={{
        background: color + "22",
        color,
        borderRadius: "9999px",
        padding: "0.2rem 0.6rem",
        fontSize: "0.75rem",
        fontWeight: 600,
      }}
    >
      {label}
    </span>
  );
}

function Empty({ text = "None recorded" }: { text?: string }) {
  return (
    <p style={{ color: "#475569", fontSize: "0.8rem", margin: "0.25rem 0" }}>
      {text}
    </p>
  );
}

// ─── Modals ──────────────────────────────────────────────────────────────────

function ModalShell({ title, onClose, onSubmit, submitting, children }: {
  title: string;
  onClose: () => void;
  onSubmit: () => void;
  submitting: boolean;
  children: preact.ComponentChildren;
}) {
  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.7)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 1000,
      }}
    >
      <div
        style={{
          background: "#1e293b",
          borderRadius: "0.75rem",
          padding: "1.5rem",
          width: "min(480px,90vw)",
          maxHeight: "90vh",
          overflowY: "auto",
          boxShadow: "0 24px 64px rgba(0,0,0,0.6)",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            marginBottom: "1rem",
          }}
        >
          <h3 style={{ margin: 0, fontSize: "1.1rem", fontWeight: 700 }}>
            {title}
          </h3>
          <button
            onClick={onClose}
            style={{
              background: "none",
              border: "none",
              color: "#94a3b8",
              fontSize: "1.5rem",
              cursor: "pointer",
            }}
          >
            ×
          </button>
        </div>
        {children}
        <div
          style={{
            display: "flex",
            gap: "0.75rem",
            justifyContent: "flex-end",
            marginTop: "1.25rem",
          }}
        >
          <button
            onClick={onClose}
            style={{
              background: "#334155",
              color: "#cbd5e1",
              border: "none",
              borderRadius: "0.5rem",
              padding: "0.6rem 1.2rem",
              cursor: "pointer",
            }}
          >
            Cancel
          </button>
          <button
            onClick={onSubmit}
            disabled={submitting}
            style={{
              background: "linear-gradient(135deg,#16a34a,#22c55e)",
              color: "#fff",
              border: "none",
              borderRadius: "0.5rem",
              padding: "0.6rem 1.2rem",
              cursor: submitting ? "default" : "pointer",
              fontWeight: 600,
              opacity: submitting ? 0.7 : 1,
            }}
          >
            {submitting ? "Saving…" : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
}

function Field(
  { label, children }: { label: string; children: preact.ComponentChildren },
) {
  return (
    <div style={{ marginBottom: "0.875rem" }}>
      <label
        style={{
          display: "block",
          fontSize: "0.8rem",
          color: "#94a3b8",
          marginBottom: "0.3rem",
          fontWeight: 600,
        }}
      >
        {label}
      </label>
      {children}
    </div>
  );
}

const inputStyle: preact.JSX.CSSProperties = {
  width: "100%",
  background: "#0f172a",
  border: "1px solid #334155",
  color: "#f1f5f9",
  borderRadius: "0.5rem",
  padding: "0.6rem 0.75rem",
  fontSize: "0.875rem",
  boxSizing: "border-box",
};

const selectStyle: preact.JSX.CSSProperties = { ...inputStyle };

// Start Batch Modal
function StartModal(
  { onClose, onDone }: { onClose: () => void; onDone: () => void },
) {
  const [batchCode, setBatchCode] = useState("");
  const [method, setMethod] = useState<CompostMethod>("HotAerobic");
  const [lat, setLat] = useState("0");
  const [lng, setLng] = useState("0");
  const [carbonRatio, setCarbonRatio] = useState("25");
  const [nitrogenRatio, setNitrogenRatio] = useState("1");
  const [notes, setNotes] = useState("");
  const [materials, setMaterials] = useState([{
    material: "",
    amount: "0",
    unit: "kg",
    type: "Browns",
    cnRatio: "",
  }]);
  const [submitting, setSubmitting] = useState(false);
  const [err, setErr] = useState("");

  const addMaterial = () =>
    setMaterials(
      (m) => [...m, {
        material: "",
        amount: "0",
        unit: "kg",
        type: "Greens",
        cnRatio: "",
      }],
    );
  const removeMaterial = (i: number) =>
    setMaterials((m) => m.filter((_, j) => j !== i));
  const updateMaterial = (i: number, field: string, val: string) =>
    setMaterials((m) =>
      m.map((item, j) => j === i ? { ...item, [field]: val } : item)
    );

  const submit = async () => {
    if (!batchCode.trim()) {
      setErr("Batch code is required");
      return;
    }
    setSubmitting(true);
    setErr("");
    try {
      await CompostAPI.start({
        batchCode: batchCode.trim(),
        method,
        location: { lat: parseFloat(lat), lng: parseFloat(lng) },
        inputs: materials.map((m) => ({
          material: m.material,
          amount: {
            value: parseFloat(m.amount) || 0,
            unit: m.unit,
            displayUnit: m.unit,
          },
          type: m.type,
          cnRatio: m.cnRatio ? parseFloat(m.cnRatio) : undefined,
        })),
        carbonRatio: carbonRatio ? parseFloat(carbonRatio) : undefined,
        nitrogenRatio: nitrogenRatio ? parseFloat(nitrogenRatio) : undefined,
        notes: notes || undefined,
      });
      onDone();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : "Failed to start batch");
    } finally {
      setSubmitting(false);
    }
  };

  const meta = METHOD_META[method];

  return (
    <ModalShell
      title="🌿 Start Compost Batch"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      {err && (
        <p
          style={{
            color: "#ef4444",
            fontSize: "0.8rem",
            marginBottom: "0.75rem",
          }}
        >
          {err}
        </p>
      )}
      <Field label="Batch Code">
        <input
          value={batchCode}
          onInput={(e) => setBatchCode((e.target as HTMLInputElement).value)}
          placeholder="e.g. HOT-2026-01"
          style={inputStyle}
        />
      </Field>
      <Field label="Method">
        <select
          value={method}
          onChange={(e) =>
            setMethod((e.target as HTMLSelectElement).value as CompostMethod)}
          style={selectStyle}
        >
          {COMPOST_METHODS.map((m) => (
            <option key={m} value={m}>
              {METHOD_META[m].icon} {METHOD_META[m].label}
            </option>
          ))}
        </select>
        <div
          style={{
            fontSize: "0.75rem",
            color: "#64748b",
            marginTop: "0.3rem",
            background: meta.color + "11",
            borderRadius: "0.375rem",
            padding: "0.4rem 0.6rem",
          }}
        >
          {method === "HotAerobic" &&
            "🔥 Target 131-149°F (55-65°C). Turn every 3-5 days. Ideal C:N: 25-30:1"}
          {method === "ColdPassive" &&
            "❄️ Slow, minimal effort. 6-12 months. Tolerant of poor C:N ratios."}
          {method === "Permaculture" &&
            "🌱 Trench, sheet mulch, or Hugelkultur. Soil-building focus."}
          {method === "KoreanNaturalFarming" &&
            "🧪 Inoculate with IMO1-4, LAB, FPJ, FAA. Indigenous microorganism culture."}
          {method === "Bokashi" &&
            "🥫 Anaerobic fermentation. Target pH 3.5-4.5 after 2-3 weeks."}
          {method === "Vermicompost" &&
            "🪱 Worm-mediated. Keep 65-95°F (18-35°C) and 60-70% moisture."}
        </div>
      </Field>
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          gap: "0.75rem",
        }}
      >
        <Field label="C:N Carbon parts">
          <input
            type="number"
            value={carbonRatio}
            onInput={(e) =>
              setCarbonRatio((e.target as HTMLInputElement).value)}
            style={inputStyle}
          />
        </Field>
        <Field label="C:N Nitrogen parts">
          <input
            type="number"
            value={nitrogenRatio}
            onInput={(e) =>
              setNitrogenRatio((e.target as HTMLInputElement).value)}
            style={inputStyle}
          />
        </Field>
      </div>
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          gap: "0.75rem",
        }}
      >
        <Field label="Latitude">
          <input
            type="number"
            value={lat}
            onInput={(e) => setLat((e.target as HTMLInputElement).value)}
            style={inputStyle}
          />
        </Field>
        <Field label="Longitude">
          <input
            type="number"
            value={lng}
            onInput={(e) => setLng((e.target as HTMLInputElement).value)}
            style={inputStyle}
          />
        </Field>
      </div>

      <div style={{ marginBottom: "0.875rem" }}>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: "0.5rem",
          }}
        >
          <label
            style={{ fontSize: "0.8rem", color: "#94a3b8", fontWeight: 600 }}
          >
            Materials
          </label>
          <button
            onClick={addMaterial}
            style={{
              background: "#334155",
              color: "#cbd5e1",
              border: "none",
              borderRadius: "0.375rem",
              padding: "0.2rem 0.6rem",
              fontSize: "0.75rem",
              cursor: "pointer",
            }}
          >
            + Add
          </button>
        </div>
        {materials.map((m, i) => (
          <div
            key={i}
            style={{
              background: "#0f172a",
              borderRadius: "0.5rem",
              padding: "0.75rem",
              marginBottom: "0.5rem",
            }}
          >
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "1fr auto",
                gap: "0.5rem",
                marginBottom: "0.5rem",
              }}
            >
              <input
                value={m.material}
                onInput={(e) =>
                  updateMaterial(
                    i,
                    "material",
                    (e.target as HTMLInputElement).value,
                  )}
                placeholder="e.g. Rice Straw"
                style={{ ...inputStyle, marginBottom: 0 }}
              />
              <button
                onClick={() => removeMaterial(i)}
                style={{
                  background: "#ef444422",
                  color: "#ef4444",
                  border: "none",
                  borderRadius: "0.375rem",
                  padding: "0 0.6rem",
                  cursor: "pointer",
                }}
              >
                ✕
              </button>
            </div>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "1fr 80px 100px 80px",
                gap: "0.4rem",
              }}
            >
              <input
                type="number"
                value={m.amount}
                onInput={(e) =>
                  updateMaterial(
                    i,
                    "amount",
                    (e.target as HTMLInputElement).value,
                  )}
                placeholder="Amount"
                style={inputStyle}
              />
              <input
                value={m.unit}
                onInput={(e) =>
                  updateMaterial(
                    i,
                    "unit",
                    (e.target as HTMLInputElement).value,
                  )}
                placeholder="kg"
                style={inputStyle}
              />
              <select
                value={m.type}
                onChange={(e) =>
                  updateMaterial(
                    i,
                    "type",
                    (e.target as HTMLSelectElement).value,
                  )}
                style={selectStyle}
              >
                <option>Browns</option>
                <option>Greens</option>
                <option>Inoculant</option>
                <option>Activator</option>
              </select>
              <input
                type="number"
                value={m.cnRatio}
                onInput={(e) =>
                  updateMaterial(
                    i,
                    "cnRatio",
                    (e.target as HTMLInputElement).value,
                  )}
                placeholder="C:N"
                style={inputStyle}
                title="C:N ratio of this material (e.g. 60 for straw, 15 for food scraps)"
              />
            </div>
          </div>
        ))}
      </div>

      <Field label="Notes (optional)">
        <textarea
          value={notes}
          onInput={(e) => setNotes((e.target as HTMLTextAreaElement).value)}
          rows={2}
          placeholder="Location details, goals, initial observations…"
          style={{ ...inputStyle, resize: "vertical" }}
        />
      </Field>
    </ModalShell>
  );
}

// Log Temperature Modal
function TempModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [tempF, setTempF] = useState("");
  const [unit, setUnit] = useState<"F" | "C">("F");
  const [submitting, setSubmitting] = useState(false);
  const [err, setErr] = useState("");
  const displayF = unit === "C" && tempF
    ? ((parseFloat(tempF) * 9 / 5) + 32).toFixed(1)
    : tempF;

  const submit = async () => {
    const f = unit === "F"
      ? parseFloat(tempF)
      : (parseFloat(tempF) * 9 / 5) + 32;
    if (isNaN(f)) {
      setErr("Enter a valid temperature");
      return;
    }
    setSubmitting(true);
    try {
      await CompostAPI.logTemp(id, {
        timestamp: new Date().toISOString(),
        temperatureF: f,
      });
      onDone();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : "Error");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="🌡 Log Temperature"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      {err && <p style={{ color: "#ef4444", fontSize: "0.8rem" }}>{err}</p>}
      <div
        style={{
          background: "#0f172a",
          borderRadius: "0.5rem",
          padding: "0.75rem",
          marginBottom: "1rem",
          fontSize: "0.8rem",
          color: "#94a3b8",
        }}
      >
        <strong style={{ color: "#f59e0b" }}>🔥 Hot Aerobic:</strong>{" "}
        131-149°F (55-65°C) optimal<br />
        <strong style={{ color: "#22c55e" }}>🪱 Vermicompost:</strong>{" "}
        65-95°F (18-35°C) optimal
      </div>
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr auto",
          gap: "0.75rem",
        }}
      >
        <Field label="Temperature">
          <input
            type="number"
            value={tempF}
            onInput={(e) => setTempF((e.target as HTMLInputElement).value)}
            placeholder={unit === "F" ? "e.g. 145" : "e.g. 63"}
            style={inputStyle}
          />
        </Field>
        <Field label="Unit">
          <select
            value={unit}
            onChange={(e) =>
              setUnit((e.target as HTMLSelectElement).value as "F" | "C")}
            style={selectStyle}
          >
            <option value="F">°F</option>
            <option value="C">°C</option>
          </select>
        </Field>
      </div>
      {tempF && unit === "C" && (
        <p style={{ fontSize: "0.8rem", color: "#94a3b8" }}>= {displayF}°F</p>
      )}
      {tempF && unit === "F" && (
        <p style={{ fontSize: "0.8rem", color: "#94a3b8" }}>
          = {((parseFloat(tempF) - 32) * 5 / 9).toFixed(1)}°C
        </p>
      )}
    </ModalShell>
  );
}

// Turn Modal
function TurnModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [date, setDate] = useState(today());
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const submit = async () => {
    setSubmitting(true);
    try {
      await CompostAPI.turn(id, date, notes || undefined);
      onDone();
    } catch {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="🔄 Turn Pile"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      <div
        style={{
          background: "#0f172a",
          borderRadius: "0.5rem",
          padding: "0.75rem",
          marginBottom: "1rem",
          fontSize: "0.8rem",
          color: "#94a3b8",
        }}
      >
        Hot Aerobic piles should be turned every 3-5 days during the
        thermophilic phase. Consistent turning ensures even breakdown and
        pathogen kill.
      </div>
      <Field label="Date of Turn">
        <input
          type="date"
          value={date}
          onInput={(e) => setDate((e.target as HTMLInputElement).value)}
          style={inputStyle}
        />
      </Field>
      <Field label="Notes (optional)">
        <textarea
          value={notes}
          onInput={(e) => setNotes((e.target as HTMLTextAreaElement).value)}
          rows={2}
          placeholder="Moisture observed, texture, smell…"
          style={{ ...inputStyle, resize: "vertical" }}
        />
      </Field>
    </ModalShell>
  );
}

// Phase Change Modal
function PhaseModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [phase, setPhase] = useState<CompostPhase>("Curing");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const phases: CompostPhase[] = [
    "Active",
    "Turning",
    "Fermentation",
    "Inoculation",
    "Curing",
    "Finished",
    "Abandoned",
  ];

  const submit = async () => {
    setSubmitting(true);
    try {
      await CompostAPI.changePhase(id, phase, notes || undefined);
      onDone();
    } catch {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="📊 Change Phase"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      <Field label="New Phase">
        <select
          value={phase}
          onChange={(e) =>
            setPhase((e.target as HTMLSelectElement).value as CompostPhase)}
          style={selectStyle}
        >
          {phases.map((p) => <option key={p} value={p}>{p}</option>)}
        </select>
      </Field>
      <Field label="Notes">
        <textarea
          value={notes}
          onInput={(e) => setNotes((e.target as HTMLTextAreaElement).value)}
          rows={2}
          placeholder="Reason for phase transition…"
          style={{ ...inputStyle, resize: "vertical" }}
        />
      </Field>
    </ModalShell>
  );
}

// KNF Inoculate Modal
function InoculateModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [inputType, setInputType] = useState<KnfInputType>("IMO3");
  const [desc, setDesc] = useState("");
  const [prepDate, setPrepDate] = useState(today());
  const [amount, setAmount] = useState("5");
  const [unit, setUnit] = useState("kg");
  const [submitting, setSubmitting] = useState(false);

  const KNF_DESCRIPTIONS: Record<KnfInputType, string> = {
    IMO1: "First-gen culture captured from forest/field soil",
    IMO2: "IMO1 propagated on cooked rice or similar substrate",
    IMO3: "IMO2 mixed with earth and organic matter, fermented",
    IMO4: "IMO3 combined with animal manure and carbon — ready to apply",
    LAB: "Lactic Acid Bacteria serum from rice wash / milk ferment",
    FPJ: "Fermented Plant Juice from young vigorous plant material",
    FAA: "Fish Amino Acid — enzymatic breakdown of fish in brown sugar",
    WSCA: "Water-Soluble Calcium — eggshell or oyster shell in vinegar",
    OHN: "Oriental Herbal Nutrient — garlic, ginger, licorice in alcohol",
  };

  const submit = async () => {
    setSubmitting(true);
    try {
      await CompostAPI.inoculate(id, {
        inputType,
        description: desc || KNF_DESCRIPTIONS[inputType],
        preparedDate: prepDate,
        amount: { value: parseFloat(amount) || 0, unit, displayUnit: unit },
      });
      onDone();
    } catch {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="🧪 Log KNF Inoculation"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      <Field label="Input Type">
        <select
          value={inputType}
          onChange={(e) =>
            setInputType((e.target as HTMLSelectElement).value as KnfInputType)}
          style={selectStyle}
        >
          {KNF_INPUT_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>
        <div
          style={{ fontSize: "0.75rem", color: "#8b5cf6", marginTop: "0.3rem" }}
        >
          {KNF_DESCRIPTIONS[inputType]}
        </div>
      </Field>
      <Field label="Description">
        <input
          value={desc}
          onInput={(e) => setDesc((e.target as HTMLInputElement).value)}
          placeholder={KNF_DESCRIPTIONS[inputType]}
          style={inputStyle}
        />
      </Field>
      <Field label="Prepared Date">
        <input
          type="date"
          value={prepDate}
          onInput={(e) => setPrepDate((e.target as HTMLInputElement).value)}
          style={inputStyle}
        />
      </Field>
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr 80px",
          gap: "0.75rem",
        }}
      >
        <Field label="Amount">
          <input
            type="number"
            value={amount}
            onInput={(e) => setAmount((e.target as HTMLInputElement).value)}
            style={inputStyle}
          />
        </Field>
        <Field label="Unit">
          <input
            value={unit}
            onInput={(e) => setUnit((e.target as HTMLInputElement).value)}
            style={inputStyle}
          />
        </Field>
      </div>
    </ModalShell>
  );
}

// pH Measurement Modal
function PHModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [date, setDate] = useState(today());
  const [ph, setPh] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const phNum = parseFloat(ph);
  const phStatus = !isNaN(phNum)
    ? (phNum >= 3.5 && phNum <= 4.5
      ? "✅ Bokashi optimal (3.5-4.5)"
      : phNum > 4.5
      ? "⬆ Too high for Bokashi — check bran coverage"
      : "⬇ Too low — monitor for over-acidification")
    : "";

  const submit = async () => {
    if (isNaN(phNum)) return;
    setSubmitting(true);
    try {
      await CompostAPI.measurePH(id, {
        date,
        pH: phNum,
        notes: notes || undefined,
      });
      onDone();
    } catch {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="📊 Log pH Measurement"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      <div
        style={{
          background: "#0f172a",
          borderRadius: "0.5rem",
          padding: "0.75rem",
          marginBottom: "1rem",
          fontSize: "0.8rem",
          color: "#94a3b8",
        }}
      >
        🥫 <strong style={{ color: "#f472b6" }}>Bokashi</strong>{" "}
        fermentation target: <strong>pH 3.5-4.5</strong>{" "}
        indicates healthy fermentation.<br />
        General compost: pH 6.5-8.0 is a healthy operating range.
      </div>
      <Field label="Date">
        <input
          type="date"
          value={date}
          onInput={(e) => setDate((e.target as HTMLInputElement).value)}
          style={inputStyle}
        />
      </Field>
      <Field label="pH Value">
        <input
          type="number"
          step="0.1"
          min="0"
          max="14"
          value={ph}
          onInput={(e) => setPh((e.target as HTMLInputElement).value)}
          placeholder="e.g. 3.8"
          style={inputStyle}
        />
        {phStatus && (
          <div
            style={{
              fontSize: "0.75rem",
              marginTop: "0.3rem",
              color: phNum >= 3.5 && phNum <= 4.5 ? "#22c55e" : "#f59e0b",
            }}
          >
            {phStatus}
          </div>
        )}
      </Field>
      <Field label="Notes">
        <textarea
          value={notes}
          onInput={(e) => setNotes((e.target as HTMLTextAreaElement).value)}
          rows={2}
          placeholder="Visual observations, smell, texture…"
          style={{ ...inputStyle, resize: "vertical" }}
        />
      </Field>
    </ModalShell>
  );
}

// Add Note Modal
function NoteModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [category, setCategory] = useState<NoteCategory>("Observation");
  const [body, setBody] = useState("");
  const [date, setDate] = useState(today());
  const [submitting, setSubmitting] = useState(false);

  const submit = async () => {
    if (!body.trim()) return;
    setSubmitting(true);
    try {
      await CompostAPI.addNote(id, { date, category, body: body.trim() });
      onDone();
    } catch {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="📝 Add Note"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      <Field label="Category">
        <select
          value={category}
          onChange={(e) =>
            setCategory((e.target as HTMLSelectElement).value as NoteCategory)}
          style={selectStyle}
        >
          {NOTE_CATEGORIES.map((c) => <option key={c} value={c}>{c}</option>)}
        </select>
      </Field>
      <Field label="Date">
        <input
          type="date"
          value={date}
          onInput={(e) => setDate((e.target as HTMLInputElement).value)}
          style={inputStyle}
        />
      </Field>
      <Field label="Note">
        <textarea
          value={body}
          onInput={(e) => setBody((e.target as HTMLTextAreaElement).value)}
          rows={4}
          placeholder="Your observation, amendment added, issue detected, or milestone reached…"
          style={{ ...inputStyle, resize: "vertical" }}
        />
      </Field>
    </ModalShell>
  );
}

// Complete Batch Modal
function CompleteModal(
  { id, onClose, onDone }: {
    id: string;
    onClose: () => void;
    onDone: () => void;
  },
) {
  const [yield_, setYield] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const submit = async () => {
    setSubmitting(true);
    try {
      await CompostAPI.complete(
        id,
        { value: parseFloat(yield_) || 0, unit: "cu yd", displayUnit: "cu yd" },
        notes || undefined,
      );
      onDone();
    } catch {
      setSubmitting(false);
    }
  };

  return (
    <ModalShell
      title="✅ Complete Batch"
      onClose={onClose}
      onSubmit={submit}
      submitting={submitting}
    >
      <div
        style={{
          background: "#16a34a22",
          border: "1px solid #16a34a55",
          borderRadius: "0.5rem",
          padding: "0.75rem",
          marginBottom: "1rem",
          fontSize: "0.8rem",
          color: "#86efac",
        }}
      >
        Marking this batch as complete. The final compost should be dark,
        crumbly, and smell earthy — like forest soil.
      </div>
      <Field label="Yield (cubic yards)">
        <input
          type="number"
          step="0.1"
          value={yield_}
          onInput={(e) => setYield((e.target as HTMLInputElement).value)}
          placeholder="e.g. 2.5"
          style={inputStyle}
        />
      </Field>
      <Field label="Final Notes">
        <textarea
          value={notes}
          onInput={(e) => setNotes((e.target as HTMLTextAreaElement).value)}
          rows={3}
          placeholder="Compost quality, colour, texture, planned use…"
          style={{ ...inputStyle, resize: "vertical" }}
        />
      </Field>
    </ModalShell>
  );
}
