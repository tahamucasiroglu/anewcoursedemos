import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initProduction, optionLabel } from '../production.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// KonsepttenVideo.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="production" data-text-loading="Yükleniyor…" data-text-failed="Sunucuya ulaşılamadı. Biraz sonra tekrar deneyin."
         data-text-correlation="(Korelasyon: {0})" data-text-saving="Kaydediliyor…" data-text-not-selected="seçilmedi"
         data-text-image-off="Kapalı — koddan çiz" data-text-audio-off="Kapalı — sessiz video" data-text-local="yerel" data-text-online="online"
         data-text-no-key="Key yok" data-text-no-permission="Yetki yok" data-text-unreadable="Key okunamıyor"
         data-text-only-turkish="Yalnız Türkçe" data-text-only-english="Yalnız İngilizce" data-text-license="Lisans uyarısı"
         data-text-status-queued="Sırada" data-text-status-producing="Üretiliyor" data-text-status-stopped="Durduruldu"
         data-text-status-waiting-for-agent="Ajan bekleniyor" data-text-status-failed="Hata" data-text-status-completed="Tamamlandı"
         data-text-move-up="{0}'ı yukarı taşı" data-text-move-down="{0}'ı aşağı taşı" data-text-remove="{0}'ı sıradan çıkar"
         data-text-resume="Kaldığı yerden devam et" data-text-select="{0} seç" data-text-page="Sayfa {0}/{1}"
         data-text-edit-in-concepts="Konseptler'de düzenle" data-text-scene-heading="Sahne {0}: {1}" data-text-scene-visual="Ekran"
         data-text-scene-narration="Seslendirme" data-text-scene-duration="Süre: {0} sn">
      <button type="button" id="production-models-open" disabled>
        <span id="production-summary-code"></span><span id="production-summary-engine"></span>
        <span id="production-summary-image"></span><span id="production-summary-audio"></span>
      </button>
      <p id="production-language-warning" hidden></p>
      <input type="checkbox" id="production-subtitles" disabled /><span id="production-subtitles-note" hidden>Sessiz videoda altyazı yok</span>
      <input type="checkbox" id="production-progress" disabled />
      <p id="production-status" role="status"></p>
      <p class="empty-state" id="production-approved-empty">Onaylanmış konsept yok — Konseptler sayfasından video için konsept seçin</p>
      <p id="production-approved-status"></p><ul id="production-approved"></ul>
      <span id="production-pager" hidden><button type="button" id="production-prev">Önceki</button><span id="production-page"></span><button type="button" id="production-next">Sonraki</button></span>
      <button type="button" id="production-enqueue" disabled>Üretim sırasına al</button>
      <p id="production-detail-hint">Ayrıntı için bir konsepte tıklayın</p><article id="production-detail" hidden></article>
      <button type="button" id="production-start" disabled>Sırayla üret</button><button type="button" id="production-stop" hidden>Durdur</button>
      <span id="production-block"></span>
      <p id="production-queue-empty">Sıra boş</p><ol id="production-queue"></ol>
    </div>
    <div id="production-models-modal" hidden>
      <div role="dialog" aria-modal="true">
        <button type="button" id="production-models-close" aria-label="Kapat">x</button>
        <p id="production-models-error" hidden>Model listesi yüklenemedi.</p>
        <select id="production-model-code"></select>
        <div id="production-effort-field" hidden><select id="production-model-effort"></select></div>
        <select id="production-model-engine"></select><select id="production-model-image"></select><select id="production-model-audio"></select>
        <p id="production-models-warning" hidden></p>
      </div>
    </div>`;
}

const model = (id, name, kind, extra = {}) => ({ id, name, kind, local: id.startsWith('local/'), languageOnly: null, licenseWarning: false, effortLevels: [], defaultEffort: null, availability: 'available', ...extra });
const MODELS = [
  model('anthropic/opus', 'Claude Opus', 'text', { effortLevels: ['low', 'medium', 'high'], defaultEffort: 'high' }),
  model('anthropic/sonnet', 'Claude Sonnet', 'text'),
  model('openai/mini', 'GPT mini', 'text', { availability: 'no_key' }),
  model('glm/x', 'GLM', 'text', { availability: 'no_permission' }),
  model('local/hyperframes', 'HyperFrames', 'video_engine'),
  model('local/remotion', 'Remotion', 'video_engine', { licenseWarning: true }),
  model('local/z-image-turbo', 'Z-Image-Turbo', 'image'),
  model('local/supertonic-3', 'Supertonic 3', 'audio', { languageOnly: 'tr' }),
];
const SETTINGS = { subtitles: true, progressBar: false, codeModel: 'anthropic/opus', codeEffort: 'high', engine: 'local/hyperframes', imageModel: null, audioModel: 'local/supertonic-3' };
const job = (id, title, number, status) => ({ id, conceptId: `c-${id}`, title, number, status, language: 'tr', fixed: null });
const stateOf = (extra = {}) => ({
  profileId: 'p1', settings: { ...SETTINGS }, models: MODELS, modelsFailed: false, jobs: [], startBlock: 'empty', startBlockMessage: 'Sıra boş',
  languageWarnings: [], runningHere: false, ...extra,
});
const list = (items) => ({ items, page: 1, pageSize: 50, total: items.length });

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const pending = () => () => $.Deferred().promise();
const flush = async () => {
  for (let i = 0; i < 8; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = () => calls.filter((c) => c.method === 'POST');
const body = (call) => JSON.parse(call.data);
const key = (target, name, extra = {}) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true, ...extra }));

async function start(state = stateOf(), approved = []) {
  routes['GET /uretim/p1'] = ok(state);
  routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok(list(approved));
  initProduction(document);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
}

describe('Konseptten Video (spec 0006)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/konseptten-video', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-3: seçenek etiketleri yerel/online, Key yok, tek dil ve Lisans uyarısı', () => {
    const text = el('production') ? el('production').dataset : {};
    expect(optionLabel(MODELS[7], text)).toBe('Supertonic 3 — yerel · Yalnız Türkçe');
    expect(optionLabel(MODELS[2], text)).toBe('GPT mini — online · Key yok');
    expect(optionLabel(MODELS[5], text)).toBe('Remotion — yerel · Lisans uyarısı');
    expect(optionLabel({ ...MODELS[1], languageOnly: 'en' }, text)).toBe('Claude Sonnet — online · Yalnız İngilizce');
  });

  it('AC-1/AC-21: özet modalı açar, odak modala geçer, Esc kapatır ve kaydeder, odak açana döner, özet yeni seçimi gösterir', async () => {
    await start();
    expect(el('production-summary-code').textContent).toBe('Claude Opus High');
    expect(el('production-summary-image').textContent).toBe('Kapalı — koddan çiz');
    const opener = el('production-models-open');
    opener.focus();
    opener.click();
    expect(el('production-models-modal').hidden).toBe(false);
    expect(el('production-models-modal').contains(document.activeElement)).toBe(true);
    expect([...el('production-model-engine').options].map((o) => o.value)).toEqual(['local/hyperframes', 'local/remotion']);
    expect([...el('production-model-image').options].map((o) => o.textContent)).toEqual(['Kapalı — koddan çiz', 'Z-Image-Turbo — yerel']);
    expect([...el('production-model-audio').options][0].textContent).toBe('Kapalı — sessiz video');
    expect([...el('production-model-code').options].find((o) => o.value === 'glm/x').disabled).toBe(true); // AC-4
    el('production-model-image').value = 'local/z-image-turbo';
    routes['POST /uretim/p1/ayarlar'] = ok(stateOf({ settings: { ...SETTINGS, imageModel: 'local/z-image-turbo' } }));
    key(el('production-model-image'), 'Escape');
    await flush();
    expect(el('production-models-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(opener);
    expect(body(posts()[0])).toEqual({ ...SETTINGS, imageModel: 'local/z-image-turbo' });
    expect(el('production-summary-image').textContent).toBe('Z-Image-Turbo');
  });

  it('AC-1: dışına tıklama ve Kapat da kaydeder; değişiklik yoksa istek gitmez', async () => {
    await start();
    el('production-models-open').click();
    el('production-models-close').click();
    await flush();
    expect(posts()).toHaveLength(0);
    el('production-models-open').click();
    el('production-model-engine').value = 'local/remotion';
    routes['POST /uretim/p1/ayarlar'] = ok(stateOf({ settings: { ...SETTINGS, engine: 'local/remotion' } }));
    el('production-models-modal').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    await flush();
    expect(body(posts()[0]).engine).toBe('local/remotion');
    expect(el('production-summary-engine').textContent).toBe('Remotion');
  });

  it('AC-2: efor listesi modele göre; model değişince varsayılan efor, eforsuz modelde alan gizli', async () => {
    await start();
    el('production-models-open').click();
    expect([...el('production-model-effort').options].map((o) => o.value)).toEqual(['low', 'medium', 'high']);
    el('production-model-code').value = 'anthropic/sonnet';
    el('production-model-code').dispatchEvent(new Event('change'));
    expect(el('production-effort-field').hidden).toBe(true);
    el('production-model-code').value = 'anthropic/opus';
    el('production-model-code').dispatchEvent(new Event('change'));
    expect(el('production-model-effort').value).toBe('high');
  });

  it('AC-5: ses Kapalı iken Altyazı pasif ve açıklama görünür; ses gelince önceki değerle etkin', async () => {
    await start(stateOf({ settings: { ...SETTINGS, audioModel: null, subtitles: true } }));
    expect(el('production-subtitles').disabled).toBe(true);
    expect(el('production-subtitles').checked).toBe(false);
    expect(el('production-subtitles-note').hidden).toBe(false);
    routes['POST /uretim/p1/ayarlar'] = ok(stateOf());
    el('production-models-open').click();
    el('production-model-audio').value = 'local/supertonic-3';
    el('production-models-close').click();
    await flush();
    expect(el('production-subtitles').disabled).toBe(false);
    expect(el('production-subtitles').checked).toBe(true);
    expect(el('production-subtitles-note').hidden).toBe(true);
  });

  it('AC-6: dil uyarısı özet yanında ve modalda; Sırayla üret pasif ve nedeni yazılı', async () => {
    const warning = 'Seçili ses modeli konsept dilini desteklemiyor';
    await start(stateOf({ jobs: [job('j1', 'English', 1, 'queued')], startBlock: 'audio_language', startBlockMessage: warning, languageWarnings: [warning] }));
    expect(el('production-language-warning').hidden).toBe(false);
    expect(el('production-language-warning').textContent).toBe(warning);
    expect(el('production-models-warning').textContent).toBe(warning);
    expect(el('production-start').disabled).toBe(true);
    expect(el('production-block').textContent).toBe(warning);
  });

  it('AC-8/AC-9: onaylı liste yalnızca approved ister; ayrıntı düz metin, düzenlenebilir öğe yok, bağlantı Konseptler düzenleme akışına', async () => {
    routes['GET /konseptler/detay/c1'] = ok({ id: 'c1', title: '<b>Başlık</b>', scenes: [{ title: 'Açılış', visual: '<script>x()</script>', narration: 'Anlatım', durationSeconds: 30 }] });
    await start(stateOf(), [{ id: 'c1', title: 'Bizans' }]);
    expect(el('production-approved-empty').hidden).toBe(true);
    expect(calls.some((c) => c.url === '/konseptler/liste/p1?durumlar=approved&sayfa=1')).toBe(true);
    el('production-approved').querySelector('[data-open]').click();
    await flush();
    const detail = el('production-detail');
    expect(detail.hidden).toBe(false);
    expect(detail.querySelector('h3').textContent).toBe('<b>Başlık</b>');
    expect(detail.querySelector('b')).toBeNull();
    expect(detail.querySelector('script')).toBeNull();
    expect(detail.textContent).toContain('Ekran: <script>x()</script>');
    expect(detail.textContent).toContain('Süre: 30 sn');
    expect(detail.querySelector('input, textarea, select, [contenteditable]')).toBeNull();
    expect(detail.querySelector('a').getAttribute('href')).toBe('/?konsept=c1');
    expect(el('production-detail-hint').hidden).toBe(true);
  });

  it('AC-10: işaret yokken pasif; işaretlenenler görünüş sırasıyla sıraya gider, liste yenilenir', async () => {
    await start(stateOf(), [{ id: 'c1', title: 'Bir' }, { id: 'c2', title: 'İki' }, { id: 'c3', title: 'Üç' }]);
    expect(el('production-enqueue').disabled).toBe(true);
    const boxes = [...el('production-approved').querySelectorAll('input[type=checkbox]')];
    expect(boxes[0].getAttribute('aria-label')).toBe('Bir seç');
    boxes[2].click();
    boxes[0].click();
    expect(el('production-enqueue').disabled).toBe(false);
    routes['POST /uretim/sira'] = ok(stateOf({ jobs: [job('j1', 'Bir', 1, 'queued'), job('j3', 'Üç', 2, 'queued')], startBlock: null, startBlockMessage: null }));
    routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok(list([{ id: 'c2', title: 'İki' }]));
    el('production-enqueue').click();
    await flush();
    expect(body(posts()[0])).toEqual({ profileId: 'p1', conceptIds: ['c1', 'c3'] });
    expect([...el('production-approved').querySelectorAll('[data-open]')].map((n) => n.textContent)).toEqual(['İki']);
    expect(el('production-enqueue').disabled).toBe(true);
    expect(el('production-start').disabled).toBe(false);
  });

  it('AC-11/AC-13/AC-21: uçlarda Yukarı/Aşağı pasif; çalışan işte üçü de pasif; erişilebilir adlar; durum metinle', async () => {
    await start(stateOf({ jobs: [job('j1', 'A', 1, 'producing'), job('j2', 'B', 2, 'queued'), job('j3', 'C', 3, 'queued')], startBlock: 'running_here', startBlockMessage: 'Üretim sürüyor', runningHere: true }));
    const rows = [...el('production-queue').children];
    const buttons = (row) => [...row.querySelectorAll('button')];
    expect(buttons(rows[0]).every((b) => b.disabled)).toBe(true);
    expect(rows[0].querySelector('.production-queue__status').textContent).toBe('Üretiliyor');
    expect(buttons(rows[1])[0].disabled).toBe(true); // Üstteki çalışan işin üstüne çıkamaz.
    expect(buttons(rows[1])[0].getAttribute('aria-label')).toBe("B'ı yukarı taşı");
    expect(buttons(rows[2])[1].disabled).toBe(true);
    expect(buttons(rows[2])[0].disabled).toBe(false);
    expect(el('production-stop').hidden).toBe(false);
    expect(el('production-start').disabled).toBe(true);
    expect(el('production-block').textContent).toBe('Üretim sürüyor');
    routes['POST /uretim/is/j3/tasi'] = ok(stateOf({ jobs: [job('j1', 'A', 1, 'producing'), job('j3', 'C', 2, 'queued'), job('j2', 'B', 3, 'queued')], runningHere: true }));
    buttons(rows[2])[0].click();
    await flush();
    expect(body(posts()[0])).toEqual({ direction: 'up' });
    expect([...el('production-queue').querySelectorAll('.production-queue__title')].map((n) => n.textContent)).toEqual(['1. A', '2. C', '3. B']);
  });

  it('AC-12/AC-18: × çıkarır ve onaylı listeyi yeniler; Durdur; Durduruldu ve Hata işinde "Kaldığı yerden devam et"', async () => {
    await start(stateOf({ jobs: [job('j1', 'A', 1, 'stopped'), job('j2', 'B', 2, 'failed')] }));
    const resumes = el('production-queue').querySelectorAll('.production-queue__resume');
    expect([...resumes].map((b) => b.textContent)).toEqual(['Kaldığı yerden devam et', 'Kaldığı yerden devam et']);
    routes['POST /uretim/is/j1/devam'] = ok(stateOf({ jobs: [job('j1', 'A', 1, 'producing'), job('j2', 'B', 2, 'failed')], runningHere: true }));
    resumes[0].click();
    await flush();
    expect(el('production-stop').hidden).toBe(false);
    routes['POST /uretim/durdur'] = ok(stateOf({ jobs: [job('j1', 'A', 1, 'stopped'), job('j2', 'B', 2, 'failed')] }));
    el('production-stop').click();
    await flush();
    expect(body(posts()[1])).toEqual({ profileId: 'p1' });
    expect(el('production-stop').hidden).toBe(true);
    routes['POST /uretim/is/j2/cikar'] = ok(stateOf({ jobs: [job('j1', 'A', 1, 'stopped')] }));
    routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok(list([{ id: 'c-j2', title: 'B' }]));
    el('production-queue').children[1].querySelector('.production-queue__remove').click();
    await flush();
    expect(el('production-queue').children).toHaveLength(1);
    expect([...el('production-approved').querySelectorAll('[data-open]')].map((n) => n.textContent)).toEqual(['B']);
  });

  it('AC-15/AC-22: yükleniyor, boş sıra, sunucu hatası korelasyonla, model listesi hatası modalda ve Sırayla üret pasif', async () => {
    routes['GET /uretim/p1'] = pending();
    routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = pending();
    initProduction(document);
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('production-status').textContent).toBe('Yükleniyor…');
    expect(el('production-start').disabled).toBe(true);

    document.body.innerHTML = pageHtml();
    routes['GET /uretim/p1'] = fail(500, { message: 'x', correlationId: 'abc123' });
    routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok(list([]));
    initProduction(document);
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('production-status').textContent).toBe('Sunucuya ulaşılamadı. Biraz sonra tekrar deneyin. (Korelasyon: abc123)');
    expect(el('production-approved-empty').hidden).toBe(false);

    document.body.innerHTML = pageHtml();
    await start(stateOf({ models: [], modelsFailed: true, startBlock: 'empty', startBlockMessage: 'Sıra boş' }));
    expect(el('production-queue-empty').hidden).toBe(false);
    expect(el('production-block').textContent).toBe('Sıra boş');
    expect(el('production-start').disabled).toBe(true);
    el('production-models-open').click();
    expect(el('production-models-error').hidden).toBe(false);
  });

  it('AC-4: Api reddederse sade mesaj gösterilir ve özet eski seçimde kalır', async () => {
    await start();
    routes['POST /uretim/p1/ayarlar'] = fail(400, { message: 'Bu modeli kullanma yetkiniz yok' });
    el('production-progress').click();
    await flush();
    expect(el('production-status').textContent).toBe('Bu modeli kullanma yetkiniz yok');
    expect(el('production-progress').checked).toBe(false);
  });
});
