// Konsept düzenleme döngüsü (spec 0015): kalemle açılan düzenleme formu (sahne başlığı, ekran tarifi, seslendirme, süre — ekleme/
// silme/sıralama yok), "Kaydet" (çağrısız), talimatla "Yeniden üret" (arka planda; 2 sn yoklama) ve sayfanın üstündeki revizyon modalı
// (Onayla / Sil / Tekrar düzenle; dışına tıklama kapatmaz, Esc "Revizyon silinsin mi?" sorar). Kaydedilmemiş değişiklik varken başka
// konsepte geçiş ya da sayfadan ayrılma uyarır. Tüm istekler api.js ($.ajax, K-6) ile /konseptler/{id}/... uçlarına. Model çıktısı
// güvenilmeyen veridir: yalnızca textContent/value ile yazılır. Metinler resx'ten data-text-* ile gelir; kurallar sunucudadır.
import { get, post } from './api.js';

const POLL_MS = 2000;
const FOCUSABLE = 'button:not([disabled]), textarea:not([disabled]), input:not([disabled])';

function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

/**
 * Elle düzenlenen sahnelerin alan sorunları (sunucudaki kuralın aynısı; sunucu yine doğrular): başlık ve seslendirme dolu, süre pozitif
 * tam sayı. Anahtar "i.alan".
 * @param {{ title: string, visual: string, narration: string, durationSeconds: string|number }[]} scenes Sahneler.
 * @returns {string[]} Sorunlu alanlar.
 */
export function sceneIssues(scenes) {
  const issues = [];
  scenes.forEach((scene, i) => {
    if (!String(scene.title || '').trim()) {
      issues.push(`${i}.title`);
    }
    if (!String(scene.narration || '').trim()) {
      issues.push(`${i}.narration`);
    }
    if (!/^\s*\d+\s*$/.test(String(scene.durationSeconds ?? '')) || Number(scene.durationSeconds) <= 0) {
      issues.push(`${i}.durationSeconds`);
    }
  });
  return issues;
}

/**
 * Düzenleme döngüsünü başlatır (sayfada #concept-editor yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @param {{ setTimer?: Function, clearTimer?: Function, win?: Window }} [options] Zamanlayıcı ve pencere (testler için).
 * @returns {object|undefined} Denetleyici.
 */
