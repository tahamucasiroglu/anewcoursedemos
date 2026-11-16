// Konseptler sayfası şeridi (spec 0005): model bilgisi + modalı, ince ayar ve kelime denetimi; ayarlar seçili profile aittir
// (profiles.js 'profile:selected' olayıyla duyurur). Tüm istekler api.js ($.ajax, K-6) ile /konsept-ayarlari/{profil} uçlarına.
// Kurallar (seçilebilir model, efor, ince ayar kodları) sunucudadır; burada yalnızca gösterim. Metinler resx'ten data-text-* ile gelir.
import { get, post } from './api.js';

const ROLES = ['concept', 'dedupe', 'topic'];
const GROUPS = ['tone', 'audience', 'narration'];
const FOCUSABLE = 'a[href], button:not([disabled]), select:not([disabled]), input:not([disabled])';

/**
 * Efor düzeyinin görünen biçimi ("high" → "High").
 * @param {string|null} effort Efor.
 * @returns {string} Metin.
 */
export function effortLabel(effort) {
  return effort ? effort.charAt(0).toUpperCase() + effort.slice(1) : '';
}

/**
 * Bir rolün şeritteki özeti: "Claude Opus 4.5 High", efor yoksa yalnızca ad, seçim yoksa "seçilmedi".
 * @param {{ modelId: string|null, modelName: string|null, effort: string|null }|undefined} role Rol.
 * @param {string} notSelected "seçilmedi" metni.
 * @returns {string} Özet.
 */
export function roleSummary(role, notSelected) {
  if (!role || !role.modelId) {
    return notSelected;
  }
  const name = role.modelName || role.modelId;
  return role.effort ? `${name} ${effortLabel(role.effort)}` : name;
}

function option(doc, value, label) {
  const node = doc.createElement('option');
  node.value = value;
  node.textContent = label;
  return node;
}

/**
 * Şeridi başlatır (sayfada #concept-strip yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @returns {object|undefined} Denetleyici (testler için).
 */
