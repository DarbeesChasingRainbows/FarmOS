import { define } from "../utils.ts";

export default define.page(function App({ Component }) {
  return (
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no" />
        <title>IoT Sensor Grid — FarmOS</title>
        <meta name="description" content="Rugged IoT sensor monitoring for farm cold chain and climate compliance" />
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700;800;900&display=swap" rel="stylesheet" />
        <style>{`
          body { font-family: 'Inter', system-ui, sans-serif; margin: 0; -webkit-tap-highlight-color: transparent; }
          * { touch-action: manipulation; }
          @keyframes pulse-red { 0%, 100% { opacity: 1; } 50% { opacity: 0.6; } }
          @keyframes slideUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
        `}</style>
      </head>
      <body class="bg-stone-900 text-amber-50 min-h-screen">
        <Component />
      </body>
    </html>
  );
});
