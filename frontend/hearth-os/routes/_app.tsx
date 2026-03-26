import { define } from "../utils.ts";

/**
 * App wrapper — outermost HTML document shell.
 * Page chrome (NavBar, ToastProvider, ConnectionBanner) lives in _layout.tsx.
 */
export default define.page(function App({ Component }) {
  return (
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>Hearth OS — FarmOS</title>
        <meta
          name="description"
          content="Farm processing, kitchen activities, indoor cultivation, and regulatory compliance management"
        />
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link
          rel="preconnect"
          href="https://fonts.gstatic.com"
          crossOrigin="anonymous"
        />
        <link
          href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap"
          rel="stylesheet"
        />
        <style>
          {`
          body { font-family: 'Inter', system-ui, sans-serif; margin: 0; }
          @keyframes slideIn {
            from { opacity: 0; transform: translateX(20px); }
            to { opacity: 1; transform: translateX(0); }
          }
          @keyframes scaleIn {
            from { opacity: 0; transform: scale(0.95); }
            to { opacity: 1; transform: scale(1); }
          }
          @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
          }
        `}
        </style>
        <script
          dangerouslySetInnerHTML={{
            __html: `
            window.__GATEWAY_URL__ = "${Deno.env.get("GATEWAY_URL") ?? "http://localhost:5050"}";
            window.addEventListener('error', e => console.log("GLOBAL ERROR:", e.message, e.filename, e.lineno));
            window.addEventListener('unhandledrejection', e => console.log("PROMISE REJECTION:", e.reason));
            console.log("App script running!");
          `,
          }}
        />
      </head>
      <body class="bg-stone-50 text-stone-900">
        <Component />
      </body>
    </html>
  );
});
