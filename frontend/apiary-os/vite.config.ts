import { defineConfig } from "vite";
import { fresh } from "@fresh/plugin-vite";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [fresh({ islandsDir: "./islands" }), tailwindcss()],
  resolve: {
    alias: {
      "npm:@preact/signals@^2.2.1": "@preact/signals",
    },
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
