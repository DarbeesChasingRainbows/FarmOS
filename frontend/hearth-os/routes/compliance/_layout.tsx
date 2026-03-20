import { define } from "../../utils.ts";

/**
 * Compliance layout — print-optimized, strips sidebar nav.
 * Uses skipInheritedLayouts to bypass the root _layout.tsx
 * so compliance pages render clean for inspector print output.
 */

export const config = {
  skipInheritedLayouts: true,
};

export default define.page(function ComplianceLayout({ Component }) {
  return (
    <div class="compliance-layout">
      {/* Minimal chrome for compliance routes */}
      <header class="px-6 py-4 border-b border-stone-200 bg-white flex items-center justify-between print:hidden">
        <div class="flex items-center gap-3">
          <a
            href="/"
            class="text-stone-400 hover:text-stone-600 transition text-sm font-medium min-h-[48px] min-w-[48px] flex items-center"
          >
            ← Back to Dashboard
          </a>
          <span class="text-stone-300">|</span>
          <h1 class="text-base font-bold text-stone-800">
            📋 Compliance & HACCP
          </h1>
        </div>
        <button
          type="button"
          onClick={() => globalThis.window?.print()}
          class="px-4 py-2.5 bg-stone-800 text-white rounded-lg text-sm font-semibold hover:bg-stone-700 transition min-h-[48px]"
        >
          🖨️ Print for Inspector
        </button>
      </header>

      <main class="p-6 max-w-5xl mx-auto">
        <Component />
      </main>

      {/* Print-specific styles */}
      <style>
        {`
          @media print {
            .compliance-layout header { display: none !important; }
            .compliance-layout main {
              padding: 0 !important;
              max-width: none !important;
              margin: 0 !important;
            }
            body {
              font-family: 'Times New Roman', serif !important;
              font-size: 11pt !important;
              color: #000 !important;
              background: #fff !important;
            }
            @page {
              size: letter;
              margin: 0.75in;
            }
          }
        `}
      </style>
    </div>
  );
});
