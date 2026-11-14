import { beforeEach, describe, expect, it } from 'vitest';
import '../theme.js';

function memoryStorage(initial = {}) {
  const data = { ...initial };
  return {
    getItem: (k) => (k in data ? data[k] : null),
    setItem: (k, v) => { data[k] = String(v); },
    data,
  };
}

describe('tema (AC-2)', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('data-theme');
  });

  it('hiç seçim yoksa açık mod gelir', () => {
    window.KonseptTheme.init(document, memoryStorage());
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('düğme açık↔koyu geçiş yapar ve seçimi hatırlar', () => {
    const storage = memoryStorage();
    window.KonseptTheme.init(document, storage);

    expect(window.KonseptTheme.toggle(document, storage)).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(storage.data[window.KonseptTheme.KEY]).toBe('dark');

    expect(window.KonseptTheme.toggle(document, storage)).toBe('light');
    expect(storage.data[window.KonseptTheme.KEY]).toBe('light');
  });

  it('sayfa yenilenince son seçim korunur', () => {
    const storage = memoryStorage({ 'anew-theme': 'dark' });
    window.KonseptTheme.init(document, storage);
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('depolama kullanılamasa da tema değişir', () => {
    const broken = { getItem: () => { throw new Error('yok'); }, setItem: () => { throw new Error('yok'); } };
    window.KonseptTheme.init(document, broken);
    expect(window.KonseptTheme.toggle(document, broken)).toBe('dark');
  });

  it('tema düğmesine tıklamak temayı değiştirir', () => {
    document.body.innerHTML = '<button id="theme-toggle" type="button"></button>';
    const storage = memoryStorage();
    window.KonseptTheme.init(document, storage);
    window.KonseptTheme.bind(document, storage);

    document.getElementById('theme-toggle').click();

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
