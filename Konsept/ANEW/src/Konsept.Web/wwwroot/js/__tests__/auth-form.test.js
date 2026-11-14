import { beforeEach, describe, expect, it } from 'vitest';
import { bindAuthForms, bindSubmitOnceForms, focusFirstInvalid } from '../auth-form.js';

function formHtml(invalidField = null) {
  const invalid = (name) => (invalidField === name ? 'aria-invalid="true"' : '');
  return `
    <form method="post" action="/kayit" data-auth-form data-text-sending="Gönderiliyor…">
      <input id="Name" name="Name" ${invalid('Name')} />
      <input id="Email" name="Email" ${invalid('Email')} />
      <input id="Password" name="Password" type="password" ${invalid('Password')} />
      <button type="submit">Kayıt ol</button>
    </form>`;
}

function submit(form) {
  const event = new Event('submit', { bubbles: true, cancelable: true });
  form.dispatchEvent(event);
  return event;
}

describe('auth-form.js — gönderim koruması ve odak (AC-16, AC-18)', () => {
  beforeEach(() => {
    document.body.innerHTML = formHtml();
  });

  it('gönderimde düğme pasifleşir ve "Gönderiliyor…" görünür', () => {
    bindAuthForms(document);
    const form = document.querySelector('form');
    const button = form.querySelector('button');

    const first = submit(form);

    expect(first.defaultPrevented).toBe(false);
    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-disabled')).toBe('true');
    expect(button.textContent).toBe('Gönderiliyor…');
    expect(form.getAttribute('aria-busy')).toBe('true');
  });

  it('çift tıklama tek gönderim yapar: ikinci gönderim engellenir', () => {
    bindAuthForms(document);
    const form = document.querySelector('form');
    let sent = 0;
    form.addEventListener('submit', (event) => {
      if (!event.defaultPrevented) {
        sent += 1;
      }
    });

    submit(form);
    const second = submit(form);

    expect(second.defaultPrevented).toBe(true);
    expect(sent).toBe(1);
  });

  it('çıkış formu tek gönderim yapar: çift tıklamada ikinci POST /cikis gitmez (review R12-1)', () => {
    document.body.innerHTML = `
      <form class="user-menu__logout" method="post" action="/cikis" data-submit-once>
        <button type="submit">Çıkış yap</button>
      </form>`;
    bindSubmitOnceForms(document);
    const form = document.querySelector('form');
    let sent = 0;
    form.addEventListener('submit', (event) => {
      if (!event.defaultPrevented) {
        sent += 1;
      }
    });

    submit(form);
    const second = submit(form);

    expect(second.defaultPrevented).toBe(true);
    expect(sent).toBe(1);
    expect(form.querySelector('button').disabled).toBe(true);
    expect(form.querySelector('button').textContent).toBe('Çıkış yap');
  });

  it('alan hatası yoksa odak form düzeyi mesaja gider (ekran okuyucu okur)', () => {
    document.body.innerHTML = `<p id="form-message" role="alert" tabindex="-1" autofocus>Şu an işleminizi yapamıyoruz (hata no: x)</p>${formHtml()}`;

    const focused = focusFirstInvalid(document);

    expect(focused.id).toBe('form-message');
    expect(document.activeElement.id).toBe('form-message');
  });

  it('hata sonrası odak ilk hatalı alana gider', () => {
    document.body.innerHTML = formHtml('Email');

    const focused = focusFirstInvalid(document);

    expect(focused.id).toBe('Email');
    expect(document.activeElement.id).toBe('Email');
  });
});
