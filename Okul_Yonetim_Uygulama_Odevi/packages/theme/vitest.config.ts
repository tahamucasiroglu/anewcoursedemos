import { defineConfig } from "vitest/config";

// Tema paketi testleri: düz Node ortamı, tek işçi, izleme modu yok (resource hygiene).
export default defineConfig({
  test: { globals: true, environment: "node", pool: "threads", maxWorkers: 1 },
});
