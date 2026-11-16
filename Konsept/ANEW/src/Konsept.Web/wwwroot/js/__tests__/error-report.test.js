import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { get, post } from '../api.js';
import { attachReportLink, initErrorReport, validateDescription } from '../error-report.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

// _Layout.cshtml'deki "Hata bildir" düğmesi + _ErrorReportModal.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function layout() {
  return `
    <meta name="csrf-token" content="t" />
    <button type="button" id="report-open" aria-label="Hata bildir" data-text-link="Bildir" data-text-link-label="Bu hatayı bildir">!</button>
    <p id="profile-error"></p>
    <div id="report-modal" hidden data-url="/hata-bildir" data-text-sending="Gönderiliyor…" data-text-sent="Bildiriminiz alındı (No: {0})"
         data-text-failed="Bildirim gönderilemedi (hata no: {0}). Yazdığınız metin duruyor, tekrar deneyebilirsiniz."
         data-text-empty="Lütfen karşılaştığınız sorunu yazın." data-text-too-short="Açıklama en az 10 karakter olmalı."
         data-text-too-long="Açıklama en fazla 4000 karakter olabilir." data-text-journal-empty="Günlük'te kayıt yok"
         data-text-preview-failed="Günlük kayıtları şu an gösterilemiyor" data-text-forbidden="Bu işlem için yetkiniz yok">
      <div role="dialog" aria-modal="true">
        <form id="report-form" novalidate>
          <textarea id="report-description"></textarea>
          <p id="report-description-error" role="alert"></p>
          <dl><dd id="report-page"></dd><dd id="report-version"></dd><dd id="report-time"></dd>
            <dt id="report-correlation-label" hidden></dt><dd id="report-correlation" hidden></dd></dl>
          <ol id="report-journal"></ol>
          <p id="report-status" role="status"></p>
          <button type="button" id="report-cancel">İptal</button><button type="submit" id="report-submit">Gönder</button>
        </form>
      </div>
    </div>
    <span id="report-result-announce" role="status"></span><p id="report-result" hidden></p>`;
}

const $ = installJquery();
const el = (id) => document.getElementById(id);
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));
const preview = { version: '1.4.2', now: '2026-11-16T09:00:00Z', journal: [{ message: 'Giriş yapıldı', occurredAt: '2026-11-16T07:30:00Z' }] };
const texts = { textEmpty: 'boş', textTooShort: 'kısa', textTooLong: 'uzun' };

function start() {
  return initErrorReport(document, { now: () => new Date('2026-11-16T09:15:00Z'), location: { pathname: '/uretilenler' } });
}

function ajaxByUrl(handlers) {
  return vi.spyOn($, 'ajax').mockImplementation((settings) => handlers(settings));
}

function submit() {
  el('report-form').dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }));
}

