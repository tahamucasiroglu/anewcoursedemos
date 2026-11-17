import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initConceptEdit } from '../concept-edit.js';
import { initProduced, POLL_MS } from '../produced.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

const STAGE_TEXTS = {
  'stage-analysis': 'Konsept inceleniyor',
  'stage-code': 'Video kodu üretiliyor',
  'stage-images': 'Görseller üretiliyor',
  'stage-voice': 'Seslendirme yapılıyor',
  'stage-render': 'Render',
  'stage-merge': 'Birleştiriliyor',
};
const PRODUCED_TEXTS = {
  loading: 'Yükleniyor…',
  'load-failed': 'Videolar yüklenemedi.',
  'item-meta': '{0} · {1} · {2} sürüm',
  'viewer-hint': 'İzlemek için soldan bir video seçin',
  'agent-offline': 'Videoyu izlemek için ajan bağlı olmalı',
  'scene-title': '{0}. {1}',
  version: 'Sürüm {0}',
  'version-label': 'Sürüm {0}, {1}, {2}, {3}, {4}',
  'status-completed': 'Tamamlandı',
  'status-queued': 'Sırada',
  'status-producing': 'Üretiliyor',
  'status-stopped': 'Durduruldu',
  'status-waiting_for_agent': 'Ajan bekleniyor',
  'status-failed': 'Hata',
  'delete-blocked': 'Önce üretimi durdurun.',
  'regenerate-label': '{0} için yeniden üret: önce konsepti düzenle',
  regenerating: 'Konsept düzenlemesi açılıyor…',
  'version-busy': 'Bu video için zaten üretimde bir sürüm var.',
  'version-no-change': 'Hiçbir ayar değişmedi.',
  'version-previewing': 'Aşamalar hesaplanıyor…',
  'version-stages': 'Yeniden üretilecek aşamalar: {0}',
  'version-online': 'Online model çağrısı yapılacak.',
  'version-offline': 'Online model çağrısı yapılmayacak.',
  'version-starting': 'Başlatılıyor…',
  'version-started': 'Yeni sürüm sıraya eklendi.',
  'version-load-failed': 'Yeni sürüm seçenekleri yüklenemedi.',
  'version-local-tag': '{0} (yerel)',
  'version-online-tag': '{0} (online)',
  'version-silent': 'Kapalı — sessiz video',
  'version-image-off': 'Kapalı — koddan çiz',
  'version-confirm-stages': 'Çalışacak aşamalar: {0}.',
  'active-status': 'Durum: {0}',
  'active-stage': '{0} — {1}',
  'active-retried': 'Sürüm kaldığı aşamadan devam ediyor.',
  'active-removed': 'Hata veren sürüm kaldırıldı.',
  'active-failed': 'İşlem yapılamadı.',
  second: 'sn',
  minute: 'dk',
  hour: 'sa',
  ...STAGE_TEXTS,
};
const attrs = (texts) => Object.entries(texts).map(([k, v]) => `data-text-${k}="${v}"`).join(' ');

function producedHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="produced" ${attrs(PRODUCED_TEXTS)}>
      <p id="produced-status" role="status"></p><button type="button" id="produced-retry" hidden>Yeniden dene</button>
      <p id="produced-empty" hidden></p><ul id="produced-list"></ul><button type="button" id="produced-more" hidden>Daha fazla göster</button>
      <p id="produced-viewer-status" role="status"></p><video id="produced-player" controls hidden></video>
      <button type="button" id="produced-stream-retry" hidden>Yeniden dene</button>
      <div id="produced-actions" hidden>
        <button type="button" id="produced-open-folder" data-action="folder" data-label="Klasörde aç">Klasörde aç</button>
        <button type="button" id="produced-open-player" data-action="player" data-label="Sistem oynatıcısında aç">Sistem oynatıcısında aç</button>
        <button type="button" id="produced-regenerate">Yeniden üret</button>
        <button type="button" id="produced-delete">Sil</button>
        <p id="produced-action-status" role="status"></p>
      </div>
      <p id="produced-panel-hint">Ayrıntılar için bir video seçin</p>
      <div id="produced-panel" hidden>
        <h2 id="produced-title"></h2>
        <div id="produced-active" hidden>
          <p id="produced-active-status"></p><p id="produced-active-error" hidden></p>
          <button type="button" id="produced-active-retry" hidden>Tekrar dene</button>
          <button type="button" id="produced-active-remove" hidden>Sürümü kaldır</button>
          <p id="produced-active-message" role="status"></p>
        </div>
        <ol id="produced-scenes"></ol><div id="produced-concept"></div><dl id="produced-details"></dl><ul id="produced-versions"></ul>
        <section id="version-panel">
          <label for="version-engine">Video motoru</label><select id="version-engine"></select>
          <label for="version-image">Görsel modeli</label><select id="version-image"></select>
          <label for="version-audio">Ses motoru</label><select id="version-audio"></select>
          <label><input type="checkbox" id="version-subtitles" /> Altyazı</label>
          <label><input type="checkbox" id="version-progress" /> İlerleme çubuğu</label>
          <p id="version-preview" role="status"></p>
          <button type="button" id="version-start">Yeni sürümü başlat</button>
          <p id="version-status" role="status"></p>
        </section>
      </div>
    </div>
    <div id="version-confirm-dialog" hidden><div role="alertdialog" aria-modal="true"><p id="version-confirm-stages"></p>
      <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Onayla ve başlat</button></div></div>
    <div id="version-remove-dialog" hidden><div role="alertdialog" aria-modal="true">
      <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Sürümü kaldır</button></div></div>
    <div id="produced-delete-dialog" hidden><div role="alertdialog">
      <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Sil</button></div></div>`;
}

const version = (id, number) => ({
  id, number, status: 'completed', changes: 'İlk sürüm', costUsd: 0, durationSeconds: 60, format: 'landscape', startedAt: '2026-11-02T07:01:00Z',
  endedAt: '2026-11-02T07:05:00Z', codeModel: 'Claude', engine: 'HyperFrames', imageModel: null, audioModel: 'Supertonic 3', subtitles: true, progressBar: false,
  aiVoiceNotice: true, conceptTitle: 'Kuşatma', scenes: [{ title: 'Açılış', visual: 'Surlar.', narration: 'Metin', durationSeconds: 60 }],
});
const detail = (extra = {}) => ({ id: 'v1', title: 'Kuşatma', totalCostUsd: 0, versions: [version('s1', 1)], active: null, deleteBlocked: false, ...extra });
const item = (status = null, stage = null) => ({ id: 'v1', title: 'Kuşatma', durationSeconds: 60, producedAt: '2026-11-02T07:05:00Z', versionCount: 1, status, stage });
const OPTIONS = {
  baseVersionId: 's1',
  engine: 'local/hyperframes',
  imageModel: null,
  audioModel: 'local/supertonic-3',
  subtitles: true,
  progressBar: false,
  engines: [{ id: 'local/hyperframes', name: 'HyperFrames', local: true }, { id: 'local/remotion', name: 'Remotion', local: true }],
  images: [],
  audios: [{ id: 'local/supertonic-3', name: 'Supertonic 3', local: true }, { id: 'local/chatterbox', name: 'Chatterbox', local: true },
    { id: 'elevenlabs/v2', name: 'ElevenLabs', local: false }],
};
const active = (status, stage = null, error = null) => ({ jobId: 'j1', kind: 'new', status, stage, error, stages: [], costUsd: 0 });

let routes;
let calls;
let timers;
let navigated;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 8; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = (url) => calls.filter((c) => c.method === 'POST' && c.url === url);
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));
const change = (node, value) => {
  node.value = value;
  node.dispatchEvent(new Event('change', { bubbles: true }));
};

function installAjax() {
  routes = {};
  calls = [];
  $.ajax = (settings) => {
    calls.push(settings);
    const handler = routes[`${settings.method} ${settings.url}`];
    return handler ? handler(settings) : $.Deferred().reject({ status: 404, responseJSON: { message: 'yok' } }).promise();
  };
}

async function openVideo(videoDetail, listItem = item()) {
  routes['GET /uretilenler/p1/liste?atla=0'] = ok({ items: [listItem], hasMore: false });
  routes['GET /uretilenler/video/v1'] = ok(videoDetail);
  routes['GET /uretilenler/video/v1/yeni-surum?taban=s1'] = ok(OPTIONS);
  routes['POST /uretim/video/s1/izle'] = fail(409, { code: 'production.agent_offline' });
  const controller = initProduced(document, {
    storage: null,
    setTimer: (fn, ms) => { timers.push({ fn, ms }); return timers.length; },
    clearTimer: () => {},
    navigate: (url) => { navigated = url; },
  });
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
  el('produced-list').querySelector('[data-video="v1"]').click();
  await flush();
  return controller;
}

describe('Üretilen Videolar — yeni sürüm ve süren sürüm (spec 0020)', () => {
  beforeEach(() => {
    document.body.innerHTML = producedHtml();
    setNavigation({ pathname: '/uretilenler', search: '', assign: vi.fn() });
    installAjax();
    timers = [];
    navigated = null;
  });

  it('AC-1/AC-4/AC-17: değişiklik yokken başlat pasif (ipucu fare + odak); yerel ses değişiminde aşamalar ve "online yapılmayacak", onaysız başlar', async () => {
    routes['POST /uretilenler/video/v1/yeni-surum/onizle'] = (settings) => ok(JSON.parse(settings.data).audioModel === 'local/chatterbox'
      ? { stages: ['voice', 'merge'], online: false, unchanged: false }
      : { stages: [], online: false, unchanged: true })();
    routes['POST /uretilenler/video/v1/yeni-surum'] = ok(active('producing', 'voice'));
    await openVideo(detail());

    expect(el('version-engine').value).toBe('local/hyperframes');
    expect([...el('version-audio').options].map((o) => o.textContent))
      .toEqual(['Kapalı — sessiz video', 'Supertonic 3 (yerel)', 'Chatterbox (yerel)', 'ElevenLabs (online)']);
    expect(el('version-image').disabled).toBe(true);
    expect(el('version-preview').textContent).toBe('Hiçbir ayar değişmedi.');
    const start = el('version-start');
    expect(start.getAttribute('aria-disabled')).toBe('true');
    expect(start.dataset.tooltip).toBe('Hiçbir ayar değişmedi.');
    expect(start.getAttribute('aria-description')).toBe('Hiçbir ayar değişmedi.');
    start.click();
    await flush();
    expect(posts('/uretilenler/video/v1/yeni-surum')).toHaveLength(0);

    change(el('version-audio'), 'local/chatterbox');
    await flush();
    expect(el('version-preview').textContent).toBe('Yeniden üretilecek aşamalar: Seslendirme yapılıyor, Birleştiriliyor Online model çağrısı yapılmayacak.');
    expect(start.hasAttribute('aria-disabled')).toBe(false);
    start.click();
    await flush();
    expect(el('version-confirm-dialog').hidden).toBe(true); // Online yok: onay sorulmaz.
    const sent = posts('/uretilenler/video/v1/yeni-surum');
    expect(sent).toHaveLength(1);
    expect(JSON.parse(sent[0].data)).toMatchObject({ baseVersionId: 's1', audioModel: 'local/chatterbox', subtitles: true, confirmOnline: false });
    expect(el('version-status').textContent).toBe('Yeni sürüm sıraya eklendi.');
  });

  it('AC-5/AC-17: online çağrıda onay penceresi aşamaları gösterir, odağı içine alır; Esc → istek yok; onayla → confirmOnline', async () => {
    routes['POST /uretilenler/video/v1/yeni-surum/onizle'] = (settings) => ok(JSON.parse(settings.data).engine === 'local/remotion'
      ? { stages: ['code', 'render', 'merge'], online: true, unchanged: false }
      : { stages: [], online: false, unchanged: true })();
    routes['POST /uretilenler/video/v1/yeni-surum'] = ok(active('queued'));
    await openVideo(detail());

    change(el('version-engine'), 'local/remotion');
    await flush();
    expect(el('version-preview').textContent).toContain('Online model çağrısı yapılacak.');
    el('version-start').click();
    await flush();
    const dialog = el('version-confirm-dialog');
    expect(dialog.hidden).toBe(false);
    expect(el('version-confirm-stages').textContent).toBe('Çalışacak aşamalar: Video kodu üretiliyor, Render, Birleştiriliyor.');
    expect(document.activeElement).toBe(dialog.querySelector('[data-confirm-cancel]'));
    key(dialog, 'Tab');
    expect(document.activeElement).toBe(dialog.querySelector('[data-confirm-ok]'));
    key(dialog, 'Escape');
    await flush();
    expect(dialog.hidden).toBe(true);
    expect(document.activeElement).toBe(el('version-start'));
    expect(posts('/uretilenler/video/v1/yeni-surum')).toHaveLength(0);

    el('version-start').click();
    await flush();
    dialog.querySelector('[data-confirm-ok]').click();
    await flush();
    expect(JSON.parse(posts('/uretilenler/video/v1/yeni-surum')[0].data)).toMatchObject({ confirmOnline: true, previewStages: ['code', 'render', 'merge'] });
  });

  it('AC-8/AC-14: süren sürümün rozeti listede ve sürüm listesinde metinli; 3 sn yoklamayla güncellenir; Sil ve Yeniden üret pasif', async () => {
    routes['POST /uretilenler/video/v1/yeni-surum/onizle'] = ok({ stages: [], online: false, unchanged: true });
    await openVideo(detail({ active: active('producing', 'voice'), deleteBlocked: true }), item('producing', 'voice'));

    const badge = el('produced-list').querySelector('[data-badge="v1"]');
    expect(badge.textContent).toBe('Üretiliyor — Seslendirme yapılıyor');
    expect(el('produced-active-status').textContent).toBe('Durum: Üretiliyor — Seslendirme yapılıyor');
    expect(el('produced-versions').querySelector('.produced-versions__item--pending').textContent).toBe('Sürüm 2 · Üretiliyor — Seslendirme yapılıyor');
    expect(el('produced-delete').getAttribute('aria-disabled')).toBe('true');
    expect(el('produced-delete').dataset.tooltip).toBe('Önce üretimi durdurun.');
    expect(el('produced-regenerate').getAttribute('aria-disabled')).toBe('true');
    expect(el('version-start').dataset.tooltip).toBe('Bu video için zaten üretimde bir sürüm var.');
    el('produced-delete').click();
    await flush();
    expect(el('produced-delete-dialog').hidden).toBe(true);
    expect(timers.at(-1).ms).toBe(POLL_MS);
    expect(POLL_MS).toBeLessThanOrEqual(5000);

    const done = detail({ versions: [version('s1', 1), version('s2', 2)] });
    routes['GET /uretilenler/video/v1'] = ok(done);
    routes['GET /uretilenler/p1/liste?atla=0'] = ok({ items: [{ ...item(), versionCount: 2 }], hasMore: false });
    const pending = timers.length;
    await timers.at(-1).fn();
    await flush();
    expect(el('produced-list').querySelector('[data-badge="v1"]')).toBeNull();
    expect(el('produced-versions').querySelectorAll('[data-version]')).toHaveLength(2);
    expect(el('produced-active').hidden).toBe(true);
    expect(el('produced-delete').hasAttribute('aria-disabled')).toBe(false);
    expect(timers.length).toBe(pending); // Süren sürüm kalmadı: yoklama durur.
  });

  it('AC-13/AC-17: hata veren sürümde sade neden, Tekrar dene ve onaylı Sürümü kaldır (Esc vazgeçer)', async () => {
    routes['POST /uretilenler/video/v1/yeni-surum/onizle'] = ok({ stages: [], online: false, unchanged: true });
    routes['POST /uretilenler/video/v1/tekrar-dene'] = ok(active('producing', 'merge'));
    routes['POST /uretilenler/video/v1/surumu-kaldir'] = ok(active('failed'));
    await openVideo(detail({ active: active('failed', null, 'Birleştiriliyor aşaması tamamlanamadı.') }), item('failed'));

    expect(el('produced-active-status').textContent).toBe('Durum: Hata');
    expect(el('produced-active-error').textContent).toBe('Birleştiriliyor aşaması tamamlanamadı.');
    expect(el('produced-active-retry').hidden).toBe(false);
    el('produced-active-retry').click();
    await flush();
    expect(posts('/uretilenler/video/v1/tekrar-dene')).toHaveLength(1);
    expect(el('produced-active-message').textContent).toBe('Sürüm kaldığı aşamadan devam ediyor.');

    el('produced-active-remove').click();
    await flush();
    const dialog = el('version-remove-dialog');
    expect(dialog.hidden).toBe(false);
    key(dialog, 'Escape');
    await flush();
    expect(posts('/uretilenler/video/v1/surumu-kaldir')).toHaveLength(0);
    el('produced-active-remove').click();
    await flush();
    dialog.querySelector('[data-confirm-ok]').click();
    await flush();
    expect(posts('/uretilenler/video/v1/surumu-kaldir')).toHaveLength(1);
  });

  it('AC-11: Yeniden üret bağlamı açar ve Konseptler\'de bu konseptin düzenlemesine gider', async () => {
    routes['POST /uretilenler/video/v1/yeni-surum/onizle'] = ok({ stages: [], online: false, unchanged: true });
    routes['POST /uretilenler/video/v1/yeniden-uret/ac?taban=s1'] = ok({ token: 'tok', conceptId: 'v1', profileId: 'p1', baseVersionId: 's1' });
    await openVideo(detail());

    expect(el('produced-regenerate').getAttribute('aria-label')).toBe('Kuşatma için yeniden üret: önce konsepti düzenle');
    el('produced-regenerate').click();
    await flush();
    expect(navigated).toBe('/konseptler?konsept=v1&yenidenUret=tok&taban=s1');
  });
});

// --- Konseptler: Yeniden üret bağlamı ---------------------------------------------------------------------------------------------------
const REGEN_TEXTS = {
  unchanged: 'Konsept değişmedi.',
  'save-first': 'Önce düzenlemeyi kaydedin.',
  'all-stages': 'Tüm aşamalar baştan çalışır: {0}',
  online: 'Online model çağrısı yapılacak.',
  started: "Yeniden üretim sıraya eklendi. Üretilen Videolar'a dönülüyor…",
  'load-failed': 'Yeniden üret bilgisi alınamadı.',
  'confirm-stages': 'Çalışacak aşamalar: {0}.',
  previewing: 'Aşamalar hesaplanıyor…',
  ...STAGE_TEXTS,
};

function conceptsHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <article id="concept-detail" tabindex="-1" hidden></article>
    <div id="concept-editor" hidden data-text-saving="Kaydediliyor…" data-text-saved="Kaydedildi." data-text-save-failed="Kaydedilemedi."
         data-text-scene="Sahne {0}" data-text-title="Sahne başlığı" data-text-visual="Ekran" data-text-narration="Seslendirme" data-text-duration="Süre">
      <h3 id="concept-edit-heading"></h3>
      <form id="concept-edit-form" novalidate><div id="concept-edit-scenes"></div><p id="concept-edit-status" role="status"></p>
        <button type="submit" id="concept-edit-save">Kaydet</button><button type="button" id="concept-edit-close">Düzenlemeyi kapat</button></form>
      <textarea id="concept-instruction"></textarea><p id="concept-instruction-error"></p><button type="button" id="concept-regenerate">Yeniden üret</button>
      <p id="concept-regen-status"></p>
      <section id="regen-panel" hidden ${attrs(REGEN_TEXTS)}>
        <p id="regen-preview" role="status"></p>
        <label id="regen-force-label" hidden><input type="checkbox" id="regen-force" /> Yine de baştan üret</label>
        <button type="button" id="regen-start">Yeni sürüm olarak üret</button><button type="button" id="regen-back">Vazgeç</button>
        <p id="regen-status" role="status"></p>
      </section>
    </div>
    <div id="revision-modal" hidden><div role="dialog"><h2 id="revision-title" tabindex="-1">Revizyon</h2>
      <p id="revision-warning" hidden></p><p id="revision-duration-warning" hidden></p><ol id="revision-scenes"></ol><p id="revision-status"></p>
      <button type="button" id="revision-approve">Onayla</button><button type="button" id="revision-discard">Sil</button>
      <textarea id="revision-instruction"></textarea><p id="revision-instruction-error"></p><button type="button" id="revision-again">Tekrar</button>
      <div id="revision-confirm" hidden><button type="button" id="revision-confirm-cancel">Vazgeç</button><button type="button" id="revision-confirm-ok">Evet</button></div>
    </div></div>
    <div id="concept-leave" hidden><button type="button" id="concept-leave-cancel">Vazgeç</button><button type="button" id="concept-leave-ok">At</button></div>
    <div id="regen-confirm-dialog" hidden><div role="alertdialog" aria-modal="true"><p id="regen-confirm-stages"></p>
      <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Onayla ve başlat</button></div></div>`;
}

