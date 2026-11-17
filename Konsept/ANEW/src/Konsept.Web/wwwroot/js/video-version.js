// Yeni sürüm ve süren sürüm (spec 0020), Üretilen Videolar'ın sağ paneli. "Yeni sürüm" seçili sürümün ayarlarından başlar (motor, görsel
// modeli, ses motoru, altyazı, ilerleme çubuğu); her değişiklikte sunucu çalışacak aşamaları ve online çağrı yapılıp yapılmayacağını önizler
// (kurallar Api'de). Hiçbir ayar değişmediyse ya da videonun süren sürümü varsa "Yeni sürümü başlat" pasiftir ve nedeni fare + klavye
// odağında ipucu olarak okunur. Online çağrı varsa aşamaları ve uyarıyı gösteren onay penceresi (tahmini tutar yok) onaylanmadan istek
// gitmez. Süren sürüm: durum rozeti (metinli), hata nedeni, Tekrar dene / Sürümü kaldır (onaylı). Tüm istekler api.js ($.ajax, K-6).
import { get, post } from './api.js';
import { confirmDialog } from './confirm-dialog.js';

/** Üretimi süren (yoklanan) durumlar. */
export const ACTIVE_STATUSES = ['queued', 'producing', 'stopped', 'waiting_for_agent'];

function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

function capital(key) {
  return key.split(/[_-]/).map((part) => part.charAt(0).toUpperCase() + part.slice(1)).join('');
}

/**
 * Aşama anahtarlarının metni ("Seslendirme yapılıyor, Birleştiriliyor").
 * @param {string[]} stages Aşamalar.
 * @param {DOMStringMap} text Metinler (data-text-stage-*).
 * @returns {string} Metin.
 */
export function stagesText(stages, text) {
  return (stages || []).map((stage) => text[`textStage${capital(stage)}`] || stage).join(', ');
}

/**
 * Durum rozetinin metni (0006 Req 13 adları; Üretiliyor'da o anki aşama).
 * @param {string} status Durum anahtarı.
 * @param {string|null} stage Çalışan aşama.
 * @param {DOMStringMap} text Metinler.
 * @returns {string} Metin.
 */
export function statusText(status, stage, text) {
  const name = text[`textStatus${status === 'waiting_for_agent' ? 'Waiting_for_agent' : capital(status)}`] || status;
  return status === 'producing' && stage ? fill(text.textActiveStage, name, stagesText([stage], text)) : name;
}

/**
 * Kontrolü pasif yapar ya da etkinleştirir (aria-disabled + ipucu; passive-controls.js eylemi engeller).
 * @param {HTMLElement} button Kontrol.
 * @param {string|null} reason Neden; null → etkin.
 */
export function setPassive(button, reason) {
  if (!button) {
    return;
  }
  if (reason) {
    button.setAttribute('aria-disabled', 'true');
    button.classList.add('passive');
    button.dataset.tooltip = reason;
    button.setAttribute('aria-description', reason);
  } else {
    button.removeAttribute('aria-disabled');
    button.classList.remove('passive');
    delete button.dataset.tooltip;
    button.removeAttribute('aria-description');
  }
}

/**
 * Yeni sürüm alanını ve süren sürüm kutusunu bağlar (öğeler sayfada yoksa ilgili kısım çalışmaz).
 * @param {Document} doc Belge.
 * @param {{ text: DOMStringMap, onChanged: Function }} options Metinler ve "video değişti" geri çağrısı (yeniden yükleme/yoklama).
 * @returns {{ render: Function, renderActive: Function, reset: Function, state: object }} Denetleyici.
 */
