import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { bindPassiveControls } from '../passive-controls.js';
import { formatDateTime, initProduced, moneyText, VOLUME_KEY } from '../produced.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Uretilenler.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="produced" data-text-loading="Yükleniyor…" data-text-load-failed="Videolar yüklenemedi. Biraz sonra tekrar deneyin."
         data-text-more-loading="Daha fazla yükleniyor…" data-text-item-meta="{0} · {1} · {2} sürüm" data-text-player="{0} oynatıcısı"
         data-text-viewer-hint="İzlemek için soldan bir video seçin" data-text-agent-offline="Videoyu izlemek için ajan bağlı olmalı"
         data-text-other-device="Bu video yalnızca ajanın kurulu olduğu bilgisayarda ({0}) izlenebilir" data-text-device-unknown="ajanın bilgisayarı"
         data-text-file-missing="Video dosyası bulunamadı" data-text-stream-error="Akış hatası: video oynatılamadı."
         data-text-sending="Gönderiliyor…" data-text-opened-folder="Klasör açıldı." data-text-opened-player="Video sistem oynatıcısında açıldı."
         data-text-timeout="Zaman aşımı" data-text-action-failed="İşlem yapılamadı. Biraz sonra tekrar deneyin."
         data-text-reason-agent="Ajan bağlı değil" data-text-reason-file="Video dosyası bulunamadı"
         data-text-scene-title="{0}. {1}" data-text-scene-visual="Görsel" data-text-scene-narration="Seslendirme"
         data-text-cost="Sürüm ücreti" data-text-total-cost="Tüm sürümlerin toplamı" data-text-free="Yerel — ücretsiz" data-text-exact-cost="Tam tutar: {0}"
         data-text-code-model="Video kodu modeli" data-text-engine="Video motoru" data-text-image-model="Görsel modeli" data-text-audio-model="Ses motoru"
         data-text-off="Kapalı" data-text-on="Açık" data-text-subtitles="Altyazı" data-text-progress-bar="İlerleme çubuğu" data-text-duration="Video süresi"
         data-text-format="Biçim" data-text-landscape="Yatay" data-text-portrait="Dikey" data-text-started="Başlangıç" data-text-ended="Bitiş"
         data-text-ai-voice="Yapay zekâ ile üretilmiş ses" data-text-version="Sürüm {0}" data-text-version-label="Sürüm {0}, {1}, {2}, {3}, {4}"
         data-text-status-completed="Tamamlandı" data-text-second="sn" data-text-minute="dk" data-text-hour="sa">
      <p id="produced-status" role="status"></p>
      <button type="button" id="produced-retry" hidden>Yeniden dene</button>
      <p class="empty-state" id="produced-empty" hidden>Henüz üretilmiş video yok.</p>
      <ul id="produced-list"></ul>
      <button type="button" id="produced-more" hidden>Daha fazla göster</button>
      <p id="produced-viewer-status" role="status">İzlemek için soldan bir video seçin</p>
      <video id="produced-player" controls hidden></video>
      <button type="button" id="produced-stream-retry" hidden>Yeniden dene</button>
      <div id="produced-actions" hidden>
        <button type="button" id="produced-open-folder" data-action="folder" data-label="Klasörde aç">Klasörde aç</button>
        <button type="button" id="produced-open-player" data-action="player" data-label="Sistem oynatıcısında aç">Sistem oynatıcısında aç</button>
        <p id="produced-action-status" role="status"></p>
      </div>
      <p id="produced-panel-hint">Ayrıntılar için bir video seçin</p>
      <div id="produced-panel" hidden>
        <h2 id="produced-title"></h2><ol id="produced-scenes"></ol><div id="produced-concept"></div><dl id="produced-details"></dl><ul id="produced-versions"></ul>
      </div>
    </div>`;
}

const scenes = (narration) => [
  { title: 'Açılış', visual: 'Surlar görünür.', narration, durationSeconds: 30 },
  { title: 'Son', visual: 'Şehir sessizleşir.', narration: 'Bir çağ kapanıyor.', durationSeconds: 30 },
];
const version = (id, number, extra = {}) => ({
  id, number, status: 'completed', changes: number === 1 ? 'İlk sürüm' : 'Değişti: konsept metni', costUsd: 0, durationSeconds: 1812, format: 'landscape',
  startedAt: '2026-11-02T07:01:00Z', endedAt: '2026-11-02T07:05:00Z', codeModel: 'Claude Sonnet 4.5', engine: 'HyperFrames', imageModel: null,
  audioModel: null, subtitles: false, progressBar: true, aiVoiceNotice: false, conceptTitle: 'Kuşatma', scenes: scenes(`Metin ${number}`), ...extra,
});
const DETAIL = {
  id: 'v1', title: 'Kuşatma', totalCostUsd: 1.24,
  versions: [
    version('s1', 1, { costUsd: 0.004, scenes: scenes('<script>alert(1)</script> İlk metin') }),
    version('s2', 2, { costUsd: 1.236, audioModel: 'Supertonic 3', subtitles: true, aiVoiceNotice: true, format: 'portrait', endedAt: '2026-11-03T21:30:00Z' }),
  ],
};
const item = (id, title, extra = {}) => ({ id, title, durationSeconds: 57, producedAt: '2026-11-02T07:05:00Z', versionCount: 1, ...extra });

let routes;
let calls;
let storage;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 8; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = (url) => calls.filter((c) => c.method === 'POST' && c.url === url);

async function start(page = { items: [item('v1', 'Kuşatma', { versionCount: 2 })], hasMore: false }) {
  routes['GET /uretilenler/p1/liste?atla=0'] = ok(page);
  routes['GET /uretilenler/video/v1'] = ok(DETAIL);
  routes['POST /uretim/video/s2/izle'] = ok({ url: 'http://127.0.0.1:47615/video/s2?t=x' });
  routes['POST /uretim/video/s1/izle'] = ok({ url: 'http://127.0.0.1:47615/video/s1?t=y' });
  const controller = initProduced(document, { storage });
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
  return controller;
}

async function select(id = 'v1') {
  el('produced-list').querySelector(`[data-video="${id}"]`).click();
  await flush();
}

describe('Üretilen Videolar (spec 0007)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/uretilenler', search: '', assign: vi.fn() });
    vi.spyOn(HTMLMediaElement.prototype, 'pause').mockImplementation(() => {});
    vi.spyOn(HTMLMediaElement.prototype, 'load').mockImplementation(() => {});
    routes = {};
    calls = [];
    const data = new Map();
    storage = { getItem: (k) => (data.has(k) ? data.get(k) : null), setItem: (k, v) => data.set(k, String(v)) };
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('AC-2/AC-3/AC-13: öğe başlık, süre, üretim tarihi, sürüm sayısıyla düğme; boşta yer tutucular; "Daha fazla göster" 51. öğeyi ekler', async () => {
    const first = Array.from({ length: 50 }, (_, i) => item(`v${i}`, `Video ${i}`));
    routes['GET /uretilenler/p1/liste?atla=50'] = ok({ items: [item('v50', 'Video 50')], hasMore: false });
    await start({ items: first, hasMore: true });

    const buttons = el('produced-list').querySelectorAll('button[data-video]');
    expect(buttons).toHaveLength(50);
    expect(buttons[0].getAttribute('type')).toBe('button');
    expect(buttons[0].querySelector('.produced-items__title').textContent).toBe('Video 0');
    expect(buttons[0].querySelector('.produced-items__meta').textContent).toBe(`57 sn · ${formatDateTime('2026-11-02T07:05:00Z')} · 1 sürüm`);
    expect(formatDateTime('2026-11-02T07:05:00Z')).toBe('02.11.2026 10:05');
    expect(el('produced-more').hidden).toBe(false);
    expect(el('produced-viewer-status').textContent).toBe('İzlemek için soldan bir video seçin');
    expect(el('produced-panel').hidden).toBe(true);
    expect(el('produced-panel-hint').hidden).toBe(false);
    expect(el('produced-actions').hidden).toBe(true);

    el('produced-more').click();
    await flush();
    expect(el('produced-list').querySelectorAll('button[data-video]')).toHaveLength(51);
    expect(el('produced-more').hidden).toBe(true);
  });

  it('AC-3: profilde video yokken boş durum metni', async () => {
    await start({ items: [], hasMore: false });
    expect(el('produced-empty').hidden).toBe(false);
    expect(el('produced-list').children).toHaveLength(0);
  });

  it('AC-1: profil değişince liste yenilenir ve seçim temizlenir', async () => {
    await start();
    await select();
    expect(el('produced-panel').hidden).toBe(false);
    routes['GET /uretilenler/p2/liste?atla=0'] = ok({ items: [item('x1', 'Başka')], hasMore: false });

    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p2' } }));
    await flush();

    expect([...el('produced-list').querySelectorAll('.produced-items__title')].map((n) => n.textContent)).toEqual(['Başka']);
    expect(el('produced-panel').hidden).toBe(true);
    expect(el('produced-player').hidden).toBe(true);
    expect(el('produced-viewer-status').textContent).toBe('İzlemek için soldan bir video seçin');
  });

  it('AC-4/AC-9: seçimde en son sürüm yüklenir; sahneler, konsept metni (düz metin), ücret biçimleri, modeller, saatler ve AI ses beyanı', async () => {
    await start();
    await select();

    expect(posts('/uretim/video/s2/izle')).toHaveLength(1);
    expect(el('produced-player').getAttribute('src')).toBe('http://127.0.0.1:47615/video/s2?t=x');
    expect(el('produced-player').getAttribute('aria-label')).toBe('Kuşatma oynatıcısı');
    expect([...el('produced-scenes').querySelectorAll('li')].map((li) => li.textContent)).toEqual(['1. AçılışMetin 2', '2. SonBir çağ kapanıyor.']);
    const details = [...el('produced-details').children].map((n) => n.textContent);
    expect(details).toEqual([
      'Sürüm ücreti', '$1,24', 'Tüm sürümlerin toplamı', '$1,24', 'Video kodu modeli', 'Claude Sonnet 4.5', 'Video motoru', 'HyperFrames',
      'Görsel modeli', 'Kapalı', 'Ses motoru', 'Supertonic 3', 'Altyazı', 'Açık', 'İlerleme çubuğu', 'Açık', 'Video süresi', '30 dk 12 sn',
      'Biçim', 'Dikey', 'Başlangıç', '10:01', 'Bitiş', '00:30', 'Yapay zekâ ile üretilmiş ses',
    ]);
    expect(el('produced-details').querySelector('.produced-money').title).toBe('Tam tutar: $1,236');

    el('produced-versions').querySelector('[data-version="s1"]').click();
    await flush();
    expect(posts('/uretim/video/s1/izle')).toHaveLength(1);
    const cost = el('produced-details').querySelector('.produced-money');
    expect(cost.textContent).toBe('<$0,01');
    expect(cost.title).toBe('Tam tutar: $0,004');
    expect(cost.tabIndex).toBe(0);
    expect(el('produced-concept').querySelector('script')).toBeNull();
    expect(el('produced-concept').textContent).toContain('Seslendirme: <script>alert(1)</script> İlk metin');
    expect(el('produced-details').textContent).not.toContain('Yapay zekâ ile üretilmiş ses');
    expect(moneyText(0)).toBeNull();
  });

  it('AC-10/AC-13: sürüm listesi numara, zaman, değişiklik, ücret, durum; seçili sürüm aria-pressed; online çağrısız sürüm "Yerel — ücretsiz"', async () => {
    await start();
    DETAIL.versions[0].costUsd = 0;
    try {
      await select();
      const rows = [...el('produced-versions').querySelectorAll('button[data-version]')];
      expect(rows.map((b) => b.querySelector('.produced-versions__number').textContent)).toEqual(['Sürüm 1', 'Sürüm 2']);
      expect(rows[0].querySelector('.produced-versions__meta').textContent).toBe('02.11.2026 10:05 · İlk sürüm · Yerel — ücretsiz · Tamamlandı');
      expect(rows[1].getAttribute('aria-label')).toBe('Sürüm 2, 04.11.2026 00:30, Değişti: konsept metni, $1,24, Tamamlandı');
      expect(rows.map((b) => b.getAttribute('aria-pressed'))).toEqual(['false', 'true']);
      rows[0].click();
      await flush();
      expect([...el('produced-versions').querySelectorAll('button')].map((b) => b.getAttribute('aria-pressed'))).toEqual(['true', 'false']);
      expect(el('produced-scenes').textContent).toContain('<script>alert(1)</script> İlk metin');
      expect(el('produced-details').querySelector('.produced-money').textContent).toBe('Yerel — ücretsiz');
    } finally {
      DETAIL.versions[0].costUsd = 0.004;
    }
  });

  it('AC-5: video oynatmaya hazır olunca mesaj kalkar; ses düzeyi bu tarayıcıda saklanır ve sonraki açılışta uygulanır', async () => {
    await start();
    await select();
    const player = el('produced-player');
    expect(el('produced-viewer-status').textContent).toBe('Yükleniyor…');
    player.dispatchEvent(new Event('loadedmetadata'));
    expect(el('produced-viewer-status').textContent).toBe('');
    expect(player.hidden).toBe(false);
    expect(player.controls).toBe(true);

    player.volume = 0.3;
    player.dispatchEvent(new Event('volumechange'));
    expect(JSON.parse(storage.getItem(VOLUME_KEY))).toEqual({ volume: 0.3, muted: false });

    document.body.innerHTML = pageHtml();
    await start();
    await select();
    expect(el('produced-player').volume).toBe(0.3);
  });

  it('AC-6: ajan çevrimdışı → tek mesaj, iki eylem pasif ve nedeni ipucunda; liste ve panel kalır; ajan gelince oynatma yeniden denenir', async () => {
    bindPassiveControls(document);
    await start();
    await select();
    document.dispatchEvent(new CustomEvent('agent:status', { detail: { connected: false, machineName: 'OFIS-PC' } }));

    expect(el('produced-viewer-status').textContent).toBe('Videoyu izlemek için ajan bağlı olmalı');
    expect(el('produced-player').hidden).toBe(true);
    for (const id of ['produced-open-folder', 'produced-open-player']) {
      expect(el(id).getAttribute('aria-disabled')).toBe('true');
      expect(el(id).dataset.tooltip).toBe('Ajan bağlı değil');
      expect(el(id).classList.contains('passive')).toBe(true);
    }
    el('produced-open-folder').click();
    await flush();
    expect(calls.filter((c) => c.url.includes('/ac'))).toHaveLength(0);
    expect(el('produced-panel').hidden).toBe(false);
    expect(el('produced-list').children).toHaveLength(1);

    document.dispatchEvent(new CustomEvent('agent:status', { detail: { connected: true, machineName: 'OFIS-PC' } }));
    await flush();
    expect(posts('/uretim/video/s2/izle')).toHaveLength(2);
    expect(el('produced-open-folder').hasAttribute('aria-disabled')).toBe(false);
  });

  it('AC-7: dosya yoksa "Video dosyası bulunamadı" ve iki eylem pasif; video listede kalır', async () => {
    await start();
    routes['POST /uretim/video/s2/izle'] = fail(404, { code: 'production.file_missing', message: 'Video dosyası bulunamadı' });
    await select();

    expect(el('produced-viewer-status').textContent).toBe('Video dosyası bulunamadı');
    expect(el('produced-open-player').getAttribute('aria-disabled')).toBe('true');
    expect(el('produced-open-player').dataset.tooltip).toBe('Video dosyası bulunamadı');
    expect(el('produced-list').children).toHaveLength(1);
  });

  it('AC-8: eylem gönderilirken "Gönderiliyor…", başarıda kısa onay; ajan 10 sn cevap vermezse "Zaman aşımı"', async () => {
    await start();
    await select();
    let resolve;
    routes['POST /uretilenler/surum/s2/ac'] = () => $.Deferred((d) => { resolve = () => d.resolve({ action: 'folder' }); }).promise();

    el('produced-open-folder').click();
    expect(el('produced-open-folder').textContent).toBe('Gönderiliyor…');
    expect(el('produced-open-folder').disabled).toBe(true);
    expect(JSON.parse(posts('/uretilenler/surum/s2/ac')[0].data)).toEqual({ action: 'folder' });
    resolve();
    await flush();
    expect(el('produced-action-status').textContent).toBe('Klasör açıldı.');
    expect(el('produced-open-folder').textContent).toBe('Klasörde aç');

    routes['POST /uretilenler/surum/s2/ac'] = fail(504, { code: 'production.file_action_timeout', message: 'Zaman aşımı' });
    el('produced-open-player').click();
    await flush();
    expect(el('produced-action-status').textContent).toBe('Zaman aşımı');
    expect(el('produced-open-player').disabled).toBe(false);
  });

  it('AC-12: liste yüklenemezse sade Türkçe hata ve "Yeniden dene" listeyi tekrar yükler', async () => {
    routes['GET /uretilenler/p1/liste?atla=0'] = fail(503, null);
    initProduced(document, { storage });
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
    await flush();
    expect(el('produced-status').textContent).toBe('Videolar yüklenemedi. Biraz sonra tekrar deneyin.');
    expect(el('produced-retry').hidden).toBe(false);

    routes['GET /uretilenler/p1/liste?atla=0'] = ok({ items: [item('v1', 'Kuşatma')], hasMore: false });
    el('produced-retry').click();
    await flush();
    expect(el('produced-status').textContent).toBe('');
    expect(el('produced-list').children).toHaveLength(1);
  });

  it('AC-14: yükleniyor; akış adresine bu cihazdan ulaşılamazsa cihaz adıyla mesaj; oynarken koparsa akış hatası + Yeniden dene', async () => {
    await start();
    document.dispatchEvent(new CustomEvent('agent:status', { detail: { connected: true, machineName: 'OFIS-PC' } }));
    routes['GET /uretilenler/video/v1'] = () => $.Deferred().promise(); // Sürer.
    await select();
    expect(el('produced-panel-hint').textContent).toBe('Yükleniyor…');
    expect(el('produced-viewer-status').textContent).toBe('Yükleniyor…');

    routes['GET /uretilenler/video/v1'] = ok(DETAIL);
    await select();
    el('produced-player').dispatchEvent(new Event('error'));
    expect(el('produced-viewer-status').textContent).toBe('Bu video yalnızca ajanın kurulu olduğu bilgisayarda (OFIS-PC) izlenebilir');
    expect(el('produced-stream-retry').hidden).toBe(false);

    el('produced-stream-retry').click();
    await flush();
    el('produced-player').dispatchEvent(new Event('loadedmetadata'));
    el('produced-player').dispatchEvent(new Event('error'));
    expect(el('produced-viewer-status').textContent).toBe('Akış hatası: video oynatılamadı.');
    expect(el('produced-stream-retry').hidden).toBe(false);
    el('produced-stream-retry').click();
    await flush();
    expect(posts('/uretim/video/s2/izle')).toHaveLength(3);
  });
});
