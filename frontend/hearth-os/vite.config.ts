import { defineConfig } from "vite";
import { fresh } from "@fresh/plugin-vite";
import tailwindcss from "@tailwindcss/vite";
import { resolve } from "node:path";

export default defineConfig({
  plugins: [fresh({ islandsDir: "./islands" }), tailwindcss()],
  resolve: {
    alias: {
      "npm:@preact/signals@^2.2.1": "@preact/signals",
    },
    // Ensure bare specifiers from shared/ resolve to this project's node_modules
    modules: [resolve(import.meta.dirname!, "node_modules"), "node_modules"],
  },
  optimizeDeps: {
    include: [
      "preact",
      "preact/jsx-runtime",
      "@preact/signals",
      "preact/hooks",
    ],
  },
  server: {
    fs: {
      allow: [".."],
    },
  },
  build: {
    minify: false,
    rollupOptions: {
      treeshake: false,
    },
  },
});
