import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { bindPassiveControls } from '../passive-controls.js';
import { initProfiles, toSeconds, validate } from '../profiles.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// _Layout.cshtml'deki profil çubuğu + _ProfileModal.cshtml + profil gerektiren sayfanın en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar" data-state="loading" data-url="/profiller" data-text-loading="Profiller yükleniyor…"
         data-text-failed="Profiller yüklenemedi" data-text-edit-none="Düzenlemek için önce bir profil oluşturun"
         data-text-edit-unavailable="Profiller yüklenmeden düzenlenemez" data-text-limit="En fazla {0} profil oluşturabilirsiniz"
         data-text-select-failed="Profil seçilemedi (hata no: {0})">
      <span id="profile-status" role="status">Profiller yükleniyor…</span>
      <select id="profile-select" aria-label="Profil seç" hidden></select>
      <button type="button" id="profile-create" hidden>Profil oluşturun</button>
      <span id="profile-alert" role="alert"></span>
    </div>
    <button type="button" id="profile-edit" aria-disabled="true" aria-label="Seçili profili düzenle">✎</button>
    <button type="button" id="profile-new" aria-label="Yeni profil">+</button>
    <main><p id="profile-required" hidden>Önce bir profil oluşturun</p><p class="empty-state" data-requires-profile>Henüz konsept yok.</p></main>
    <div id="profile-modal" hidden data-text-new="Yeni profil" data-text-edit="Profili düzenle" data-text-save="Kaydet"
         data-text-saving="Kaydediliyor…" data-text-save-failed="Profil şu an kaydedilemedi (hata no: {0})"
         data-text-delete-failed="Profil şu an silinemedi (hata no: {0})" data-text-delete-blocked="Bu profilin konseptleri olduğu için silinemez"
         data-text-confirm-template="Doldurduğunuz alanlar şablonla değiştirilecek" data-text-confirm-discard="Değişiklikler kaydedilmeyecek"
         data-text-confirm-delete="Bu profil kalıcı olarak silinecek" data-text-forbidden="Bu işlem için yetkiniz yok"
         data-text-name-required="Kanal adını girin." data-text-name-too-long="Kanal adı en fazla 80 karakter olabilir."
         data-text-language-required="Listeden bir video dili seçin." data-text-format-required="Yatay ya da dikey biçimi seçin."
         data-text-duration-invalid="Hedef süre 15 saniye ile 15 dakika arasında olmalı." data-text-content-required="Kanal içeriğini girin."
         data-text-content-too-long="Kanal içeriği en fazla 2000 karakter olabilir.">
      <div role="dialog" aria-modal="true" aria-labelledby="profile-modal-title">
        <h2 id="profile-modal-title">Yeni profil</h2>
        <button type="button" id="profile-modal-close" aria-label="Kapat">×</button>
        <form id="profile-form" novalidate>
          <div id="profile-template-field"><label for="profile-template">Hazır şablon</label>
            <select id="profile-template"><option value="">Şablon seçin</option></select></div>
          <label for="profile-name">Kanal adı</label><input id="profile-name" aria-describedby="profile-name-error" /><p id="profile-name-error"></p>
          <label for="profile-language">Video dili</label>
          <select id="profile-language" aria-describedby="profile-language-error"><option value="tr">Türkçe</option><option value="en">İngilizce</option></select>
          <p id="profile-language-error"></p>
          <fieldset aria-describedby="profile-format-error"><legend>Biçim</legend>
            <label><input type="radio" name="format" value="landscape" id="profile-format-landscape" /> Yatay</label>
            <label><input type="radio" name="format" value="portrait" id="profile-format-portrait" /> Dikey</label>
            <p id="profile-format-error"></p></fieldset>
          <label for="profile-minutes">Dakika</label><input id="profile-minutes" type="number" aria-describedby="profile-duration-error" />
          <label for="profile-seconds">Saniye</label><input id="profile-seconds" type="number" aria-describedby="profile-duration-error" />
          <p id="profile-duration-error"></p>
          <label for="profile-content">Kanal içeriği</label><textarea id="profile-content" aria-describedby="profile-content-error"></textarea>
          <p id="profile-content-error"></p>
          <p id="profile-form-error" role="alert"></p>
          <button type="button" id="profile-delete">Sil</button><span id="profile-delete-reason" hidden>Bu profilin konseptleri olduğu için silinemez</span>
          <button type="button" id="profile-cancel">Vazgeç</button>
          <button type="submit" id="profile-save">Kaydet</button>
        </form>
      </div>
    </div>
    <div id="confirm-dialog" hidden>
      <div role="alertdialog" aria-modal="true"><p id="confirm-text"></p>
        <button type="button" id="confirm-cancel">Vazgeç</button><button type="button" id="confirm-ok">Onayla</button></div>
    </div>`;
}

const templates = [
  { id: 'tarih-bizans', title: 'Tarih – Bizans', channelName: "Bizans'ın İzinde", language: 'tr', format: 'landscape', durationSeconds: 600, content: 'Bizans içerik.' },
  { id: 'teknoloji', title: 'Teknoloji', channelName: 'Teknoloji Rehberi', language: 'tr', format: 'portrait', durationSeconds: 60, content: 'Teknoloji içerik.' },
  { id: 'tarih-osmanli', title: 'Tarih – Osmanlı', channelName: 'Osmanlı Kronikleri', language: 'tr', format: 'landscape', durationSeconds: 720, content: 'O.' },
  { id: 'antik-teknoloji', title: 'Antik teknoloji', channelName: 'Antik Mühendislik', language: 'tr', format: 'landscape', durationSeconds: 480, content: 'A.' },
  { id: 'gunumuz-teknolojisi', title: 'Günümüz teknolojisi', channelName: 'Bugünün Teknolojisi', language: 'tr', format: 'portrait', durationSeconds: 90, content: 'G.' },
];
const profile = (id, name, extra = {}) => ({
  id, channelName: name, language: 'en', format: 'portrait', durationSeconds: 95, content: `${name} içerik`, createdAt: '2026-11-16T10:00:00Z', canDelete: true, ...extra,
});
const listOf = (items, selectedId = items[0] && items[0].id) => ({ items, selectedId: selectedId || null, maxProfiles: 20, templates });

let routes;
let calls;
let reload;

const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const pending = () => () => $.Deferred().promise();
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));
const key = (target, name, extra = {}) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, ...extra }));
const posts = () => calls.filter((c) => c.method === 'POST');
const body = (call) => JSON.parse(call.data);
const confirmShown = () => !el('confirm-dialog').hidden;
const modalShown = () => !el('profile-modal').hidden;

function fillValid() {
  el('profile-name').value = '  Yeni Kanal ';
  el('profile-format-landscape').checked = true;
  el('profile-minutes').value = '1';
  el('profile-seconds').value = '30';
  el('profile-content').value = 'Kanal içeriği';
}

async function start(list) {
  routes['GET /profiller'] = ok(list);
  const controller = await initProfiles(document, { reload });
  return controller;
}

describe('Profiller (spec 0003)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    reload = vi.fn();
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-13: yükleniyor durumunda kalem pasif, sayfa listesi gizli; liste alınamazsa hata metni', async () => {
    routes['GET /profiller'] = pending();
    initProfiles(document, { reload });
    await flush();
    expect(el('profile-status').textContent).toBe('Profiller yükleniyor…');
    expect(el('profile-edit').getAttribute('aria-disabled')).toBe('true');
    expect(document.querySelector('[data-requires-profile]').hidden).toBe(true);
    expect(el('profile-required').hidden).toBe(true);

    document.body.innerHTML = pageHtml();
    routes['GET /profiller'] = fail(502, { code: 'api.unavailable', message: 'x' });
    await initProfiles(document, { reload });
    expect(el('profile-status').textContent).toBe('Profiller yüklenemedi');
    expect(el('profile-status').hidden).toBe(false);
    expect(el('profile-edit').getAttribute('aria-disabled')).toBe('true');
    expect(el('profile-edit').title).toBe('Profiller yüklenmeden düzenlenemez');
    expect(document.querySelector('[data-requires-profile]').hidden).toBe(true);
    expect(el('profile-select').hidden).toBe(true);
  });

  it('AC-12: profil yokken boş durum, yönlendirme yeni modalı açar, kalem pasif + neden, sayfa "Önce bir profil oluşturun"', async () => {
    await start(listOf([]));

    expect(el('profile-select').hidden).toBe(true);
    expect(el('profile-create').hidden).toBe(false);
    expect(el('profile-edit').getAttribute('aria-disabled')).toBe('true');
    expect(el('profile-edit').title).toBe('Düzenlemek için önce bir profil oluşturun');
    expect(el('profile-required').hidden).toBe(false);
    expect(document.querySelector('[data-requires-profile]').hidden).toBe(true);
    expect(posts()).toHaveLength(0);

    el('profile-create').click();
    expect(modalShown()).toBe(true);
    expect(el('profile-modal-title').textContent).toBe('Yeni profil');
  });

  it('AC-1/AC-2: artı boş modal açar (Türkçe varsayılan, odak ilk alanda); Kaydet kırpılmamış değerleri gönderir, modal kapanır, sayfa yeniden yüklenir', async () => {
    await start(listOf([profile('a', 'Alfa')]));
    routes['POST /profiller'] = ok(profile('n', 'Yeni Kanal'));
    el('profile-new').focus();

    el('profile-new').click();
    expect(modalShown()).toBe(true);
    expect(el('profile-name').value).toBe('');
    expect(el('profile-language').value).toBe('tr');
    expect(el('profile-format-landscape').checked || el('profile-format-portrait').checked).toBe(false);
    expect(el('profile-minutes').value).toBe('');
    expect(el('profile-content').value).toBe('');
    expect(el('profile-template-field').hidden).toBe(false);
    expect(document.activeElement).toBe(el('profile-template'));

    fillValid();
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();

    expect(posts()).toHaveLength(1);
    expect(body(posts()[0])).toEqual({ channelName: '  Yeni Kanal ', language: 'tr', format: 'landscape', durationSeconds: 90, content: 'Kanal içeriği' });
    expect(modalShown()).toBe(false);
    expect(reload).toHaveBeenCalledTimes(1);
    expect(document.activeElement).toBe(el('profile-new'));
  });

  it('AC-3: beş şablon listelenir; seçilince alanlar dolar; değiştirilip kaydedilir; kaydetmeden kapatılırsa istek yok', async () => {
    await start(listOf([profile('a', 'Alfa')]));
    routes['POST /profiller'] = ok(profile('n', 'X'));
    el('profile-new').click();

    const options = [...el('profile-template').options].map((o) => o.textContent);
    expect(options).toEqual(['Şablon seçin', 'Tarih – Bizans', 'Teknoloji', 'Tarih – Osmanlı', 'Antik teknoloji', 'Günümüz teknolojisi']);
    el('profile-template').value = 'teknoloji';
    el('profile-template').dispatchEvent(new Event('change'));
    await flush();
    expect(confirmShown()).toBe(false);
    expect(el('profile-name').value).toBe('Teknoloji Rehberi');
    expect(el('profile-format-portrait').checked).toBe(true);
    expect(el('profile-minutes').value).toBe('1');
    expect(el('profile-seconds').value).toBe('0');
    expect(el('profile-content').value).toBe('Teknoloji içerik.');
    expect(posts()).toHaveLength(0);

    // Şablon seçip kaydetmeden kapatma: onay → kapanır, hiçbir istek yok.
    el('profile-cancel').click();
    await flush();
    expect(confirmShown()).toBe(true);
    el('confirm-ok').click();
    await flush();
    expect(modalShown()).toBe(false);
    expect(posts()).toHaveLength(0);

    // Şablonu değiştirip kaydetme: değiştirilmiş değerler gider.
    el('profile-new').click();
    el('profile-template').value = 'tarih-bizans';
    el('profile-template').dispatchEvent(new Event('change'));
    await flush();
    el('profile-name').value = 'Benim Bizans';
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(body(posts()[0])).toMatchObject({ channelName: 'Benim Bizans', format: 'landscape', durationSeconds: 600, content: 'Bizans içerik.' });
  });

  it('AC-4: kullanıcı alan doldurmuşken şablon onay ister; vazgeçince alanlar aynı, onaylayınca şablon yazılır; düzenlemede şablon yok', async () => {
    await start(listOf([profile('a', 'Alfa')]));
    el('profile-new').click();
    el('profile-name').value = 'Elle yazdım';

    el('profile-template').value = 'teknoloji';
    el('profile-template').dispatchEvent(new Event('change'));
    await flush();
    expect(el('confirm-text').textContent).toBe('Doldurduğunuz alanlar şablonla değiştirilecek');
    el('confirm-cancel').click();
    await flush();
    expect(el('profile-name').value).toBe('Elle yazdım');
    expect(el('profile-template').value).toBe('');

    el('profile-template').value = 'teknoloji';
    el('profile-template').dispatchEvent(new Event('change'));
    await flush();
    el('confirm-ok').click();
    await flush();
    expect(el('profile-name').value).toBe('Teknoloji Rehberi');

    // Şablonla dolmuş alanlar kullanıcı girdisi sayılmaz: başka şablon onaysız uygulanır.
    el('profile-template').value = 'tarih-bizans';
    el('profile-template').dispatchEvent(new Event('change'));
    await flush();
    expect(confirmShown()).toBe(false);
    expect(el('profile-name').value).toBe("Bizans'ın İzinde");

    el('profile-cancel').click();
    await flush();
    el('confirm-ok').click();
    await flush();
    el('profile-edit').click();
    expect(el('profile-template-field').hidden).toBe(true);
  });

  it('AC-5/AC-11: kalem seçili profili değerleriyle açar ve o profile kaydeder; seçim listesi sunucu sırasında, seçim POST + yeniden yükleme', async () => {
    await start(listOf([profile('c', 'Cem'), profile('ç', 'Çağ', { language: 'tr', format: 'landscape', durationSeconds: 900 }), profile('d', 'Dünya')], 'ç'));
    routes['POST /profiller/%C3%A7'] = ok(profile('ç', 'Çağ 2'));
    routes['POST /profiller/secili'] = ok(profile('d', 'Dünya'));

    expect([...el('profile-select').options].map((o) => o.textContent)).toEqual(['Cem', 'Çağ', 'Dünya']);
    expect(el('profile-select').value).toBe('ç');
    expect(el('profile-edit').hasAttribute('aria-disabled')).toBe(false);

    el('profile-edit').click();
    expect(el('profile-modal-title').textContent).toBe('Profili düzenle');
    expect(el('profile-name').value).toBe('Çağ');
    expect(el('profile-language').value).toBe('tr');
    expect(el('profile-format-landscape').checked).toBe(true);
    expect(el('profile-minutes').value).toBe('15');
    expect(el('profile-seconds').value).toBe('0');
    el('profile-name').value = 'Çağ 2';
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(posts()[0].url).toBe('/profiller/%C3%A7');
    expect(body(posts()[0]).channelName).toBe('Çağ 2');
    expect(reload).toHaveBeenCalledTimes(1);

    el('profile-select').value = 'd';
    el('profile-select').dispatchEvent(new Event('change'));
    await flush();
    expect(posts()[1].url).toBe('/profiller/secili');
    expect(body(posts()[1])).toEqual({ profileId: 'd' });
    expect(reload).toHaveBeenCalledTimes(2);
  });

  it('AC-6: değişiklik yoksa Esc/kapat doğrudan kapatır; değişiklik varsa onay — vazgeçince değerlerle açık kalır', async () => {
    await start(listOf([profile('a', 'Alfa')]));
    el('profile-edit').click();
    key(el('profile-name'), 'Escape');
    await flush();
    expect(confirmShown()).toBe(false);
    expect(modalShown()).toBe(false);

    el('profile-edit').click();
    el('profile-content').value = 'değişti';
    el('profile-modal-close').click();
    await flush();
    expect(el('confirm-text').textContent).toBe('Değişiklikler kaydedilmeyecek');
    key(el('confirm-cancel'), 'Escape');
    await flush();
    expect(modalShown()).toBe(true);
    expect(el('profile-content').value).toBe('değişti');

    key(el('profile-content'), 'Escape');
    await flush();
    el('confirm-ok').click();
    await flush();
    expect(modalShown()).toBe(false);
    expect(posts()).toHaveLength(0);
  });

  it('AC-7/AC-8: arayüz kuralları — boş, yalnızca boşluk, sınırlar; hata alanın yanında, değerler korunur, istek gitmez', async () => {
    await start(listOf([profile('a', 'Alfa')]));
    el('profile-new').click();
    el('profile-name').value = '   ';
    el('profile-content').value = 'x'.repeat(2001);
    el('profile-minutes').value = '0';
    el('profile-seconds').value = '14';
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();

    expect(posts()).toHaveLength(0);
    expect(el('profile-name-error').textContent).toBe('Kanal adını girin.');
    expect(el('profile-name').getAttribute('aria-invalid')).toBe('true');
    expect(el('profile-format-error').textContent).toBe('Yatay ya da dikey biçimi seçin.');
    expect(el('profile-duration-error').textContent).toBe('Hedef süre 15 saniye ile 15 dakika arasında olmalı.');
    expect(el('profile-content-error').textContent).toBe('Kanal içeriği en fazla 2000 karakter olabilir.');
    expect(el('profile-content').value).toHaveLength(2001);
    expect(modalShown()).toBe(true);
    expect(document.activeElement).toBe(el('profile-name'));

    const m = el('profile-modal').dataset;
    const base = { channelName: 'a', language: 'tr', format: 'portrait', durationSeconds: 15, content: 'b' };
    expect(validate(base, m)).toEqual({});
    expect(validate({ ...base, channelName: ` ${'a'.repeat(80)} `, content: 'c'.repeat(2000), durationSeconds: 900 }, m)).toEqual({});
    expect(validate({ ...base, channelName: 'a'.repeat(81) }, m)).toEqual({ channelname: 'Kanal adı en fazla 80 karakter olabilir.' });
    expect(validate({ ...base, durationSeconds: 901 }, m)).toEqual({ durationseconds: 'Hedef süre 15 saniye ile 15 dakika arasında olmalı.' });
    expect(validate({ ...base, language: 'de' }, m)).toEqual({ language: 'Listeden bir video dili seçin.' });
    expect(toSeconds('15', '0')).toBe(900);
    expect(toSeconds('15', '1')).toBe(901);
    expect(toSeconds('', '15')).toBe(15);
    expect(toSeconds('1', '60')).toBeNull();
    expect(toSeconds('', '')).toBeNull();
  });

  it('AC-7: sunucunun alan hatası (aynı ad) alanın yanında gösterilir, değerler korunur, modal açık', async () => {
    await start(listOf([profile('a', 'İstanbul')]));
    routes['POST /profiller'] = fail(400, { code: 'profiles.invalid', message: 'Girilen bilgiler geçersiz.', errors: { ChannelName: ['Bu kanal adını başka bir profilinizde kullanıyorsunuz.'] } });
    el('profile-new').click();
    fillValid();
    el('profile-name').value = 'istanbul';
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();

    expect(el('profile-name-error').textContent).toBe('Bu kanal adını başka bir profilinizde kullanıyorsunuz.');
    expect(el('profile-name').value).toBe('istanbul');
    expect(modalShown()).toBe(true);
    expect(reload).not.toHaveBeenCalled();
  });

  it('AC-9: 20 profilde artı modalı açmaz ve sınır uyarısı gösterir', async () => {
    const items = Array.from({ length: 20 }, (_, i) => profile(`p${i}`, `Kanal ${i}`));
    await start(listOf(items));

    el('profile-new').click();

    expect(modalShown()).toBe(false);
    expect(el('profile-alert').textContent).toBe('En fazla 20 profil oluşturabilirsiniz');
  });

  it('AC-10: kayıt sürerken Kaydet pasif ve çift gönderim tek istek; sunucu hatasında modal açık, hata no ile mesaj, değerler korunur', async () => {
    await start(listOf([profile('a', 'Alfa')]));
    routes['POST /profiller'] = pending();
    el('profile-new').click();
    fillValid();
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(posts()).toHaveLength(1);
    expect(el('profile-save').disabled).toBe(true);
    expect(el('profile-save').textContent).toBe('Kaydediliyor…');
    key(el('profile-name'), 'Escape');
    await flush();
    expect(modalShown()).toBe(true);

    document.body.innerHTML = pageHtml();
    calls = [];
    await start(listOf([profile('a', 'Alfa')]));
    routes['POST /profiller'] = fail(502, { code: 'api.unavailable', message: 'x', correlationId: 'abc123' });
    el('profile-new').click();
    fillValid();
    el('profile-form').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(el('profile-form-error').textContent).toBe('Profil şu an kaydedilemedi (hata no: abc123)');
    expect(modalShown()).toBe(true);
    expect(el('profile-name').value).toBe('  Yeni Kanal ');
    expect(el('profile-save').disabled).toBe(false);
  });

  it('AC-14: Sil onay sorar; vazgeçince istek yok; onaylanınca silinir ve sayfa yenilenir; hatada profil kalır ve mesaj görünür; konseptli profilde Sil pasif', async () => {
    await start(listOf([profile('a', 'Alfa'), profile('b', 'Beta', { canDelete: false })]));
    el('profile-edit').click();
    expect(el('profile-delete').hidden).toBe(false);

    el('profile-delete').click();
    await flush();
    expect(el('confirm-text').textContent).toBe('Bu profil kalıcı olarak silinecek');
    el('confirm-cancel').click();
    await flush();
    expect(posts()).toHaveLength(0);
    expect(modalShown()).toBe(true);

    routes['POST /profiller/a/sil'] = fail(503, { correlationId: 'z9' });
    el('profile-delete').click();
    await flush();
    el('confirm-ok').click();
    await flush();
    expect(el('profile-form-error').textContent).toBe('Profil şu an silinemedi (hata no: z9)');
    expect(modalShown()).toBe(true);
    expect(reload).not.toHaveBeenCalled();

    routes['POST /profiller/a/sil'] = ok(listOf([profile('b', 'Beta')]));
    el('profile-delete').click();
    await flush();
    el('confirm-ok').click();
    await flush();
    expect(modalShown()).toBe(false);
    expect(reload).toHaveBeenCalledTimes(1);

    document.body.innerHTML = pageHtml();
    await start(listOf([profile('b', 'Beta', { canDelete: false })]));
    el('profile-edit').click();
    expect(el('profile-delete').disabled).toBe(true);
    expect(el('profile-delete-reason').hidden).toBe(false);
  });

  it('AC-18: odak modal içinde kalır (Tab/Shift+Tab), onay penceresinde de; pasif kalem klavyeyle de bir şey yapmaz', async () => {
    bindPassiveControls(document);
    await start(listOf([]));
    el('profile-edit').focus();
    key(el('profile-edit'), 'Enter');
    el('profile-edit').click();
    expect(modalShown()).toBe(false);

    el('profile-new').click();
    el('profile-save').focus();
    key(el('profile-save'), 'Tab');
    expect(document.activeElement).toBe(el('profile-modal-close'));
    key(el('profile-modal-close'), 'Tab', { shiftKey: true });
    expect(document.activeElement).toBe(el('profile-save'));

    el('profile-name').value = 'x';
    key(el('profile-name'), 'Escape');
    await flush();
    expect(document.activeElement).toBe(el('confirm-cancel'));
    key(el('confirm-cancel'), 'Tab');
    expect(document.activeElement).toBe(el('confirm-ok'));
    el('confirm-ok').click();
    await flush();
    expect(modalShown()).toBe(false);
    expect(el('profile-name').getAttribute('aria-describedby')).toBe('profile-name-error');
  });
});
