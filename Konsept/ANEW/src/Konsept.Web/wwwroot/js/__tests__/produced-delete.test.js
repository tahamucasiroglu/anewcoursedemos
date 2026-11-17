import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initProduced } from '../produced.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Uretilenler.cshtml'in Sil için gereken en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="produced" data-text-loading="Yükleniyor…" data-text-load-failed="Videolar yüklenemedi." data-text-item-meta="{0} · {1} · {2} sürüm"
         data-text-viewer-hint="İzlemek için soldan bir video seçin" data-text-agent-offline="Videoyu izlemek için ajan bağlı olmalı"
         data-text-reason-agent="Ajan bağlı değil" data-text-reason-file="Video dosyası bulunamadı" data-text-scene-title="{0}. {1}"
         data-text-version="Sürüm {0}" data-text-version-label="Sürüm {0}, {1}, {2}, {3}, {4}" data-text-status-completed="Tamamlandı"
         data-text-delete-label="Videoyu sil: {0}" data-text-deleted="Video Silinenler'e taşındı." data-text-delete-failed="Video silinemedi; yerinde kaldı."
         data-text-second="sn" data-text-minute="dk" data-text-hour="sa">
      <p id="produced-status" role="status"></p>
      <button type="button" id="produced-retry" hidden>Yeniden dene</button>
      <p class="empty-state" id="produced-empty" hidden>Henüz üretilmiş video yok.</p>
      <ul id="produced-list"></ul>
      <button type="button" id="produced-more" hidden>Daha fazla göster</button>
      <p id="produced-viewer-status" role="status"></p>
      <video id="produced-player" controls hidden></video>
      <button type="button" id="produced-stream-retry" hidden>Yeniden dene</button>
      <div id="produced-actions" hidden>
        <button type="button" id="produced-open-folder" data-action="folder" data-label="Klasörde aç">Klasörde aç</button>
        <button type="button" id="produced-open-player" data-action="player" data-label="Sistem oynatıcısında aç">Sistem oynatıcısında aç</button>
        <button type="button" id="produced-delete">Sil</button>
        <p id="produced-action-status" role="status"></p>
      </div>
      <p id="produced-panel-hint">Ayrıntılar için bir video seçin</p>
      <div id="produced-panel" hidden>
        <h2 id="produced-title"></h2><ol id="produced-scenes"></ol><div id="produced-concept"></div><dl id="produced-details"></dl><ul id="produced-versions"></ul>
      </div>
    </div>
    <div id="produced-delete-dialog" hidden>
      <div role="alertdialog"><p>Video tüm sürümleriyle Silinenler'e taşınacak; dosyalar bilgisayarınızda kalır.</p>
        <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Sil</button></div>
    </div>`;
}

const version = (id, number) => ({
  id, number, status: 'completed', changes: 'İlk sürüm', costUsd: 0, durationSeconds: 60, format: 'landscape', startedAt: '2026-11-02T07:01:00Z',
  endedAt: '2026-11-02T07:05:00Z', codeModel: 'Claude', engine: 'HyperFrames', imageModel: null, audioModel: null, subtitles: false, progressBar: true,
  aiVoiceNotice: false, conceptTitle: 'Kuşatma', scenes: [{ title: 'Açılış', visual: 'Surlar.', narration: 'Metin', durationSeconds: 60 }],
});
const DETAIL = { id: 'v1', title: 'Kuşatma', totalCostUsd: 0, versions: [version('s1', 1), version('s2', 2)] };
const item = (id, title) => ({ id, title, durationSeconds: 60, producedAt: '2026-11-02T07:05:00Z', versionCount: 2 });

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 8; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = (url) => calls.filter((c) => c.method === 'POST' && c.url === url);
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));
const listed = () => [...el('produced-list').querySelectorAll('[data-video]')].map((b) => b.dataset.video);

async function startAndSelect() {
  routes['GET /uretilenler/p1/liste?atla=0'] = ok({ items: [item('v1', 'Kuşatma'), item('v2', 'Kalan')], hasMore: false });
  routes['GET /uretilenler/video/v1'] = ok(DETAIL);
  routes['POST /uretim/video/s2/izle'] = fail(409, { code: 'production.agent_offline' }); // Ajan çevrimdışı: Sil yine çalışır.
  initProduced(document, { storage: null });
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
  el('produced-list').querySelector('[data-video="v1"]').click();
  await flush();
}

describe('Üretilen Videolar — Sil (spec 0019)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/uretilenler', search: '', assign: vi.fn() });
    vi.spyOn(HTMLMediaElement.prototype, 'pause').mockImplementation(() => {});
    vi.spyOn(HTMLMediaElement.prototype, 'load').mockImplementation(() => {});
    routes = {};
    calls = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('AC-1/AC-10: Sil eylem satırında, erişilebilir adla; onay odağı alır, Esc vazgeçer — hiçbir şey değişmez', async () => {
    await startAndSelect();
    const button = el('produced-delete');
    expect(el('produced-actions').hidden).toBe(false);
    expect(button.getAttribute('aria-label')).toBe('Videoyu sil: Kuşatma');
    expect(button.hasAttribute('aria-disabled')).toBe(false); // Ajan yokken de etkin (AC-2).
    button.focus();
    button.click();
    expect(el('produced-delete-dialog').hidden).toBe(false);
    expect(document.activeElement.textContent).toBe('Vazgeç');
    key(document.activeElement, 'Escape');
    await flush();
    expect(el('produced-delete-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(button);
    expect(posts('/uretilenler/video/v1/sil')).toHaveLength(0);
    expect(listed()).toEqual(['v1', 'v2']);
    expect(el('produced-panel').hidden).toBe(false);
  });

  it('AC-1/AC-2: onaylanınca (ajan çevrimdışıyken de) video listeden kalkar, seçim temizlenir, mesaj görünür', async () => {
    routes['POST /uretilenler/video/v1/sil'] = ok({ id: 'v1', title: 'Kuşatma', versionCount: 2 });
    await startAndSelect();
    el('produced-delete').click();
    el('produced-delete-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(posts('/uretilenler/video/v1/sil')).toHaveLength(1);
    expect(listed()).toEqual(['v2']);
    expect(el('produced-panel').hidden).toBe(true);
    expect(el('produced-actions').hidden).toBe(true);
    expect(el('produced-status').textContent).toBe("Video Silinenler'e taşındı.");
    expect(document.activeElement.dataset.video).toBe('v2');
  });

  it('AC-1: sunucu hatasında video listede kalır, sade mesaj; son video silinince boş durum', async () => {
    routes['POST /uretilenler/video/v1/sil'] = fail(500, {});
    await startAndSelect();
    el('produced-delete').click();
    el('produced-delete-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(listed()).toEqual(['v1', 'v2']);
    expect(el('produced-action-status').textContent).toBe('Video silinemedi; yerinde kaldı.');

    routes['POST /uretilenler/video/v1/sil'] = ok({ id: 'v1' });
    routes['POST /uretilenler/video/v2/sil'] = ok({ id: 'v2' });
    routes['GET /uretilenler/video/v2'] = ok({ ...DETAIL, id: 'v2', title: 'Kalan' });
    el('produced-delete').click();
    el('produced-delete-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    el('produced-list').querySelector('[data-video="v2"]').click();
    await flush();
    el('produced-delete').click();
    el('produced-delete-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(listed()).toEqual([]);
    expect(el('produced-empty').hidden).toBe(false);
  });
});
