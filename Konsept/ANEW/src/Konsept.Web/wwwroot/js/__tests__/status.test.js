import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { POLL_INTERVAL_MS, refresh, render, startPolling } from '../status.js';
import { ajaxRejecting, ajaxResolving, installJquery, layoutHtml } from './fixture.js';

const $ = installJquery();
const text = (id) => document.getElementById(id).textContent;
const state = (id) => document.getElementById(id).dataset.state;

const healthy = {
  apiAvailable: true,
  version: '0.1.0',
  databaseAvailable: true,
  agent: { isConnected: true, machineName: 'TAHA-PC' },
  lastEvent: { type: 'agent.connected', level: 0, message: 'Ajan bağlandı: TAHA-PC (v0.1.0)', occurredAt: '2026-11-14T09:30:00Z' },
};

describe('durum alanı ve Günlük şeridi (AC-4, AC-6, AC-8)', () => {
  beforeEach(() => {
    document.body.innerHTML = layoutHtml();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it('sürümü ve API/DB/ajan durumunu gösterir', () => {
    render(document, healthy);

    expect(text('status-version')).toBe('v0.1.0');
    expect(text('status-api')).toBe('API ✓');
    expect(text('status-db')).toBe('DB ✓');
    expect(text('status-agent')).toBe('Ajan bağlı');
    expect(state('status-agent')).toBe('ok');
  });

  it('API kapalıyken "API erişilemiyor" hata durumu', () => {
    render(document, { apiAvailable: false });

    expect(text('status-api')).toBe('API erişilemiyor');
    expect(state('status-api')).toBe('error');
  });

  it('DB kapalıyken "DB erişilemiyor" hata durumu', () => {
    render(document, { ...healthy, databaseAvailable: false, lastEvent: null });

    expect(text('status-db')).toBe('DB erişilemiyor');
    expect(state('status-db')).toBe('error');
  });

  it('ajan bağlı değilken "Ajan bağlı değil"', () => {
    render(document, { ...healthy, agent: { isConnected: false } });

    expect(text('status-agent')).toBe('Ajan bağlı değil');
  });

  it('Günlük şeridi son olayı seviyesiyle gösterir', () => {
    render(document, { ...healthy, lastEvent: { ...healthy.lastEvent, level: 1, message: 'Ajan bağlantısı koptu: TAHA-PC' } });

    expect(text('journal-text')).toContain('Ajan bağlantısı koptu: TAHA-PC');
    expect(document.getElementById('journal').dataset.level).toBe('warning');
  });

  it('istekler $.ajax ile Web controller üzerinden gider', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, healthy));

    await refresh(document);

    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({ url: '/system/status', method: 'GET' }));
    expect(text('status-agent')).toBe('Ajan bağlı');
  });

  it('Web\'e de ulaşılamazsa "API erişilemiyor"', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 0, null));

    await refresh(document);

    expect(text('status-api')).toBe('API erişilemiyor');
  });

  it('durum 3 sn\'de bir yenilenir', async () => {
    vi.useFakeTimers();
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, healthy));

    const id = startPolling(document);
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS * 2);
    clearInterval(id);

    expect(POLL_INTERVAL_MS).toBe(3000);
    expect(ajax).toHaveBeenCalledTimes(3);
  });
});
