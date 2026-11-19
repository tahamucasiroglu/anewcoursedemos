import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// Web paneli: geliştirme sunucusu 5173 portunda. Testler jsdom ile.
export default defineConfig({
  plugins: [react()],
  // Geliştirmede /api istekleri yerel API'ye iletilir: web, Pi'deki gibi sayfayla aynı adresi kullanır (giriş çerezi,
  // spec 0004). VITE_API_BASE_URL=/ ile çalışılır.
  server: { port: 5173, strictPort: true, proxy: { "/api": "http://localhost:5080" } },
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
