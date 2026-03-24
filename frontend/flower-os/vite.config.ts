import { defineConfig } from "vite";
import { fresh } from "@fresh/plugin-vite";
import tailwindcss from "@tailwindcss/vite";
import { resolve } from "node:path";

export default defineConfig({
  plugins: [fresh(), tailwindcss()],
  resolve: {
    // Ensure bare specifiers from shared/ resolve to this project's node_modules
    modules: [resolve(import.meta.dirname!, "node_modules"), "node_modules"],
  },
  server: {
    fs: {
      allow: [".."],
    },
  },
});
