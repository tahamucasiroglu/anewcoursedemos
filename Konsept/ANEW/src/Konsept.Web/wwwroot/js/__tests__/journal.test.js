import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { entryText, formatDuration, formatTime, initJournal, renderJournal } from '../journal.js';
import { BACKGROUND_POLL_INTERVAL_MS, POLL_INTERVAL_MS, refresh, render, startPolling } from '../status.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

// _Layout.cshtml'deki Günlük şeridi + _JournalModal.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function layout() {
  return `
    <meta name="csrf-token" content="t" />
    <button type="button" id="journal" data-level="info" data-state="loading"
            data-text-loading="Yükleniyor…" data-text-empty="Henüz bir işlem yok" data-text-stale="Günlük güncellenemiyor"
            data-text-warning="Uyarı" data-text-error="Hata" data-text-critical="Kritik"
            data-text-unit-second="sn" data-text-unit-minute="dk" data-text-unit-hour="sa">
      <span>Günlük:</span><span id="journal-level" hidden></span><span id="journal-text">Yükleniyor…</span>
      <span id="journal-profile" hidden></span><span id="journal-stale" hidden>Günlük güncellenemiyor</span>
    </button>
    <span id="journal-announce" role="status" aria-live="polite"></span>
    <div id="system-status" data-text-api-ok="API ✓" data-text-api-down="API erişilemiyor" data-text-db-ok="DB ✓"
         data-text-db-down="DB erişilemiyor" data-text-agent-connected="Ajan bağlı" data-text-agent-disconnected="Ajan bağlı değil"
         data-text-journal-empty="Henüz bir işlem yok">
      <span id="status-version"></span><span id="status-api"></span><span id="status-db"></span><span id="status-agent"></span>
    </div>
    <div id="journal-modal" hidden data-text-loading="Yükleniyor…" data-text-day-empty="Bu günde işlem yok"
         data-text-load-failed="Günlük yüklenemedi" data-text-more="Daha fazla">
      <div role="dialog" aria-modal="true">
        <button type="button" id="journal-modal-close">Kapat</button>
        <button type="button" id="journal-prev">Önceki gün</button><h3 id="journal-day"></h3>
        <button type="button" id="journal-next">Sonraki gün</button>
        <select id="journal-filter"><option value="all">Tümü</option><option value="warning">Uyarı</option><option value="error">Hata</option></select>
        <p id="journal-status"></p><button type="button" id="journal-retry" hidden>Tekrar dene</button>
        <ol id="journal-list"></ol><button type="button" id="journal-more" hidden>Daha fazla</button>
      </div>
    </div>`;
}

const $ = installJquery();
const el = (id) => document.getElementById(id);
const event = (over) => ({ id: 'e1', type: 't', level: 0, message: 'Giriş yapıldı', occurredAt: '2026-11-16T07:30:00Z', ...over });
const status = (lastEvent) => ({ apiAvailable: true, version: '0.1.0', databaseAvailable: true, agent: { isConnected: false }, lastEvent });
const day = (over) => ({ date: '2026-11-16', isToday: true, isFirstDay: false, items: [], hasMore: false, ...over });
const units = { second: 'sn', minute: 'dk', hour: 'sa' };
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

