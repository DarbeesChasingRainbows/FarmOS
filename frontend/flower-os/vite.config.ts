import { defineConfig } from "vite";
import { fresh } from "@fresh/plugin-vite";
import tailwindcss from "@tailwindcss/vite";
import { resolve } from "node:path";

export default defineConfig({
  plugins: [fresh(), tailwindcss()],
  resolve: {
    // shared/farmos-client.ts imports @msgpack/msgpack but lives outside
    // this project's directory, so Vite can't find the package from shared/'s
    // location. Point the bare specifier to this project's installed copy.
    alias: {
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
});
