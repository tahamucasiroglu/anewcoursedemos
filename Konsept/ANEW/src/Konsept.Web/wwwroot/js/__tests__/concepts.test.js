import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { effortLabel, initConcepts, roleSummary } from '../concepts.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Konseptler.cshtml'deki şerit + model modalının en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  const role = (code, label) => `<span data-role="${code}">${label}: <span data-role-value>seçilmedi</span></span>`;
  const group = (name, label) => `<span data-group="${name}">${label}: <span data-group-value>Belirtilmedi</span></span>`;
  const radios = (name, codes) => [''].concat(codes)
    .map((c) => `<label><input type="radio" name="${name}" value="${c}" ${c === '' ? 'checked' : ''} /> ${c === '' ? 'Belirtilmedi' : `L-${c}`}</label>`).join('');
  const modalRole = (code, label) => `
    <div class="field" data-role="${code}">
      <label for="concept-model-${code}">${label} modeli</label>
      <select id="concept-model-${code}" data-model-select><option value="">seçilmedi</option></select>
      <span data-effort-field hidden><label for="concept-effort-${code}">${label} eforu</label><select id="concept-effort-${code}" data-effort-select></select></span>
    </div>`;
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <button type="button" id="profile-new">+</button>
    <div data-requires-no-profile hidden><button type="button" id="concepts-add-profile">Profil ekle</button></div>
    <section id="concept-strip" data-state="loading" data-url="/konsept-ayarlari" data-text-loading="Ayarlar yükleniyor…"
             data-text-load-failed="Ayarlar alınamadı. Sayfayı yenileyip tekrar deneyin." data-text-saving="Kaydediliyor…"
             data-text-saved="Kaydedildi" data-text-save-failed="Kaydedilemedi, önceki seçim korundu. Biraz sonra tekrar deneyin."
             data-text-not-selected="seçilmedi" data-text-unspecified="Belirtilmedi">
      <button type="button" id="concept-models-open" disabled>${role('concept', 'Konsept')}${role('dedupe', 'Tekrar denetimi')}${role('topic', 'Konu önerisi')}</button>
      <button type="button" id="concept-finetune-toggle" aria-expanded="false" disabled>${group('tone', 'Ton')}${group('audience', 'İzleyici')}${group('narration', 'Anlatım biçimi')}</button>
      <label><input type="checkbox" id="concept-wordcheck" disabled /> Kelime denetimi</label>
      <p id="concept-settings-status" role="status">Ayarlar yükleniyor…</p>
      <div id="concept-finetune" hidden>
        <fieldset>${radios('tone', ['mysterious', 'calm'])}</fieldset>
        <fieldset>${radios('audience', ['family', 'expert'])}</fieldset>
        <fieldset>${radios('narration', ['list', 'what_if'])}</fieldset>
      </div>
    </section>
    <div id="concept-models-modal" hidden>
      <div role="dialog" aria-modal="true" aria-labelledby="concept-models-title">
        <h2 id="concept-models-title">Model bilgisi</h2>
        <button type="button" id="concept-models-close" aria-label="Kapat ve kaydet">×</button>
        <div id="concept-models-empty" hidden><p>Seçilebilir model yok.</p><a href="/ayarlar">Ayarlar'a git</a></div>
        ${modalRole('concept', 'Konsept üretimi')}${modalRole('dedupe', 'Anlamca tekrar denetimi')}${modalRole('topic', 'Konu önerisi')}
        <p id="concept-models-status" role="status"></p>
      </div>
    </div>`;
}

const OPUS = { id: 'anthropic/claude-opus-4-5', name: 'Claude Opus 4.5', effortLevels: ['low', 'medium', 'high'], defaultEffort: 'high' };
const SONNET = { id: 'anthropic/claude-sonnet-4-5', name: 'Claude Sonnet 4.5', effortLevels: [], defaultEffort: null };
const GPT = { id: 'openai/gpt-5-mini', name: 'GPT-5 mini', effortLevels: ['low', 'medium', 'high'], defaultEffort: 'medium' };

function settingsOf(extra = {}) {
  return {
    profileId: 'p1',
    roles: [
      { role: 'concept', modelId: OPUS.id, modelName: OPUS.name, effort: 'high' },
      { role: 'dedupe', modelId: SONNET.id, modelName: SONNET.name, effort: null },
      { role: 'topic', modelId: null, modelName: null, effort: null },
    ],
    tone: 'calm',
    audience: null,
    narration: null,
    wordCheck: false,
    models: [OPUS, SONNET, GPT],
    ...extra,
  };
}

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const pending = () => () => $.Deferred().promise();
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));
const key = (target, name, extra = {}) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, ...extra }));
const posts = () => calls.filter((c) => c.method === 'POST');
const body = (call) => JSON.parse(call.data);
const strip = (role) => document.querySelector(`#concept-strip [data-role="${role}"] [data-role-value]`).textContent;
const group = (name) => document.querySelector(`[data-group="${name}"] [data-group-value]`).textContent;
const modalShown = () => !el('concept-models-modal').hidden;
const change = (node, value) => {
  if (value !== undefined) {
    node.value = value;
  }
  node.dispatchEvent(new Event('change', { bubbles: true }));
};

