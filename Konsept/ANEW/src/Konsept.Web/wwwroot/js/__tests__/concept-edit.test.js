import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initConceptEdit, sceneIssues } from '../concept-edit.js';
import { initConceptWork } from '../concept-work.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Konseptler.cshtml'deki Üretilenler + düzenleme alanı + revizyon modalının en küçük karşılığı (metinler resx'teki değerler).
const WORK_TEXTS = {
  'request-failed': 'İşlem yapılamadı. Biraz sonra tekrar deneyin.',
  loading: 'Yükleniyor…',
  'load-failed': 'Konseptler alınamadı.',
  select: 'Seç: {0}',
  approve: 'Onayla: {0}',
  unapprove: 'Onayı geri al: {0}',
  page: 'Sayfa {0}/{1}',
  'scene-heading': 'Sahne {0}: {1}',
  'scene-visual': 'Ekran',
  'scene-narration': 'Seslendirme',
  'scene-duration': 'Süre: {0} sn',
  'detail-failed': 'Konsept alınamadı.',
  'status-generated': 'Üretildi',
  'status-approved': 'Onaylandı',
  'status-queued': 'Sırada',
  'status-video_ready': 'Video oldu',
  edit: 'Düzenle: {0}',
  'edit-locked-queue': 'Önce üretim sırasından çıkarın',
  'edit-locked-video': "Bu konseptin videosu var; metin değişikliği Üretilen Videolar'daki 'Yeniden üret' ile yapılır",
  'link-not-found': 'Konsept bulunamadı.',
};
const EDIT_TEXTS = {
  scene: 'Sahne {0}',
  title: 'Sahne başlığı',
  visual: 'Ekran',
  narration: 'Seslendirme',
  duration: 'Süre (saniye)',
  'title-required': 'Sahne başlığını yazın.',
  'narration-required': 'Seslendirme metnini yazın.',
  'duration-invalid': 'Süre pozitif tam saniye olmalı.',
  invalid: 'Bazı alanlar geçersiz; alanların yanındaki uyarılara bakın.',
  saving: 'Kaydediliyor…',
  saved: 'Kaydedildi.',
  'save-failed': 'Kaydedilemedi. Metniniz korundu; biraz sonra tekrar deneyin.',
  'instruction-required': 'Neyin düzeltilmesini istediğinizi yazın.',
  regenerating: 'Yeniden üretiliyor…',
  'regenerate-failed': 'Yeniden üretilemedi; konsept değişmedi.',
  'request-failed': 'İşlem yapılamadı.',
  approved: 'Revizyon onaylandı; konsept güncellendi.',
  'scene-heading': 'Sahne {0}: {1}',
  'scene-duration': 'Süre: {0} sn',
};

const attrs = (texts) => Object.entries(texts).map(([k, v]) => `data-text-${k}="${v}"`).join(' ');

function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="concept-work" ${attrs(WORK_TEXTS)}>
      <form id="topic-form"><input id="topic-count" value="10" /><button type="submit" id="topic-generate">Konu üret</button><p id="topic-count-error"></p></form>
      <p id="topic-progress"></p><button type="button" id="topic-stop" hidden></button><ul id="topic-notes"></ul><p id="topic-empty"></p><ul id="topic-list"></ul>
      <p id="topic-produce-error"></p><button type="button" id="topic-produce"></button>
      <form id="idea-form"><textarea id="idea-text"></textarea><p id="idea-text-error"></p><input id="idea-count" value="5" />
        <button type="submit" id="idea-generate"></button><p id="idea-count-error"></p></form>
      <p id="idea-progress"></p><button type="button" id="idea-stop" hidden></button><ul id="idea-notes"></ul>
      <fieldset id="concept-filter"><input type="checkbox" value="generated" checked /><input type="checkbox" value="approved" checked /></fieldset>
      <p id="concepts-empty"></p><p id="concepts-status" role="status"></p><ul id="concept-list"></ul>
      <button type="button" id="concepts-approve-selected" disabled></button>
      <span id="concepts-pager" hidden><button type="button" id="concepts-prev"></button><span id="concepts-page"></span><button type="button" id="concepts-next"></button></span>
      <section>
        <p id="concept-detail-hint">Okumak için Üretilenler'den bir konsept seçin.</p><article id="concept-detail" tabindex="-1" hidden></article>
        <div id="concept-editor" hidden ${attrs(EDIT_TEXTS)}>
          <h3 id="concept-edit-heading"></h3>
          <form id="concept-edit-form" novalidate><div id="concept-edit-scenes"></div><p id="concept-edit-status" role="status"></p>
            <button type="submit" id="concept-edit-save">Kaydet</button><button type="button" id="concept-edit-close">Düzenlemeyi kapat</button></form>
          <label for="concept-instruction">Ne düzeltilsin?</label><textarea id="concept-instruction"></textarea>
          <p id="concept-instruction-error" role="alert"></p><button type="button" id="concept-regenerate">Yeniden üret</button>
          <p id="concept-regen-status" role="status"></p>
        </div>
      </section>
    </div>
    <div id="revision-modal" hidden>
      <div role="dialog" aria-modal="true" aria-labelledby="revision-title">
        <h2 id="revision-title" tabindex="-1">Konsept revizyonu</h2>
        <p id="revision-warning" hidden>Kelime tekrarı</p><p id="revision-duration-warning" hidden>Süre uyumsuz</p>
        <ol id="revision-scenes"></ol><p id="revision-status" role="status"></p>
        <button type="button" id="revision-approve">Onayla</button><button type="button" id="revision-discard">Sil</button>
        <textarea id="revision-instruction"></textarea><p id="revision-instruction-error" role="alert"></p>
        <button type="button" id="revision-again">Tekrar düzenle</button>
        <div id="revision-confirm" role="alertdialog" hidden><p>Revizyon silinsin mi?</p>
          <button type="button" id="revision-confirm-cancel">Vazgeç</button><button type="button" id="revision-confirm-ok">Evet, sil</button></div>
      </div>
    </div>
    <div id="concept-leave" hidden><div role="alertdialog"><p>Kaydedilmemiş değişiklikler kaybolacak.</p>
      <button type="button" id="concept-leave-cancel">Vazgeç</button><button type="button" id="concept-leave-ok">Değişiklikleri at</button></div></div>`;
}

const LIST_URL = '/konseptler/liste/p1?durumlar=generated%2Capproved&sayfa=1';
const row = (id, title, status = 'generated') => ({ id, title, status, wordRepetitionWarning: false, durationMismatchWarning: false, createdAt: '2026-11-16T10:00:00Z' });
const detail = (id, extra = {}) => ({
  id,
  title: `Konsept ${id}`,
  status: 'generated',
  scenes: [
    { title: 'Açılış', visual: 'Surlar', narration: 'Sabah başlıyor.', durationSeconds: 20 },
    { title: 'Son', visual: '', narration: 'Bir çağ kapanıyor.', durationSeconds: 40 },
  ],
  wordRepetitionWarning: false,
  durationMismatchWarning: false,
  ...extra,
});
const revision = (status, extra = {}) => ({
  conceptId: 'c1',
  id: 'r1',
  status,
  scenes: status === 'ready' ? [{ title: 'Yeni', visual: '<img src=x>', narration: 'Revize anlatım.', durationSeconds: 60 }] : [],
  wordRepetitionWarning: false,
  durationMismatchWarning: false,
  error: null,
  ...extra,
});

let routes;
let calls;
let editors = [];
let timers;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 6; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = () => calls.filter((c) => c.method === 'POST');
const body = (call) => JSON.parse(call.data);
const input = (index, name) => el(`concept-edit-${index}-${name}`);
const type = (node, value) => {
  node.value = value;
  node.dispatchEvent(new Event('input', { bubbles: true }));
};
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));

async function start(items = [row('c1', 'Birinci'), row('c2', 'İkinci')]) {
  routes['GET /konsept-uretim/p1'] = ok({ profileId: 'p1', readiness: { topics: true, fromTopics: true, idea: true }, suggestions: [] });
  routes[`GET ${LIST_URL}`] = ok({ items, page: 1, pageSize: 50, total: items.length });
  routes['GET /konseptler/detay/c1'] = ok(detail('c1'));
  routes['GET /konseptler/detay/c2'] = ok(detail('c2'));
  routes['GET /konseptler/c1/revizyon'] = ok({ conceptId: 'c1', id: null, status: 'none', scenes: [] });
  routes['GET /konseptler/c2/revizyon'] = ok({ conceptId: 'c2', id: null, status: 'none', scenes: [] });
  const options = { setTimer: (fn, ms) => { timers.push({ fn, ms }); return timers.length; }, clearTimer: () => {} };
  const edit = initConceptEdit(document, { ...options, win: null });
  editors.push(edit);
  initConceptWork(document, options);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
  return edit;
}

async function openEditor(id = 'c1') {
  el('concept-list').querySelector(`[data-edit="${id}"]`).click();
  await flush();
}

describe('Konsept düzenleme döngüsü (spec 0015)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    timers = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  afterEach(() => {
    editors.forEach((e) => e.dispose());
    editors = [];
    globalThis.history.replaceState(null, '', '/');
  });

  it('AC-3: istemci kuralı sunucununkiyle aynı — başlık/seslendirme dolu, süre pozitif tam sayı, ekran tarifi boş olabilir', () => {
    expect(sceneIssues([{ title: 'B', visual: '', narration: 'N', durationSeconds: '5' }])).toEqual([]);
    expect(sceneIssues([{ title: ' ', visual: 'V', narration: ' ', durationSeconds: '0' }])).toEqual(['0.title', '0.narration', '0.durationSeconds']);
    expect(sceneIssues([{ title: 'B', narration: 'N', durationSeconds: '1.5' }, { title: 'B', narration: 'N', durationSeconds: '-2' }]))
      .toEqual(['0.durationSeconds', '1.durationSeconds']);
  });

  it('AC-1/AC-15: kalem Üretildi/Onaylandı\'da etkin ve adı "Düzenle: <başlık>"; Sırada/Video oldu\'da pasif, açıklaması okunur ve istek gitmez', async () => {
    await start([row('c1', 'Birinci'), row('c3', 'Üçüncü', 'queued'), row('c4', 'Dördüncü', 'video_ready')]);
    const pencils = [...el('concept-list').querySelectorAll('[data-edit]')];
    expect(pencils.map((p) => p.getAttribute('aria-label'))).toEqual(['Düzenle: Birinci', 'Düzenle: Üçüncü', 'Düzenle: Dördüncü']);
    expect(pencils[0].hasAttribute('aria-disabled')).toBe(false);
    expect(pencils[1].getAttribute('aria-disabled')).toBe('true');
    expect(el(pencils[1].getAttribute('aria-describedby')).textContent).toBe('Önce üretim sırasından çıkarın');
    expect(el(pencils[2].getAttribute('aria-describedby')).textContent).toContain("'Yeniden üret' ile yapılır");
    const before = calls.length;
    pencils[1].click();
    pencils[2].click();
    await flush();
    expect(calls.length).toBe(before);
    expect(el('concept-editor').hidden).toBe(true);
  });

  it('AC-2/AC-3: kalemle form açılır (ekle/sil/sırala yok); kaydederken "Kaydediliyor…", sonra detay ve liste güncellenir; hata metni korur', async () => {
    await start();
    await openEditor();
    expect(el('concept-editor').hidden).toBe(false);
    expect(el('concept-detail').hidden).toBe(true);
    expect(el('concept-edit-scenes').querySelectorAll('button').length).toBe(0);
    expect(document.activeElement).toBe(input(0, 'title'));
    type(input(1, 'narration'), 'Değişen anlatım.');

    let resolveSave;
    routes['POST /konseptler/c1/sahneler'] = () => $.Deferred((d) => { resolveSave = d; }).promise();
    el('concept-edit-form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await flush();
    expect(el('concept-edit-status').textContent).toBe('Kaydediliyor…');
    expect(body(posts()[0]).scenes[1]).toEqual({ title: 'Son', visual: '', narration: 'Değişen anlatım.', durationSeconds: 40 });
    const listCalls = calls.filter((c) => c.url === LIST_URL).length;
    resolveSave.resolve(detail('c1', { status: 'approved', scenes: body(posts()[0]).scenes }));
    await flush();
    expect(el('concept-edit-status').textContent).toBe('Kaydedildi.');
    expect(el('concept-detail').textContent).toContain('Değişen anlatım.');
    expect(calls.filter((c) => c.url === LIST_URL).length).toBeGreaterThan(listCalls);

    type(input(0, 'narration'), 'Başka metin.');
    routes['POST /konseptler/c1/sahneler'] = fail(500, null);
    el('concept-edit-save').click();
    await flush();
    expect(el('concept-edit-status').textContent).toBe('Kaydedilemedi. Metniniz korundu; biraz sonra tekrar deneyin.');
    expect(input(0, 'narration').value).toBe('Başka metin.');

    routes['POST /konseptler/c1/sahneler'] = fail(400, { message: 'Girilen bilgiler geçersiz.', errors: { 'Scenes[1].durationSeconds': ['Süre pozitif tam saniye olmalı.'] } });
    el('concept-edit-save').click();
    await flush();
    expect(el('concept-edit-1-durationSeconds-error').textContent).toBe('Süre pozitif tam saniye olmalı.');
  });

  it('AC-3: boş başlık, boş seslendirme ve geçersiz süre alanın yanında hata gösterir ve istek gitmez', async () => {
    await start();
    await openEditor();
    type(input(0, 'title'), '  ');
    type(input(1, 'narration'), '');
    type(input(1, 'durationSeconds'), '2.5');
    el('concept-edit-save').click();
    await flush();
    expect(el('concept-edit-0-title-error').textContent).toBe('Sahne başlığını yazın.');
    expect(el('concept-edit-1-narration-error').textContent).toBe('Seslendirme metnini yazın.');
    expect(el('concept-edit-1-durationSeconds-error').textContent).toBe('Süre pozitif tam saniye olmalı.');
    expect(posts().length).toBe(0);
  });

  it('AC-4: kaydedilmemiş değişiklikle başka konsepte geçiş uyarır; vazgeç düzenlemeyi korur, onay değişiklikleri atar; Yeniden üret değişiklikleri gönderir', async () => {
    await start();
    await openEditor();
    type(input(0, 'narration'), 'Kaydedilmemiş anlatım');
    const detailCalls = () => calls.filter((c) => c.url === '/konseptler/detay/c2').length;
    el('concept-list').querySelector('[data-open="c2"]').click();
    await flush();
    expect(el('concept-leave').hidden).toBe(false);
    expect(detailCalls()).toBe(0);
    el('concept-leave-cancel').click();
    expect(el('concept-leave').hidden).toBe(true);
    expect(input(0, 'narration').value).toBe('Kaydedilmemiş anlatım');

    routes['POST /konseptler/c1/yeniden-uret'] = ok(revision('running'));
    type(el('concept-instruction'), 'Kısalt');
    el('concept-regenerate').click();
    await flush();
    expect(body(posts()[0])).toMatchObject({ instruction: 'Kısalt' });
    expect(body(posts()[0]).scenes[0].narration).toBe('Kaydedilmemiş anlatım');

    el('concept-list').querySelector('[data-open="c2"]').click();
    await flush();
    el('concept-leave-ok').click();
    await flush();
    expect(detailCalls()).toBe(1);
    expect(el('concept-editor').hidden).toBe(true);
  });

  it('AC-5/AC-8/AC-12: boş talimat reddedilir; yeniden üretilirken alanlar pasif ve duyurulur, çift tıklama tek istek; hata revizyon göstermez', async () => {
    await start();
    await openEditor();
    el('concept-regenerate').click();
    await flush();
    expect(el('concept-instruction-error').textContent).toBe('Neyin düzeltilmesini istediğinizi yazın.');
    type(el('concept-instruction'), '   ');
    el('concept-regenerate').click();
    await flush();
    expect(posts().length).toBe(0);

    routes['POST /konseptler/c1/yeniden-uret'] = ok(revision('running'));
    type(el('concept-instruction'), 'İkinci sahneyi kısalt');
    el('concept-regenerate').click();
    el('concept-regenerate').click();
    await flush();
    expect(posts().length).toBe(1);
    expect(body(posts()[0])).toEqual({ instruction: 'İkinci sahneyi kısalt' });
    expect(el('concept-instruction').disabled).toBe(true);
    expect(el('concept-regenerate').disabled).toBe(true);
    expect(el('concept-regen-status').textContent).toBe('Yeniden üretiliyor…');
    expect(el('concept-regen-status').getAttribute('role')).toBe('status');

    routes['GET /konseptler/c1/revizyon'] = ok(revision('failed', { error: 'Model çıktısı konsept biçimine uymadı; üretilemedi.' }));
    timers.pop().fn();
    await flush();
    expect(el('revision-modal').hidden).toBe(true);
    expect(el('concept-regen-status').textContent).toBe('Model çıktısı konsept biçimine uymadı; üretilemedi.');
    expect(el('concept-regenerate').disabled).toBe(false);
    expect(timers.length).toBe(0); // Otomatik yeni deneme yok.
    expect(posts().length).toBe(1);
  });

  it('AC-6/AC-7/AC-9/AC-10/AC-15: revizyon modalı odak alır, dışına tıklama kapatmaz, Esc onay sorar; Tekrar düzenle aynı modalda; Sil ve Onayla', async () => {
    await start();
    await openEditor();
    routes['POST /konseptler/c1/yeniden-uret'] = ok(revision('running'));
    type(el('concept-instruction'), 'Düzelt');
    el('concept-regenerate').focus();
    el('concept-regenerate').click();
    await flush();
    routes['GET /konseptler/c1/revizyon'] = ok(revision('ready', { wordRepetitionWarning: true }));
    timers.pop().fn();
    await flush();

    expect(el('revision-modal').hidden).toBe(false);
    expect(document.activeElement).toBe(el('revision-title'));
    expect(el('revision-warning').hidden).toBe(false);
    expect(el('revision-scenes').textContent).toContain('Revize anlatım.');
    expect(el('revision-scenes').querySelector('img')).toBeNull(); // Model çıktısı düz metin.

    el('revision-modal').click();
    expect(el('revision-modal').hidden).toBe(false);
    key(el('revision-approve'), 'Escape');
    expect(el('revision-confirm').hidden).toBe(false);
    expect(document.activeElement).toBe(el('revision-confirm-cancel'));
    el('revision-confirm-cancel').click();
    expect(el('revision-confirm').hidden).toBe(true);
    expect(el('revision-modal').hidden).toBe(false);

    // Tekrar düzenle: boş talimat reddedilir; dolu talimat yeni revizyonu aynı modalda gösterir (sahne gönderilmez).
    el('revision-again').click();
    await flush();
    expect(el('revision-instruction-error').textContent).toBe('Neyin düzeltilmesini istediğinizi yazın.');
    type(el('revision-instruction'), 'Daha kısa');
    el('revision-again').click();
    await flush();
    expect(body(posts()[1])).toEqual({ instruction: 'Daha kısa' });
    expect(el('revision-status').textContent).toBe('Yeniden üretiliyor…');
    routes['GET /konseptler/c1/revizyon'] = ok(revision('ready', { id: 'r2', scenes: [{ title: 'İkinci tur', visual: '', narration: 'Tur iki.', durationSeconds: 60 }] }));
    timers.pop().fn();
    await flush();
    expect(el('revision-modal').hidden).toBe(false);
    expect(el('revision-scenes').textContent).toContain('Tur iki.');

    // Esc → onay → revizyon silinir, konsept aynen; odak açan kontrole döner.
    routes['POST /konseptler/c1/revizyon/sil'] = ok({ conceptId: 'c1', status: 'none', scenes: [] });
    key(el('revision-approve'), 'Escape');
    el('revision-confirm-ok').click();
    await flush();
    expect(posts()[2].url).toBe('/konseptler/c1/revizyon/sil');
    expect(el('revision-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(el('concept-regenerate'));

    // Yeni tur → Onayla: konsept güncellenir, detay ve liste yenilenir.
    el('concept-regenerate').click();
    await flush();
    routes['GET /konseptler/c1/revizyon'] = ok(revision('ready'));
    timers.pop().fn();
    await flush();
    routes['POST /konseptler/c1/revizyon/onayla'] = ok(detail('c1', { status: 'approved', scenes: revision('ready').scenes }));
    el('revision-approve').click();
    await flush();
    expect(el('revision-modal').hidden).toBe(true);
    expect(input(0, 'narration').value).toBe('Revize anlatım.');
    expect(el('concept-edit-status').textContent).toBe('Revizyon onaylandı; konsept güncellendi.');
    expect(el('concept-detail').textContent).toContain('Revize anlatım.');
  });

  it('AC-13: /?konsept=<id> o konsepti düzenleme alanında açar; bulunamazsa sade mesaj ve sayfa normal açılır', async () => {
    globalThis.history.replaceState(null, '', '/?konsept=c2');
    await start();
    expect(calls.some((c) => c.url === '/konseptler/detay/c2')).toBe(true);
    expect(el('concept-editor').hidden).toBe(false);
    expect(el('concept-edit-heading').textContent).toBe('Konsept c2');

    document.body.innerHTML = pageHtml();
    calls = [];
    globalThis.history.replaceState(null, '', '/?konsept=yok');
    await start();
    expect(el('concept-detail-hint').textContent).toBe('Konsept bulunamadı.');
    expect(el('concept-list').querySelectorAll('.concept-row').length).toBe(2);
  });
});
