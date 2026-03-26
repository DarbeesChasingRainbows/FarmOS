import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowSettingsPanel from "../../islands/ArrowSettingsPanel.tsx";

export default define.page(function SettingsPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head><title>Settings — Hearth OS</title></Head>
      <header class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Settings</h1>
        <p class="text-stone-500 mt-1">Manage farm configuration, dropdown menus, notifications, and integrations.</p>
      </header>

      <ArrowSettingsPanel />
    </div>
  );
});
