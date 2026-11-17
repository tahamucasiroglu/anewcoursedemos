import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initSettings, trialText } from '../settings.js';
import { installJquery } from './fixture.js';

const $ = installJquery();

// Views/Settings/Index.cshtml'in Modeller bölümü (metinler resx'teki değerler).
function settingsHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div class="settings" id="settings" data-keys-url="/ayarlar/api-keyler" data-models-url="/ayarlar/modeller"
         data-text-kind-text="Metin" data-text-kind-image="Görsel" data-text-available="Kullanılabilir" data-text-no-key="Key yok"
         data-text-key-unreadable="Key okunamıyor" data-text-forbidden="Bu işlem için yetkiniz yok" data-text-no-permission="Yetki yok"
         data-trial-url="/ayarlar/modeller/dene" data-text-trial="Dene" data-text-trial-label="{0} modelini dene (ücretli kısa çağrı)"
         data-text-trial-running="Deneniyor…" data-text-trial-result="Yanıt: {0} · {1} giriş / {2} çıkış token · \${3}"
         data-text-trial-truncated="(yarım kaldı)" data-text-trial-estimated="(tahmini)"
         data-text-trial-failed="Deneme yapılamadı. (Hata no: {0})" data-text-load-failed="Ayarlar şu an yüklenemedi"
         data-text-models-failed="Modeller şu an yüklenemedi">
      <p id="keys-loading" role="status">Yükleniyor…</p>
      <ul id="key-list" hidden></ul>
      <p id="keys-error" role="alert" hidden></p>
      <select id="model-kind"><option value="">Tümü</option></select>
      <select id="model-provider"><option value="">Tümü</option></select>
      <p id="models-loading" role="status">Yükleniyor…</p>
      <ul id="model-list" hidden></ul>
      <p id="models-empty" hidden>Bu filtrede model yok</p>
      <p id="models-error" role="alert" hidden></p>
    </div>`;
}

const catalog = () => ({
  version: '1.1.0',
  providers: [{ id: 'anthropic', name: 'Anthropic' }, { id: 'openai', name: 'OpenAI' }],
  models: [
    { id: 'anthropic/claude-sonnet-5-5', providerId: 'anthropic', providerName: 'Anthropic', name: 'Claude Sonnet 5.5', kind: 'text', availability: 'no_permission' },
    { id: 'openai/gpt-6.1-terra', providerId: 'openai', providerName: 'OpenAI', name: 'GPT-6.1 Terra', kind: 'text', availability: 'available' },
    { id: 'openai/gpt-image-2.6-flare', providerId: 'openai', providerName: 'OpenAI', name: 'gpt-image-2.6-flare', kind: 'image', availability: 'available' },
  ],
});

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const row = (id) => document.querySelector(`[data-model-id="${id}"]`);
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

describe('Modeller: Yetki yok ve Dene (spec 0013)', () => {
  beforeEach(() => {
    document.body.innerHTML = settingsHtml();
    setNavigation({ pathname: '/ayarlar', search: '', assign: vi.fn() });
    calls = [];
    routes = { 'GET /ayarlar/api-keyler': ok([]), 'GET /ayarlar/modeller': ok(catalog()) };
    vi.spyOn($, 'ajax').mockImplementation((settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('AC-2/Req 3: yetkisiz model "Yetki yok" görünür ve denenemez; yalnızca kullanılabilir metin modelinde "Dene" vardır', async () => {
    await initSettings(document);

    expect(row('anthropic/claude-sonnet-5-5').querySelector('.badge').textContent).toBe('Yetki yok');
    expect(row('anthropic/claude-sonnet-5-5').querySelector('.model-row__trial-button')).toBeNull();
    expect(row('openai/gpt-image-2.6-flare').querySelector('.model-row__trial-button')).toBeNull();
    const button = row('openai/gpt-6.1-terra').querySelector('.model-row__trial-button');
    expect(button.textContent).toBe('Dene');
    expect(button.getAttribute('aria-label')).toBe('GPT-6.1 Terra modelini dene (ücretli kısa çağrı)');
  });

  it('Dene: sürerken pasif ve tek istek; sonuç metni, token ve maliyet ($, Türkçe virgül) gösterir', async () => {
    let release;
    routes['POST /ayarlar/modeller/dene'] = () => $.Deferred((d) => { release = () => d.resolve({ text: 'Merhaba', truncated: false, inputTokens: 12, outputTokens: 34, costUsd: 0.00081, isEstimated: false }); }).promise();
    await initSettings(document);
    const button = row('openai/gpt-6.1-terra').querySelector('.model-row__trial-button');
    const result = row('openai/gpt-6.1-terra').querySelector('.model-row__trial');

    button.click();
    button.click();
    expect(button.disabled).toBe(true);
    expect(result.textContent).toBe('Deneniyor…');
    release();
    await flush();

    const posts = calls.filter((c) => c.method === 'POST');
    expect(posts).toHaveLength(1);
    expect(JSON.parse(posts[0].data)).toEqual({ modelId: 'openai/gpt-6.1-terra' });
    expect(result.textContent).toBe('Yanıt: Merhaba · 12 giriş / 34 çıkış token · $0,000810');
    expect(result.getAttribute('role')).toBe('status');
    expect(button.disabled).toBe(false);
  });

  it('Dene: Api\'nin Türkçe nedeni (key yok, yetki yok, sağlayıcı reddi) gösterilir; bilinmeyen hatada korelasyon no', async () => {
    await initSettings(document);
    const button = row('openai/gpt-6.1-terra').querySelector('.model-row__trial-button');
    const result = row('openai/gpt-6.1-terra').querySelector('.model-row__trial');

    routes['POST /ayarlar/modeller/dene'] = fail(400, { message: 'OpenAI için API key girilmemiş — Ayarlar' });
    button.click();
    await flush();
    expect(result.textContent).toBe('OpenAI için API key girilmemiş — Ayarlar');

    routes['POST /ayarlar/modeller/dene'] = fail(409, { message: "Sağlayıcı API key'i reddetti" });
    button.click();
    await flush();
    expect(result.textContent).toBe("Sağlayıcı API key'i reddetti");

    routes['POST /ayarlar/modeller/dene'] = fail(502, { code: 'api.unavailable', correlationId: 'abc123' });
    button.click();
    await flush();
    expect(result.textContent).toBe('Deneme yapılamadı. (Hata no: abc123)');
  });

  it('sonuç metni yarım kaldı ve tahmini işaretlerini ekler', () => {
    const text = document.getElementById('settings').dataset;
    expect(trialText({ text: 'x', truncated: true, inputTokens: null, outputTokens: null, costUsd: 0.0000026, isEstimated: true }, text))
      .toBe('Yanıt: x · - giriş / - çıkış token · $0,000003 (yarım kaldı) (tahmini)');
    // review r1 L-5: model çıktısındaki yer tutucu ve "$" desenleri olduğu gibi kalır.
    expect(trialText({ text: "a $& b {1} $' c", truncated: false, inputTokens: 7, outputTokens: 8, costUsd: 0, isEstimated: false }, text))
      .toBe("Yanıt: a $& b {1} $' c · 7 giriş / 8 çıkış token · $0,000000");
  });
});
