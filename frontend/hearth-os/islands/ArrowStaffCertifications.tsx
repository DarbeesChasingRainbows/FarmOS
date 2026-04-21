import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";

interface Cert {
  id: string;
  staffName: string;
  certType: string;
  issuedDate: string;
  expiryDate: string;
  issuer: string;
  notes?: string;
}

const daysUntilExpiry = (d: string) => Math.floor((new Date(d).getTime() - Date.now()) / 86_400_000);
const certStatus = (c: Cert) => {
  const d = daysUntilExpiry(c.expiryDate);
  if (d < 0) return "attention";
  if (d <= 30) return "idle";
  return "active";
};
const certLabel = (c: Cert) => {
  const d = daysUntilExpiry(c.expiryDate);
  if (d < 0) return "Expired";
  if (d === 0) return "Expires Today";
  if (d <= 30) return `${d}d left`;
  return "Valid";
};
const statusCls = (c: Cert) => {
  const s = certStatus(c);
  return s === "attention" ? "bg-red-50 text-red-700" : s === "idle" ? "bg-amber-50 text-amber-700" : "bg-emerald-50 text-emerald-700";
};

const MOCK_CERTS: Cert[] = [
  { id: "cert-1", staffName: "Maria Santos", certType: "Food Manager Certification", issuedDate: "2022-03-15", expiryDate: "2027-03-15", issuer: "ServSafe" },
  { id: "cert-2", staffName: "James Liu", certType: "Food Handler Card", issuedDate: "2025-02-01", expiryDate: "2026-03-28", issuer: "County Health Dept", notes: "Renewal reminder sent" },
  { id: "cert-3", staffName: "Sam Rivera", certType: "Food Handler Card", issuedDate: "2024-06-10", expiryDate: "2025-03-20", issuer: "County Health Dept" },
  { id: "cert-4", staffName: "Ana Kowalski", certType: "HACCP Certification", issuedDate: "2024-01-20", expiryDate: "2026-01-20", issuer: "NSF International" },
];

