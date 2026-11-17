// Admin Hata bildirimleri sekmesi (spec 0025): liste (25'erli sayfa, en yeni üstte; kaynak Tümü/Web/Ajan/API, durum Açık/Yeni/
// İnceleniyor/Çözüldü/Yok sayıldı/Tümü — varsayılan Açık), detay penceresi ve durum + not değiştirme. Veri /admin/hata-bildirimleri'nden
// $.ajax ile (api.js, K-6) gelir; Api verisi yalnızca textContent ile yazılır (bildirimdeki <script> çalışmaz, düz metin görünür).
// Pencere: odak pencereye taşınır, Tab içeride döner, Esc kapatır (kayıt sürerken değil), kapanınca odak açan düğmeye döner; kayıt
// sürerken Kaydet pasif ve "İşleniyor…"; hata pencerede gösterilir, durum değişmez; not 500 karakteri aşınca Kaydet pasif + uyarı.
import { get, post } from './api.js';
import { failureText } from './admin-users.js';

const NOTE_MAX = 500;

function fill(template, ...values) {
  return values.reduce((text, value, i) => text.replace(`{${i}}`, String(value)), template || '');
}

/**
 * Hata bildirimleri sekmesini başlatır.
 * @param {Document} doc Belge.
 * @param {{ formatTime: (iso: string) => string, correlation: string, loading: string, pageText: string,
 *   onStatusChanged?: () => void }} options Saat biçimi, metinler ve durum değişince çağrılan geri bildirim (İstatistikler yenilenir).
 * @returns {{ load: Function, open: Function, save: Function }|null} Denetim.
 */
