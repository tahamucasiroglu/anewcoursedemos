import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// Web paneli: geliştirme sunucusu 5173 portunda (API'nin CORS izinli adresi). Testler jsdom ile.
export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    // CSS işlenir: tema testleri CSS içeriğini okur (spec 0002, AC-1).
    css: true,
    pool: "threads",
    maxWorkers: 1,
  },
});
