import { define } from "../utils.ts";
import NavBar from "../components/NavBar.tsx";

export default define.page(function App({ Component, url }) {
  return (
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>Asset OS — Farm Asset Registry</title>
        <link
          href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap"
          rel="stylesheet"
        />
      </head>
      <body>
        <div class="flex min-h-screen">
          <NavBar currentPath={url.pathname} />
          <main class="flex-1 overflow-auto">
            <Component />
          </main>
        </div>
      </body>
    </html>
  );
});
