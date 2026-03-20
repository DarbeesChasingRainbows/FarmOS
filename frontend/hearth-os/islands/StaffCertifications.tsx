import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import { showToast } from "../utils/toastState.ts";
import {
  CertSchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";

interface Cert {
  id: string;
  staffName: string;
  certType: string;
  issuedDate: string;
  expiryDate: string;
  issuer: string;
  notes?: string;
}

function daysUntilExpiry(expiryDate: string): number {
  return Math.floor((new Date(expiryDate).getTime() - Date.now()) / 86_400_000);
}

function certStatus(cert: Cert): "active" | "attention" | "idle" {
  const days = daysUntilExpiry(cert.expiryDate);
  if (days < 0) return "attention";
  if (days <= 30) return "idle";
  return "active";
}

function certStatusLabel(cert: Cert): string {
  const days = daysUntilExpiry(cert.expiryDate);
  if (days < 0) return "Expired";
  if (days === 0) return "Expires Today";
  if (days <= 30) return `${days}d left`;
  return "Valid";
}

export default function StaffCertifications() {
  const certs = useSignal<Cert[]>([
    {
      id: "cert-1",
      staffName: "Maria Santos",
      certType: "Food Manager Certification",
      issuedDate: "2022-03-15",
      expiryDate: "2027-03-15",
      issuer: "ServSafe",
    },
    {
      id: "cert-2",
      staffName: "James Liu",
      certType: "Food Handler Card",
      issuedDate: "2025-02-01",
      expiryDate: "2026-03-28",
      issuer: "County Health Dept",
      notes: "Renewal reminder sent",
    },
    {
      id: "cert-3",
      staffName: "Sam Rivera",
      certType: "Food Handler Card",
      issuedDate: "2024-06-10",
      expiryDate: "2025-03-20",
      issuer: "County Health Dept",
    },
    {
      id: "cert-4",
      staffName: "Ana Kowalski",
      certType: "HACCP Certification",
      issuedDate: "2024-01-20",
      expiryDate: "2026-01-20",
      issuer: "NSF International",
    },
  ]);

  const showAddForm = useSignal(false);
  const selectedId = useSignal<string | null>(null);
  const isSidebarOpen = useSignal(false);

  // Form fields
  const fStaffName = useSignal("");
  const fCertType = useSignal("Food Handler Card");
  const fIssuedDate = useSignal("");
  const fExpiryDate = useSignal("");
  const fIssuer = useSignal("");
  const fNotes = useSignal("");
  const formErrors = useSignal<FieldErrors>({});
  const isSubmitting = useSignal(false);

  const selected = certs.value.find((c) => c.id === selectedId.value);

  const openSidebar = (id: string) => {
    selectedId.value = id;
    isSidebarOpen.value = true;
  };
  const closeSidebar = () => {
    isSidebarOpen.value = false;
    setTimeout(() => {
      selectedId.value = null;
    }, 300);
  };
  const closeForm = () => {
    showAddForm.value = false;
    formErrors.value = {};
  };

  const handleAdd = async (e: Event) => {
    e.preventDefault();
    const result = CertSchema.safeParse({
      staffName: fStaffName.value,
      certType: fCertType.value,
      issuedDate: fIssuedDate.value,
      expiryDate: fExpiryDate.value,
      issuer: fIssuer.value,
      notes: fNotes.value || undefined,
    });
    if (!result.success) {
      formErrors.value = extractErrors(result);
      return;
    }
    isSubmitting.value = true;
    try {
      const { KitchenAPI } = await import("../utils/farmos-client.ts");
      await KitchenAPI.addCert(result.data);
      showToast(
        "success",
        "Certification added!",
        `${fCertType.value} for ${fStaffName.value} saved.`,
      );
      closeForm();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to save",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 transition ${
      formErrors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  return (
    <div class="relative flex min-h-[400px]">
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[400px]" : ""
        }`}
      >
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
          {certs.value.map((cert) => {
            const status = certStatus(cert);
            const isSelected = selectedId.value === cert.id;
            const days = daysUntilExpiry(cert.expiryDate);
            return (
              <button
                type="button"
                onClick={() => openSidebar(cert.id)}
                class={`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left cursor-pointer w-full ${
                  isSelected
                    ? "border-amber-400 ring-2 ring-amber-200"
                    : status === "attention"
                    ? "border-red-200 bg-red-50/20"
                    : status === "idle"
                    ? "border-amber-200 bg-amber-50/20"
                    : "border-stone-200 hover:border-amber-200"
                }`}
              >
                <div class="flex items-start justify-between mb-3">
                  <div>
                    <p class="text-base font-bold text-stone-800">
                      {cert.staffName}
                    </p>
                    <p class="text-xs text-stone-500 mt-0.5">{cert.certType}</p>
                  </div>
                  <StatusBadge variant={status} label={certStatusLabel(cert)} />
                </div>
                <div class="flex items-center justify-between text-xs text-stone-400 pt-3 border-t border-stone-100">
                  <span>Issuer: {cert.issuer}</span>
                  <span>
                    Expires: {new Date(cert.expiryDate).toLocaleDateString()}
                  </span>
                </div>
                {days >= 0 && days <= 30 && (
                  <p class="text-xs text-amber-700 mt-1 font-semibold">
                    ⚠️ Renewal needed within {days} day{days !== 1 ? "s" : ""}
                  </p>
                )}
                {days < 0 && (
                  <p class="text-xs text-red-600 mt-1 font-semibold">
                    🚨 Expired — immediate renewal required
                  </p>
                )}
              </button>
            );
          })}
        </div>

        {/* Add Cert Button */}
        <button
          onClick={() => showAddForm.value = true}
          class="bg-blue-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-blue-700 transition shadow-sm flex items-center gap-2"
        >
          <span>+</span> Add Certification
        </button>

        {/* Add Cert Modal */}
        {showAddForm.value && (
          <div
            class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
            onClick={(e) => {
              if (e.target === e.currentTarget) closeForm();
            }}
          >
            <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
              <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
                <h3 class="text-lg font-bold text-stone-800">
                  Add Certification
                </h3>
                <button
                  onClick={closeForm}
                  class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
                >
                  ✕
                </button>
              </div>
              <form onSubmit={handleAdd} class="p-6 flex flex-col gap-4">
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Staff Name
                  </label>
                  <input
                    type="text"
                    class={`mt-1 ${inputClass("staffName")}`}
                    placeholder="Full name"
                    value={fStaffName.value}
                    onInput={(e) =>
                      fStaffName.value = (e.target as HTMLInputElement).value}
                  />
                  {formErrors.value.staffName && (
                    <p class="text-xs text-red-500 mt-1">
                      {formErrors.value.staffName}
                    </p>
                  )}
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Certification Type
                  </label>
                  <select
                    class={`mt-1 ${inputClass("certType")}`}
                    value={fCertType.value}
                    onChange={(e) =>
                      fCertType.value = (e.target as HTMLSelectElement).value}
                  >
                    <option>Food Handler Card</option>
                    <option>Food Manager Certification</option>
                    <option>HACCP Certification</option>
                    <option>Allergen Awareness</option>
                    <option>Other</option>
                  </select>
                </div>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">
                      Issued Date
                    </label>
                    <input
                      type="date"
                      class={`mt-1 ${inputClass("issuedDate")}`}
                      value={fIssuedDate.value}
                      onInput={(e) =>
                        fIssuedDate.value =
                          (e.target as HTMLInputElement).value}
                    />
                    {formErrors.value.issuedDate && (
                      <p class="text-xs text-red-500 mt-1">
                        {formErrors.value.issuedDate}
                      </p>
                    )}
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">
                      Expiry Date
                    </label>
                    <input
                      type="date"
                      class={`mt-1 ${inputClass("expiryDate")}`}
                      value={fExpiryDate.value}
                      onInput={(e) =>
                        fExpiryDate.value =
                          (e.target as HTMLInputElement).value}
                    />
                    {formErrors.value.expiryDate && (
                      <p class="text-xs text-red-500 mt-1">
                        {formErrors.value.expiryDate}
                      </p>
                    )}
                  </div>
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Issuing Authority
                  </label>
                  <input
                    type="text"
                    class={`mt-1 ${inputClass("issuer")}`}
                    placeholder="e.g. ServSafe, County Health"
                    value={fIssuer.value}
                    onInput={(e) =>
                      fIssuer.value = (e.target as HTMLInputElement).value}
                  />
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Notes (optional)
                  </label>
                  <input
                    type="text"
                    class={`mt-1 ${inputClass("notes")}`}
                    value={fNotes.value}
                    onInput={(e) =>
                      fNotes.value = (e.target as HTMLInputElement).value}
                  />
                </div>
                <div class="flex justify-end gap-3">
                  <button
                    type="button"
                    onClick={closeForm}
                    class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting.value}
                    class="bg-blue-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition"
                  >
                    {isSubmitting.value ? "Saving..." : "Save Certification"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>

      {isSidebarOpen.value && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30"
          onClick={closeSidebar}
        />
      )}

      <aside
        class={`fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen.value ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selected && (
          <div class="p-6">
            <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
              <div>
                <h3 class="text-xl font-bold text-stone-800">
                  {selected.staffName}
                </h3>
                <p class="text-sm text-stone-500 mt-1">{selected.certType}</p>
                <div class="mt-2">
                  <StatusBadge
                    variant={certStatus(selected)}
                    label={certStatusLabel(selected)}
                  />
                </div>
              </div>
              <button
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>
            <div class="flex flex-col gap-4">
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                  <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                    Issued
                  </p>
                  <p class="text-base font-semibold text-stone-800 mt-1">
                    {new Date(selected.issuedDate).toLocaleDateString()}
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                  <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                    Expires
                  </p>
                  <p
                    class={`text-base font-semibold mt-1 ${
                      certStatus(selected) === "attention"
                        ? "text-red-600"
                        : certStatus(selected) === "idle"
                        ? "text-amber-700"
                        : "text-stone-800"
                    }`}
                  >
                    {new Date(selected.expiryDate).toLocaleDateString()}
                  </p>
                </div>
              </div>
              <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                  Issuing Authority
                </p>
                <p class="text-base font-semibold text-stone-800 mt-1">
                  {selected.issuer}
                </p>
              </div>
              {selected.notes && (
                <div class="bg-amber-50 rounded-xl p-4 border border-amber-100">
                  <p class="text-xs text-amber-700 font-medium">
                    📝 {selected.notes}
                  </p>
                </div>
              )}
            </div>
          </div>
        )}
      </aside>
    </div>
  );
}