async function start(settings = settingsOf()) {
  routes['GET /konsept-ayarlari/p1'] = ok(settings);
  const controller = initConcepts(document);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
  return controller;
}

describe('Konseptler şeridi (spec 0005)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-1: özet biçimi — model + efor, efor yoksa yalnızca ad, seçim yoksa "seçilmedi"', () => {
    expect(effortLabel('high')).toBe('High');
    expect(roleSummary({ modelId: 'x', modelName: 'Claude Opus 4.5', effort: 'high' }, 'seçilmedi')).toBe('Claude Opus 4.5 High');
    expect(roleSummary({ modelId: 'x', modelName: 'GLM-4.6', effort: null }, 'seçilmedi')).toBe('GLM-4.6');
    expect(roleSummary({ modelId: null }, 'seçilmedi')).toBe('seçilmedi');
  });

  it('AC-1/AC-7/AC-9: seçili profilin ayarları $.ajax ile yüklenir; yüklenirken durum metni ve kontroller pasif; şerit seçimleri gösterir', async () => {
    routes['GET /konsept-ayarlari/p1'] = pending();
    initConcepts(document);
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('concept-settings-status').textContent).toBe('Ayarlar yükleniyor…');
    expect(el('concept-models-open').disabled).toBe(true);

    document.body.innerHTML = pageHtml();
    await start(settingsOf({ wordCheck: true }));
    expect(calls[calls.length - 1]).toMatchObject({ method: 'GET', url: '/konsept-ayarlari/p1' });
    expect(strip('concept')).toBe('Claude Opus 4.5 High');
    expect(strip('dedupe')).toBe('Claude Sonnet 4.5');
    expect(strip('topic')).toBe('seçilmedi');
    expect(group('tone')).toBe('L-calm');
    expect(group('audience')).toBe('Belirtilmedi');
    expect(el('concept-wordcheck').checked).toBe(true);
    expect(el('concept-models-open').disabled).toBe(false);
    expect(el('concept-settings-status').textContent).toBe('');
  });

  it('AC-7: ayarlar alınamazsa sade Türkçe mesaj; kontroller pasif kalır', async () => {
    routes['GET /konsept-ayarlari/p1'] = fail(502, { message: 'x' });
    initConcepts(document);
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('concept-settings-status').textContent).toBe('Ayarlar alınamadı. Sayfayı yenileyip tekrar deneyin.');
    expect(el('concept-settings-status').dataset.kind).toBe('error');
    expect(el('concept-models-open').disabled).toBe(true);
    expect(el('concept-wordcheck').disabled).toBe(true);
  });

  it('AC-2/AC-9: modal açılır, odak modala geçer; yalnızca gelen modeller listelenir; efor yalnızca desteklenen düzeyler, efor yoksa gizli', async () => {
    await start();
    el('concept-models-open').focus();
    el('concept-models-open').click();

    expect(modalShown()).toBe(true);
    expect(el('concept-models-modal').contains(document.activeElement)).toBe(true);
    expect([...el('concept-model-concept').options].map((o) => o.value)).toEqual(['', OPUS.id, SONNET.id, GPT.id]);
    expect(el('concept-model-concept').value).toBe(OPUS.id);
    expect([...el('concept-effort-concept').options].map((o) => o.textContent)).toEqual(['Low', 'Medium', 'High']);
    expect(el('concept-effort-concept').value).toBe('high');
    expect(el('concept-effort-dedupe').closest('[data-effort-field]').hidden).toBe(true);
    expect(el('concept-models-empty').hidden).toBe(true);
  });

  it('AC-2: model seçilince katalog varsayılan eforu gelir; kapat düğmesi kaydeder, şerit yeni seçimi gösterir, odak açan kontrole döner', async () => {
    await start();
    el('concept-models-open').focus();
    el('concept-models-open').click();
    change(el('concept-model-topic'), GPT.id);
    expect(el('concept-effort-topic').closest('[data-effort-field]').hidden).toBe(false);
    expect(el('concept-effort-topic').value).toBe('medium');
    change(el('concept-model-dedupe'), OPUS.id);
    expect(el('concept-effort-dedupe').value).toBe('high');
    change(el('concept-effort-dedupe'), 'low');

    const saved = settingsOf({
      roles: [
        { role: 'concept', modelId: OPUS.id, modelName: OPUS.name, effort: 'high' },
        { role: 'dedupe', modelId: OPUS.id, modelName: OPUS.name, effort: 'low' },
        { role: 'topic', modelId: GPT.id, modelName: GPT.name, effort: 'medium' },
      ],
    });
    routes['POST /konsept-ayarlari/p1'] = ok(saved);
    el('concept-models-close').click();
    await flush();

    // Yalnızca değişen roller gider (review r1 F-2; plan A-2).
    expect(body(posts()[0])).toEqual({
      models: [
        { role: 'dedupe', modelId: OPUS.id, effort: 'low' },
        { role: 'topic', modelId: GPT.id, effort: 'medium' },
      ],
    });
    expect(posts()[0].headers.RequestVerificationToken).toBe('test-token');
    expect(modalShown()).toBe(false);
    expect(strip('dedupe')).toBe('Claude Opus 4.5 Low');
    expect(strip('topic')).toBe('GPT-5 mini Medium');
    expect(document.activeElement).toBe(el('concept-models-open'));
    expect(el('concept-settings-status').textContent).toBe('Kaydedildi');
  });

  it('AC-2/AC-9: modal dışına tıklama ve Esc kapatır; değişiklik yoksa istek gitmez; değişiklik dışarı tıklamayla kaydedilir', async () => {
    await start();
    el('concept-models-open').click();
    key(el('concept-model-concept'), 'Escape');
    await flush();
    expect(modalShown()).toBe(false);
    expect(posts()).toHaveLength(0);

    el('concept-models-open').click();
    change(el('concept-model-concept'), '');
    routes['POST /konsept-ayarlari/p1'] = ok(settingsOf({ roles: [{ role: 'concept', modelId: null }, { role: 'dedupe', modelId: null }, { role: 'topic', modelId: null }] }));
    el('concept-models-modal').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    await flush();
    expect(body(posts()[0]).models[0]).toEqual({ role: 'concept', modelId: null, effort: null });
    expect(modalShown()).toBe(false);
    expect(strip('concept')).toBe('seçilmedi');
  });

  it('AC-9: odak modal içinde kalır (Tab / Shift+Tab)', async () => {
    await start();
    el('concept-models-open').click();
    el('concept-effort-topic').closest('[data-effort-field]').hidden = true;
    el('concept-model-topic').focus();
    key(el('concept-model-topic'), 'Tab');
    expect(document.activeElement).toBe(el('concept-models-close'));
    key(el('concept-models-close'), 'Tab', { shiftKey: true });
    expect(document.activeElement).toBe(el('concept-model-topic'));
  });

  it('AC-7: kaydetme hatasında sade mesaj, önceki seçim modalda ve şeritte korunur; modal açık kalır', async () => {
    await start();
    el('concept-models-open').click();
    change(el('concept-model-concept'), GPT.id);
    routes['POST /konsept-ayarlari/p1'] = fail(400, { message: 'Girilen bilgiler geçersiz.' });
    el('concept-models-close').click();
    await flush();

    expect(modalShown()).toBe(true);
    expect(el('concept-models-status').textContent).toBe('Kaydedilemedi, önceki seçim korundu. Biraz sonra tekrar deneyin.');
    expect(el('concept-model-concept').value).toBe(OPUS.id);
    expect(strip('concept')).toBe('Claude Opus 4.5 High');

    el('concept-models-close').click(); // Değişiklik kalmadı: kapanır, yeni istek yok.
    await flush();
    expect(modalShown()).toBe(false);
    expect(posts()).toHaveLength(1);
  });

  it('AC-3: seçilebilir model yoksa açıklama + Ayarlar bağlantısı, seçim kutuları pasif', async () => {
    await start(settingsOf({ roles: [{ role: 'concept', modelId: null }, { role: 'dedupe', modelId: null }, { role: 'topic', modelId: null }], models: [] }));
    el('concept-models-open').click();
    expect(el('concept-models-empty').hidden).toBe(false);
    expect(el('concept-models-empty').querySelector('a').getAttribute('href')).toBe('/ayarlar');
    expect(el('concept-model-concept').disabled).toBe(true);
    expect(el('concept-models-modal').contains(document.activeElement)).toBe(true);
  });

  it('AC-5/AC-9: ince ayar alanı açılır/kapanır (Esc); grupta tek seçim kaydedilir ve şeritte görünür; hata olursa önceki seçim geri gelir', async () => {
    await start();
    el('concept-finetune-toggle').click();
    expect(el('concept-finetune').hidden).toBe(false);
    expect(el('concept-finetune-toggle').getAttribute('aria-expanded')).toBe('true');
    expect(document.activeElement.value).toBe('calm');

    routes['POST /konsept-ayarlari/p1'] = ok(settingsOf({ tone: 'mysterious', audience: 'family' }));
    const family = document.querySelector('input[name="audience"][value="family"]');
    family.checked = true;
    change(family);
    await flush();
    expect(body(posts()[0])).toEqual({ fineTune: { tone: 'calm', audience: 'family', narration: null } });
    expect(group('tone')).toBe('L-mysterious');
    expect(group('audience')).toBe('L-family');
    expect(document.querySelectorAll('input[name="tone"]:checked')).toHaveLength(1);

    routes['POST /konsept-ayarlari/p1'] = fail(500, {});
    const list = document.querySelector('input[name="narration"][value="list"]');
    list.checked = true;
    change(list);
    await flush();
    expect(group('narration')).toBe('Belirtilmedi');
    expect(document.querySelector('input[name="narration"]:checked').value).toBe('');
    expect(el('concept-settings-status').textContent).toBe('Kaydedilemedi, önceki seçim korundu. Biraz sonra tekrar deneyin.');

    key(list, 'Escape');
    expect(el('concept-finetune').hidden).toBe(true);
    expect(document.activeElement).toBe(el('concept-finetune-toggle'));
  });

  it('AC-6: kelime denetimi işaretlenince ve kaldırılınca kaydedilir; hata olursa önceki durum geri gelir', async () => {
    await start();
    routes['POST /konsept-ayarlari/p1'] = ok(settingsOf({ wordCheck: true }));
    el('concept-wordcheck').checked = true;
    change(el('concept-wordcheck'));
    await flush();
    expect(body(posts()[0])).toEqual({ wordCheck: true });
    expect(el('concept-wordcheck').checked).toBe(true);

    routes['POST /konsept-ayarlari/p1'] = fail(502, {});
    el('concept-wordcheck').checked = false;
    change(el('concept-wordcheck'));
    await flush();
    expect(body(posts()[1])).toEqual({ wordCheck: false });
    expect(el('concept-wordcheck').checked).toBe(true);
  });

  it('AC-4: profil değişince (yeni olay) şerit o profilin ayarlarını yükler', async () => {
    await start();
    routes['GET /konsept-ayarlari/p2'] = ok(settingsOf({ profileId: 'p2', roles: [{ role: 'concept', modelId: null }, { role: 'dedupe', modelId: null }, { role: 'topic', modelId: null }], tone: null }));
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p2' } }));
    await flush();
    expect(strip('concept')).toBe('seçilmedi');
    expect(group('tone')).toBe('Belirtilmedi');
  });

  it('Review r1 F-2: artık seçilemeyen kayıtlı model, başka rol değiştirilince gönderilmez (silinmez)', async () => {
    await start(settingsOf({ roles: [
      { role: 'concept', modelId: OPUS.id, modelName: OPUS.name, effort: 'high' },
      { role: 'dedupe', modelId: 'glm/glm-4.6', modelName: 'GLM-4.6', effort: null },
      { role: 'topic', modelId: null, modelName: null, effort: null },
    ] }));
    expect(strip('dedupe')).toBe('GLM-4.6');
    el('concept-models-open').click();
    change(el('concept-model-topic'), SONNET.id);
    routes['POST /konsept-ayarlari/p1'] = ok(settingsOf());
    el('concept-models-close').click();
    await flush();
    expect(body(posts()[0])).toEqual({ models: [{ role: 'topic', modelId: SONNET.id, effort: null }] });
  });

  it('Review r1 F-1: şerit kayıtları sıralıdır; uçuştaki istek bitmeden gelen değişiklikler birleşip son değerle tek istekte gider', async () => {
    await start();
    const resolvers = [];
    routes['POST /konsept-ayarlari/p1'] = () => {
      const d = $.Deferred();
      resolvers.push(d);
      return d.promise();
    };
    const pick = (name, value) => {
      const radio = document.querySelector(`input[name="${name}"][value="${value}"]`);
      radio.checked = true;
      change(radio);
    };
    pick('tone', 'mysterious');
    pick('tone', '');
    el('concept-wordcheck').checked = true;
    change(el('concept-wordcheck'));
    pick('tone', 'calm');
    await flush();
    expect(posts()).toHaveLength(1); // İkinci istek ilk bitmeden gitmez.
    expect(body(posts()[0])).toEqual({ fineTune: { tone: 'mysterious', audience: null, narration: null } });

    resolvers[0].resolve(settingsOf({ tone: 'mysterious' })); // Eski yanıt şeridi eski değere çevirmez.
    await flush();
    expect(document.querySelector('input[name="tone"]:checked').value).toBe('calm');
    expect(posts()).toHaveLength(2);
    expect(body(posts()[1])).toEqual({ fineTune: { tone: 'calm', audience: null, narration: null }, wordCheck: true });

    resolvers[1].resolve(settingsOf({ tone: 'calm', wordCheck: true }));
    await flush();
    expect(group('tone')).toBe('L-calm');
    expect(el('concept-wordcheck').checked).toBe(true);
    expect(el('concept-settings-status').textContent).toBe('Kaydedildi');
  });

  it('Review r2 R2-1: kuyrukta hata sonra başarı — hata mesajı ve sunucudaki değer; şerit kaydı uçuştayken model kaydı bekler', async () => {
    await start();
    const resolvers = [];
    routes['POST /konsept-ayarlari/p1'] = () => {
      const d = $.Deferred();
      resolvers.push(d);
      return d.promise();
    };
    el('concept-wordcheck').checked = true;
    change(el('concept-wordcheck'));
    const calm = document.querySelector('input[name="audience"][value="expert"]');
    calm.checked = true;
    change(calm);
    resolvers[0].reject({ status: 500, responseJSON: {} });
    await flush();
    resolvers[1].resolve(settingsOf({ audience: 'expert' }));
    await flush();
    expect(el('concept-wordcheck').checked).toBe(false);
    expect(group('audience')).toBe('L-expert');
    expect(el('concept-settings-status').textContent).toBe('Kaydedilemedi, önceki seçim korundu. Biraz sonra tekrar deneyin.');

    el('concept-wordcheck').checked = true;
    change(el('concept-wordcheck'));
    el('concept-models-open').click();
    change(el('concept-model-topic'), GPT.id);
    el('concept-models-close').click();
    await flush();
    expect(posts()).toHaveLength(3); // Model kaydı şerit kaydı bitene kadar gitmez.
    resolvers[2].resolve(settingsOf({ wordCheck: true }));
    await flush();
    expect(posts()).toHaveLength(4);
    expect(body(posts()[3])).toEqual({ models: [{ role: 'topic', modelId: GPT.id, effort: 'medium' }] });
    resolvers[3].resolve(settingsOf({ wordCheck: true }));
    await flush();
    expect(modalShown()).toBe(false);
  });

  it('AC-7: profil yokken "Profil ekle" üst bardaki + kontrolünü çalıştırır', () => {
    initConcepts(document);
    const plus = vi.fn();
    el('profile-new').addEventListener('click', plus);
    el('concepts-add-profile').click();
    expect(plus).toHaveBeenCalledTimes(1);
  });
});
