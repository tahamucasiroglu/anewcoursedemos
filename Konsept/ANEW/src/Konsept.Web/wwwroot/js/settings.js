// Ayarlar (spec 0004 Req 6–14): "API key'ler" satırları ve salt okunur "Modeller" listesi; yetkisiz model "Yetki yok"
// (spec 0013 Req 3) ve kullanılabilir metin modelinde "Dene" (kısa, ücretli deneme çağrısı; plan 0013 Risk 3). Tüm istekler api.js ($.ajax, K-6).
// Kaydedilen key bir daha hiçbir yerde tam haliyle görünmez: sunucu yalnızca maskeli hali döndürür. Yazılmış ama kaydedilmemiş
// key hiçbir yerde (localStorage, sessionStorage, çerez) saklanmaz; Vazgeç/Esc alanı temizler ve hiçbir istek göndermez.
// Metinler resx'ten, #settings öğesinin data-text-* öznitelikleriyle gelir.
import { get, post } from './api.js';
import { attachReportLink } from './error-report.js';

const TOAST_MS = 3000;
const JUST_NOW_MS = 60 * 1000;

function format(template, value) {
  return (template || '').replace('{0}', value);
}

function el(doc, tag, className, text) {
  const node = doc.createElement(tag);
  if (className) {
    node.className = className;
  }
  if (text !== undefined) {
    node.textContent = text;
  }
  return node;
}

function show(node, visible) {
  node.hidden = !visible;
}

/**
 * Satırın durum metni: "sk-…a3f · Doğrulandı · az önce güncellendi", "Girilmemiş" ya da "Key okunamıyor — …".
 * @param {object} row Sunucunun satırı.
 * @param {DOMStringMap} text Metinler.
 * @param {Date} now Şimdiki zaman.
 * @returns {string} Metin.
 */
export function statusText(row, text, now = new Date()) {
  if (row.state === 'unreadable') {
    return text.textUnreadable;
  }
  if (row.state !== 'saved') {
    return text.textNotSet;
  }
  const validation = row.validation === 'verified' ? text.textVerified : text.textUnverified;
  const updated = row.updatedAt ? new Date(row.updatedAt) : null;
  let when = '';
  if (updated) {
    when = now - updated < JUST_NOW_MS
      ? text.textJustNow
      : format(text.textUpdatedAt, updated.toLocaleString('tr-TR', { timeZone: 'Europe/Istanbul' }));
  }
  return [row.hint, validation, when].filter(Boolean).join(' · ');
}

/**
 * Hata yanıtından kullanıcıya gösterilecek metin: alan hatası (biçim, sağlayıcı reddi) varsa o; yetki yoksa yetki metni;
 * diğer her durumda genel Türkçe metin + korelasyon numarası. Girilen key hiçbir zaman metne girmez.
 * @param {{ status: number, body: any }} failure api.js reddi.
 * @param {DOMStringMap} text Metinler.
 * @returns {string} Metin.
 */
export function failureText(failure, text) {
  const body = (failure && failure.body) || {};
  if (failure && failure.status === 403) {
    return text.textForbidden;
  }
  if (failure && failure.status === 400 && body.errors) {
    const key = Object.keys(body.errors).find((name) => name.toLowerCase() === 'apikey');
    if (key && body.errors[key] && body.errors[key][0]) {
      return body.errors[key][0];
    }
  }
  if (failure && failure.status === 400 && body.message) {
    return body.message;
  }
  return format(text.textFailed, body.correlationId || '-');
}

function toast(root, message) {
  const node = root.querySelector('#settings-toast');
  node.textContent = message;
  show(node, true);
  clearTimeout(node.timer);
  node.timer = setTimeout(() => show(node, false), TOAST_MS);
}

