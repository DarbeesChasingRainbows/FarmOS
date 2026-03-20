import { define } from "../utils.ts";
import NavBar from "../components/NavBar.tsx";

export default define.page(function Layout({ Component, url }) {
  return (
    <div class="flex min-h-screen">
      <NavBar currentPath={url.pathname} />
      <div class="flex-1 flex flex-col overflow-y-auto">
        <main class="flex-1">
          <Component />
        </main>
      </div>
    </div>
  );
});
