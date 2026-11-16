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

  it('spec 0007: ajan durumu ve cihaz adı agent:status olayıyla sayfaya iletilir', () => {
    const seen = [];
    const listener = (event) => seen.push(event.detail);
    document.addEventListener('agent:status', listener);
    render(document, healthy);
    render(document, { ...healthy, agent: { isConnected: false } });
    render(document, { apiAvailable: false });
    document.removeEventListener('agent:status', listener);

    expect(seen).toEqual([{ connected: true, machineName: 'TAHA-PC' }, { connected: false, machineName: null }]);
  });

  it('Günlük şeridi son olayı seviyesiyle gösterir', () => {
    render(document, { ...healthy, lastEvent: { ...healthy.lastEvent, level: 1, message: 'Ajan bağlantısı koptu: TAHA-PC' } });

    expect(text('journal-text')).toContain('Ajan bağlantısı koptu: TAHA-PC');
    expect(document.getElementById('journal').dataset.level).toBe('warning');
  });

  it('kritik olay (seviye 3) kritik olarak gösterilir (spec 0008 A-1)', () => {
    render(document, { ...healthy, lastEvent: { ...healthy.lastEvent, level: 3, message: 'Kritik hata' } });

    expect(document.getElementById('journal').dataset.level).toBe('critical');
  });

  it('ajan bağlı değilken "Ajanı eşleştir" bağlantısı görünür, bağlıyken gizlenir (spec 0012 AC-2)', () => {
    document.getElementById('system-status').insertAdjacentHTML('beforeend', '<a id="status-agent-pair" href="/ajan-eslestir" hidden>Ajanı eşleştir</a>');
    const pair = document.getElementById('status-agent-pair');

    render(document, { ...healthy, agent: { isConnected: false } });
    expect(pair.hidden).toBe(false);
    expect(pair.getAttribute('href')).toBe('/ajan-eslestir');

    render(document, healthy);
    expect(pair.hidden).toBe(true);

    render(document, { apiAvailable: false });
    expect(pair.hidden).toBe(true);
  });

  it('ajan bağlı değilken "Ajanı indir" bağlantısı görünür, bağlıyken ve API yokken gizlenir (spec 0009 AC-1)', () => {
    document.getElementById('system-status').insertAdjacentHTML('beforeend', '<a id="status-agent-download" href="/ajan/indir" download hidden>Ajanı indir</a>');
    const download = document.getElementById('status-agent-download');

    render(document, { ...healthy, agent: { isConnected: false } });
    expect(download.hidden).toBe(false);
    expect(download.getAttribute('href')).toBe('/ajan/indir');

    render(document, healthy);
    expect(download.hidden).toBe(true);

    render(document, { apiAvailable: false });
    expect(download.hidden).toBe(true);
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