function renderModels(root, state) {
  const doc = root.ownerDocument;
  const text = root.dataset;
  const list = root.querySelector('#model-list');
  const kind = root.querySelector('#model-kind').value;
  const provider = root.querySelector('#model-provider').value;
  const models = state.models.filter((m) => (!kind || m.kind === kind) && (!provider || m.providerId === provider));
  const labels = {
    available: text.textAvailable,
    no_key: text.textNoKey,
    unreadable: text.textKeyUnreadable,
    no_permission: text.textNoPermission,
  };
  // Tür etiketi açık eşlemeyle (spec 0006 Req 18: ses ve video motoru türleri).
  const kindLabels = { text: text.textKindText, image: text.textKindImage, audio: text.textKindAudio, video_engine: text.textKindVideoEngine };
  list.replaceChildren(...models.map((model) => {
    const item = el(doc, 'li', 'model-row');
    item.dataset.modelId = model.id;
    item.append(
      el(doc, 'span', 'model-row__name', model.name),
      el(doc, 'span', 'model-row__provider', model.providerName),
      el(doc, 'span', 'model-row__kind', kindLabels[model.kind] || text.textKindText),
      el(doc, 'span', `badge badge--${model.availability}`, labels[model.availability] || ''),
    );
    if (model.kind === 'text' && model.availability === 'available') {
      appendTrial(root, item, model);
    }
    return item;
  }));
  show(list, models.length > 0);
  show(root.querySelector('#models-empty'), models.length === 0);
}

/**
 * Deneme sonucunun metni: "Yanıt: … · 12 giriş / 34 çıkış token · $0,000810 (tahmini)".
 * @param {object} result Sunucunun deneme sonucu.
 * @param {DOMStringMap} text Metinler.
 * @returns {string} Metin.
 */
export function trialText(result, text) {
  const cost = Number(result.costUsd || 0).toFixed(6).replace('.', ',');
  // Model metni en son ve fonksiyonla yerleştirilir: içindeki "$&", "{1}" gibi diziler yorumlanmaz (review r1 L-5).
  const line = (text.textTrialResult || '')
    .replace('{1}', () => String(result.inputTokens ?? '-'))
    .replace('{2}', () => String(result.outputTokens ?? '-'))
    .replace('{3}', () => cost)
    .replace('{0}', () => result.text || '');
  return [line, result.truncated ? text.textTrialTruncated : '', result.isEstimated ? text.textTrialEstimated : '']
    .filter(Boolean)
    .join(' ');
}

function trialFailureText(failure, text) {
  const body = (failure && failure.body) || {};
  if (failure && failure.status === 403) {
    return text.textForbidden;
  }
  if (failure && [400, 404, 409].includes(failure.status) && body.message) {
    return body.message;
  }
  return format(text.textTrialFailed, body.correlationId || '-');
}

function appendTrial(root, item, model) {
  const doc = root.ownerDocument;
  const text = root.dataset;
  const button = el(doc, 'button', 'button model-row__trial-button', text.textTrial);
  button.type = 'button';
  button.setAttribute('aria-label', format(text.textTrialLabel, model.name));
  const result = el(doc, 'span', 'model-row__trial');
  result.setAttribute('role', 'status');
  result.setAttribute('aria-live', 'polite');
  button.addEventListener('click', async () => {
    if (button.disabled) {
      return;
    }
    button.disabled = true;
    result.textContent = text.textTrialRunning;
    try {
      result.textContent = trialText(await post(text.trialUrl, { modelId: model.id }), text);
    } catch (failure) {
      result.textContent = trialFailureText(failure, text);
    } finally {
      button.disabled = false;
    }
  });
  item.append(button, result);
}

/**
 * Modeller listesini yükler; filtreler listeyi istemcide daraltır.
 * @param {HTMLElement} root #settings.
 * @param {object} state Sayfa durumu.
 * @returns {Promise<void>}
 */