export default function ArrowStaffCertifications() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      certs: [...MOCK_CERTS],
      showForm: false,
      selectedId: null as string | null,
      sidebarOpen: false,
      fStaffName: "",
      fCertType: "Food Handler Card",
      fIssuedDate: "",
      fExpiryDate: "",
      fIssuer: "",
      fNotes: "",
      errors: {} as Record<string, string>,
      submitting: false,
    });

    const selected = () => state.certs.find(c => c.id === state.selectedId);
    const openSidebar = (id: string) => { state.selectedId = id; state.sidebarOpen = true; };
    const closeSidebar = () => { state.sidebarOpen = false; setTimeout(() => { state.selectedId = null; }, 300); };
    const closeForm = () => { state.showForm = false; clearErrors(state.errors); };

    const handleAdd = async (e: Event) => {
      e.preventDefault();
      const errs: Record<string, string> = {};
      if (!state.fStaffName) errs.staffName = "Required";
      if (!state.fIssuedDate) errs.issuedDate = "Required";
      if (!state.fExpiryDate) errs.expiryDate = "Required";
      if (!state.fIssuer) errs.issuer = "Required";
      if (Object.keys(errs).length) { setErrors(state.errors, errs); return; }
      state.submitting = true;
      try {
        const { KitchenAPI } = await import("../utils/farmos-client.ts");
        await KitchenAPI.addCert({
          staffName: state.fStaffName,
          certType: state.fCertType,
          issuedDate: state.fIssuedDate,
          expiryDate: state.fExpiryDate,
          issuer: state.fIssuer,
          notes: state.fNotes || undefined,
        });
        showToast("success", "Certification added!", `${state.fCertType} for ${state.fStaffName} saved.`);
        closeForm();
      } catch (err: unknown) {
        showToast("error", "Failed to save", err instanceof Error ? err.message : "Unknown error");
      } finally {
        state.submitting = false;
      }
    };

    html`
      <div class="relative flex min-h-[400px]">
        <div class="${() => `flex-1 transition-all duration-300 ${state.sidebarOpen ? 'mr-[400px]' : ''}`}">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
            ${() => state.certs.map(cert => {
              const days = daysUntilExpiry(cert.expiryDate);
              return html`
                <button type="button" @click="${() => openSidebar(cert.id)}"
                  class="${`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left cursor-pointer w-full ${
                    state.selectedId === cert.id ? 'border-amber-400 ring-2 ring-amber-200'
                    : certStatus(cert) === 'attention' ? 'border-red-200 bg-red-50/20'
                    : certStatus(cert) === 'idle' ? 'border-amber-200 bg-amber-50/20'
                    : 'border-stone-200 hover:border-amber-200'
                  }`}">
                  <div class="flex items-start justify-between mb-3">
                    <div>
                      <p class="text-base font-bold text-stone-800">${cert.staffName}</p>
                      <p class="text-xs text-stone-500 mt-0.5">${cert.certType}</p>
                    </div>
                    <span class="${`px-2 py-0.5 rounded-full text-xs font-medium ${statusCls(cert)}`}">${certLabel(cert)}</span>
                  </div>
                  <div class="flex items-center justify-between text-xs text-stone-400 pt-3 border-t border-stone-100">
                    <span>Issuer: ${cert.issuer}</span>
                    <span>Expires: ${new Date(cert.expiryDate).toLocaleDateString()}</span>
                  </div>
                  ${days >= 0 && days <= 30 ? html`<p class="text-xs text-amber-700 mt-1 font-semibold">⚠️ Renewal needed within ${days} day${days !== 1 ? "s" : ""}</p>` : html`<span></span>`}
                  ${days < 0 ? html`<p class="text-xs text-red-600 mt-1 font-semibold">🚨 Expired — immediate renewal required</p>` : html`<span></span>`}
                </button>
              `.key(cert.id);
            })}
          </div>

          <button @click="${() => state.showForm = true}" class="bg-blue-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-blue-700 transition shadow-sm flex items-center gap-2">
            <span>+</span> Add Certification
          </button>

          ${() => state.showForm ? html`
            <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50" @click="${(e: Event) => { if (e.target === e.currentTarget) closeForm(); }}">
              <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden">
                <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
                  <h3 class="text-lg font-bold text-stone-800">Add Certification</h3>
                  <button @click="${closeForm}" class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition">✕</button>
                </div>
                <form @submit="${handleAdd}" class="p-6 flex flex-col gap-4">
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Staff Name</label>
                    <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Full name"
                      @input="${(e: Event) => state.fStaffName = (e?.target as HTMLInputElement)?.value ?? ''}">
                    ${() => state.errors.staffName ? html`<p class="text-xs text-red-500 mt-1">${state.errors.staffName}</p>` : html`<span></span>`}
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Certification Type</label>
                    <select class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm"
                      @change="${(e: Event) => state.fCertType = (e?.target as HTMLSelectElement)?.value ?? ''}">
                      <option>Food Handler Card</option>
                      <option>Food Manager Certification</option>
                      <option>HACCP Certification</option>
                      <option>Allergen Awareness</option>
                      <option>Other</option>
                    </select>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">Issued Date</label>
                      <input type="date" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm"
                        @input="${(e: Event) => state.fIssuedDate = (e?.target as HTMLInputElement)?.value ?? ''}">
                      ${() => state.errors.issuedDate ? html`<p class="text-xs text-red-500 mt-1">${state.errors.issuedDate}</p>` : html`<span></span>`}
                    </div>
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">Expiry Date</label>
                      <input type="date" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm"
                        @input="${(e: Event) => state.fExpiryDate = (e?.target as HTMLInputElement)?.value ?? ''}">
                      ${() => state.errors.expiryDate ? html`<p class="text-xs text-red-500 mt-1">${state.errors.expiryDate}</p>` : html`<span></span>`}
                    </div>
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Issuing Authority</label>
                    <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="e.g. ServSafe, County Health"
                      @input="${(e: Event) => state.fIssuer = (e?.target as HTMLInputElement)?.value ?? ''}">
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Notes (optional)</label>
                    <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm"
                      @input="${(e: Event) => state.fNotes = (e?.target as HTMLInputElement)?.value ?? ''}">
                  </div>
                  <div class="flex justify-end gap-3">
                    <button type="button" @click="${closeForm}" class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition">Cancel</button>
                    <button type="submit" class="bg-blue-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-blue-700 transition">${() => state.submitting ? "Saving..." : "Save Certification"}</button>
                  </div>
                </form>
              </div>
            </div>
          ` : html`<span></span>`}
        </div>

        ${() => state.sidebarOpen ? html`<div class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30" @click="${closeSidebar}"></div>` : html`<span></span>`}

        <aside class="${() => `fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${state.sidebarOpen ? 'translate-x-0' : 'translate-x-full'}`}">
          ${() => {
            const cert = selected();
            if (!cert) return html`<span></span>`;
            return html`
              <div class="p-6">
                <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
                  <div>
                    <h3 class="text-xl font-bold text-stone-800">${cert.staffName}</h3>
                    <p class="text-sm text-stone-500 mt-1">${cert.certType}</p>
                    <div class="mt-2"><span class="${`px-2 py-0.5 rounded-full text-xs font-medium ${statusCls(cert)}`}">${certLabel(cert)}</span></div>
                  </div>
                  <button @click="${closeSidebar}" class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition">✕</button>
                </div>
                <div class="flex flex-col gap-4">
                  <div class="grid grid-cols-2 gap-3">
                    <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                      <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">Issued</p>
                      <p class="text-base font-semibold text-stone-800 mt-1">${new Date(cert.issuedDate).toLocaleDateString()}</p>
                    </div>
                    <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                      <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">Expires</p>
                      <p class="${`text-base font-semibold mt-1 ${certStatus(cert) === 'attention' ? 'text-red-600' : certStatus(cert) === 'idle' ? 'text-amber-700' : 'text-stone-800'}`}">${new Date(cert.expiryDate).toLocaleDateString()}</p>
                    </div>
                  </div>
                  <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                    <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">Issuing Authority</p>
                    <p class="text-base font-semibold text-stone-800 mt-1">${cert.issuer}</p>
                  </div>
                  ${cert.notes ? html`
                    <div class="bg-amber-50 rounded-xl p-4 border border-amber-100">
                      <p class="text-xs text-amber-700 font-medium">📝 ${cert.notes}</p>
                    </div>
                  ` : html`<span></span>`}
                </div>
              </div>
            `;
          }}
        </aside>
      </div>
    `(containerRef.current);
  }, []);

  return <div ref={containerRef} />;
}