export function initConceptEdit(doc, options = {}) {
  const editor = doc.getElementById('concept-editor');
  if (!editor) {
    return undefined;
  }
  const setTimer = options.setTimer || ((fn, ms) => globalThis.setTimeout(fn, ms));
  const clearTimer = options.clearTimer || ((id) => globalThis.clearTimeout(id));
  const win = options.win || globalThis.window;
  const text = editor.dataset;
  const el = (id) => doc.getElementById(id);
  const form = el('concept-edit-form');
  const scenesBox = el('concept-edit-scenes');
  const saveButton = el('concept-edit-save');
  const closeButton = el('concept-edit-close');
  const editStatus = el('concept-edit-status');
  const instruction = el('concept-instruction');
  const regenerateButton = el('concept-regenerate');
  const regenStatus = el('concept-regen-status');
  const modal = el('revision-modal');
  const modalScenes = el('revision-scenes');
  const modalWarning = el('revision-warning');
  const modalStatus = el('revision-status');
  const againInstruction = el('revision-instruction');
  const againButton = el('revision-again');
  const approveButton = el('revision-approve');
  const discardButton = el('revision-discard');
  const confirmBox = el('revision-confirm');
  const leaveBox = el('concept-leave');
  const state = { concept: null, editing: false, dirty: false, busy: false, saving: false, timer: null, opener: null, leave: null };

  function node(tag, className, content) {
    const element = doc.createElement(tag);
    if (className) {
      element.className = className;
    }
    if (content !== undefined) {
      element.textContent = content;
    }
    return element;
  }

  function message(error, fallback) {
    return (error && error.body && error.body.message) || fallback;
  }

  // --- Düzenleme formu ---------------------------------------------------------------------------------------------------------------
  function field(index, name, label, value, multiline) {
    const id = `concept-edit-${index}-${name}`;
    const wrap = node('div', 'field concept-editor__field');
    const caption = node('label', 'field__label', label);
    caption.htmlFor = id;
    const input = node(multiline ? 'textarea' : 'input', `field__input${name === 'durationSeconds' ? ' field__input--short' : ''}`);
    input.id = id;
    input.dataset.scene = String(index);
    input.dataset.field = name;
    if (multiline) {
      input.rows = 3;
    } else if (name === 'durationSeconds') {
      input.type = 'number';
      input.min = '1';
      input.step = '1';
      input.inputMode = 'numeric';
    } else {
      input.type = 'text';
    }
    input.value = String(value ?? '');
    const error = node('p', 'field__error');
    error.id = `${id}-error`;
    error.setAttribute('role', 'alert');
    input.setAttribute('aria-describedby', error.id);
    wrap.append(caption, input, error);
    return wrap;
  }

  function renderForm(scenes) {
    scenesBox.replaceChildren(...scenes.map((scene, i) => {
      const box = node('fieldset', 'concept-editor__scene');
      box.append(
        node('legend', '', fill(text.textScene, i + 1)),
        field(i, 'title', text.textTitle, scene.title, false),
        field(i, 'visual', text.textVisual, scene.visual, true),
        field(i, 'narration', text.textNarration, scene.narration, true),
        field(i, 'durationSeconds', text.textDuration, scene.durationSeconds, false));
      return box;
    }));
  }

  function readScenes() {
    const scenes = (state.concept ? state.concept.scenes : []).map(() => ({}));
    for (const input of scenesBox.querySelectorAll('[data-field]')) {
      scenes[Number(input.dataset.scene)][input.dataset.field] = input.value;
    }
    return scenes;
  }

  function toBody(scenes) {
    // Süre sayı olarak gider (ondalık/metin sunucuda da reddedilir).
    return scenes.map((s) => ({ ...s, durationSeconds: /^\s*-?\d+(\.\d+)?\s*$/.test(String(s.durationSeconds)) ? Number(s.durationSeconds) : s.durationSeconds }));
  }

  function clearErrors() {
    for (const error of scenesBox.querySelectorAll('.field__error')) {
      error.textContent = '';
    }
  }

  function showIssues(keys) {
    clearErrors();
    for (const key of keys) {
      const [index, name] = key.split('.');
      const error = el(`concept-edit-${index}-${name}-error`);
      if (error) {
        error.textContent = name === 'title' ? text.textTitleRequired : name === 'narration' ? text.textNarrationRequired : text.textDurationInvalid;
      }
    }
  }

  // Sunucunun alan hataları ("Scenes[0].title") alanın yanına yazılır.
  function showServerErrors(error) {
    const errors = (error && error.body && error.body.errors) || {};
    clearErrors();
    let shown = false;
    for (const [key, messages] of Object.entries(errors)) {
      const match = /^scenes\[(\d+)\]\.(\w+)$/i.exec(key);
      const target = match && el(`concept-edit-${match[1]}-${match[2]}-error`);
      if (target) {
        target.textContent = messages[0];
        shown = true;
      }
    }
    return shown;
  }

  function validate() {
    const scenes = readScenes();
    const issues = sceneIssues(scenes);
    showIssues(issues);
    if (issues.length) {
      const first = el(`concept-edit-${issues[0].replace('.', '-')}`);
      if (first) {
        first.focus();
      }
      return null;
    }
    return toBody(scenes);
  }

  function setDirty(dirty) {
    state.dirty = dirty;
    editor.dataset.dirty = dirty ? 'true' : 'false';
  }

  function startEdit(concept) {
    state.concept = concept;
    state.editing = true;
    setDirty(false);
    renderForm(concept.scenes);
    instruction.value = '';
    el('concept-instruction-error').textContent = '';
    editStatus.textContent = '';
    regenStatus.textContent = '';
    el('concept-edit-heading').textContent = concept.title;
    editor.hidden = false;
    const article = el('concept-detail');
    article.dataset.editing = 'true';
    article.hidden = true;
    setBusy(false);
    const first = scenesBox.querySelector('[data-field]');
    if (first) {
      first.focus();
    }
    pollRevision(); // Önceden kalmış hazır revizyon varsa modal yeniden açılır (ücretli revizyon kaybolmasın).
  }

  function stopEdit() {
    state.editing = false;
    setDirty(false);
    stopPolling();
    editor.hidden = true;
    const article = el('concept-detail');
    delete article.dataset.editing;
    if (article.dataset.id) {
      article.hidden = false;
    }
  }

  function setBusy(busy) {
    state.busy = busy;
    instruction.disabled = busy;
    regenerateButton.disabled = busy;
    saveButton.disabled = busy || state.saving;
    againInstruction.disabled = busy;
    againButton.disabled = busy;
    approveButton.disabled = busy;
    discardButton.disabled = busy;
  }

  async function save() {
    if (state.saving || state.busy) {
      return;
    }
    const scenes = validate();
    if (!scenes) {
      return;
    }
    state.saving = true;
    saveButton.disabled = true;
    editStatus.dataset.kind = '';
    editStatus.textContent = text.textSaving;
    try {
      const updated = await post(`/konseptler/${encodeURIComponent(state.concept.id)}/sahneler`, { scenes });
      state.concept = updated;
      setDirty(false);
      editStatus.textContent = text.textSaved;
      doc.dispatchEvent(new CustomEvent('concept:changed', { detail: { concept: updated } }));
    } catch (error) {
      // Metin korunur; alan hatası alanın yanında, değilse sade mesaj (AC-2, AC-3).
      editStatus.dataset.kind = 'error';
      editStatus.textContent = showServerErrors(error) ? text.textInvalid : message(error, text.textSaveFailed);
    } finally {
      state.saving = false;
      saveButton.disabled = state.busy;
    }
  }

  // --- Yeniden üretim + yoklama -------------------------------------------------------------------------------------------------------
  function stopPolling() {
    if (state.timer) {
      clearTimer(state.timer);
      state.timer = null;
    }
  }

  function reportError(messageText) {
    if (!modal.hidden) {
      modalStatus.dataset.kind = 'error';
      modalStatus.textContent = messageText;
    } else {
      regenStatus.dataset.kind = 'error';
      regenStatus.textContent = messageText;
    }
  }

  function handleRevision(revision) {
    if (!state.editing || !state.concept || revision.conceptId !== state.concept.id) {
      return;
    }
    if (revision.status === 'running') {
      setBusy(true);
      const status = modal.hidden ? regenStatus : modalStatus;
      status.dataset.kind = '';
      status.textContent = text.textRegenerating;
      state.timer = setTimer(pollRevision, POLL_MS);
      return;
    }
    setBusy(false);
    if (revision.status === 'ready') {
      regenStatus.textContent = '';
      openModal(revision);
    } else if (revision.status === 'failed') {
      reportError(revision.error || text.textRegenerateFailed); // Revizyon yok, konsept aynen; otomatik tekrar yok (AC-8).
    }
  }

  async function pollRevision() {
    state.timer = null;
    if (!state.editing || !state.concept) {
      return;
    }
    try {
      handleRevision(await get(`/konseptler/${encodeURIComponent(state.concept.id)}/revizyon`));
    } catch (error) {
      setBusy(false);
      reportError(message(error, text.textRegenerateFailed));
    }
  }

  async function regenerate(source, errorNode, withScenes) {
    if (state.busy) {
      return; // Çift tıklama ikinci istek göndermez (AC-12; sunucu da reddeder).
    }
    const value = source.value.trim();
    if (!value) {
      errorNode.textContent = text.textInstructionRequired; // AC-5.
      source.focus();
      return;
    }
    errorNode.textContent = '';
    const body = { instruction: value };
    if (withScenes && state.dirty) {
      const scenes = validate(); // Kaydedilmemiş elle değişiklikler modele gider (AC-4).
      if (!scenes) {
        return;
      }
      body.scenes = scenes;
    }
    setBusy(true);
    try {
      handleRevision(await post(`/konseptler/${encodeURIComponent(state.concept.id)}/yeniden-uret`, body));
    } catch (error) {
      setBusy(false);
      const fieldMessage = error && error.body && error.body.errors && error.body.errors.Instruction;
      if (fieldMessage) {
        errorNode.textContent = fieldMessage[0];
      } else {
        reportError(message(error, text.textRegenerateFailed));
      }
    }
  }

  // --- Revizyon modalı ----------------------------------------------------------------------------------------------------------------
  function renderRevision(revision) {
    modalWarning.hidden = !revision.wordRepetitionWarning; // "Kelime tekrarı" uyarılı revizyon (AC-10).
    el('revision-duration-warning').hidden = !revision.durationMismatchWarning;
    modalScenes.replaceChildren(...revision.scenes.map((scene, index) => {
      const item = node('li', 'concept-detail__scene');
      item.append(
        node('h4', 'concept-detail__scene-title', fill(text.textSceneHeading, index + 1, scene.title)),
        node('p', '', `${text.textVisual}: ${scene.visual}`),
        node('p', '', `${text.textNarration}: ${scene.narration}`),
        node('p', 'concept-detail__duration', fill(text.textSceneDuration, scene.durationSeconds)));
      return item;
    }));
  }

  function focusables(container) {
    return [...container.querySelectorAll(FOCUSABLE)].filter((n) => !n.closest('[hidden]'));
  }

  function openModal(revision) {
    renderRevision(revision);
    modalStatus.dataset.kind = '';
    modalStatus.textContent = '';
    againInstruction.value = '';
    el('revision-instruction-error').textContent = '';
    confirmBox.hidden = true;
    if (modal.hidden) {
      state.opener = doc.activeElement && doc.activeElement !== doc.body ? doc.activeElement : regenerateButton;
      modal.hidden = false;
    }
    el('revision-title').focus(); // Odak modala geçer (AC-15).
  }

  function closeModal() {
    modal.hidden = true;
    confirmBox.hidden = true;
    const opener = state.opener && doc.contains(state.opener) ? state.opener : regenerateButton;
    if (opener && !opener.disabled) {
      opener.focus(); // Odak açan kontrole döner (AC-15).
    } else {
      instruction.focus();
    }
  }

  async function approve() {
    setBusy(true);
    try {
      const updated = await post(`/konseptler/${encodeURIComponent(state.concept.id)}/revizyon/onayla`, {});
      setBusy(false);
      closeModal();
      startEditKeepFocus(updated);
      editStatus.textContent = text.textApproved;
      doc.dispatchEvent(new CustomEvent('concept:changed', { detail: { concept: updated } }));
    } catch (error) {
      setBusy(false);
      reportError(message(error, text.textRequestFailed));
    }
  }

  function startEditKeepFocus(concept) {
    state.concept = concept;
    setDirty(false);
    renderForm(concept.scenes);
    clearErrors();
  }

  async function discard() {
    setBusy(true);
    try {
      await post(`/konseptler/${encodeURIComponent(state.concept.id)}/revizyon/sil`, {});
      setBusy(false);
      closeModal(); // Konsept aynen kalır (AC-6).
    } catch (error) {
      setBusy(false);
      reportError(message(error, text.textRequestFailed));
    }
  }

  function askDiscard() {
    confirmBox.hidden = false;
    el('revision-confirm-cancel').focus();
  }

  function trapTab(event, container) {
    const nodes = focusables(container);
    if (!nodes.length) {
      return;
    }
    const index = nodes.indexOf(doc.activeElement);
    if (event.shiftKey && index <= 0) {
      event.preventDefault();
      nodes[nodes.length - 1].focus();
    } else if (!event.shiftKey && index === nodes.length - 1) {
      event.preventDefault();
      nodes[0].focus();
    }
  }

  // Dışına tıklama modalı kapatmaz (AC-9): backdrop'a tıklama dinlenmez.
  modal.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      if (!confirmBox.hidden) {
        confirmBox.hidden = true; // Vazgeç: modal revizyonla açık kalır.
        approveButton.focus();
      } else if (!state.busy) {
        askDiscard();
      }
    } else if (event.key === 'Tab') {
      trapTab(event, confirmBox.hidden ? modal : confirmBox);
    }
  });
  approveButton.addEventListener('click', approve);
  discardButton.addEventListener('click', discard);
  againButton.addEventListener('click', () => regenerate(againInstruction, el('revision-instruction-error'), false));
  el('revision-confirm-ok').addEventListener('click', discard);
  el('revision-confirm-cancel').addEventListener('click', () => {
    confirmBox.hidden = true;
    approveButton.focus();
  });

  // --- Form olayları ------------------------------------------------------------------------------------------------------------------
  form.addEventListener('input', (event) => {
    if (event.target.matches('[data-field]')) {
      setDirty(true);
      editStatus.textContent = '';
    }
  });
  form.addEventListener('submit', (event) => {
    event.preventDefault();
    save();
  });
  regenerateButton.addEventListener('click', () => regenerate(instruction, el('concept-instruction-error'), true));
  closeButton.addEventListener('click', () => {
    if (state.dirty) {
      askLeave(() => stopEdit());
    } else {
      stopEdit();
    }
  });

  // --- Kaydedilmemiş değişiklik uyarısı (AC-4) ----------------------------------------------------------------------------------------
  function askLeave(proceed) {
    state.leave = { proceed, opener: doc.activeElement };
    leaveBox.hidden = false;
    el('concept-leave-cancel').focus();
  }

  function closeLeave(go) {
    const leave = state.leave;
    state.leave = null;
    leaveBox.hidden = true;
    if (go && leave) {
      setDirty(false); // Değişiklikler atılır.
      leave.proceed();
    } else {
      const first = scenesBox.querySelector('[data-field]');
      if (first) {
        first.focus(); // Düzenleme ve değişiklikler kalır.
      }
    }
  }

  el('concept-leave-ok').addEventListener('click', () => closeLeave(true));
  el('concept-leave-cancel').addEventListener('click', () => closeLeave(false));
  leaveBox.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      closeLeave(false);
    } else if (event.key === 'Tab') {
      trapTab(event, leaveBox);
    }
  });

  // Belge/pencere dinleyicileri dispose ile kaldırılabilir (sayfa yeniden kurulursa ya da testlerde).
  const listeners = [
    [doc, 'concept:before-open', (event) => {
      if (state.editing && state.dirty) {
        event.preventDefault();
        askLeave(event.detail.proceed);
      }
    }],
    [doc, 'concept:opened', (event) => {
      const { concept, edit } = event.detail;
      stopPolling();
      if (edit && (concept.status === 'generated' || concept.status === 'approved')) {
        startEdit(concept);
      } else {
        stopEdit();
      }
    }],
    [doc, 'profile:selected', () => {
      stopEdit();
      state.concept = null;
    }],
  ];
  if (win) {
    listeners.push([win, 'beforeunload', (event) => {
      if (state.editing && state.dirty) {
        event.preventDefault(); // Tarayıcı kendi "ayrılmak istiyor musunuz" uyarısını gösterir (AC-4).
        event.returnValue = '';
      }
    }]);
  }
  for (const [target, name, handler] of listeners) {
    target.addEventListener(name, handler);
  }

  function dispose() {
    stopPolling();
    for (const [target, name, handler] of listeners) {
      target.removeEventListener(name, handler);
    }
  }

  return { state, save, dispose, regenerate: () => regenerate(instruction, el('concept-instruction-error'), true), pollRevision };
}
