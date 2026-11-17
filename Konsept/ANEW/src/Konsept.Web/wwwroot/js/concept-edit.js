// Konsept düzenleme döngüsü (spec 0015): kalemle açılan düzenleme formu (sahne başlığı, ekran tarifi, seslendirme, süre — ekleme/
// silme/sıralama yok), "Kaydet" (çağrısız), talimatla "Yeniden üret" (arka planda; 2 sn yoklama) ve sayfanın üstündeki revizyon modalı
// (Onayla / Sil / Tekrar düzenle; dışına tıklama kapatmaz, Esc "Revizyon silinsin mi?" sorar). Kaydedilmemiş değişiklik varken başka
// konsepte geçiş ya da sayfadan ayrılma uyarır. Tüm istekler api.js ($.ajax, K-6) ile /konseptler/{id}/... uçlarına. Model çıktısı
// güvenilmeyen veridir: yalnızca textContent/value ile yazılır. Metinler resx'ten data-text-* ile gelir; kurallar sunucudadır.
// Yeniden üret bağlamı (spec 0020): adres ?konsept=<id>&yenidenUret=<belirteç>&taban=<sürüm> ise "Video oldu" konsept bu döngüyle düzenlenir
// (istekler /konseptler/{id}/baglam/... ve belirteç); kaydedilince/revizyon onaylanınca #regen-panel çalışacak aşamaları ve online uyarısını
// gösterir, metin değişmediyse "Konsept değişmedi" (başlat pasif) ve "Yine de baştan üret"; başlatma onay penceresinden geçer.
import { get, post } from './api.js';
import { confirmDialog } from './confirm-dialog.js';
import { setPassive, stagesText } from './video-version.js';

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
 * @param {{ setTimer?: Function, clearTimer?: Function, win?: Window, navigate?: Function }} [options] Zamanlayıcı, pencere ve sayfa
 *   değiştirme (testler için).
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
  const state = { concept: null, editing: false, dirty: false, busy: false, saving: false, timer: null, opener: null, leave: null, regen: null, preview: null };
  const navigate = options.navigate || ((url) => globalThis.location.assign(url));
  const params = new URLSearchParams((doc.location && doc.location.search) || '');
  if (params.get('yenidenUret') && params.get('konsept')) {
    state.regen = { conceptId: params.get('konsept'), token: params.get('yenidenUret'), base: params.get('taban') || '' };
  }
  const regenPanel = el('regen-panel');
  const regenText = regenPanel ? regenPanel.dataset : {};

  function inContext(concept = state.concept) {
    return Boolean(state.regen && concept && concept.id === state.regen.conceptId);
  }

  /** Düzenleme isteğinin adresi: bağlamda /baglam/... + belirteç (spec 0020), değilse 0015 uçları. */
  function editUrl(suffix) {
    const id = encodeURIComponent(state.concept.id);
    return inContext() ? `/konseptler/${id}/baglam/${suffix}?regen=${encodeURIComponent(state.regen.token)}` : `/konseptler/${id}/${suffix}`;
  }

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
    updateRegenStart();
  }

  // --- Yeniden üret bağlamı (spec 0020) ------------------------------------------------------------------------------------------------
  function updateRegenStart() {
    if (!regenPanel || !state.regen) {
      return;
    }
    // "Yine de baştan üret" yalnızca metin değişmediyse sunulur (review r1 I-3).
    const offered = Boolean(state.preview && state.preview.unchanged);
    if (el('regen-force-label')) {
      el('regen-force-label').hidden = !offered;
    }
    if (!offered) {
      el('regen-force').checked = false;
    }
    const force = el('regen-force').checked;
    const reason = state.dirty ? regenText.textSaveFirst
      : !state.preview ? regenText.textPreviewing
        : state.preview.unchanged && !force ? regenText.textUnchanged : null;
    setPassive(el('regen-start'), reason);
  }

  async function refreshRegen() {
    if (!regenPanel || !inContext()) {
      return;
    }
    regenPanel.hidden = false;
    state.preview = null;
    el('regen-preview').textContent = regenText.textPreviewing;
    updateRegenStart();
    try {
      const preview = await get(`/uretilenler/video/${encodeURIComponent(state.concept.id)}/yeniden-uret?belirtec=${encodeURIComponent(state.regen.token)}`
        + `&taban=${encodeURIComponent(state.regen.base)}`);
      state.preview = preview;
      const lines = [fill(regenText.textAllStages, stagesText(preview.stages, regenText))];
      if (preview.online) {
        lines.push(regenText.textOnline);
      }
      if (preview.unchanged) {
        lines.push(regenText.textUnchanged);
      }
      el('regen-preview').textContent = lines.join(' ');
    } catch (error) {
      el('regen-preview').textContent = message(error, regenText.textLoadFailed);
    }
    updateRegenStart();
  }

  async function startRegen() {
    const button = el('regen-start');
    if (!state.preview || button.getAttribute('aria-disabled') === 'true') {
      return;
    }
    el('regen-confirm-stages').textContent = fill(regenText.textConfirmStages, stagesText(state.preview.stages, regenText));
    if (!(await confirmDialog(el('regen-confirm-dialog'), button))) {
      return; // Vazgeç / Esc: iş yok, çağrı yok (AC-5, AC-12).
    }
    const status = el('regen-status');
    try {
      await post(`/uretilenler/video/${encodeURIComponent(state.concept.id)}/yeniden-uret`, {
        token: state.regen.token, baseVersionId: state.regen.base || null, force: el('regen-force').checked, confirmOnline: true,
      });
      status.textContent = regenText.textStarted;
      state.regen = null;
      navigate('/uretilenler');
    } catch (error) {
      status.textContent = message(error, regenText.textLoadFailed);
    }
  }

  if (regenPanel) {
    el('regen-force').addEventListener('change', () => updateRegenStart());
    el('regen-start').addEventListener('click', () => startRegen());
    el('regen-back').addEventListener('click', () => navigate('/uretilenler')); // Düzenleme iptal: hiçbir iş oluşmaz.
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
    if (regenPanel) {
      regenPanel.hidden = !inContext(concept);
    }
    refreshRegen();
    pollRevision(true); // Önceden kalmış hazır revizyon varsa modal yeniden açılır (ücretli revizyon kaybolmasın).
  }

  function stopEdit() {
    state.editing = false;
    setDirty(false);
    stopPolling();
    editor.hidden = true;
    if (regenPanel) {
      regenPanel.hidden = true;
    }
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
      const updated = await post(editUrl('sahneler'), { scenes });
      state.concept = updated;
      setDirty(false);
      editStatus.textContent = text.textSaved;
      doc.dispatchEvent(new CustomEvent('concept:changed', { detail: { concept: updated } }));
      refreshRegen(); // Bağlamda: kaydedilen metinle önizleme ve onay adımı (spec 0020 Req 15).
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

  function handleRevision(revision, initial = false) {
    if (!state.editing || !state.concept || revision.conceptId !== state.concept.id) {
      return;
    }
    if (revision.status === 'running') {
      setBusy(true);
      const status = modal.hidden ? regenStatus : modalStatus;
      status.dataset.kind = '';
      status.textContent = text.textRegenerating;
      state.timer = setTimer(() => pollRevision(), POLL_MS);
      return;
    }
    setBusy(false);
    if (revision.status === 'ready') {
      regenStatus.textContent = '';
      openModal(revision);
      if (revision.error && !initial) {
        // Tekrar düzenle başarısız: önceki (ücretli) revizyon modalda kalır, hata yanında duyurulur (review r1 M-1).
        modalStatus.dataset.kind = 'error';
        modalStatus.textContent = revision.error;
      }
    } else if (revision.status === 'failed' && !initial) {
      // Açılıştaki eski hata yeniden gösterilmez (review r1 L-3).
      reportError(revision.error || text.textRegenerateFailed); // Revizyon yok, konsept aynen; otomatik tekrar yok (AC-8).
    }
  }

  async function pollRevision(initial = false) {
    state.timer = null;
    if (!state.editing || !state.concept) {
      return;
    }
    try {
      handleRevision(await get(`/konseptler/${encodeURIComponent(state.concept.id)}/revizyon`), initial);
    } catch (error) {
      setBusy(false);
      if (!initial) {
        reportError(message(error, text.textRegenerateFailed));
      }
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
      handleRevision(await post(editUrl('yeniden-uret'), body));
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
      const updated = await post(editUrl('revizyon/onayla'), {});
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
    refreshRegen(); // Revizyon onaylandı: bağlamda önizleme ve onay adımına dönülür (spec 0020 Req 15).
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
      if (edit && (concept.status === 'generated' || concept.status === 'approved' || (concept.status === 'video_ready' && inContext(concept)))) {
        startEdit(concept);
      } else {
        stopEdit();
      }
    }],
    [doc, 'concept:deleted', (event) => {
      if (state.concept && state.concept.id === event.detail.id) {
        stopEdit(); // Silinen konseptin düzenleyicisi kapanır (spec 0016).
        el('concept-detail').hidden = true;
        state.concept = null;
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
