import { useEffect, useRef } from "preact/hooks";
import { html } from "@arrow-js/core";

interface SectionCard {
  icon: string;
  title: string;
  description: string;
  href: string;
  bgClass: string;
  borderClass: string;
  iconBgClass: string;
}

const SECTION_CARDS: SectionCard[] = [
  {
    icon: "\u{1F517}",
    title: "Traceability & FSMA 204",
    description:
      "Track Key Data Elements across Critical Tracking Events for 24-Hour FDA compliance.",
    href: "/compliance/traceability",
    bgClass: "bg-emerald-50",
    borderClass: "border-emerald-200",
    iconBgClass: "bg-emerald-100",
  },
  {
    icon: "\u{1F4CB}",
    title: "HACCP Plan",
    description:
      "Hazard analysis, critical control points, monitoring procedures, and corrective actions.",
    href: "/compliance/haccp-plan",
    bgClass: "bg-sky-50",
    borderClass: "border-sky-200",
    iconBgClass: "bg-sky-100",
  },
  {
    icon: "\u{26A0}\u{FE0F}",
    title: "CAPA Tracker",
    description:
      "Corrective and preventive actions log with root cause analysis and resolution tracking.",
    href: "/compliance/capa",
    bgClass: "bg-amber-50",
    borderClass: "border-amber-200",
    iconBgClass: "bg-amber-100",
  },
];

function renderSectionCard(card: SectionCard) {
  return html`
    <a
      href="${card.href}"
      class="${`block rounded-xl border ${card.borderClass} ${card.bgClass} p-5 transition hover:shadow-lg`}"
    >
      <div
        class="${`inline-flex items-center justify-center w-10 h-10 rounded-lg ${card.iconBgClass} text-xl mb-3`}"
      >
        ${card.icon}
      </div>
      <h3 class="font-bold text-stone-800 mb-1">${card.title}</h3>
      <p class="text-sm text-stone-600">${card.description}</p>
    </a>
  `;
}

export default function ArrowComplianceHub() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const template = html`
      <div class="px-6 pt-6 pb-2 max-w-7xl mx-auto">
        <div class="flex items-center justify-between mb-6">
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Kitchen Compliance
          </h1>
          <button
            type="button"
            class="no-print bg-stone-800 hover:bg-stone-900 text-white text-sm font-bold py-2.5 px-5 rounded-lg shadow-md transition flex items-center gap-2"
            @click="${() => globalThis.print()}"
          >
            \u{1F5A8}\u{FE0F} Print for Inspector
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          ${SECTION_CARDS.map(renderSectionCard)}
        </div>
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef} />;
}
