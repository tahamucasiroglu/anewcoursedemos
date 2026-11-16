// "Hata bildir" modalı (spec 0022 Req 1–3, 13): sağ üstteki düğme ve ProblemDetails hata mesajlarındaki "Bildir" bağlantısı açar.
// Otomatik eklenecek bilgiler (sayfa, versiyon, zaman, varsa son hatanın numarası, son 20 Günlük kaydı) gönderimden önce görünür;
// asıl bağlamı (Günlük, versiyon, kullanıcı) sunucu ekler. İptal/Esc metni atar; yalnızca gönderimde sunucu hatası olursa metin korunur.
// Gönderiliyorken düğme pasiftir (çift tık tek kayıt). Metinler resx'ten (data-text-*), içerik her zaman textContent ile yazılır.
import { get, lastCorrelationId, post } from './api.js';
import { formatTime } from './journal.js';

export const MIN_LENGTH = 10;
export const MAX_LENGTH = 4000;
const FOCUSABLE = 'button:not([disabled]):not([hidden]), textarea';
const RESULT_MS = 8000;

function format(template, value) {
  return (template || '').replace('{0}', value);
}

/**
 * Açıklamanın doğrulama mesajı (Api ile aynı kural: boş, 10'dan kısa, 4000'den uzun).
 * @param {string} text Açıklama.
 * @param {{textEmpty?:string, textTooShort?:string, textTooLong?:string}} t Metinler.
 * @returns {string} Mesaj; geçerliyse boş.
 */
export function validateDescription(text, t) {
  const length = (text || '').trim().length;
  if (length === 0) {
    return t.textEmpty || '';
  }
  if (length < MIN_LENGTH) {
    return t.textTooShort || '';
  }
  if (length > MAX_LENGTH) {
    return t.textTooLong || '';
  }
  return '';
}

/**
 * Bir ProblemDetails hata mesajının yanına "Bildir" bağlantısı ekler (Req 2): yalnızca sunucu hatasında (5xx) ve korelasyon numarası
 * varsa. Tıklanınca modal o numarayla açılır (initErrorReport'un belge düzeyindeki dinleyicisi).
 * @param {HTMLElement} element Mesajın yazıldığı öğe (metni önceden yazılmış olmalı).
 * @param {{status?:number, body?:any}} failure api.js hatası.
 */
export function attachReportLink(element, failure) {
  const correlationId = failure && failure.body && failure.body.correlationId;
  if (!element || !correlationId || !failure.status || failure.status < 500) {
    return;
  }
  const doc = element.ownerDocument;
  const opener = doc.getElementById('report-open');
  if (!opener) {
    return;
  }
  const link = doc.createElement('button');
  link.type = 'button';
  link.className = 'report-link';
  link.dataset.reportCorrelation = correlationId;
  link.textContent = opener.dataset.textLink || 'Bildir';
  link.setAttribute('aria-label', opener.dataset.textLinkLabel || link.textContent);
  element.append(' ', link);
}

/**
 * Modalı bağlar.
 * @param {Document} doc Belge.
 * @param {{now?: () => Date, location?: {pathname:string}}} [options] Testler için saat ve konum.
 * @returns {{open: (correlationId?: string) => void, close: () => void}|null} Denetim; düğme yoksa (girişsiz) null.
 */
