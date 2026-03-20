import { define } from "../utils.ts";
import NavBar from "../components/NavBar.tsx";
import ToastProvider from "../islands/ToastProvider.tsx";
import ConnectionBanner from "../islands/ConnectionBanner.tsx";

/**
 * Root layout — wraps all routes with shared page chrome.
 * Fresh 2.x layout inheritance: _app.tsx (HTML shell) → _layout.tsx (page chrome) → route
 */
export default define.page(function Layout({ Component, url }) {
  return (
    <div class="flex min-h-screen">
      <NavBar currentPath={url.pathname} />
      <div class="flex-1 flex flex-col overflow-y-auto">
        <ConnectionBanner />
        <main class="flex-1">
          <Component />
        </main>
      </div>
      <ToastProvider />
    </div>
  );
});
