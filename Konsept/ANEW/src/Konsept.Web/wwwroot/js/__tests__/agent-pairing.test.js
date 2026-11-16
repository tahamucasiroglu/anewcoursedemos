import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { initPairing } from '../agent-pairing.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Views/AgentPairing/Index.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <section id="pairing" data-lookup-url="/ajan-eslestir/sorgula" data-approve-url="/ajan-eslestir/onayla"
             data-reject-url="/ajan-eslestir/reddet" data-text-sending="Gönderiliyor…"
             data-text-approved='Onaylandı. Ajan birkaç saniye içinde "Eşleşti" gösterecek.'
             data-text-rejected="Reddedildi. Ajan yeni kod alabilir." data-text-failed="İşlem tamamlanamadı, tekrar deneyin."
             data-text-code-required="Ajan penceresindeki kodu girin." data-text-device-format="{0} · sürüm {1}">
      <form id="pairing-form" novalidate>
        <label for="pairing-code">Eşleştirme kodu</label>
        <input id="pairing-code" aria-describedby="pairing-error" />
        <button id="pairing-lookup" type="submit">Kodu gönder</button>
      </form>
      <div id="pairing-device" hidden>
        <strong id="pairing-device-name"></strong>
        <button id="pairing-approve" type="button">Onayla</button>
        <button id="pairing-reject" type="button">Reddet</button>
      </div>
      <p id="pairing-status" role="status" aria-live="polite"></p>
      <p id="pairing-error" role="alert" hidden></p>
    </section>`;
}

const device = { code: 'ABCD-2345', machineName: 'TAHA-PC', agentVersion: '0.5.0', expiresAt: '2026-11-16T09:10:00Z' };
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

describe('"Ajanı eşleştir" sayfası (spec 0012 AC-1, AC-4, AC-10, AC-14)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    initPairing(document);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('Enter (form gönderimi) kodu sorar; cihaz makine adı + sürümle görünür, odak "Onayla"ya gider', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, device));
    el('pairing-code').value = ' abcd-2345 ';

    el('pairing-form').requestSubmit();
    await flush();

    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({ url: '/ajan-eslestir/sorgula', method: 'POST', data: JSON.stringify({ code: 'abcd-2345' }) }));
    expect(el('pairing-device').hidden).toBe(false);
    expect(el('pairing-device-name').textContent).toBe('TAHA-PC · sürüm 0.5.0');
    expect(document.activeElement).toBe(el('pairing-approve'));
  });

  it('gönderilirken "Gönderiliyor…" görünür, düğmeler pasiftir ve çift tıklama ikinci onay isteği göndermez', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, device));
    el('pairing-code').value = 'ABCD-2345';
    el('pairing-form').requestSubmit();
    await flush();

    const pending = $.Deferred();
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(pending.promise());
    ajax.mockClear();
    el('pairing-approve').click();
    el('pairing-approve').click();

    expect(el('pairing-status').textContent).toBe('Gönderiliyor…');
    expect(el('pairing-approve').disabled).toBe(true);
    expect(el('pairing-reject').disabled).toBe(true);
    expect(el('pairing-lookup').disabled).toBe(true);
    expect(ajax).toHaveBeenCalledTimes(1);

    pending.resolve(device);
    await flush();

    expect(el('pairing-status').textContent).toBe('Onaylandı. Ajan birkaç saniye içinde "Eşleşti" gösterecek.');
    expect(el('pairing-approve').disabled).toBe(false);
    expect(el('pairing-device').hidden).toBe(true);
  });

  it('"Reddet" sonrası "Reddedildi" görünür', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, device));
    el('pairing-code').value = 'ABCD-2345';
    el('pairing-form').requestSubmit();
    await flush();

    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, device));
    el('pairing-reject').click();
    await flush();

    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({ url: '/ajan-eslestir/reddet' }));
    expect(el('pairing-status').textContent).toBe('Reddedildi. Ajan yeni kod alabilir.');
  });

  it('kod geçersiz/süresi dolmuş/çok fazla deneme: Türkçe hata role="alert" ile duyurulur ve odak kod alanına döner', async () => {
    for (const [status, message] of [
      [404, 'Kod bulunamadı. Ajan penceresindeki kodu kontrol edin.'],
      [409, 'Kodun süresi dolmuş. Ajanda yeni kod alın.'],
      [429, 'Çok fazla deneme, biraz sonra tekrar deneyin'],
    ]) {
      vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, status, { code: 'x', message }));
      el('pairing-code').value = 'ZZZZ-9999';
      el('pairing-lookup').focus();

      el('pairing-form').requestSubmit();
      await flush();

      expect(el('pairing-error').hidden).toBe(false);
      expect(el('pairing-error').textContent).toBe(message);
      expect(el('pairing-error').getAttribute('role')).toBe('alert');
      expect(el('pairing-code').getAttribute('aria-invalid')).toBe('true');
      expect(document.activeElement).toBe(el('pairing-code'));
      expect(el('pairing-device').hidden).toBe(true);
    }
  });

  it('boş kod gönderilmez; "kodu girin" hatası alanla ilişkili görünür', () => {
    const ajax = vi.spyOn($, 'ajax');

    el('pairing-form').requestSubmit();

    expect(ajax).not.toHaveBeenCalled();
    expect(el('pairing-error').textContent).toBe('Ajan penceresindeki kodu girin.');
    expect(el('pairing-code').getAttribute('aria-describedby')).toBe('pairing-error');
    expect(document.activeElement).toBe(el('pairing-code'));
  });
});