export function initErrorReport(doc, options = {}) {
  const opener = doc.getElementById('report-open');
  const modal = doc.getElementById('report-modal');
  if (!opener || !modal) {
    return null;
  }
  const el = (id) => doc.getElementById(id);
  const t = modal.dataset;
  const form = el('report-form');
  const description = el('report-description');
  const error = el('report-description-error');
  const status = el('report-status');
  const submit = el('report-submit');
  const now = options.now || (() => new Date());
  const location = options.location || globalThis.location || { pathname: '/' };
  const state = { correlationId: '', sending: false, seq: 0 };

  function setError(message) {
    error.textContent = message;
    if (message) {
      description.setAttribute('aria-invalid', 'true');
    } else {
      description.removeAttribute('aria-invalid');
    }
  }

  function fillJournal(items) {
    const list = el('report-journal');
    list.textContent = '';
    if (!items || items.length === 0) {
      const li = doc.createElement('li');
      li.textContent = t.textJournalEmpty || '';
      list.append(li);
      return;
    }
    for (const item of items) {
      const li = doc.createElement('li');
      li.textContent = `${formatTime(item.occurredAt)} — ${item.message}`;
      list.append(li);
    }
  }

  async function loadPreview(seq) {
    el('report-version').textContent = '…';
    try {
      const preview = await get(`${t.url}/onizleme`);
      if (seq !== state.seq) {
        return;
      }
      el('report-version').textContent = preview.version || '';
      fillJournal(preview.journal || []);
    } catch {
      if (seq !== state.seq) {
        return;
      }
      el('report-version').textContent = '-';
      const list = el('report-journal');
      list.textContent = '';
      const li = doc.createElement('li');
      li.textContent = t.textPreviewFailed || '';
      list.append(li);
    }
  }

  function open(correlationId) {
    state.seq += 1;
    state.correlationId = correlationId || lastCorrelationId() || '';
    description.value = '';
    setError('');
    status.textContent = '';
    submit.disabled = false;
    el('report-page').textContent = location.pathname || '/';
    el('report-time').textContent = formatTime(now().toISOString());
    el('report-correlation').textContent = state.correlationId;
    el('report-correlation').hidden = !state.correlationId;
    el('report-correlation-label').hidden = !state.correlationId;
    modal.hidden = false;
    description.focus();
    loadPreview(state.seq);
  }

  function close() {
    if (modal.hidden) {
      return;
    }
    state.seq += 1;
    modal.hidden = true;
    description.value = ''; // İptal/Esc: yazılan metin atılır (Req 3).
    setError('');
    status.textContent = '';
    opener.focus();
  }

  function showResult(number) {
    const text = format(t.textSent, number);
    const box = el('report-result');
    const announce = el('report-result-announce');
    if (announce) {
      announce.textContent = text;
    }
    if (box) {
      box.textContent = text;
      box.hidden = false;
      setTimeout(() => {
        box.hidden = true;
      }, RESULT_MS);
    }
  }

  async function send() {
    if (state.sending) {
      return; // Çift tık tek kayıt (AC-3).
    }
    const message = validateDescription(description.value, t);
    setError(message);
    if (message) {
      description.focus();
      return;
    }
    state.sending = true;
    submit.disabled = true;
    status.textContent = t.textSending || '';
    try {
      const receipt = await post(t.url, {
        description: description.value,
        page: location.pathname || '/',
        correlationId: state.correlationId || null,
      });
      state.sending = false;
      close();
      showResult(receipt.number);
    } catch (failure) {
      state.sending = false;
      submit.disabled = false;
      const body = (failure && failure.body) || {};
      const field = body.errors && (body.errors.description || body.errors.Description);
      if (failure && failure.status === 400 && field && field[0]) {
        status.textContent = '';
        setError(field[0]);
      } else if (failure && failure.status === 429 && body.message) {
        status.textContent = body.message;
      } else if (failure && failure.status === 403) {
        status.textContent = t.textForbidden || '';
      } else {
        status.textContent = format(t.textFailed, body.correlationId || '-'); // Metin korunur (Req 3).
      }
    }
  }

  opener.addEventListener('click', () => open());
  el('report-cancel').addEventListener('click', close);
  form.addEventListener('submit', (event) => {
    event.preventDefault();
    send();
  });
  doc.addEventListener('click', (event) => {
    const link = event.target && event.target.closest ? event.target.closest('[data-report-correlation]') : null;
    if (link) {
      open(link.dataset.reportCorrelation);
    }
  });
  doc.addEventListener('keydown', (event) => {
    if (modal.hidden) {
      return;
    }
    if (event.key === 'Escape') {
      event.preventDefault();
      close();
      return;
    }
    if (event.key === 'Tab') {
      const nodes = [...modal.querySelectorAll(FOCUSABLE)];
      if (nodes.length === 0) {
        return;
      }
      const first = nodes[0];
      const last = nodes[nodes.length - 1];
      const active = doc.activeElement;
      if (!modal.contains(active)) {
        event.preventDefault();
        first.focus();
      } else if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    }
  });

  return { open, close };
}