describe('Günlük şeridi (spec 0021)', () => {
  beforeEach(() => {
    document.body.innerHTML = layout();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it('süre biçimi: 59 sn, 1 dk 0 sn, 30 dk 12 sn, 1 sa 0 dk, 1 sa 5 dk (AC-3)', () => {
    expect(formatDuration(59, units)).toBe('59 sn');
    expect(formatDuration(60, units)).toBe('1 dk 0 sn');
    expect(formatDuration(1812, units)).toBe('30 dk 12 sn');
    expect(formatDuration(3600, units)).toBe('1 sa 0 dk');
    expect(formatDuration(3900, units)).toBe('1 sa 5 dk');
  });

  it('süreli kayıt: metin · süre · Europe/Istanbul HH:mm başlangıç–bitiş, süre farktan (AC-3)', () => {
    const e = event({ message: 'Video hazırlandı', startedAt: '2026-11-16T07:30:03Z', endedAt: '2026-11-16T07:31:00Z' });

    expect(formatTime('2026-11-16T21:05:00Z')).toBe('00:05');
    expect(entryText(e, units)).toBe('Video hazırlandı · 57 sn · 10:30–10:31');
    renderJournal(document, e);
    expect(el('journal-text').textContent).toBe('10:30 — Video hazırlandı · 57 sn · 10:30–10:31');
  });

  it('yükleniyor → boş "Henüz bir işlem yok" → kayıt (AC-4, AC-1)', () => {
    expect(el('journal').dataset.state).toBe('loading');
    expect(el('journal-text').textContent).toBe('Yükleniyor…');

    render(document, status(null));
    expect(el('journal-text').textContent).toBe('Henüz bir işlem yok');

    render(document, status(event()));
    expect(el('journal-text').textContent).toBe('10:30 — Giriş yapıldı');
  });

  it('güncellenemeyince son kayıt kalır ve işaret görünür; bağlantı dönünce kalkar (AC-4)', async () => {
    render(document, status(event()));
    vi.spyOn($, 'ajax').mockReturnValueOnce(ajaxRejecting($, 0, null));

    await refresh(document);
    expect(el('journal-text').textContent).toBe('10:30 — Giriş yapıldı');
    expect(el('journal-stale').hidden).toBe(false);

    render(document, { ...status(null), databaseAvailable: false });
    expect(el('journal-text').textContent).toBe('10:30 — Giriş yapıldı');
    expect(el('journal-stale').hidden).toBe(false);

    $.ajax.mockReturnValueOnce(ajaxResolving($, status(event({ id: 'e2', message: 'Çıkış yapıldı' }))));
    await refresh(document);
    expect(el('journal-stale').hidden).toBe(true);
    expect(el('journal-text').textContent).toContain('Çıkış yapıldı');
  });

  it('seviye: Uyarı sarı + ikon + "Uyarı"; Hata/Kritik kırmızı + etiket; Bilgi etiketsiz (AC-2)', () => {
    renderJournal(document, event({ level: 1 }));
    expect(el('journal').dataset.level).toBe('warning');
    expect(el('journal-level').hidden).toBe(false);
    expect(el('journal-level').textContent).toContain('Uyarı');
    expect(el('journal-level').querySelector('[aria-hidden="true"]').textContent).not.toBe('');

    renderJournal(document, event({ level: 2 }));
    expect(el('journal').dataset.level).toBe('error');
    expect(el('journal-level').textContent).toContain('Hata');

    renderJournal(document, event({ level: 3 }));
    expect(el('journal').dataset.level).toBe('critical');
    expect(el('journal-level').textContent).toContain('Kritik');

    renderJournal(document, event({ level: 0 }));
    expect(el('journal').dataset.level).toBe('info');
    expect(el('journal-level').hidden).toBe(true);
  });

  it('yalnızca yeni Uyarı/Hata/Kritik kayıtlar polite duyurulur; Bilgi ve ilk yükleme duyurulmaz (AC-11)', () => {
    const announce = el('journal-announce');
    expect(announce.getAttribute('aria-live')).toBe('polite');

    renderJournal(document, event({ id: 'a', level: 2, message: 'ilk' }));
    expect(announce.textContent).toBe('');

    renderJournal(document, event({ id: 'b', level: 0, message: 'bilgi' }));
    expect(announce.textContent).toBe('');

    renderJournal(document, event({ id: 'c', level: 1, message: 'uyarı geldi' }));
    expect(announce.textContent).toBe('Uyarı: uyarı geldi');

    renderJournal(document, event({ id: 'd', level: 3, message: 'kritik geldi' }));
    expect(announce.textContent).toBe('Kritik: kritik geldi');
  });

  it('profil etiketi: profile ait kayıtta ad görünür, ait olmayanda etiket yok (AC-10)', () => {
    renderJournal(document, event({ profileName: 'Kanal B' }));
    expect(el('journal-profile').hidden).toBe(false);
    expect(el('journal-profile').textContent).toBe('Kanal B');

    renderJournal(document, event({ id: 'x', profileName: null }));
    expect(el('journal-profile').hidden).toBe(true);
  });

  it('HTML içeren metin çalıştırılmadan düz metin görünür (AC-12)', () => {
    renderJournal(document, event({ message: '<img src=x onerror="window.__xss=1"><script>window.__xss=1</script>' }));

    expect(el('journal-text').querySelector('img, script')).toBeNull();
    expect(el('journal-text').textContent).toContain('<script>');
    expect(globalThis.__xss).toBeUndefined();
  });

  it('önplanda 3 sn, arka planda 15 sn yoklar; görünür olunca beklemeden yeniler (AC-1)', async () => {
    vi.useFakeTimers();
    let hidden = false;
    Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden });
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, status(event())));

    const id = startPolling(document);
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);
    expect(ajax).toHaveBeenCalledTimes(2);

    hidden = true;
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS * 4);
    expect(ajax).toHaveBeenCalledTimes(2);
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);
    expect(ajax).toHaveBeenCalledTimes(3);
    expect(BACKGROUND_POLL_INTERVAL_MS).toBe(15000);

    hidden = false;
    document.dispatchEvent(new Event('visibilitychange'));
    expect(ajax).toHaveBeenCalledTimes(4);
    clearInterval(id);
    delete document.hidden;
  });
});