export function initConcepts(doc) {
  const strip = doc.getElementById('concept-strip');
  if (!strip) {
    return undefined;
  }
  const text = strip.dataset;
  const el = (id) => doc.getElementById(id);
  const status = el('concept-settings-status');
  const openButton = el('concept-models-open');
  const toggle = el('concept-finetune-toggle');
  const panel = el('concept-finetune');
  const wordCheck = el('concept-wordcheck');
  const modal = el('concept-models-modal');
  const closeButton = el('concept-models-close');
  const modalStatus = el('concept-models-status');
  const empty = el('concept-models-empty');
  const addProfile = el('concepts-add-profile');
  const state = { profileId: null, settings: null, opener: null, pending: false, snapshot: null };

  function setStatus(message, kind) {
    status.textContent = message;
    status.dataset.kind = kind || '';
  }

  function setEnabled(enabled) {
    openButton.disabled = !enabled;
    toggle.disabled = !enabled;
    wordCheck.disabled = !enabled;
  }

  function radios(group) {
    return [...panel.querySelectorAll(`input[name="${group}"]`)];
  }

  function optionText(group, value) {
    const radio = radios(group).find((r) => r.value === value);
    return radio ? radio.parentElement.textContent.trim() : text.textUnspecified;
  }

  function render(settings) {
    state.settings = settings;
    for (const role of ROLES) {
      const node = strip.querySelector(`[data-role="${role}"] [data-role-value]`);
      node.textContent = roleSummary(settings.roles.find((r) => r.role === role), text.textNotSelected);
    }
    for (const group of GROUPS) {
      const value = settings[group] || '';
      strip.querySelector(`[data-group="${group}"] [data-group-value]`).textContent = value ? optionText(group, value) : text.textUnspecified;
      for (const radio of radios(group)) {
        radio.checked = radio.value === value;
      }
    }
    wordCheck.checked = Boolean(settings.wordCheck);
    strip.dataset.state = 'ready';
  }

  function url() {
    return `${text.url}/${encodeURIComponent(state.profileId)}`;
  }

  async function load(profileId) {
    state.profileId = profileId;
    strip.dataset.state = 'loading';
    setEnabled(false);
    setStatus(text.textLoading);
    try {
      render(await get(url()));
      setEnabled(true);
      setStatus('');
    } catch {
      strip.dataset.state = 'error';
      setStatus(text.textLoadFailed, 'error'); // Sade Türkçe mesaj (AC-7).
    }
  }

  // --- İnce ayar ve kelime denetimi: her değişiklik hemen kaydedilir; hata olursa önceki değer geri gelir (AC-7). ------------
  async function saveStrip(body) {
    setStatus(text.textSaving);
    try {
      render(await post(url(), body));
      setStatus(text.textSaved);
    } catch {
      render(state.settings);
      setStatus(text.textSaveFailed, 'error');
    }
  }

  function setPanel(open) {
    panel.hidden = !open;
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    if (open) {
      const checked = panel.querySelector('input:checked') || panel.querySelector('input');
      checked.focus();
    }
  }

  toggle.addEventListener('click', () => setPanel(panel.hidden));
  panel.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      setPanel(false);
      toggle.focus();
    }
  });
  panel.addEventListener('change', () => {
    const value = (group) => {
      const checked = radios(group).find((r) => r.checked);
      return checked && checked.value ? checked.value : null;
    };
    saveStrip({ fineTune: { tone: value('tone'), audience: value('audience'), narration: value('narration') } });
  });
  wordCheck.addEventListener('change', () => saveStrip({ wordCheck: wordCheck.checked }));

  // --- Model bilgisi modalı ---------------------------------------------------------------------------------------------------
  function rowOf(role) {
    return modal.querySelector(`[data-role="${role}"]`);
  }

  function fillEffort(row, effort) {
    const models = state.settings.models || [];
    const model = models.find((m) => m.id === row.querySelector('[data-model-select]').value);
    const levels = model ? model.effortLevels || [] : [];
    const effortSelect = row.querySelector('[data-effort-select]');
    // Efor yalnızca modelin desteklediği düzeylerden; desteklemeyen modelde efor kontrolü görünmez (AC-2).
    row.querySelector('[data-effort-field]').hidden = levels.length === 0;
    effortSelect.replaceChildren(...levels.map((l) => option(doc, l, effortLabel(l))));
    if (levels.length) {
      const fallback = levels.includes(model.defaultEffort) ? model.defaultEffort : levels[0];
      effortSelect.value = effort && levels.includes(effort) ? effort : fallback;
    }
  }

  function fillModal() {
    const models = state.settings.models || [];
    empty.hidden = models.length > 0; // Seçilebilir model yok: açıklama + Ayarlar yönlendirmesi (AC-3).
    for (const role of ROLES) {
      const row = rowOf(role);
      const select = row.querySelector('[data-model-select]');
      select.replaceChildren(select.options[0], ...models.map((m) => option(doc, m.id, m.name)));
      const current = state.settings.roles.find((r) => r.role === role);
      select.value = current && models.some((m) => m.id === current.modelId) ? current.modelId : '';
      select.disabled = models.length === 0;
      fillEffort(row, current && current.effort);
    }
  }

  function readModal() {
    return ROLES.map((role) => {
      const row = rowOf(role);
      const modelId = row.querySelector('[data-model-select]').value || null;
      const effort = modelId && !row.querySelector('[data-effort-field]').hidden ? row.querySelector('[data-effort-select]').value : null;
      return { role, modelId, effort };
    });
  }

  function focusables() {
    return [...modal.querySelectorAll(FOCUSABLE)].filter((node) => !node.closest('[hidden]'));
  }

  function openModal() {
    if (!state.settings) {
      return;
    }
    state.opener = doc.activeElement;
    fillModal();
    modalStatus.textContent = '';
    state.snapshot = JSON.stringify(readModal());
    modal.hidden = false;
    focusables()[0].focus(); // Odak modala geçer (AC-9).
  }

  function closeModal() {
    modal.hidden = true;
    const opener = state.opener;
    if (opener && typeof opener.focus === 'function' && doc.contains(opener)) {
      opener.focus(); // Odak açan kontrole döner (AC-9).
    }
  }

  async function requestClose() {
    if (state.pending) {
      return;
    }
    const models = readModal();
    if (JSON.stringify(models) === state.snapshot) {
      closeModal();
      return;
    }
    state.pending = true;
    modalStatus.textContent = text.textSaving;
    try {
      render(await post(url(), { models }));
      state.pending = false;
      closeModal();
      setStatus(text.textSaved);
    } catch {
      state.pending = false;
      fillModal(); // Önceki (kayıtlı) seçim korunur (AC-7).
      state.snapshot = JSON.stringify(readModal());
      modalStatus.textContent = text.textSaveFailed;
    }
  }

  openButton.addEventListener('click', openModal);
  closeButton.addEventListener('click', requestClose);
  modal.addEventListener('click', (event) => {
    if (event.target === modal) {
      requestClose(); // Modal dışına tıklama kapatır ve kaydeder (AC-2).
    }
  });
  modal.addEventListener('change', (event) => {
    if (event.target.matches('[data-model-select]')) {
      fillEffort(event.target.closest('[data-role]'), null); // Model seçilince katalog varsayılan eforu (AC-2).
    }
  });
  modal.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      requestClose();
    } else if (event.key === 'Tab') {
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

  if (addProfile) {
    addProfile.addEventListener('click', () => {
      const plus = el('profile-new');
      if (plus) {
        plus.click();
      }
    });
  }

  doc.addEventListener('profile:selected', (event) => load(event.detail.id));
  const bar = el('profile-bar');
  if (bar && bar.dataset.selectedId) {
    load(bar.dataset.selectedId);
  }
  return { load, state, openModal, requestClose };
}
