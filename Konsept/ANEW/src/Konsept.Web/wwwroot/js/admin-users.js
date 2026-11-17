// Admin Kullanıcılar sekmesi (spec 0024): liste (25'erli sayfa, ad/e-posta araması — en az 2 karakter —, Tümü/Aktif/Banlı), ban ve
// ban kaldırma onay pencereleri. Veri /admin/kullanicilar'dan $.ajax ile (api.js, K-6) gelir; Api verisi yalnızca textContent ile
// yazılır. Pencere: odak pencereye taşınır, Tab içeride döner, Esc vazgeçer (işlem sürerken değil), kapanınca odak açan düğmeye
// döner; istek sürerken onay pasif ve "İşleniyor…" (çift tıklama tek istek); hata pencerede gösterilir, durum değişmez.
import { get, post } from './api.js';

const REASON_MAX = 500;
const MIN_SEARCH = 2;

function fill(template, ...values) {
  return values.reduce((text, value, i) => text.replace(`{${i}}`, String(value)), template || '');
}

/**
 * Hata metni: 403 yetkisiz metni; beklenen 4xx (400/404/409) Api'nin Türkçe sade mesajı; diğerleri sabit metin. Korelasyon numarası
 * eklenir; ham ayrıntı (stack trace, SQL) asla gösterilmez.
 * @param {{status?: number, body?: any}} failure Hata.
 * @param {string} fallback Sabit metin.
 * @param {string} correlationTemplate "Hata no: {0}".
 * @returns {string} Metin.
 */
export function failureText(failure, fallback, correlationTemplate) {
  const body = (failure && failure.body) || {};
  if (failure && failure.status === 403) {
    return body.message || fallback;
  }
  const expected = failure && [400, 404, 409].includes(failure.status) && typeof body.message === 'string';
  const base = expected ? body.message : fallback;
  return body.correlationId ? `${base} ${fill(correlationTemplate, body.correlationId)}` : base;
}

/**
 * Kullanıcılar sekmesini başlatır.
 * @param {Document} doc Belge.
 * @param {{ formatTime: (iso: string) => string, correlation: string }} options Saat biçimi ve "Hata no: {0}" şablonu.
 * @returns {{ load: Function }|null} Denetim.
 */