describe('Günlük geçmişi modalı (spec 0021)', () => {
  beforeEach(() => {
    document.body.innerHTML = layout();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('tık açar, odak içeri alınır, yükleniyor görünür; kayıtlar gün başlıklı, en yenisi üstte; Esc kapatır, odak şeride döner (AC-5)', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue($.Deferred().promise());
    initJournal(document);

    el('journal').click();
    expect(el('journal-modal').hidden).toBe(false);
    expect(el('journal-modal').contains(document.activeElement)).toBe(true);
    expect(el('journal-status').textContent).toBe('Yükleniyor…');
    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({ url: '/gunluk?tarih=&filtre=all&atla=0', method: 'GET' }));

    ajax.mockReturnValue(ajaxResolving($, day({ items: [event({ id: 'n', message: 'yeni' }), event({ id: 'o', message: 'eski' })] })));
    el('journal-filter').dispatchEvent(new Event('change'));
    await flush();
    expect(el('journal-day').textContent).toContain('16 Kasım 2026');
    expect([...el('journal-list').children].map((li) => li.textContent)).toEqual([
      expect.stringContaining('yeni'), expect.stringContaining('eski'),
    ]);

    el('journal-modal').dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(el('journal-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(el('journal'));
  });

  it('Kapat ve dışarı tıklama kapatır (AC-5)', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, day()));
    const journal = initJournal(document);

    await journal.open();
    el('journal-modal-close').click();
    expect(el('journal-modal').hidden).toBe(true);

    await journal.open();
    el('journal-modal').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(el('journal-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(el('journal'));
  });

  it('boş gün "Bu günde işlem yok"; hata "Günlük yüklenemedi" + tekrar dene (AC-5)', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, day()));
    const journal = initJournal(document);

    await journal.open();
    expect(el('journal-status').textContent).toBe('Bu günde işlem yok');

    ajax.mockReturnValue(ajaxRejecting($, 500, null));
    el('journal-filter').dispatchEvent(new Event('change'));
    await flush();
    expect(el('journal-status').textContent).toBe('Günlük yüklenemedi');
    expect(el('journal-retry').hidden).toBe(false);

    ajax.mockReturnValue(ajaxResolving($, day({ items: [event()] })));
    el('journal-retry').click();
    await flush();
    expect(el('journal-retry').hidden).toBe(true);
    expect(el('journal-list').children).toHaveLength(1);
  });

  it('"Daha fazla" sonraki 50\'yi ekler (yinelenen atlanır), devamı yoksa gizlenir; filtre "Hata" isteği gider (AC-6)', async () => {
    const first = Array.from({ length: 50 }, (_, i) => event({ id: `k${i}`, message: `kayıt ${i}` }));
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, day({ items: first, hasMore: true })));
    const journal = initJournal(document);

    await journal.open();
    expect(el('journal-list').children).toHaveLength(50);
    expect(el('journal-more').hidden).toBe(false);

    ajax.mockReturnValue(ajaxResolving($, day({ items: [event({ id: 'k49' }), event({ id: 'k50', message: 'kayıt 50' })], hasMore: false })));
    el('journal-more').click();
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/gunluk?tarih=2026-11-16&filtre=all&atla=50' }));
    expect(el('journal-list').children).toHaveLength(51);
    expect(el('journal-more').hidden).toBe(true);

    el('journal-filter').value = 'error';
    el('journal-filter').dispatchEvent(new Event('change'));
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/gunluk?tarih=2026-11-16&filtre=error&atla=0' }));
  });

  it('bugünde "Sonraki gün", ilk günde "Önceki gün" pasif; önceki gün bir gün geri ister (AC-7)', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, day({ isToday: true, isFirstDay: false })));
    const journal = initJournal(document);

    await journal.open();
    expect(el('journal-next').disabled).toBe(true);
    expect(el('journal-prev').disabled).toBe(false);

    ajax.mockReturnValue(ajaxResolving($, day({ date: '2026-11-15', isToday: false, isFirstDay: true })));
    el('journal-prev').click();
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/gunluk?tarih=2026-11-15&filtre=all&atla=0' }));
    expect(el('journal-prev').disabled).toBe(true);
    expect(el('journal-next').disabled).toBe(false);
  });

  it('odaktaki "Önceki gün" ilk günde pasifleşince odak modalda kalır, Esc yine kapatır (review r1 M-1)', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, day({ isToday: true, isFirstDay: false })));
    const journal = initJournal(document);
    await journal.open();

    ajax.mockReturnValue(ajaxResolving($, day({ date: '2026-11-15', isToday: false, isFirstDay: true })));
    el('journal-prev').focus();
    el('journal-prev').click();
    await flush();
    expect(el('journal-prev').disabled).toBe(true);
    expect(el('journal-modal').contains(document.activeElement)).toBe(true);

    document.body.focus();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(el('journal-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(el('journal'));
  });
});
