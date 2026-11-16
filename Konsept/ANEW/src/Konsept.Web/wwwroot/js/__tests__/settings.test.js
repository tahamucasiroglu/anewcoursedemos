import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initSettings, statusText } from '../settings.js';
import { installJquery } from './fixture.js';

const $ = installJquery();

// Views/Settings/Index.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function settingsHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div class="settings" id="settings" data-keys-url="/ayarlar/api-keyler" data-models-url="/ayarlar/modeller"
         data-text-not-set="Girilmemiş" data-text-verified="Doğrulandı" data-text-unverified="Doğrulanmadı"
         data-text-unreadable="Key okunamıyor — lütfen yeniden girin" data-text-just-now="az önce güncellendi"
         data-text-updated-at="{0} güncellendi" data-text-add="Ekle" data-text-change="Değiştir" data-text-delete="Sil"
         data-text-save="Kaydet" data-text-cancel="Vazgeç" data-text-saving="Doğrulanıyor…" data-text-input-label="{0} API key"
         data-text-saved="API key kaydedildi" data-text-deleted="API key silindi"
         data-text-delete-confirm="Bu key silinecek; {0} modelleri kullanılamaz olacak. Sıradaki işler bu sağlayıcıyı kullanıyorsa duracak."
         data-text-failed="İşlem şu an yapılamadı (hata no: {0})" data-text-load-failed="Ayarlar şu an yüklenemedi"
         data-text-models-failed="Modeller şu an yüklenemedi" data-text-kind-text="Metin" data-text-kind-image="Görsel"
         data-text-available="Kullanılabilir" data-text-no-key="Key yok" data-text-key-unreadable="Key okunamıyor"
         data-text-forbidden="Bu işlem için yetkiniz yok">
      <p id="keys-loading" role="status">Yükleniyor…</p>
      <ul id="key-list" hidden></ul>
      <p id="keys-error" role="alert" hidden></p>
      <select id="model-kind"><option value="">Tümü</option><option value="text">Metin</option><option value="image">Görsel</option></select>
      <select id="model-provider"><option value="">Tümü</option></select>
      <p id="models-loading" role="status">Yükleniyor…</p>
      <ul id="model-list" hidden></ul>
      <p id="models-empty" hidden>Bu filtrede model yok</p>
      <p id="models-error" role="alert" hidden></p>
      <p id="settings-toast" role="status" hidden></p>
      <div id="delete-dialog" hidden>
        <div role="alertdialog" aria-modal="true">
          <p id="delete-dialog-text"></p>
          <button type="button" id="delete-cancel">Vazgeç</button>
          <button type="button" id="delete-confirm">Sil</button>
        </div>
      </div>
    </div>`;
}

const now = new Date().toISOString();
const keys = () => [
  { providerId: 'anthropic', providerName: 'Anthropic', state: 'none', hint: null, validation: null, updatedAt: null },
  { providerId: 'openai', providerName: 'OpenAI', state: 'saved', hint: 'sk-…a3f', validation: 'verified', updatedAt: now },
  { providerId: 'kimi', providerName: 'Kimi', state: 'unreadable', hint: null, validation: null, updatedAt: now },
];
const catalog = () => ({
  version: '1.0.0',
  providers: [{ id: 'anthropic', name: 'Anthropic' }, { id: 'openai', name: 'OpenAI' }, { id: 'kimi', name: 'Kimi' }],
  models: [
    { id: 'anthropic/claude', providerId: 'anthropic', providerName: 'Anthropic', name: 'Claude', kind: 'text', availability: 'no_key' },
    { id: 'openai/gpt-5', providerId: 'openai', providerName: 'OpenAI', name: 'GPT-5', kind: 'text', availability: 'available' },
    { id: 'openai/gpt-image-1', providerId: 'openai', providerName: 'OpenAI', name: 'gpt-image-1', kind: 'image', availability: 'available' },
    { id: 'kimi/k2', providerId: 'kimi', providerName: 'Kimi', name: 'Kimi K2', kind: 'text', availability: 'unreadable' },
  ],
});

let routes;
let calls;

function respond(settings) {
  calls.push(settings);
  const handler = routes[`${settings.method} ${settings.url}`];
  return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
}

const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const row = (provider) => document.querySelector(`.key-row[data-provider="${provider}"]`);
const status = (provider) => row(provider).querySelector('.key-row__status').textContent;
const posts = () => calls.filter((c) => c.method === 'POST');
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true }));

describe('Ayarlar (spec 0004)', () => {
  beforeEach(() => {
    document.body.innerHTML = settingsHtml();
    setNavigation({ pathname: '/ayarlar', search: '', assign: vi.fn() });
    calls = [];
    routes = {
      'GET /ayarlar/api-keyler': ok(keys()),
      'GET /ayarlar/modeller': ok(catalog()),
    };
    vi.spyOn($, 'ajax').mockImplementation(respond);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('AC-13/14: yüklenirken yükleniyor görünür; satırlar Girilmemiş / maskeli+Doğrulandı+az önce / okunamıyor gösterir', async () => {
    let release;
    routes['GET /ayarlar/api-keyler'] = () => $.Deferred((d) => { release = () => d.resolve(keys()); }).promise();
    const started = initSettings(document);
    expect(document.getElementById('keys-loading').hidden).toBe(false);
    release();
    await started;

    expect(document.getElementById('keys-loading').hidden).toBe(true);
    expect(status('anthropic')).toBe('Girilmemiş');
    expect(row('anthropic').querySelector('.key-row__edit').textContent).toBe('Ekle');
    expect(row('anthropic').querySelector('.key-row__delete')).toBeNull();
    expect(status('openai')).toBe('sk-…a3f · Doğrulandı · az önce güncellendi');
    expect(row('openai').querySelector('.key-row__edit').textContent).toBe('Değiştir');
    expect(status('kimi')).toBe('Key okunamıyor — lütfen yeniden girin');
    expect(row('kimi').querySelector('.key-row__edit').textContent).toBe('Değiştir');
  });

  it('AC-17: düzenleme alanı şifre tipinde, etiketi sağlayıcı adını içerir, hata alanı aria-describedby ile bağlı', async () => {
    await initSettings(document);
    row('anthropic').querySelector('.key-row__edit').click();

    const input = row('anthropic').querySelector('input');
    expect(row('anthropic').querySelector('.key-form').hidden).toBe(false);
    expect(input.type).toBe('password');
    expect(document.querySelector(`label[for="${input.id}"]`).textContent).toBe('Anthropic API key');
    const error = document.getElementById(input.getAttribute('aria-describedby'));
    expect(error.getAttribute('role')).toBe('alert');
    expect(document.activeElement).toBe(input);
  });

  it('AC-12: Vazgeç ve Esc alanı temizleyip kapatır; hiçbir istek gitmez, tarayıcı depolamasına yazılmaz', async () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    await initSettings(document);
    const before = calls.length;
    const target = row('openai');

    target.querySelector('.key-row__edit').click();
    target.querySelector('input').value = 'sk-taslak-key-12345';
    target.querySelector('.key-form__cancel').click();
    expect(target.querySelector('.key-form').hidden).toBe(true);
    expect(target.querySelector('input').value).toBe('');

    target.querySelector('.key-row__edit').click();
    target.querySelector('input').value = 'sk-taslak-key-67890';
    key(target.querySelector('input'), 'Escape');
    expect(target.querySelector('.key-form').hidden).toBe(true);
    expect(target.querySelector('input').value).toBe('');

    expect(calls.length).toBe(before);
    expect(status('openai')).toBe('sk-…a3f · Doğrulandı · az önce güncellendi');
    expect(setItem).not.toHaveBeenCalled();
    expect(document.cookie).not.toContain('taslak');
  });

  it('AC-3/14: Kaydet sürerken pasif; art arda gönderim tek istek; başarıda maskeli satır, alan temizlenir, bildirim görünür', async () => {
    await initSettings(document);
    let release;
    routes['POST /ayarlar/api-keyler/anthropic'] = () => $.Deferred((d) => {
      release = () => d.resolve({ providerId: 'anthropic', providerName: 'Anthropic', state: 'saved', hint: 'sk-…xyz', validation: 'verified', updatedAt: new Date().toISOString() });
    }).promise();
    const target = row('anthropic');
    target.querySelector('.key-row__edit').click();
    target.querySelector('input').value = 'sk-ant-gercek-olmayan-key-xyz';
    const form = target.querySelector('.key-form');

    form.dispatchEvent(new Event('submit', { cancelable: true }));
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    expect(posts()).toHaveLength(1);
    expect(JSON.parse(posts()[0].data)).toEqual({ apiKey: 'sk-ant-gercek-olmayan-key-xyz' });
    expect(posts()[0].headers.RequestVerificationToken).toBe('test-token');
    expect(target.querySelector('.key-form__save').disabled).toBe(true);

    release();
    await flush();

    expect(status('anthropic')).toBe('sk-…xyz · Doğrulandı · az önce güncellendi');
    expect(row('anthropic').querySelector('input').value).toBe('');
    expect(row('anthropic').querySelector('.key-form').hidden).toBe(true);
    expect(document.getElementById('settings-toast').textContent).toBe('API key kaydedildi');
    expect(document.getElementById('settings-toast').hidden).toBe(false);
    expect(document.body.innerHTML).not.toContain('sk-ant-gercek-olmayan-key-xyz');
  });

  it('AC-6/10: biçim hatası ya da sağlayıcı reddi alanın yanında görünür; satır değişmez', async () => {
    await initSettings(document);
    routes['POST /ayarlar/api-keyler/openai'] = fail(400, { code: 'ai.key_rejected', message: "Sağlayıcı bu key'i reddetti", errors: { ApiKey: ["Sağlayıcı bu key'i reddetti"] } });
    const target = row('openai');
    target.querySelector('.key-row__edit').click();
    target.querySelector('input').value = 'sk-reddedilen-key-123';

    target.querySelector('.key-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();

    const input = target.querySelector('input');
    expect(document.getElementById(input.getAttribute('aria-describedby')).textContent).toBe("Sağlayıcı bu key'i reddetti");
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(target.querySelector('.key-form__save').disabled).toBe(false);
    expect(status('openai')).toBe('sk-…a3f · Doğrulandı · az önce güncellendi');
  });

  it('AC-15: sunucu hatasında Türkçe genel mesaj + korelasyon no; mesajda key yok; satır değişmez', async () => {
    await initSettings(document);
    routes['POST /ayarlar/api-keyler/anthropic'] = fail(503, { code: 'api.error', message: 'Beklenmeyen bir hata oluştu.', correlationId: 'corr-42' });
    const target = row('anthropic');
    target.querySelector('.key-row__edit').click();
    target.querySelector('input').value = 'sk-hata-anindaki-key-999';

    target.querySelector('.key-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();

    const message = target.querySelector('.key-form__error').textContent;
    expect(message).toBe('İşlem şu an yapılamadı (hata no: corr-42)');
    expect(message).not.toContain('sk-hata');
    expect(status('anthropic')).toBe('Girilmemiş');
  });

  it('AC-8/17: Sil önce onay metnini gösterir; Esc vazgeçer (istek yok); odak pencerede kalır; onayla satır Girilmemiş olur', async () => {
    await initSettings(document);
    routes['POST /ayarlar/api-keyler/openai/sil'] = ok({ providerId: 'openai', providerName: 'OpenAI', state: 'none', hint: null, validation: null, updatedAt: null });
    const dialog = document.getElementById('delete-dialog');

    row('openai').querySelector('.key-row__delete').click();
    expect(dialog.hidden).toBe(false);
    expect(document.getElementById('delete-dialog-text').textContent)
      .toBe('Bu key silinecek; OpenAI modelleri kullanılamaz olacak. Sıradaki işler bu sağlayıcıyı kullanıyorsa duracak.');
    expect(document.activeElement.id).toBe('delete-cancel');
    key(document.activeElement, 'Tab');
    expect(document.activeElement.id).toBe('delete-confirm');
    key(document.activeElement, 'Tab');
    expect(document.activeElement.id).toBe('delete-cancel');
    key(document.activeElement, 'Escape');
    expect(dialog.hidden).toBe(true);
    expect(posts()).toHaveLength(0);
    expect(status('openai')).toBe('sk-…a3f · Doğrulandı · az önce güncellendi');

    row('openai').querySelector('.key-row__delete').click();
    document.getElementById('delete-confirm').click();
    await flush();

    expect(posts().map((c) => c.url)).toEqual(['/ayarlar/api-keyler/openai/sil']);
    expect(dialog.hidden).toBe(true);
    expect(status('openai')).toBe('Girilmemiş');
    expect(row('openai').querySelector('.key-row__delete')).toBeNull();
    expect(document.getElementById('settings-toast').textContent).toBe('API key silindi');
  });

  it('AC-16: modeller etiketleriyle listelenir; tür ve sağlayıcı filtresi daraltır; eşleşme yoksa boş durum', async () => {
    await initSettings(document);
    const names = () => [...document.querySelectorAll('#model-list .model-row__name')].map((n) => n.textContent);
    const badge = (id) => document.querySelector(`[data-model-id="${id}"] .badge`).textContent;

    expect(names()).toEqual(['Claude', 'GPT-5', 'gpt-image-1', 'Kimi K2']);
    expect(badge('openai/gpt-5')).toBe('Kullanılabilir');
    expect(badge('anthropic/claude')).toBe('Key yok');
    expect(badge('kimi/k2')).toBe('Key okunamıyor');
    expect([...document.querySelectorAll('#model-provider option')].map((o) => o.textContent)).toEqual(['Tümü', 'Anthropic', 'OpenAI', 'Kimi']);

    const kind = document.getElementById('model-kind');
    const provider = document.getElementById('model-provider');
    kind.value = 'image';
    kind.dispatchEvent(new Event('change'));
    expect(names()).toEqual(['gpt-image-1']);
    provider.value = 'kimi';
    provider.dispatchEvent(new Event('change'));
    expect(names()).toEqual([]);
    expect(document.getElementById('models-empty').hidden).toBe(false);
    expect(document.getElementById('model-list').hidden).toBe(true);
    kind.value = '';
    kind.dispatchEvent(new Event('change'));
    expect(names()).toEqual(['Kimi K2']);
    expect(document.getElementById('models-empty').hidden).toBe(true);
  });

  it('AC-16: katalog alınamazsa Türkçe hata; ayarlar alınamazsa Türkçe hata', async () => {
    routes['GET /ayarlar/modeller'] = fail(502, { code: 'api.unavailable' });
    routes['GET /ayarlar/api-keyler'] = fail(502, { code: 'api.unavailable' });

    await initSettings(document);

    expect(document.getElementById('models-error').textContent).toBe('Modeller şu an yüklenemedi');
    expect(document.getElementById('models-error').hidden).toBe(false);
    expect(document.getElementById('models-loading').hidden).toBe(true);
    expect(document.getElementById('keys-error').textContent).toBe('Ayarlar şu an yüklenemedi');
  });

  it('Ayarlar bölümü olmayan sayfada istek yapılmaz; eski güncelleme zamanı tarihle gösterilir', async () => {
    document.body.innerHTML = '<main></main>';
    await initSettings(document);
    expect(calls).toHaveLength(0);

    const text = document.createElement('div');
    text.dataset.textVerified = 'Doğrulandı';
    text.dataset.textUpdatedAt = '{0} güncellendi';
    const old = statusText({ state: 'saved', hint: 'sk-…a3f', validation: 'verified', updatedAt: '2026-01-02T10:00:00Z' }, text.dataset, new Date('2026-11-16T10:00:00Z'));
    expect(old).toMatch(/^sk-…a3f · Doğrulandı · .*2026.* güncellendi$/);
  });
});