export function initVersionControls(doc, { text, onChanged }) {
  const el = (id) => doc.getElementById(id);
  const panel = el('version-panel');
  const fields = {
    engine: el('version-engine'),
    image: el('version-image'),
    audio: el('version-audio'),
    subtitles: el('version-subtitles'),
    progress: el('version-progress'),
  };
  const startButton = el('version-start');
  const preview = el('version-preview');
  const status = el('version-status');
  const state = { key: null, video: null, base: null, options: null, preview: null, previewToken: 0, busy: false };

  function message(error, fallback) {
    return (error && error.body && error.body.message) || fallback;
  }

  function option(value, label) {
    const element = doc.createElement('option');
    element.value = value;
    element.textContent = label;
    return element;
  }

  function fillSelect(select, models, current, extra) {
    const list = [...(extra ? [extra] : []), ...models.map((m) => ({ id: m.id, label: fill(m.local ? text.textVersionLocalTag : text.textVersionOnlineTag, m.name) }))];
    if (current !== null && current !== undefined && !list.some((m) => m.id === current)) {
      list.unshift({ id: current, label: current }); // Tabanın seçimi listede yoksa (yetkisi kalkmış) yine görünür.
    }
    select.replaceChildren(...list.map((m) => option(m.id, m.label)));
    select.value = current ?? '';
  }

  function current() {
    return {
      baseVersionId: state.base.id,
      engine: fields.engine.value,
      imageModel: fields.image.value || null,
      audioModel: fields.audio.value || null,
      subtitles: Boolean(fields.subtitles.checked) && Boolean(fields.audio.value),
      progressBar: Boolean(fields.progress.checked),
    };
  }

  function activeReason() {
    return state.video && state.video.active ? text.textVersionBusy : null;
  }

  function updateStart() {
    const reason = activeReason() || (!state.preview ? text.textVersionPreviewing : state.preview.unchanged ? text.textVersionNoChange : null);
    setPassive(startButton, reason);
  }

  async function refreshPreview() {
    if (!state.options) {
      return;
    }
    fields.subtitles.disabled = !fields.audio.value; // Sessiz videoda altyazı olmaz.
    if (!fields.audio.value) {
      fields.subtitles.checked = false;
    }
    const token = ++state.previewToken;
    state.preview = null;
    preview.textContent = text.textVersionPreviewing;
    updateStart();
    try {
      const result = await post(`/uretilenler/video/${encodeURIComponent(state.video.id)}/yeni-surum/onizle`, current());
      if (token !== state.previewToken) {
        return;
      }
      state.preview = result;
      preview.textContent = result.unchanged
        ? text.textVersionNoChange
        : `${fill(text.textVersionStages, stagesText(result.stages, text))} ${result.online ? text.textVersionOnline : text.textVersionOffline}`;
    } catch (error) {
      if (token === state.previewToken) {
        preview.textContent = message(error, text.textVersionLoadFailed);
      }
    }
    updateStart();
  }

  async function load(video, version) {
    state.options = null;
    state.preview = null;
    status.textContent = '';
    preview.textContent = text.textVersionPreviewing;
    updateStart();
    try {
      const options = await get(`/uretilenler/video/${encodeURIComponent(video.id)}/yeni-surum?taban=${encodeURIComponent(version.id)}`);
      if (state.key !== `${video.id}:${version.id}`) {
        return;
      }
      state.options = options;
      fillSelect(fields.engine, options.engines || [], options.engine);
      // Görsel kapalı tabanda görsel açılamaz (kod istemi değişir); açık tabanda kapatılamaz — yalnızca model değişir.
      if (options.imageModel) {
        fillSelect(fields.image, options.images || [], options.imageModel);
        fields.image.disabled = false;
      } else {
        fields.image.replaceChildren(option('', text.textVersionImageOff));
        fields.image.disabled = true;
      }
      fillSelect(fields.audio, options.audios || [], options.audioModel ?? '', { id: '', label: text.textVersionSilent });
      fields.subtitles.checked = Boolean(options.subtitles);
      fields.progress.checked = Boolean(options.progressBar);
      await refreshPreview();
    } catch (error) {
      if (state.key === `${video.id}:${version.id}`) {
        preview.textContent = message(error, text.textVersionLoadFailed);
      }
    }
  }

  async function start() {
    if (!state.preview || state.busy || startButton.getAttribute('aria-disabled') === 'true') {
      return;
    }
    const body = { ...current(), confirmOnline: false, previewStages: state.preview.stages }; // Onay gördüğü aşamalara bağlı (review r1 M-1).
    if (state.preview.online) {
      el('version-confirm-stages').textContent = fill(text.textVersionConfirmStages, stagesText(state.preview.stages, text));
      if (!(await confirmDialog(el('version-confirm-dialog'), startButton))) {
        return; // Vazgeç / Esc: istek yok, iş yok.
      }
      body.confirmOnline = true;
    }
    state.busy = true;
    status.textContent = text.textVersionStarting;
    try {
      await post(`/uretilenler/video/${encodeURIComponent(state.video.id)}/yeni-surum`, body);
      status.textContent = text.textVersionStarted;
      onChanged();
    } catch (error) {
      status.textContent = message(error, text.textVersionLoadFailed);
    } finally {
      state.busy = false;
    }
  }

  if (panel) {
    for (const field of Object.values(fields)) {
      field.addEventListener('change', () => refreshPreview());
    }
    startButton.addEventListener('click', () => start());
  }

  // --- Süren sürüm --------------------------------------------------------------------------------------------------------------------
  const activeBox = el('produced-active');
  const retryButton = el('produced-active-retry');
  const removeButton = el('produced-active-remove');
  const activeMessage = el('produced-active-message');

  function renderActive(video) {
    state.video = video;
    updateStart();
    const versions = el('produced-versions');
    const pending = versions && versions.querySelector('.produced-versions__item--pending');
    if (pending) {
      pending.remove();
    }
    if (!activeBox) {
      return;
    }
    const active = video && video.active;
    activeBox.hidden = !active;
    if (!active) {
      return;
    }
    const label = statusText(active.status, active.stage, text);
    const badge = el('produced-active-status');
    badge.textContent = fill(text.textActiveStatus, label);
    badge.className = `badge produced-badge produced-badge--${active.status}`;
    const errorBox = el('produced-active-error');
    errorBox.hidden = !active.error;
    errorBox.textContent = active.error || '';
    retryButton.hidden = active.status !== 'failed';
    removeButton.hidden = active.status !== 'failed';
    if (versions) {
      // Sürüm listesinde süren sürüm de durumuyla görünür (Req 9); seçilemez (henüz videosu yok).
      const item = doc.createElement('li');
      item.className = 'produced-versions__item produced-versions__item--pending';
      item.textContent = `${fill(text.textVersion, video.versions.length + 1)} · ${label}`;
      versions.append(item);
    }
  }

  async function act(url, success) {
    activeMessage.textContent = '';
    try {
      await post(url, {});
      activeMessage.textContent = success;
      onChanged();
    } catch (error) {
      activeMessage.textContent = message(error, text.textActiveFailed);
    }
  }

  if (retryButton) {
    retryButton.addEventListener('click', () => act(`/uretilenler/video/${encodeURIComponent(state.video.id)}/tekrar-dene`, text.textActiveRetried));
  }
  if (removeButton) {
    removeButton.addEventListener('click', async () => {
      if (await confirmDialog(el('version-remove-dialog'), removeButton)) {
        await act(`/uretilenler/video/${encodeURIComponent(state.video.id)}/surumu-kaldir`, text.textActiveRemoved);
      }
    });
  }

  return {
    state,
    renderActive,
    /** Seçili video/sürüm: taban değiştiyse alan yeniden kurulur (kullanıcının seçimi yoklamada kaybolmaz). */
    render(video, version) {
      renderActive(video);
      const key = `${video.id}:${version.id}`;
      if (!panel || key === state.key) {
        return;
      }
      state.key = key;
      state.base = version;
      load(video, version);
    },
    reset() {
      state.key = null;
      state.video = null;
      state.options = null;
      state.preview = null;
      if (activeBox) {
        activeBox.hidden = true;
      }
    },
  };
}