export function initAdminReports(doc, options) {
  const panel = doc.getElementById('admin-reports');
  if (!panel) {
    return null;
  }
  const t = panel.dataset;
  const el = (id) => doc.getElementById(id);
  const state = { page: 1, source: 'all', status: 'open', request: 0, detailRequest: 0, dialog: null };
  const text = (failure, fallback) => failureText(failure, fallback, options.correlation);
  const sourceText = (source) => ({ web: t.textSourceWeb, agent: t.textSourceAgent, api: t.textSourceApi })[source] || source;
  const statusText = (status) => ({
    new: t.textStatusNew, reviewing: t.textStatusReviewing, resolved: t.textStatusResolved, ignored: t.textStatusIgnored,
  })[status] || status;
  const orNone = (value) => (value === null || value === undefined || value === '' ? t.textNone : String(value));

  function row(item) {
    const tr = doc.createElement('tr');
    const cell = (value) => {
      const td = doc.createElement('td');
      td.textContent = value;
      return td;
    };
    const reporter = item.reporterName === t.textNone ? t.textNone : `${item.reporterName} (${item.reporterEmail})`;
    const status = cell(statusText(item.status));
    status.className = `admin-status admin-status--${item.status}`;
    const action = doc.createElement('td');
    const button = doc.createElement('button');
    button.type = 'button';
    button.className = 'button';
    button.dataset.reportNumber = String(item.number);
    button.textContent = t.textOpen;
    button.setAttribute('aria-label', fill(t.textOpenLabel, item.number));
    button.addEventListener('click', () => open(item.number, button));
    action.append(button);
    tr.append(cell(options.formatTime(item.createdAt)), cell(sourceText(item.source)), cell(reporter), cell(item.summary), status, action);
    return tr;
  }

  async function load() {
    const request = ++state.request;
    const status = el('admin-reports-status');
    const table = el('admin-report-table');
    status.textContent = options.loading;
    el('admin-reports-error').hidden = true;
    const query = `sayfa=${state.page}&kaynak=${encodeURIComponent(state.source)}&durum=${encodeURIComponent(state.status)}`;
    try {
      const data = await get(`${t.reportsUrl}?${query}`);
      if (request !== state.request) {
        return;
      }
      const items = data.items || [];
      if (items.length === 0 && (data.total || 0) > 0 && state.page > 1) {
        // Son sayfadaki satır filtreden çıktı (ör. Açık'ta Çözüldü yapıldı): bir önceki sayfa yüklenir.
        state.page -= 1;
        return load();
      }
      el('admin-report-rows').replaceChildren(...items.map(row));
      table.hidden = items.length === 0;
      status.textContent = items.length > 0 ? '' : (data.filtered ? t.textNoMatch : t.textEmpty);
      const pages = Math.max(1, Math.ceil((data.total || 0) / (data.pageSize || 25)));
      state.page = Math.min(state.page, pages);
      el('admin-report-pager').hidden = pages <= 1;
      el('admin-report-page').textContent = fill(options.pageText, state.page, pages);
      el('admin-report-prev').disabled = state.page <= 1;
      el('admin-report-next').disabled = state.page >= pages;
    } catch (failure) {
      if (request === state.request) {
        status.textContent = '';
        table.hidden = true;
        el('admin-reports-error-text').textContent = text(failure, t.textFailed);
        el('admin-reports-error').hidden = false;
      }
    }
  }

  // --- Detay penceresi ---
  function updateNote() {
    const note = el('admin-report-note');
    const tooLong = note.value.length > NOTE_MAX;
    el('admin-report-note-count').textContent = fill(t.textNoteCount, note.value.length);
    el('admin-report-note-error').hidden = !tooLong;
    note.setAttribute('aria-invalid', tooLong ? 'true' : 'false');
    if (!state.dialog || !state.dialog.busy) {
      el('admin-report-save').disabled = tooLong || !state.dialog || !state.dialog.detail;
    }
    return tooLong;
  }

  function showDetail(detail) {
    const report = detail.report || {};
    const fields = {
      'admin-report-time': options.formatTime(report.createdAt),
      'admin-report-source': sourceText(report.source),
      'admin-report-reporter': detail.reporterName === t.textNone ? t.textNone : `${detail.reporterName} (${detail.reporterEmail})`,
      'admin-report-current': statusText(report.status),
      'admin-report-version': orNone(report.productVersion),
      'admin-report-agent-version': orNone(report.agentVersion),
      'admin-report-os': orNone(report.operatingSystem),
      'admin-report-component': orNone([report.component, report.step].filter(Boolean).join(' / ')),
      'admin-report-page-path': orNone(report.page),
      'admin-report-correlation': orNone(report.correlationId),
      'admin-report-description': orNone(report.description),
      'admin-report-attachments': orNone(report.attachments),
      'admin-report-note-current': orNone(detail.adminNote),
    };
    for (const [id, value] of Object.entries(fields)) {
      el(id).textContent = value;
    }
    el('admin-report-status').value = report.status || 'new';
    el('admin-report-note').value = detail.adminNote || '';
    el('admin-report-body').hidden = false;
    el('admin-report-form').hidden = false;
  }

  async function loadDetail() {
    const dialog = state.dialog;
    const request = ++state.detailRequest;
    el('admin-report-loading').textContent = options.loading;
    el('admin-report-error').hidden = true;
    el('admin-report-body').hidden = true;
    el('admin-report-form').hidden = true;
    dialog.detail = null;
    updateNote();
    try {
      const detail = await get(`${t.reportsUrl}/${encodeURIComponent(dialog.number)}`);
      if (request !== state.detailRequest || state.dialog !== dialog) {
        return;
      }
      dialog.detail = detail;
      el('admin-report-loading').textContent = '';
      showDetail(detail);
      updateNote();
    } catch (failure) {
      if (request === state.detailRequest && state.dialog === dialog) {
        el('admin-report-loading').textContent = '';
        el('admin-report-error-text').textContent = text(failure, t.textDetailFailed);
        el('admin-report-error').hidden = false;
      }
    }
  }

  function open(number, opener) {
    state.dialog = { number, opener, busy: false, detail: null };
    el('admin-report-title').textContent = fill(t.textDetailTitle, number);
    el('admin-report-message').hidden = true;
    el('admin-report-message').textContent = '';
    el('admin-report-close').disabled = false;
    el('admin-report-dialog').hidden = false;
    el('admin-report-close').focus();
    return loadDetail();
  }

  function close() {
    const dialog = state.dialog;
    if (!dialog || dialog.busy) {
      return;
    }
    el('admin-report-dialog').hidden = true;
    state.dialog = null;
    state.detailRequest += 1;
    // Satır yeniden çizildiyse odak aynı bildirimin yeni düğmesine, satır filtreden çıktıysa filtreye döner.
    const target = (dialog.opener && dialog.opener.isConnected ? dialog.opener : null)
      || panel.querySelector(`button[data-report-number="${dialog.number}"]`)
      || el('admin-report-source-filter');
    target.focus();
  }

  async function save() {
    const dialog = state.dialog;
    if (!dialog || dialog.busy || !dialog.detail || updateNote()) {
      return;
    }
    const button = el('admin-report-save');
    const label = button.textContent;
    dialog.busy = true;
    button.disabled = true;
    el('admin-report-close').disabled = true;
    button.textContent = t.textProcessing;
    el('admin-report-message').hidden = true;
    try {
      const detail = await post(`${t.reportsUrl}/${encodeURIComponent(dialog.number)}/durum`, {
        status: el('admin-report-status').value,
        note: el('admin-report-note').value,
      });
      dialog.busy = false;
      dialog.detail = detail;
      button.textContent = label;
      el('admin-report-close').disabled = false;
      showDetail(detail);
      updateNote();
      el('admin-report-message').textContent = t.textSaved;
      el('admin-report-message').hidden = false;
      dialog.opener = null; // Liste yeniden çizilir: odak kapanınca aynı bildirimin yeni düğmesine döner.
      if (options.onStatusChanged) {
        options.onStatusChanged();
      }
      await load();
    } catch (failure) {
      dialog.busy = false;
      button.textContent = label;
      el('admin-report-close').disabled = false;
      updateNote();
      el('admin-report-message').textContent = text(failure, t.textSaveFailed);
      el('admin-report-message').hidden = false;
    }
  }

  el('admin-report-dialog').addEventListener('keydown', (event) => {
    if (!state.dialog) {
      return;
    }
    if (event.key === 'Escape') {
      event.preventDefault();
      close();
      return;
    }
    if (event.key === 'Tab') {
      const focusable = [...event.currentTarget.querySelectorAll('select, textarea, button')]
        .filter((node) => !node.disabled && !node.closest('[hidden]'));
      if (focusable.length === 0) {
        event.preventDefault();
        return;
      }
      const index = focusable.indexOf(doc.activeElement);
      const next = event.shiftKey ? index - 1 : index + 1;
      event.preventDefault();
      focusable[(next + focusable.length) % focusable.length].focus();
    }
  });
  el('admin-report-close').addEventListener('click', () => close());
  el('admin-report-save').addEventListener('click', () => save());
  el('admin-report-retry').addEventListener('click', () => loadDetail());
  el('admin-report-note').addEventListener('input', () => updateNote());

  el('admin-report-filter').addEventListener('submit', (event) => {
    event.preventDefault();
    state.source = el('admin-report-source-filter').value;
    state.status = el('admin-report-status-filter').value;
    state.page = 1;
    return load();
  });
  el('admin-reports-retry').addEventListener('click', () => load());
  el('admin-report-prev').addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    return load();
  });
  el('admin-report-next').addEventListener('click', () => {
    state.page += 1;
    return load();
  });

  return { load, open, save, close };
}