export async function loadModels(root, state) {
  const loading = root.querySelector('#models-loading');
  const error = root.querySelector('#models-error');
  show(loading, true);
  show(error, false);
  try {
    const catalog = await get(root.dataset.modelsUrl);
    state.models = (catalog && catalog.models) || [];
    const select = root.querySelector('#model-provider');
    if (select.options.length <= 1) {
      for (const provider of (catalog && catalog.providers) || []) {
        const option = root.ownerDocument.createElement('option');
        option.value = provider.id;
        option.textContent = provider.name;
        select.append(option);
      }
    }
    renderModels(root, state);
  } catch (failure) {
    error.textContent = failure && failure.status === 403 ? root.dataset.textForbidden : root.dataset.textModelsFailed;
    show(error, true);
    show(root.querySelector('#model-list'), false);
    show(root.querySelector('#models-empty'), false);
  } finally {
    show(loading, false);
  }
}

function closeForm(row) {
  const form = row.querySelector('.key-form');
  const input = form.querySelector('input');
  input.value = '';
  form.querySelector('.key-form__error').textContent = '';
  input.removeAttribute('aria-invalid');
  show(form, false);
  row.querySelector('.key-row__edit').focus();
}

function renderRow(root, state, row, data) {
  const doc = root.ownerDocument;
  const text = root.dataset;
  const id = data.providerId;
  row.dataset.state = data.state;
  row.replaceChildren();

  const name = el(doc, 'span', 'key-row__name', data.providerName);
  const status = el(doc, 'span', 'key-row__status', statusText(data, text));
  if (data.state === 'unreadable') {
    status.classList.add('key-row__status--error');
  }
  const actions = el(doc, 'span', 'key-row__actions');
  const edit = el(doc, 'button', 'button key-row__edit', data.state === 'none' ? text.textAdd : text.textChange);
  edit.type = 'button';
  actions.append(edit);
  if (data.state !== 'none') {
    const remove = el(doc, 'button', 'button button--danger key-row__delete', text.textDelete);
    remove.type = 'button';
    remove.addEventListener('click', () => openDeleteDialog(root, state, row, data));
    actions.append(remove);
  }

  const form = el(doc, 'form', 'key-form');
  form.noValidate = true;
  form.autocomplete = 'off';
  show(form, false);
  const inputId = `key-input-${id}`;
  const errorId = `key-error-${id}`;
  const label = el(doc, 'label', 'key-form__label', format(text.textInputLabel, data.providerName));
  label.htmlFor = inputId;
  const input = el(doc, 'input', 'key-form__input');
  input.type = 'password';
  input.id = inputId;
  input.name = 'apiKey';
  input.autocomplete = 'new-password'; // Tarayıcı key'i şifre olarak kaydetmeyi önermesin (review r1 I-1).
  input.spellcheck = false;
  input.setAttribute('aria-describedby', errorId);
  const save = el(doc, 'button', 'button button--primary key-form__save', text.textSave);
  save.type = 'submit';
  const cancel = el(doc, 'button', 'button key-form__cancel', text.textCancel);
  cancel.type = 'button';
  const error = el(doc, 'p', 'key-form__error');
  error.id = errorId;
  error.setAttribute('role', 'alert');
  form.append(label, input, save, cancel, error);

  edit.addEventListener('click', () => {
    show(form, true);
    input.focus();
  });
  // Kayıt sürerken Vazgeç/Esc alanı kapatmaz (review r1 L-1): istek zaten gitmiştir; sonucu alanın yanında görünür.
  cancel.addEventListener('click', () => {
    if (form.dataset.pending !== 'true') {
      closeForm(row);
    }
  });
  form.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      if (form.dataset.pending !== 'true') {
        closeForm(row);
      }
    }
  });
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (form.dataset.pending === 'true') {
      return; // Art arda tıklama: tek kayıt, tek doğrulama (AC-14).
    }
    form.dataset.pending = 'true';
    save.disabled = true;
    cancel.disabled = true;
    save.textContent = text.textSaving;
    error.textContent = '';
    try {
      const saved = await post(`${root.dataset.keysUrl}/${encodeURIComponent(id)}`, { apiKey: input.value });
      input.value = '';
      show(root.querySelector('#keys-error'), false);
      renderRow(root, state, row, saved);
      row.querySelector('.key-row__edit').focus();
      toast(root, text.textSaved);
      loadModels(root, state);
    } catch (failure) {
      error.textContent = failureText(failure, text);
      attachReportLink(error, failure);
      input.setAttribute('aria-invalid', 'true');
      save.disabled = false;
      cancel.disabled = false;
      save.textContent = text.textSave;
      delete form.dataset.pending;
    }
  });

  row.append(name, status, actions, form);
}

