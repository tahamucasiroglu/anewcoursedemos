import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initSuggestionReset } from '../suggestion-reset.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Ayarlar'daki "Öneriler" bölümünün en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <section id="suggestion-reset" data-text-no-profile="Önce bir profil seçin" data-text-resetting="Sıfırlanıyor…"
             data-text-done="Öneriler sıfırlandı" data-text-failed="Öneriler sıfırlanamadı; hiçbir şey değişmedi.">
      <button type="button" id="suggestion-reset-button" aria-disabled="true">Önerileri sıfırla</button>
      <p id="suggestion-reset-status" role="status"></p>
      <div id="suggestion-reset-dialog" hidden>
        <div role="alertdialog"><p>Seçili profilde önerilip seçmediğin başlıklar yeniden önerilebilir olacak; üretilmiş ve silinmiş konseptler etkilenmez.</p>
          <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Sıfırla</button></div>
      </div>
    </section>`;
}

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 6; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = () => calls.filter((c) => c.method === 'POST');
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));
const select = (id) => document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id } }));

describe('Ayarlar → Önerileri sıfırla (spec 0016)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/ayarlar', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-9: seçili profil yokken pasif; tıklama onay açmaz ve istek göndermez', async () => {
    initSuggestionReset(document);
    expect(el('suggestion-reset-button').getAttribute('aria-disabled')).toBe('true');
    el('suggestion-reset-button').click();
    await flush();
    expect(el('suggestion-reset-dialog').hidden).toBe(true);
    expect(el('suggestion-reset-status').textContent).toBe('Önce bir profil seçin');
    expect(posts()).toHaveLength(0);
  });

  it('AC-9/AC-12: onay ne silineceğini söyler; Esc vazgeçer (odak düğmeye döner); onaylanınca seçili profil gönderilir', async () => {
    initSuggestionReset(document);
    select('p1');
    const button = el('suggestion-reset-button');
    expect(button.getAttribute('aria-disabled')).toBe('false');
    button.focus();
    button.click();
    expect(el('suggestion-reset-dialog').hidden).toBe(false);
    expect(el('suggestion-reset-dialog').textContent).toContain('üretilmiş ve silinmiş konseptler etkilenmez');
    expect(document.activeElement.textContent).toBe('Vazgeç');
    key(document.activeElement, 'Escape');
    await flush();
    expect(document.activeElement).toBe(button);
    expect(posts()).toHaveLength(0);

    routes['POST /oneriler/sifirla'] = ok({ profileId: 'p1', cleared: 3 });
    button.click();
    el('suggestion-reset-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(posts()).toHaveLength(1);
    expect(JSON.parse(posts()[0].data)).toEqual({ profileId: 'p1' });
    expect(el('suggestion-reset-status').textContent).toBe('Öneriler sıfırlandı');
  });

  it('AC-10: sunucu hatasında sade mesaj', async () => {
    initSuggestionReset(document);
    select('p1');
    routes['POST /oneriler/sifirla'] = fail(500, {});
    el('suggestion-reset-button').click();
    el('suggestion-reset-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(el('suggestion-reset-status').textContent).toBe('Öneriler sıfırlanamadı; hiçbir şey değişmedi.');
  });
});
