import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { costText, initProduction, POLL_MS } from '../production.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// KonsepttenVideo.cshtml'in 0017 kısmının en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="production" data-text-loading="Yükleniyor…" data-text-failed="Sunucuya ulaşılamadı. Biraz sonra tekrar deneyin."
         data-text-correlation="(Korelasyon: {0})" data-text-saving="Kaydediliyor…" data-text-not-selected="seçilmedi"
         data-text-image-off="Kapalı — koddan çiz" data-text-audio-off="Kapalı — sessiz video"
         data-text-status-queued="Sırada" data-text-status-producing="Üretiliyor" data-text-status-stopped="Durduruldu"
         data-text-status-waiting-for-agent="Ajan bekleniyor" data-text-status-failed="Hata"
         data-text-move-up="{0}'ı yukarı taşı" data-text-move-down="{0}'ı aşağı taşı" data-text-remove="{0}'ı sıradan çıkar"
         data-text-resume="Kaldığı yerden devam et" data-text-page="Sayfa {0}/{1}"
         data-text-stage-analysis="Konsept inceleniyor" data-text-stage-code="Video kodu üretiliyor" data-text-stage-images="Görseller üretiliyor"
         data-text-stage-voice="Seslendirme yapılıyor" data-text-stage-render="Video kodu videoya dönüştürülüyor (render)" data-text-stage-merge="Birleştiriliyor"
         data-text-state-pending="bekliyor" data-text-state-running="çalışıyor" data-text-state-done="tamamlandı" data-text-state-skipped="atlandı"
         data-text-state-failed="hata" data-text-stages="{0} aşamaları" data-text-cost="Maliyet: {0}"
         data-text-watch="{0} videosunu izle" data-text-player="{0} oynatıcısı">
      <button type="button" id="production-models-open" disabled>
        <span id="production-summary-code"></span><span id="production-summary-engine"></span>
        <span id="production-summary-image"></span><span id="production-summary-audio"></span>
      </button>
      <p id="production-language-warning" hidden></p>
      <input type="checkbox" id="production-subtitles" disabled /><span id="production-subtitles-note" hidden></span>
      <input type="checkbox" id="production-progress" disabled />
      <p id="production-status" role="status"></p>
      <p id="production-approved-empty"></p><p id="production-approved-status"></p><ul id="production-approved"></ul>
      <span id="production-pager" hidden><button type="button" id="production-prev"></button><span id="production-page"></span><button type="button" id="production-next"></button></span>
      <button type="button" id="production-enqueue" disabled></button>
      <p id="production-detail-hint"></p><article id="production-detail" hidden></article>
      <button type="button" id="production-start" disabled>Sırayla üret</button><button type="button" id="production-stop" hidden>Durdur</button>
      <span id="production-block"></span>
      <p id="production-agent-offline" role="alert" hidden>Ajan bağlı değil</p>
      <p id="production-halted" role="alert" hidden>Art arda 3 iş hata verdi — sıra durduruldu</p>
      <p id="production-queue-empty">Sıra boş</p><ol id="production-queue"></ol>
      <p id="production-live" aria-live="polite"></p>
      <p id="production-videos-status" role="status"></p>
      <p id="production-videos-empty" hidden>Henüz üretilmiş video yok</p>
      <ul id="production-videos"></ul>
      <p id="production-viewer-status" role="status">İzlemek için listeden bir video seçin</p>
      <video id="production-player" controls hidden></video>
    </div>
    <div id="production-models-modal" hidden>
      <div role="dialog" aria-modal="true">
        <button type="button" id="production-models-close">x</button>
        <p id="production-models-error" hidden></p>
        <select id="production-model-code"></select>
        <div id="production-effort-field" hidden><select id="production-model-effort"></select></div>
        <select id="production-model-engine"></select><select id="production-model-image"></select><select id="production-model-audio"></select>
        <p id="production-models-warning" hidden></p>
      </div>
    </div>`;
}

const SETTINGS = { subtitles: true, progressBar: false, codeModel: 'anthropic/opus', codeEffort: null, engine: 'local/hyperframes', imageModel: null, audioModel: null };
const STAGES = (states) => ['analysis', 'code', 'images', 'voice', 'render', 'merge'].map((stage, i) => ({ stage, state: states[i], device: null }));
const job = (id, title, status, extra = {}) => ({ id, conceptId: `c-${id}`, title, number: 1, status, language: 'tr', fixed: null, stages: [], costUsd: 0, error: null, ...extra });
const stateOf = (extra = {}) => ({
  profileId: 'p1', settings: { ...SETTINGS }, models: [], modelsFailed: false, jobs: [], startBlock: null, startBlockMessage: null,
  languageWarnings: [], runningHere: false, agentConnected: true, queueHalted: false, ...extra,
});

let routes;
let calls;
let timers;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 8; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const gets = (url) => calls.filter((c) => c.method === 'GET' && c.url === url).length;

async function start(state, videos = []) {
  routes['GET /uretim/p1'] = ok(state);
  routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok({ items: [], page: 1, pageSize: 50, total: 0 });
  routes['GET /uretim/p1/videolar'] = ok(videos);
  initProduction(document, timers);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
}

describe('Konseptten Video — üretim hattı (spec 0017)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/konseptten-video', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    timers = { pending: [], setTimeout: vi.fn((fn) => { timers.pending.push(fn); return timers.pending.length; }), clearTimeout: vi.fn() };
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-7/AC-12/AC-26: iş satırında aşamalar sırayla metin + ikonla (yalnızca renk değil), maliyet ve Hata açıklaması görünür', async () => {
    await start(stateOf({
      jobs: [
        job('j1', 'Kuşatma', 'producing', { stages: STAGES(['done', 'done', 'skipped', 'skipped', 'running', 'pending']), costUsd: 0.0042 }),
        job('j2', 'Düşüş', 'failed', { stages: STAGES(['done', 'failed', 'pending', 'pending', 'pending', 'pending']), costUsd: 0.25, error: 'API key yok — Ayarlar' }),
      ],
    }));
    const [first, second] = el('production-queue').querySelectorAll('li.production-queue__item');
    const stages = [...first.querySelectorAll('.production-stages__item')].map((n) => n.textContent);
    expect(stages).toEqual([
      '✓Konsept inceleniyor: tamamlandı', '✓Video kodu üretiliyor: tamamlandı', '–Görseller üretiliyor: atlandı', '–Seslendirme yapılıyor: atlandı',
      '◔Video kodu videoya dönüştürülüyor (render): çalışıyor', '○Birleştiriliyor: bekliyor',
    ]);
    expect(first.querySelector('.production-stages').getAttribute('aria-label')).toBe('Kuşatma aşamaları');
    expect(first.querySelector('.production-stages__icon').getAttribute('aria-hidden')).toBe('true');
    expect(first.querySelector('.production-queue__cost').textContent).toBe('Maliyet: $0,0042');
    expect(second.querySelector('.production-queue__cost').textContent).toBe('Maliyet: $0,25');
    expect(second.querySelector('.production-queue__error').textContent).toBe('API key yok — Ayarlar');
    expect(first.querySelector('.production-queue__error')).toBeNull();
    expect(el('production-live').textContent).toBe('Kuşatma: Video kodu videoya dönüştürülüyor (render)');
    expect(costText(0)).toBe('$0,00');
  });

  it('AC-14/AC-12: "Ajan bağlı değil" yalnızca Ajan bekleniyor işi varken ve ajan bağlı değilken; sıra durunca uyarı', async () => {
    await start(stateOf({ jobs: [job('j1', 'Bekleyen', 'waiting_for_agent')], agentConnected: false }));
    expect(el('production-agent-offline').hidden).toBe(false);
    routes['POST /uretim/durdur'] = ok(stateOf({ jobs: [job('j1', 'Bekleyen', 'stopped')], agentConnected: false, queueHalted: true }));
    el('production-stop').hidden = false;
    el('production-stop').click();
    await flush();
    expect(el('production-agent-offline').hidden).toBe(true);
    expect(el('production-halted').hidden).toBe(false);
  });

  it('AC-7/AC-8: çalışan iş varken durum 3 sn\'de bir yoklanır (≤5 sn), biten iş Üretilenler\'i yeniler; çalışan iş yoksa yoklama durur', async () => {
    await start(stateOf({ jobs: [job('j1', 'Kuşatma', 'producing', { stages: STAGES(['running', 'pending', 'skipped', 'skipped', 'pending', 'pending']) })] }));
    expect(POLL_MS).toBeLessThanOrEqual(5000);
    expect(timers.setTimeout).toHaveBeenLastCalledWith(expect.any(Function), POLL_MS);
    expect(gets('/uretim/p1')).toBe(1);
    expect(gets('/uretim/p1/videolar')).toBe(1);

    routes['GET /uretim/p1'] = ok(stateOf({ jobs: [] }));
    routes['GET /uretim/p1/videolar'] = ok([{ id: 'v1', title: 'Kuşatma', endedAt: '2026-11-16T10:00:00Z' }]);
    await timers.pending.shift()();
    await flush();
    expect(gets('/uretim/p1')).toBe(2);
    expect(gets('/uretim/p1/videolar')).toBe(2);
    expect(el('production-videos').textContent).toBe('Kuşatma');
    expect(timers.pending).toHaveLength(0);
  });

  it('AC-23/AC-24/AC-26: Üretilenler yükleniyor → liste (düğme, erişilebilir ad); tıklayınca izleme alanında oynar; ajan yoksa ya da dosya yoksa sade mesaj', async () => {
    routes['GET /uretim/p1'] = ok(stateOf());
    routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok({ items: [], page: 1, pageSize: 50, total: 0 });
    let release;
    routes['GET /uretim/p1/videolar'] = () => $.Deferred((d) => { release = () => d.resolve([{ id: 'v2', title: 'Yeni' }, { id: 'v1', title: 'Eski' }]); }).promise();
    initProduction(document, timers);
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('production-videos-status').textContent).toBe('Yükleniyor…');
    release();
    await flush();
    const buttons = [...el('production-videos').querySelectorAll('button')];
    expect(buttons.map((b) => b.textContent)).toEqual(['Yeni', 'Eski']);
    expect(buttons[0].getAttribute('aria-label')).toBe('Yeni videosunu izle');
    expect(el('production-videos-empty').hidden).toBe(true);

    routes['POST /uretim/video/v2/izle'] = ok({ url: 'http://127.0.0.1:47615/video/v2?t=abc' });
    buttons[0].click();
    await flush();
    expect(el('production-player').hidden).toBe(false);
    expect(el('production-player').getAttribute('src')).toBe('http://127.0.0.1:47615/video/v2?t=abc');
    expect(el('production-player').getAttribute('aria-label')).toBe('Yeni oynatıcısı');
    expect(el('production-player').hasAttribute('controls')).toBe(true);

    routes['POST /uretim/video/v1/izle'] = fail(409, { title: 'Videoyu izlemek için ajan bağlı olmalı' });
    buttons[1].click();
    await flush();
    expect(el('production-player').hidden).toBe(true);
    expect(el('production-viewer-status').textContent).toBe('Videoyu izlemek için ajan bağlı olmalı');
    routes['POST /uretim/video/v1/izle'] = fail(404, { title: 'Video dosyası bulunamadı' });
    buttons[1].click();
    await flush();
    expect(el('production-viewer-status').textContent).toBe('Video dosyası bulunamadı');
  });

  it('AC-23: boşken "Henüz üretilmiş video yok"; sunucuya ulaşılamazsa sade hata', async () => {
    await start(stateOf(), []);
    expect(el('production-videos-empty').hidden).toBe(false);
    document.body.innerHTML = pageHtml();
    routes['GET /uretim/p1/videolar'] = fail(503, null);
    initProduction(document, timers);
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('production-videos-status').textContent).toContain('Sunucuya ulaşılamadı');
  });
});