function openDeleteDialog(root, state, row, data) {
  const dialog = root.querySelector('#delete-dialog');
  const confirm = root.querySelector('#delete-confirm');
  const cancel = root.querySelector('#delete-cancel');
  const opener = row.querySelector('.key-row__delete');
  root.querySelector('#delete-dialog-text').textContent = format(root.dataset.textDeleteConfirm, data.providerName);
  show(dialog, true);
  cancel.focus();

  const close = () => {
    show(dialog, false);
    dialog.removeEventListener('keydown', onKey);
    confirm.removeEventListener('click', onConfirm);
    cancel.removeEventListener('click', onCancel);
  };
  const onCancel = () => {
    close();
    if (opener) {
      opener.focus();
    }
  };
  const onKey = (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      onCancel();
    } else if (event.key === 'Tab') {
      // Odak pencerede kalır (AC-17).
      event.preventDefault();
      (root.ownerDocument.activeElement === cancel ? confirm : cancel).focus();
    }
  };
  const onConfirm = async () => {
    confirm.disabled = true;
    try {
      const empty = await post(`${root.dataset.keysUrl}/${encodeURIComponent(data.providerId)}/sil`);
      close();
      show(root.querySelector('#keys-error'), false);
      renderRow(root, state, row, empty);
      row.querySelector('.key-row__edit').focus();
      toast(root, root.dataset.textDeleted);
      loadModels(root, state);
    } catch (failure) {
      onCancel(); // Odak açan düğmeye döner (review r1 L-2); satır değişmez.
      const error = root.querySelector('#keys-error');
      error.textContent = failureText(failure, root.dataset);
      attachReportLink(error, failure);
      show(error, true);
    } finally {
      confirm.disabled = false;
    }
  };
  dialog.addEventListener('keydown', onKey);
  confirm.addEventListener('click', onConfirm);
  cancel.addEventListener('click', onCancel);
}

/**
 * Sağlayıcı satırlarını yükler.
 * @param {HTMLElement} root #settings.
 * @param {object} state Sayfa durumu.
 * @returns {Promise<void>}
 */
export async function loadKeys(root, state) {
  const loading = root.querySelector('#keys-loading');
  const list = root.querySelector('#key-list');
  const error = root.querySelector('#keys-error');
  show(loading, true);
  try {
    const rows = await get(root.dataset.keysUrl);
    list.replaceChildren(...(rows || []).map((data) => {
      const row = el(root.ownerDocument, 'li', 'key-row');
      row.dataset.provider = data.providerId;
      renderRow(root, state, row, data);
      return row;
    }));
    show(list, true);
  } catch (failure) {
    error.textContent = failure && failure.status === 403 ? root.dataset.textForbidden : root.dataset.textLoadFailed;
    show(error, true);
  } finally {
    show(loading, false);
  }
}

/**
 * Ayarlar sayfasını başlatır (sayfada #settings yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @returns {Promise<void>}
 */
export async function initSettings(doc) {
  const root = doc.getElementById('settings');
  if (!root) {
    return;
  }
  const state = { models: [] };
  root.querySelector('#model-kind').addEventListener('change', () => renderModels(root, state));
  root.querySelector('#model-provider').addEventListener('change', () => renderModels(root, state));
  await Promise.all([loadKeys(root, state), loadModels(root, state)]);
}
