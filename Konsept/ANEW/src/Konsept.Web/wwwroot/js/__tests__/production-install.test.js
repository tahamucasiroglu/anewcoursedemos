import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { awaitingInstall, initProduction, sizeText } from '../production.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// KonsepttenVideo.cshtml'in en küçük karşılığı + spec 0027 indirme onayı penceresi (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="production" data-text-loading="Yükleniyor…" data-text-agent-update-required="Ajan güncellemesi gerekli" data-text-agent-offline="Ajan bağlı değil" data-text-failed="Sunucuya ulaşılamadı. Biraz sonra tekrar deneyin."
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
         data-text-watch="{0} videosunu izle" data-text-player="{0} oynatıcısı"
         data-text-awaiting-install="Onay bekleniyor — indirme gerekli" data-text-installing="İndiriliyor ve kuruluyor: {0}" data-text-install-open="İndirmeyi onayla…"
         data-text-install-text="“{0}” videosu için ajan bilgisayarına şunların indirilip kurulması gerekiyor:" data-text-install-item="{0} — yaklaşık {1} indirme, diskte {2}"
         data-text-install-total="Toplam: yaklaşık {0} indirme, diskte {1}.">
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
    <div id="production-install-modal" hidden>
      <div role="dialog" aria-modal="true">
        <p id="production-install-text"></p><ul id="production-install-list"></ul><p id="production-install-total"></p>
        <p id="production-install-message" role="alert" hidden></p>
        <button type="button" id="production-install-decline">Vazgeç (üretimi durdur)</button>
        <button type="button" id="production-install-approve">İndir ve devam et</button>
      </div>
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
const job = (id, title, status, extra = {}) => ({ id, conceptId: `c-${id}`, title, number: 1, status, language: 'tr', fixed: null, stages: [], costUsd: 0, error: null, install: null, ...extra });
const stateOf = (extra = {}) => ({
  profileId: 'p1', settings: { ...SETTINGS }, models: [], modelsFailed: false, jobs: [], startBlock: null, startBlockMessage: null,
  languageWarnings: [], runningHere: true, agentConnected: true, queueHalted: false, ...extra,
});
const MB = 1024 * 1024;
const PENDING = {
  approved: false,
  components: [
    { id: 'hyperframes', name: 'HyperFrames', downloadBytes: 40 * MB, installedBytes: 115 * MB },
    { id: 'model', name: 'Model', downloadBytes: 3 * 1024 * MB, installedBytes: 6 * 1024 * MB },
  ],
};

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
const posts = (url) => calls.filter((c) => c.method === 'POST' && c.url === url).length;

async function start(state) {
  routes['GET /uretim/p1'] = ok(state);
  routes['GET /konseptler/liste/p1?durumlar=approved&sayfa=1'] = ok({ items: [], page: 1, pageSize: 50, total: 0 });
  routes['GET /uretim/p1/videolar'] = ok([]);
  initProduction(document, timers);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
}

describe('Konseptten Video — eksik bileşen indirme onayı (spec 0027)', () => {
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

  it('AC-8: bekleyen indirmede pencere kendiliğinden açılır (ad, boyut, toplam), odak İndir ve devam et\'te; satırda durum ve düğme', async () => {
    await start(stateOf({ jobs: [job('j1', 'Kuşatma', 'waiting_for_agent', { install: PENDING })] }));
    expect(el('production-install-modal').hidden).toBe(false);
    expect(el('production-install-text').textContent).toBe('“Kuşatma” videosu için ajan bilgisayarına şunların indirilip kurulması gerekiyor:');
    expect([...el('production-install-list').children].map((n) => n.textContent)).toEqual([
      'HyperFrames — yaklaşık 40 MB indirme, diskte 115 MB',
      'Model — yaklaşık 3,0 GB indirme, diskte 6,0 GB',
    ]);
    expect(el('production-install-total').textContent).toBe('Toplam: yaklaşık 3,0 GB indirme, diskte 6,1 GB.');
    expect(document.activeElement).toBe(el('production-install-approve'));
    const row = el('production-queue').querySelector('li');
    expect(row.querySelector('.production-queue__status').textContent).toBe('Onay bekleniyor — indirme gerekli');
    expect(row.querySelector('.production-queue__install').textContent).toBe('İndirmeyi onayla…');
  });

  it('AC-8: İndir ve devam et onay ucunu çağırır, pencere kapanır, satırda "İndiriliyor ve kuruluyor"', async () => {
    await start(stateOf({ jobs: [job('j1', 'Kuşatma', 'waiting_for_agent', { install: PENDING })] }));
    routes['POST /uretim/is/j1/indir-onayla'] = ok(stateOf({ jobs: [job('j1', 'Kuşatma', 'producing', { install: { ...PENDING, approved: true } })] }));
    el('production-install-approve').click();
    await flush();
    expect(posts('/uretim/is/j1/indir-onayla')).toBe(1);
    expect(el('production-install-modal').hidden).toBe(true);
    const row = el('production-queue').querySelector('li');
    expect(row.querySelector('.production-queue__note').textContent).toBe('İndiriliyor ve kuruluyor: HyperFrames, Model');
    expect(row.querySelector('.production-queue__install')).toBeNull();
  });

  it('AC-8: Vazgeç reddetme ucunu çağırır; Hata mesajı satırda görünür', async () => {
    await start(stateOf({ jobs: [job('j1', 'Kuşatma', 'waiting_for_agent', { install: PENDING })] }));
    routes['POST /uretim/is/j1/indir-reddet'] = ok(stateOf({
      runningHere: false, jobs: [job('j1', 'Kuşatma', 'failed', { error: 'İndirme onaylanmadı: HyperFrames, Model. Üretim durdu.' })],
    }));
    el('production-install-decline').click();
    await flush();
    expect(posts('/uretim/is/j1/indir-reddet')).toBe(1);
    expect(el('production-install-modal').hidden).toBe(true);
    expect(el('production-queue').querySelector('.production-queue__error').textContent).toBe('İndirme onaylanmadı: HyperFrames, Model. Üretim durdu.');
  });

  it('AC-8: Esc yalnızca kapatır (uç çağrılmaz), yoklama yeniden açmaz; satırdaki düğme yeniden açar; Tab pencerede döner', async () => {
    await start(stateOf({ jobs: [job('j1', 'Kuşatma', 'waiting_for_agent', { install: PENDING })] }));
    el('production-install-approve').dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
    expect(document.activeElement).toBe(el('production-install-decline'));
    el('production-install-decline').dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(el('production-install-modal').hidden).toBe(true);
    expect(calls.filter((c) => c.method === 'POST')).toHaveLength(0);
    timers.pending.shift()();
    await flush();
    expect(el('production-install-modal').hidden).toBe(true);
    const open = el('production-queue').querySelector('.production-queue__install');
    open.click();
    expect(el('production-install-modal').hidden).toBe(false);
  });

  it('AC-8: komut reddedilirse pencerede sunucu mesajı; pencere açık kalır', async () => {
    await start(stateOf({ jobs: [job('j1', 'Kuşatma', 'waiting_for_agent', { install: PENDING })] }));
    routes['POST /uretim/is/j1/indir-onayla'] = fail(409, { message: 'Bu işin onay bekleyen bir indirmesi yok.' });
    el('production-install-approve').click();
    await flush();
    expect(el('production-install-modal').hidden).toBe(false);
    expect(el('production-install-message').hidden).toBe(false);
    expect(el('production-install-message').textContent).toBe('Bu işin onay bekleyen bir indirmesi yok.');
    expect(el('production-install-approve').disabled).toBe(false);
  });

  it('boyut metni ve bekleyen indirme kuralı', () => {
    expect(sizeText(0)).toBe('1 MB');
    expect(sizeText(512 * MB)).toBe('512 MB');
    expect(sizeText(1536 * MB)).toBe('1,5 GB');
    expect(awaitingInstall(job('a', 'A', 'waiting_for_agent', { install: PENDING }))).toBe(true);
    expect(awaitingInstall(job('a', 'A', 'waiting_for_agent', { install: { ...PENDING, approved: true } }))).toBe(false);
    expect(awaitingInstall(job('a', 'A', 'producing', { install: PENDING }))).toBe(false);
  });
});
