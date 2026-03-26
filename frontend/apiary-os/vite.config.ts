import { defineConfig } from "vite";
import { fresh } from "@fresh/plugin-vite";
import tailwindcss from "@tailwindcss/vite";
import { resolve } from "node:path";

export default defineConfig({
  plugins: [fresh({ islandsDir: "./islands" }), tailwindcss()],
  resolve: {
    alias: {
      "npm:@preact/signals@^2.2.1": "@preact/signals",
      // shared/farmos-client.ts imports @msgpack/msgpack but lives outside
      // this project, so Vite can't resolve it from shared/'s location.
      "@msgpack/msgpack": resolve(
        import.meta.dirname!,
        "node_modules/@msgpack/msgpack/dist.esm/index.mjs",
      ),
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
