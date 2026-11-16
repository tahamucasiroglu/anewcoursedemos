// Profiller (spec 0003): üst bardaki profil seçimi, kalem (seçili profili düzenle), artı (yeni profil), profil modalı, hazır şablonlar,
// onay pencereleri ve profil gerektiren sayfaların "Önce bir profil oluşturun" boş durumu. Tüm istekler api.js ($.ajax, K-6).
// Seçim sunucuda hatırlanır; seçim/oluşturma/silme sonrası açık sayfa yeniden yüklenir (seçili profilin bağlamı, Req 8, BR-2).
// Kurallar sunucuda da uygulanır; burada yalnızca erken geri bildirim içindir. Metinler resx'ten data-text-* ile gelir.
import { get, post } from './api.js';
import { attachReportLink } from './error-report.js';

const NAME_MAX = 80;
const CONTENT_MAX = 2000;
const MIN_SECONDS = 15;
const MAX_SECONDS = 900;
const FIELD_IDS = {
  channelname: 'profile-name-error',
  language: 'profile-language-error',
  format: 'profile-format-error',
  durationseconds: 'profile-duration-error',
  content: 'profile-content-error',
};
const FOCUSABLE = 'button:not([disabled]):not([hidden]), select:not([hidden]), input:not([type="hidden"]), textarea';

function format(template, value) {
  return (template || '').replace('{0}', value);
}

function show(node, visible) {
  if (node) {
    node.hidden = !visible;
  }
}

/**
 * Dakika + saniye alanlarından toplam saniye; boş ya da tam sayı olmayan değer null.
 * @param {string} minutes Dakika.
 * @param {string} seconds Saniye.
 * @returns {number|null} Saniye.
 */
export function toSeconds(minutes, seconds) {
  const m = (minutes || '').trim();
  const s = (seconds || '').trim();
  if (m === '' && s === '') {
    return null;
  }
  if ((m !== '' && !/^\d+$/.test(m)) || (s !== '' && !/^\d+$/.test(s))) {
    return null;
  }
  const sec = s === '' ? 0 : Number(s);
  if (sec > 59) {
    return null;
  }
  return (m === '' ? 0 : Number(m)) * 60 + sec;
}

/**
 * Alan kuralları (sunucudakiyle aynı; kırpılmış uzunluk): hata metni haritası (alan → metin).
 * @param {{ channelName: string, language: string, format: string, durationSeconds: number|null, content: string }} values Değerler.
 * @param {DOMStringMap} text Metinler.
 * @returns {Record<string, string>} Hatalar (boşsa geçerli).
 */
export function validate(values, text) {
  const errors = {};
  const name = (values.channelName || '').trim();
  const content = (values.content || '').trim();
  if (!name) {
    errors.channelname = text.textNameRequired;
  } else if (name.length > NAME_MAX) {
    errors.channelname = text.textNameTooLong;
  }
  if (values.language !== 'tr' && values.language !== 'en') {
    errors.language = text.textLanguageRequired;
  }
  if (values.format !== 'landscape' && values.format !== 'portrait') {
    errors.format = text.textFormatRequired;
  }
  const d = values.durationSeconds;
  if (d === null || d === undefined || d < MIN_SECONDS || d > MAX_SECONDS) {
    errors.durationseconds = text.textDurationInvalid;
  }
  if (!content) {
    errors.content = text.textContentRequired;
  } else if (content.length > CONTENT_MAX) {
    errors.content = text.textContentTooLong;
  }
  return errors;
}

function correlationOf(failure) {
  return (failure && failure.body && failure.body.correlationId) || '-';
}

/**
 * Profil özelliklerini başlatır (üst barda #profile-bar yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @param {{ reload?: () => void }} [options] Sayfa yeniden yükleme (testte enjekte edilir).
 * @returns {Promise<object|undefined>} Denetleyici (testler için).
 */