const concept = (status = 'video_ready', narration = 'Sabah başlıyor.') => ({
  id: 'c1', title: 'Kuşatma', status, scenes: [{ title: 'Açılış', visual: 'Surlar', narration, durationSeconds: 60 }],
  wordRepetitionWarning: false, durationMismatchWarning: false,
});

describe('Konseptler — Yeniden üret bağlamı (spec 0020)', () => {
  let editor;

  beforeEach(() => {
    document.body.innerHTML = conceptsHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    installAjax();
    navigated = null;
  });

  afterEach(() => {
    editor?.dispose();
    window.history.replaceState({}, '', '/');
  });

  function open(c) {
    editor = initConceptEdit(document, { win: null, setTimer: () => 0, clearTimer: () => {}, navigate: (url) => { navigated = url; } });
    routes['GET /konseptler/c1/revizyon'] = ok({ conceptId: 'c1', id: null, status: 'none', scenes: [] });
    document.dispatchEvent(new CustomEvent('concept:opened', { detail: { concept: c, edit: true } }));
  }

  it('AC-18: bağlam dışında Video oldu konsept düzenlemeye açılmaz', async () => {
    open(concept());
    await flush();
    expect(el('concept-editor').hidden).toBe(true);
    expect(el('regen-panel').hidden).toBe(true);
  });

  it('AC-11/AC-12/AC-18: bağlamda düzenlenir (istekler belirteçle); değişmediyse başlat pasif, "Yine de baştan üret" + onay → başlar', async () => {
    window.history.replaceState({}, '', '/konseptler?konsept=c1&yenidenUret=tok&taban=s1');
    routes['GET /uretilenler/video/c1/yeniden-uret?belirtec=tok&taban=s1'] = ok({ stages: ['analysis', 'code', 'render', 'merge'], online: true, unchanged: true });
    routes['POST /konseptler/c1/baglam/sahneler?regen=tok'] = ok(concept('video_ready', 'Yeni anlatım.'));
    routes['POST /uretilenler/video/c1/yeniden-uret'] = ok(active('queued'));
    open(concept());
    await flush();

    expect(el('concept-editor').hidden).toBe(false);
    expect(el('regen-panel').hidden).toBe(false);
    expect(el('regen-preview').textContent)
      .toBe('Tüm aşamalar baştan çalışır: Konsept inceleniyor, Video kodu üretiliyor, Render, Birleştiriliyor Online model çağrısı yapılacak. Konsept değişmedi.');
    const start = el('regen-start');
    expect(start.dataset.tooltip).toBe('Konsept değişmedi.');
    expect(el('regen-force-label').hidden).toBe(false);
    start.click();
    await flush();
    expect(el('regen-confirm-dialog').hidden).toBe(true);

    el('regen-force').checked = true;
    el('regen-force').dispatchEvent(new Event('change'));
    expect(start.hasAttribute('aria-disabled')).toBe(false);
    start.click();
    await flush();
    const dialog = el('regen-confirm-dialog');
    expect(dialog.hidden).toBe(false);
    expect(el('regen-confirm-stages').textContent).toBe('Çalışacak aşamalar: Konsept inceleniyor, Video kodu üretiliyor, Render, Birleştiriliyor.');
    key(dialog, 'Escape');
    await flush();
    expect(posts('/uretilenler/video/c1/yeniden-uret')).toHaveLength(0); // İptal: iş yok.

    // Metni düzenleyip kaydet: istek bağlam ucuna belirteçle gider, önizleme yenilenir.
    routes['GET /uretilenler/video/c1/yeniden-uret?belirtec=tok&taban=s1'] = ok({ stages: ['analysis', 'code', 'render', 'merge'], online: true, unchanged: false });
    const narration = document.querySelector('[data-field="narration"]');
    narration.value = 'Yeni anlatım.';
    narration.dispatchEvent(new Event('input', { bubbles: true }));
    expect(start.dataset.tooltip).toBe('Önce düzenlemeyi kaydedin.');
    el('concept-edit-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(posts('/konseptler/c1/baglam/sahneler?regen=tok')).toHaveLength(1);
    expect(el('regen-force-label').hidden).toBe(true); // Metin değişti: "Yine de baştan üret" sunulmaz.
    expect(el('regen-force').checked).toBe(false);
    expect(start.hasAttribute('aria-disabled')).toBe(false);
    start.click();
    await flush();
    dialog.querySelector('[data-confirm-ok]').click();
    await flush();
    expect(JSON.parse(posts('/uretilenler/video/c1/yeniden-uret')[0].data)).toEqual({ token: 'tok', baseVersionId: 's1', force: false, confirmOnline: true });
    expect(navigated).toBe('/uretilenler');
  });
});
