import { useState } from "preact/hooks";
import {
  extractErrors,
  type FieldErrors,
  RegisterEquipmentSchema,
} from "../utils/schemas.ts";

export default function RegisterEquipmentForm() {
  const [isOpen, setIsOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});

  const closeModal = () => {
    setIsOpen(false);
    setErrors({});
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();
    setErrors({});
    setIsSubmitting(true);

    const formData = new FormData(e.target as HTMLFormElement);
    const data = Object.fromEntries(formData.entries());

    const result = RegisterEquipmentSchema.safeParse(data);
    if (!result.success) {
      setErrors(extractErrors(result));
      setIsSubmitting(false);
      return;
    }

    try {
      const { EquipmentAPI } = await import("../utils/assets-client.ts");
      await EquipmentAPI.register(result.data);
      // Let the parent island handle refresh, or do a hard reload for simplicity
      globalThis.location?.reload();
      closeModal();
    } catch (err: unknown) {
      console.error(err);
      setErrors({ form: "Failed to register equipment. Please try again." });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <button
        onClick={() => setIsOpen(true)}
        class="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg transition-colors font-medium shadow-sm hover:shadow-md cursor-pointer"
      >
        <span class="text-xl leading-none">+</span>
        Register Equipment
      </button>

      {isOpen && (
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-stone-900/40 backdrop-blur-xs p-4 overflow-y-auto">
          <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg border border-stone-200 overflow-hidden my-8">
            <div class="px-6 py-4 border-b border-stone-100 flex justify-between items-center bg-stone-50">
              <h2 class="text-xl font-bold text-stone-800 flex items-center gap-2">
                <span>🚜</span> Register Equipment
              </h2>
              <button
                onClick={closeModal}
                class="text-stone-400 hover:text-stone-700 bg-stone-100 hover:bg-stone-200 rounded-full w-8 h-8 flex items-center justify-center transition-colors cursor-pointer"
              >
                ✕
              </button>
            </div>

            <div class="max-h-[70vh] overflow-y-auto">
              <form onSubmit={onSubmit} class="p-6 space-y-4">
                {errors.form && (
                  <div class="p-3 bg-red-50 text-red-700 rounded-lg text-sm border border-red-100">
                    {errors.form}
                  </div>
                )}

                <div>
                  <label class="block text-sm font-medium text-stone-700 mb-1">
                    Name
                  </label>
                  <input
                    name="name"
                    type="text"
                    placeholder="e.g. Primary Tractor"
                    class="w-full px-3 py-2 border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                  />
                  {errors.name && (
                    <p class="text-red-500 text-xs mt-1">{errors.name}</p>
                  )}
                </div>
                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-stone-700 mb-1">
                      Make
                    </label>
                    <input
                      name="make"
                      type="text"
                      placeholder="e.g. John Deere"
                      class="w-full px-3 py-2 border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                    />
                    {errors.make && (
                      <p class="text-red-500 text-xs mt-1">{errors.make}</p>
                    )}
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-stone-700 mb-1">
                      Model
                    </label>
                    <input
                      name="model"
                      type="text"
                      placeholder="e.g. 5075E"
                      class="w-full px-3 py-2 border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                    />
                    {errors.model && (
                      <p class="text-red-500 text-xs mt-1">{errors.model}</p>
                    )}
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-stone-700 mb-1">
                      Year
                    </label>
                    <input
                      name="year"
                      type="number"
                      placeholder="2019"
                      class="w-full px-3 py-2 border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                    />
                    {errors.year && (
                      <p class="text-red-500 text-xs mt-1">{errors.year}</p>
                    )}
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-stone-700 mb-1">
                      GPS Latitude
                    </label>
                    <input
                      name="lat"
                      type="number"
                      step="any"
                      placeholder="43.1"
                      class="w-full px-3 py-2 border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                    />
                    {errors.lat && (
                      <p class="text-red-500 text-xs mt-1">{errors.lat}</p>
                    )}
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-stone-700 mb-1">
                      GPS Longitude
                    </label>
                    <input
                      name="lng"
                      type="number"
                      step="any"
                      placeholder="-89.3"
                      class="w-full px-3 py-2 border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                    />
                    {errors.lng && (
                      <p class="text-red-500 text-xs mt-1">{errors.lng}</p>
                    )}
                  </div>
                </div>

                <div class="pt-4 flex justify-end gap-3 border-t border-stone-100">
                  <button
                    type="button"
                    onClick={closeModal}
                    class="px-4 py-2 text-stone-600 hover:text-stone-900 hover:bg-stone-50 rounded-lg transition-colors font-medium"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting}
                    class="px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg transition-colors font-medium shadow-sm disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isSubmitting ? "Saving..." : "Register"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