describe('Hata bildir modalı (spec 0022)', () => {
  beforeEach(() => {
    document.body.innerHTML = layout();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('düğme (tık; klavyede yerel <button>) modalı açar, odak açıklamaya alınır; sayfa, zaman, versiyon ve son Günlük kayıtları görünür (AC-1)', async () => {
    ajaxByUrl(() => ajaxResolving($, preview));
    start();

    el('report-open').click();
    await flush();

    expect(el('report-modal').hidden).toBe(false);
    expect(document.activeElement).toBe(el('report-description'));
    expect(el('report-page').textContent).toBe('/uretilenler');
    expect(el('report-time').textContent).toBe('12:15');
    expect(el('report-version').textContent).toBe('1.4.2');
    expect(el('report-journal').textContent).toContain('10:30 — Giriş yapıldı');
    expect(el('report-correlation').hidden).toBe(true);
    expect(el('report-open').tagName).toBe('BUTTON');
  });

  it('geçerli açıklamayla Gönder numarayı gösterir, modal kapanır; istek açıklama + sayfa taşır (AC-1)', async () => {
    const ajax = ajaxByUrl((s) => (s.method === 'GET' ? ajaxResolving($, preview) : ajaxResolving($, { number: 42 })));
    start();
    el('report-open').click();
    el('report-description').value = 'Video üretimi yarıda kaldı';

    submit();
    await flush();

    const sent = ajax.mock.calls.map((c) => c[0]).find((s) => s.method === 'POST');
    expect(sent.url).toBe('/hata-bildir');
    expect(JSON.parse(sent.data)).toEqual({ description: 'Video üretimi yarıda kaldı', page: '/uretilenler', correlationId: null });
    expect(el('report-modal').hidden).toBe(true);
    expect(el('report-result').textContent).toBe('Bildiriminiz alındı (No: 42)');
    expect(el('report-result-announce').textContent).toBe('Bildiriminiz alındı (No: 42)');
  });

  it('İptal ve Esc istek göndermeden kapatır, odak "Hata bildir"e döner, yeniden açınca alan boştur (AC-2)', async () => {
    const ajax = ajaxByUrl(() => ajaxResolving($, preview));
    start();
    el('report-open').click();
    el('report-description').value = 'yarım kalan metin burada';
    el('report-cancel').click();
    expect(el('report-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(el('report-open'));

    el('report-open').click();
    expect(el('report-description').value).toBe('');
    el('report-description').value = 'yine yarım kalan metin';
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await flush();

    expect(el('report-modal').hidden).toBe(true);
    expect(document.activeElement).toBe(el('report-open'));
    el('report-open').click();
    expect(el('report-description').value).toBe('');
    expect(ajax.mock.calls.some((c) => c[0].method === 'POST')).toBe(false);
  });

  it('boş, 9 ve 4001 karakter alana bağlı Türkçe mesajla reddedilir, istek gitmez; 10 ve 4000 geçerlidir (AC-3)', async () => {
    expect(validateDescription('', texts)).toBe('boş');
    expect(validateDescription('a'.repeat(9), texts)).toBe('kısa');
    expect(validateDescription('a'.repeat(4001), texts)).toBe('uzun');
    expect(validateDescription('a'.repeat(10), texts)).toBe('');
    expect(validateDescription('a'.repeat(4000), texts)).toBe('');

    const ajax = ajaxByUrl(() => ajaxResolving($, preview));
    start();
    el('report-open').click();
    el('report-description').value = '123456789';
    submit();
    await flush();

    expect(el('report-description-error').textContent).toBe('Açıklama en az 10 karakter olmalı.');
    expect(el('report-description').getAttribute('aria-invalid')).toBe('true');
    expect(ajax.mock.calls.some((c) => c[0].method === 'POST')).toBe(false);
  });

  it('gönderiliyorken düğme pasiftir, çift tık tek istek; sunucu hatasında metin korunur ve tekrar denenebilir (AC-3)', async () => {
    let finish;
    const ajax = ajaxByUrl((s) => {
      if (s.method === 'GET') {
        return ajaxResolving($, preview);
      }
      const deferred = $.Deferred();
      finish = () => deferred.reject({ status: 503, responseJSON: { message: 'x', correlationId: 'corr-9' } });
      return deferred.promise();
    });
    start();
    el('report-open').click();
    el('report-description').value = 'Ayarlar sayfası açılmıyor';

    submit();
    submit();
    expect(el('report-submit').disabled).toBe(true);
    expect(el('report-status').textContent).toBe('Gönderiliyor…');
    finish();
    await flush();

    expect(ajax.mock.calls.filter((c) => c[0].method === 'POST')).toHaveLength(1);
    expect(el('report-modal').hidden).toBe(false);
    expect(el('report-description').value).toBe('Ayarlar sayfası açılmıyor');
    expect(el('report-status').textContent).toContain('hata no: corr-9');
    expect(el('report-submit').disabled).toBe(false);
  });

  it('sınır aşılınca "Çok fazla bildirim…" gösterilir (AC-12)', async () => {
    ajaxByUrl((s) => (s.method === 'GET'
      ? ajaxResolving($, preview)
      : ajaxRejecting($, 429, { message: 'Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin' })));
    start();
    el('report-open').click();
    el('report-description').value = 'on birinci bildirim';
    submit();
    await flush();

    expect(el('report-status').textContent).toBe('Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin');
  });

  it('ProblemDetails hata mesajındaki "Bildir" modalı o korelasyonla açar ve gönderilen kayıtta bulunur (AC-4)', async () => {
    const ajax = ajaxByUrl((s) => (s.method === 'GET' ? ajaxResolving($, preview) : ajaxResolving($, { number: 7 })));
    start();
    el('profile-error').textContent = 'Profil şu an kaydedilemedi (hata no: corr-77)';
    attachReportLink(el('profile-error'), { status: 503, body: { correlationId: 'corr-77' } });
    attachReportLink(el('profile-error'), { status: 400, body: { correlationId: 'corr-x' } }); // 4xx: bağlantı yok

    const links = el('profile-error').querySelectorAll('[data-report-correlation]');
    expect(links).toHaveLength(1);
    expect(links[0].textContent).toBe('Bildir');
    links[0].click();
    await flush();
    expect(el('report-modal').hidden).toBe(false);
    expect(el('report-correlation').textContent).toBe('corr-77');
    expect(el('report-correlation').hidden).toBe(false);
    el('report-description').value = 'Kaydet düğmesi hata verdi';
    submit();
    await flush();

    const sent = ajax.mock.calls.map((c) => c[0]).find((s) => s.method === 'POST');
    expect(JSON.parse(sent.data).correlationId).toBe('corr-77');
    expect(document.querySelector('#report-modal input[type="file"]')).toBeNull();
  });

  it('düğmeyle açılınca son başarısız isteğin korelasyon numarası eklenir (Req 1)', async () => {
    ajaxByUrl((s) => (s.url === '/profiller'
      ? ajaxRejecting($, 503, { correlationId: 'corr-last' })
      : ajaxResolving($, preview)));
    start();
    await get('/profiller').catch(() => {});
    el('report-open').click();
    await flush();

    expect(el('report-correlation').textContent).toBe('corr-last');
    expect(typeof post).toBe('function');
  });

  it('açıklama ve Günlük metni düz metin yazılır (HTML çalıştırılmaz)', async () => {
    ajaxByUrl(() => ajaxResolving($, { ...preview, journal: [{ message: '<img src=x onerror=alert(1)>', occurredAt: '2026-11-16T07:30:00Z' }] }));
    start();
    el('report-open').click();
    await flush();

    expect(el('report-journal').querySelector('img')).toBeNull();
    expect(el('report-journal').textContent).toContain('<img src=x onerror=alert(1)>');
  });
});