export async function initProfiles(doc, options = {}) {
  const bar = doc.getElementById('profile-bar');
  if (!bar) {
    return undefined;
  }
  const reload = options.reload || (() => globalThis.location.reload());
  const modal = doc.getElementById('profile-modal');
  const confirmBox = doc.getElementById('confirm-dialog');
  const text = bar.dataset;
  const mtext = modal.dataset;
  const el = (id) => doc.getElementById(id);
  const select = el('profile-select');
  const status = el('profile-status');
  const createButton = el('profile-create');
  const alertBox = el('profile-alert');
  const editButton = el('profile-edit');
  const newButton = el('profile-new');
  const form = el('profile-form');
  const templateSelect = el('profile-template');
  const fields = {
    name: el('profile-name'),
    language: el('profile-language'),
    minutes: el('profile-minutes'),
    seconds: el('profile-seconds'),
    content: el('profile-content'),
  };
  const save = el('profile-save');
  const cancel = el('profile-cancel');
  const closeButton = el('profile-modal-close');
  const deleteButton = el('profile-delete');
  const deleteReason = el('profile-delete-reason');
  const formError = el('profile-form-error');

  const state = { list: null, mode: null, editing: null, initial: null, baseline: null, opener: null, pending: false };

  // --- Üst bar ---------------------------------------------------------------------------------------------------------
  function setEdit(enabled, reason) {
    if (enabled) {
      editButton.removeAttribute('aria-disabled');
      editButton.title = editButton.getAttribute('aria-label');
      delete editButton.dataset.reason;
    } else {
      editButton.setAttribute('aria-disabled', 'true');
      editButton.title = reason;
      editButton.dataset.reason = reason;
    }
  }

  function setPageState(profileState) {
    bar.dataset.state = profileState;
    doc.body.dataset.profileState = profileState;
    // Profil gerektiren sayfa: yalnızca seçili profil varken içeriğini gösterir (AC-12, AC-13).
    for (const node of doc.querySelectorAll('[data-requires-profile]')) {
      show(node, profileState === 'ready');
    }
    show(el('profile-required'), profileState === 'none');
  }

  function selected() {
    const list = state.list;
    return list && list.items.find((p) => p.id === list.selectedId);
  }

  function render(list) {
    state.list = list;
    alertBox.textContent = '';
    show(status, false);
    if (!list.items.length) {
      show(select, false);
      show(createButton, true);
      setEdit(false, text.textEditNone);
      setPageState('none');
      return;
    }
    select.replaceChildren(...list.items.map((p) => {
      const option = doc.createElement('option');
      option.value = p.id;
      option.textContent = p.channelName;
      return option;
    }));
    select.value = list.selectedId || list.items[0].id;
    show(select, true);
    show(createButton, false);
    setEdit(true);
    setPageState('ready');
  }

  async function load() {
    status.textContent = text.textLoading;
    show(status, true);
    show(select, false);
    show(createButton, false);
    setEdit(false, text.textEditUnavailable);
    setPageState('loading');
    try {
      render(await get(bar.dataset.url));
    } catch (failure) {
      status.textContent = failure && failure.status === 403 ? mtext.textForbidden : text.textFailed;
      setEdit(false, text.textEditUnavailable);
      setPageState('error');
    }
  }

  select.addEventListener('change', async () => {
    const previous = state.list ? state.list.selectedId : null;
    try {
      await post(`${bar.dataset.url}/secili`, { profileId: select.value });
      reload();
    } catch (failure) {
      select.value = previous;
      alertBox.textContent = format(text.textSelectFailed, correlationOf(failure));
      attachReportLink(alertBox, failure);
    }
  });

  // --- Onay penceresi ----------------------------------------------------------------------------------------------------
  function ask(message) {
    const ok = el('confirm-ok');
    const no = el('confirm-cancel');
    const back = doc.activeElement;
    el('confirm-text').textContent = message;
    show(confirmBox, true);
    no.focus();
    return new Promise((resolve) => {
      const finish = (answer) => {
        show(confirmBox, false);
        confirmBox.removeEventListener('keydown', onKey);
        ok.removeEventListener('click', onOk);
        no.removeEventListener('click', onNo);
        if (back && typeof back.focus === 'function') {
          back.focus();
        }
        resolve(answer);
      };
      const onOk = () => finish(true);
      const onNo = () => finish(false);
      const onKey = (event) => {
        if (event.key === 'Escape') {
          event.preventDefault();
          event.stopPropagation();
          finish(false);
        } else if (event.key === 'Tab') {
          event.preventDefault(); // Odak onay penceresinde kalır.
          (doc.activeElement === no ? ok : no).focus();
        }
      };
      confirmBox.addEventListener('keydown', onKey);
      ok.addEventListener('click', onOk);
      no.addEventListener('click', onNo);
    });
  }

  // --- Modal --------------------------------------------------------------------------------------------------------------
  function readValues() {
    const checked = form.querySelector('input[name="format"]:checked');
    return {
      channelName: fields.name.value,
      language: fields.language.value,
      format: checked ? checked.value : '',
      durationSeconds: toSeconds(fields.minutes.value, fields.seconds.value),
      minutes: fields.minutes.value,
      seconds: fields.seconds.value,
      content: fields.content.value,
    };
  }

  function snapshot() {
    const v = readValues();
    return JSON.stringify([v.channelName, v.language, v.format, v.minutes, v.seconds, v.content]);
  }

  function writeValues(v) {
    fields.name.value = v.channelName || '';
    fields.language.value = v.language || 'tr';
    for (const radio of form.querySelectorAll('input[name="format"]')) {
      radio.checked = radio.value === v.format;
    }
    if (v.durationSeconds === null || v.durationSeconds === undefined) {
      fields.minutes.value = '';
      fields.seconds.value = '';
    } else {
      fields.minutes.value = String(Math.floor(v.durationSeconds / 60));
      fields.seconds.value = String(v.durationSeconds % 60);
    }
    fields.content.value = v.content || '';
  }

  function clearErrors() {
    for (const id of Object.values(FIELD_IDS)) {
      el(id).textContent = '';
    }
    for (const node of form.querySelectorAll('[aria-invalid]')) {
      node.removeAttribute('aria-invalid');
    }
    formError.textContent = '';
  }

  function showErrors(errors) {
    const targets = {
      channelname: [fields.name],
      language: [fields.language],
      format: [...form.querySelectorAll('input[name="format"]')],
      durationseconds: [fields.minutes, fields.seconds],
      content: [fields.content],
    };
    let first = null;
    for (const [key, message] of Object.entries(errors)) {
      const name = key.toLowerCase();
      if (!FIELD_IDS[name]) {
        continue;
      }
      el(FIELD_IDS[name]).textContent = Array.isArray(message) ? message[0] : message;
      for (const node of targets[name]) {
        node.setAttribute('aria-invalid', 'true');
      }
      first = first || targets[name][0];
    }
    if (first) {
      first.focus();
    }
  }

  function focusables() {
    return [...modal.querySelectorAll(FOCUSABLE)].filter((node) => !node.closest('[hidden]'));
  }

  function focusFirstField() {
    const first = focusables().find((node) => form.contains(node));
    if (first) {
      first.focus();
    }
  }

  function open(mode) {
    if (mode === 'new' && state.list && state.list.items.length >= state.list.maxProfiles) {
      alertBox.textContent = format(text.textLimit, state.list.maxProfiles); // 20 profil: modal açılmaz (AC-9).
      return;
    }
    const profile = mode === 'edit' ? selected() : null;
    if (mode === 'edit' && !profile) {
      return;
    }
    state.mode = mode;
    state.editing = profile;
    state.opener = doc.activeElement;
    alertBox.textContent = '';
    clearErrors();
    el('profile-modal-title').textContent = mode === 'new' ? mtext.textNew : mtext.textEdit;
    show(el('profile-template-field'), mode === 'new');
    templateSelect.value = '';
    delete templateSelect.dataset.applied;
    if (mode === 'new') {
      const templates = (state.list && state.list.templates) || [];
      templateSelect.replaceChildren(templateSelect.options[0], ...templates.map((t) => {
        const option = doc.createElement('option');
        option.value = t.id;
        option.textContent = t.title;
        return option;
      }));
      writeValues({ language: 'tr', durationSeconds: null });
    } else {
      writeValues(profile);
    }
    show(deleteButton, mode === 'edit');
    deleteButton.disabled = mode === 'edit' && !profile.canDelete;
    show(deleteReason, mode === 'edit' && !profile.canDelete);
    save.disabled = false;
    save.textContent = mtext.textSave;
    state.initial = snapshot();
    state.baseline = state.initial;
    show(modal, true);
    focusFirstField(); // Odak ilk alana (AC-18).
  }

  function close() {
    show(modal, false);
    state.mode = null;
    const opener = state.opener;
    if (opener && typeof opener.focus === 'function' && doc.contains(opener)) {
      opener.focus();
    }
  }

  async function requestClose() {
    if (state.pending) {
      return;
    }
    if (snapshot() !== state.initial && !(await ask(mtext.textConfirmDiscard))) {
      return; // Onaylanmadı: modal değerleriyle açık kalır (AC-6).
    }
    close();
  }

  templateSelect.addEventListener('change', async () => {
    const template = ((state.list && state.list.templates) || []).find((t) => t.id === templateSelect.value);
    if (!template) {
      return;
    }
    // Kullanıcının doldurduğu/değiştirdiği alan varsa onay (AC-4); şablonla dolmuş alanlar kullanıcı girdisi sayılmaz.
    if (snapshot() !== state.baseline && !(await ask(mtext.textConfirmTemplate))) {
      templateSelect.value = templateSelect.dataset.applied || '';
      return;
    }
    writeValues(template);
    templateSelect.dataset.applied = template.id;
    state.baseline = snapshot();
    clearErrors();
  });

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (state.pending) {
      return; // Çift tıklama tek kayıt (AC-10).
    }
    clearErrors();
    const values = readValues();
    const errors = validate(values, mtext);
    if (Object.keys(errors).length) {
      showErrors(errors);
      return;
    }
    state.pending = true;
    save.disabled = true;
    save.textContent = mtext.textSaving;
    const body = {
      channelName: values.channelName,
      language: values.language,
      format: values.format,
      durationSeconds: values.durationSeconds,
      content: values.content,
    };
    const url = state.mode === 'edit' ? `${bar.dataset.url}/${encodeURIComponent(state.editing.id)}` : bar.dataset.url;
    try {
      await post(url, body);
      state.pending = false;
      close();
      reload();
    } catch (failure) {
      state.pending = false;
      save.disabled = false;
      save.textContent = mtext.textSave;
      const problem = (failure && failure.body) || {};
      if (failure && failure.status === 400 && problem.errors) {
        showErrors(problem.errors);
      } else if (failure && failure.status === 409 && problem.message) {
        formError.textContent = problem.message;
      } else if (failure && failure.status === 403) {
        formError.textContent = mtext.textForbidden;
      } else {
        formError.textContent = format(mtext.textSaveFailed, correlationOf(failure));
        attachReportLink(formError, failure);
      }
    }
  });

  deleteButton.addEventListener('click', async () => {
    if (state.pending || !state.editing || !state.editing.canDelete) {
      return;
    }
    if (!(await ask(mtext.textConfirmDelete))) {
      return; // Onaylanmadı: profil kalır (AC-14).
    }
    state.pending = true;
    deleteButton.disabled = true;
    try {
      await post(`${bar.dataset.url}/${encodeURIComponent(state.editing.id)}/sil`);
      state.pending = false;
      close();
      reload();
    } catch (failure) {
      state.pending = false;
      deleteButton.disabled = false;
      formError.textContent = failure && failure.status === 403
        ? mtext.textForbidden
        : format(mtext.textDeleteFailed, correlationOf(failure));
      attachReportLink(formError, failure);
      deleteButton.focus();
    }
  });

  cancel.addEventListener('click', requestClose);
  closeButton.addEventListener('click', requestClose);
  modal.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      requestClose();
    } else if (event.key === 'Tab') {
      // Odak modal içinde kalır (AC-18).
      const nodes = focusables();
      const index = nodes.indexOf(doc.activeElement);
      if (event.shiftKey && index <= 0) {
        event.preventDefault();
        nodes[nodes.length - 1].focus();
      } else if (!event.shiftKey && index === nodes.length - 1) {
        event.preventDefault();
        nodes[0].focus();
      }
    }
  });

  newButton.addEventListener('click', () => open('new'));
  createButton.addEventListener('click', () => open('new'));
  editButton.addEventListener('click', () => {
    if (editButton.getAttribute('aria-disabled') !== 'true') {
      open('edit');
    }
  });

  await load();
  return { load, open, state };
}
