import { define } from "../utils.ts";
import ArrowNavBar from "../islands/ArrowNavBar.tsx";
import ArrowToastProvider from "../islands/ArrowToastProvider.tsx";
import ConnectionBanner from "../islands/ConnectionBanner.tsx";

/**
 * Root layout — wraps all routes with shared page chrome.
 * Fresh 2.x layout inheritance: _app.tsx (HTML shell) → _layout.tsx (page chrome) → route
 */
export default define.page(function Layout({ Component, url }) {
  return (
    <div class="flex min-h-screen bg-stone-50 text-stone-900 font-sans selection:bg-orange-100 selection:text-orange-900">
      <ArrowNavBar currentPath={url.pathname} />
      <div class="flex-1 flex flex-col overflow-y-auto">
        <ConnectionBanner />
        <main class="flex-1">
          <Component />
        </main>
      </div>
      <ArrowToastProvider />
    </div>
  );
});