export function initAdminUsers(doc, options) {
  const panel = doc.getElementById('admin-users');
  if (!panel) {
    return null;
  }
  const t = panel.dataset;
  const el = (id) => doc.getElementById(id);
  const state = { page: 1, search: '', status: 'all', request: 0, dialog: null };
  const correlationText = (failure, fallback) => failureText(failure, fallback, options.correlation);

  function historyList(user) {
    const list = doc.createElement('ul');
    list.className = 'admin-history';
    for (const record of user.history || []) {
      const item = doc.createElement('li');
      const template = record.action === 'ban' ? t.textHistoryBan : t.textHistoryUnban;
      item.textContent = fill(template, record.adminName, options.formatTime(record.at));
      if (record.reason) {
        const reason = doc.createElement('span');
        reason.className = 'admin-history__reason';
        reason.textContent = ` ${fill(t.textHistoryReason, record.reason)}`;
        item.append(reason);
      }
      list.append(item);
    }
    return list;
  }

  function actionCell(user) {
    const cell = doc.createElement('td');
    const button = doc.createElement('button');
    button.type = 'button';
    button.className = 'button';
    button.dataset.userId = user.id;
    const banned = user.status === 'banned';
    button.textContent = banned ? t.textUnban : t.textBan;
    cell.append(button);
    if (!banned && user.role === 'admin') {
      // Admin satırında Banla pasif + metin ipucu (Req 7); sunucu da reddeder.
      button.disabled = true;
      const hint = doc.createElement('span');
      hint.className = 'field__hint admin-users__hint';
      hint.id = `admin-user-hint-${user.id}`;
      hint.textContent = user.isSelf ? t.textCannotSelf : t.textCannotAdmin;
      button.title = hint.textContent;
      button.setAttribute('aria-describedby', hint.id);
      cell.append(hint);
    } else {
      button.addEventListener('click', () => openDialog(banned ? 'unban' : 'ban', user, button));
    }
    if ((user.history || []).length > 0) {
      cell.append(historyList(user));
    }
    return cell;
  }

  function row(user) {
    const tr = doc.createElement('tr');
    const text = (value) => {
      const td = doc.createElement('td');
      td.textContent = value;
      return td;
    };
    const status = text(user.status === 'banned' ? t.textBanned : t.textActive);
    status.className = user.status === 'banned' ? 'admin-status admin-status--banned' : 'admin-status';
    tr.append(
      text(user.name),
      text(user.email),
      text(options.formatTime(user.registeredAt)),
      text(user.lastLoginAt ? options.formatTime(user.lastLoginAt) : t.textNever),
      status,
      text(user.role === 'admin' ? t.textRoleAdmin : t.textRoleUser),
      text(String(user.clips ?? 0)),
      actionCell(user),
    );
    return tr;
  }

  async function load() {
    const request = ++state.request;
    const status = el('admin-users-status');
    const table = el('admin-user-table');
    status.textContent = panel.closest('#admin-panel').dataset.textLoading;
    el('admin-users-error').hidden = true;
    const query = `sayfa=${state.page}&ara=${encodeURIComponent(state.search)}&durum=${encodeURIComponent(state.status)}`;
    try {
      const data = await get(`${t.usersUrl}?${query}`);
      if (request !== state.request) {
        return;
      }
      const items = data.items || [];
      el('admin-user-rows').replaceChildren(...items.map(row));
      table.hidden = items.length === 0;
      status.textContent = items.length > 0 ? '' : (data.filtered ? t.textNoMatch : t.textEmpty);
      const pages = Math.max(1, Math.ceil((data.total || 0) / (data.pageSize || 25)));
      state.page = Math.min(state.page, pages);
      el('admin-user-pager').hidden = pages <= 1;
      el('admin-user-page').textContent = fill(panel.closest('#admin-panel').dataset.textPage, state.page, pages);
      el('admin-user-prev').disabled = state.page <= 1;
      el('admin-user-next').disabled = state.page >= pages;
    } catch (failure) {
      if (request === state.request) {
        status.textContent = '';
        table.hidden = true;
        el('admin-users-error-text').textContent = correlationText(failure, t.textFailed);
        el('admin-users-error').hidden = false;
      }
    }
  }

  // --- Onay pencereleri ---
  function parts(kind) {
    return {
      backdrop: el(`admin-${kind}-dialog`),
      title: el(`admin-${kind}-title`),
      message: el(`admin-${kind}-message`),
      ok: el(`admin-${kind}-ok`),
      cancel: el(`admin-${kind}-cancel`),
    };
  }

  function updateReason() {
    const reason = el('admin-ban-reason');
    const length = reason.value.length;
    const tooLong = length > REASON_MAX;
    el('admin-ban-reason-count').textContent = fill(t.textReasonCount, length);
    el('admin-ban-reason-error').hidden = !tooLong;
    reason.setAttribute('aria-invalid', tooLong ? 'true' : 'false');
    if (!state.dialog || !state.dialog.busy) {
      el('admin-ban-ok').disabled = tooLong;
    }
    return tooLong;
  }

  function openDialog(kind, user, opener) {
    const p = parts(kind);
    state.dialog = { kind, user, opener, busy: false, label: p.ok.textContent };
    p.title.textContent = fill(kind === 'ban' ? t.textBanTitle : t.textUnbanTitle, user.name);
    p.message.hidden = true;
    p.message.textContent = '';
    p.ok.disabled = false;
    p.cancel.disabled = false;
    p.backdrop.hidden = false;
    if (kind === 'ban') {
      el('admin-ban-reason').value = '';
      updateReason();
      el('admin-ban-reason').focus();
    } else {
      p.cancel.focus();
    }
  }

  function closeDialog() {
    const dialog = state.dialog;
    if (!dialog || dialog.busy) {
      return;
    }
    parts(dialog.kind).backdrop.hidden = true;
    state.dialog = null;
    const target = dialog.opener && dialog.opener.isConnected
      ? dialog.opener
      : panel.querySelector(`button[data-user-id="${dialog.user.id}"]`);
    if (target) {
      target.focus();
    }
  }

  async function confirm(kind) {
    const dialog = state.dialog;
    if (!dialog || dialog.kind !== kind || dialog.busy || (kind === 'ban' && updateReason())) {
      return;
    }
    const p = parts(kind);
    dialog.busy = true;
    p.ok.disabled = true;
    p.cancel.disabled = true;
    p.ok.textContent = t.textProcessing;
    p.message.hidden = true;
    const url = `${t.usersUrl}/${encodeURIComponent(dialog.user.id)}/${kind === 'ban' ? 'banla' : 'ban-kaldir'}`;
    try {
      await post(url, kind === 'ban' ? { reason: el('admin-ban-reason').value } : {});
      dialog.busy = false;
      p.ok.textContent = dialog.label;
      dialog.opener = null; // Satırlar yeniden çizilir: odak aynı kullanıcının yeni düğmesine döner.
      await load();
      closeDialog();
    } catch (failure) {
      dialog.busy = false;
      p.ok.textContent = dialog.label;
      p.ok.disabled = false;
      p.cancel.disabled = false;
      p.message.textContent = correlationText(failure, kind === 'ban' ? t.textBanFailed : t.textUnbanFailed);
      p.message.hidden = false;
    }
  }

  function trapKeys(event) {
    if (!state.dialog) {
      return;
    }
    if (event.key === 'Escape') {
      event.preventDefault();
      closeDialog();
      return;
    }
    if (event.key === 'Tab') {
      const focusable = [...event.currentTarget.querySelectorAll('textarea, button')].filter((node) => !node.disabled);
      if (focusable.length === 0) {
        event.preventDefault();
        return;
      }
      const index = focusable.indexOf(doc.activeElement);
      const next = event.shiftKey ? index - 1 : index + 1;
      event.preventDefault();
      focusable[(next + focusable.length) % focusable.length].focus();
    }
  }

  for (const kind of ['ban', 'unban']) {
    const p = parts(kind);
    p.backdrop.addEventListener('keydown', trapKeys);
    p.cancel.addEventListener('click', () => closeDialog());
    p.ok.addEventListener('click', () => confirm(kind));
  }
  el('admin-ban-reason').addEventListener('input', () => updateReason());

  el('admin-user-filter').addEventListener('submit', (event) => {
    event.preventDefault();
    const search = el('admin-user-search').value.trim();
    state.search = search.length >= MIN_SEARCH ? search : ''; // 2 karakterden kısa arama uygulanmaz (Req 2).
    state.status = el('admin-user-status').value;
    state.page = 1;
    return load();
  });
  el('admin-users-retry').addEventListener('click', () => load());
  el('admin-user-prev').addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    return load();
  });
  el('admin-user-next').addEventListener('click', () => {
    state.page += 1;
    return load();
  });

  return { load, confirm };
}
