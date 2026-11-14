import { defineConfig } from 'vitest/config';

// Tarayıcı JS testleri: jsdom, tek işçi (iz bırakmaz, docs/testing.md Resource hygiene), izleme modu yok.
export default defineConfig({
  test: {
    environment: 'jsdom',
    include: ['wwwroot/js/__tests__/**/*.test.js'],
    pool: 'threads',
    maxWorkers: 1,
    watch: false,
  },
});
