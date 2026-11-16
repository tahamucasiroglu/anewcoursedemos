// Üretilen Videolar (spec 0007): solda seçili profilin videoları (50'şerli, "Daha fazla göster"), ortada ajanın yerel akışından izleme
// (tarayıcının yerel kontrolleri; ses düzeyi bu tarayıcıda hatırlanır), altında Klasörde aç / Sistem oynatıcısında aç; sağda sahneler,
// konsept metni, ayrıntılar ve ücret, sürümler. Ajan durumu status.js'in 3 sn yoklamasından ('agent:status') gelir: ajan yokken mesaj ve pasif
// eylemler, ajan gelince oynatma kendiliğinden yeniden denenir. Akış adresi yüklenmeden hata verirse bu cihaz ajanın bilgisayarı değildir.
// Tüm istekler api.js ($.ajax, K-6); LLM metni güvenilmeyen veridir, yalnızca textContent. Metinler resx'ten data-text-*.
import { get, post } from './api.js';
import { formatDuration, formatTime } from './journal.js';

export const VOLUME_KEY = 'anew.produced.volume';
const dateFormat = new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul', day: '2-digit', month: '2-digit', year: 'numeric' });

function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

/**
 * Para metni (E-3): USD, 2 ondalık; $0,01 altı "<$0,01"; tam değer sent altı hassasiyetle (ipucu için). Sıfır → null (yerel — ücretsiz).
 * @param {number} usd Tutar.
 * @returns {{text:string, exact:string}|null} Metin.
 */
export function moneyText(usd) {
  const value = Number(usd || 0);
  if (!(value > 0)) {
    return null;
  }
  const exact = `$${value.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 6, useGrouping: false })}`;
  const text = value < 0.01 ? '<$0,01' : `$${value.toFixed(2).replace('.', ',')}`;
  return { text, exact };
}

/**
 * Tarih + saat (Europe/Istanbul; saat HH:mm).
 * @param {string} iso UTC zaman.
 * @returns {string} Metin.
 */
export function formatDateTime(iso) {
  return `${dateFormat.format(new Date(iso))} ${formatTime(iso)}`;
}

/**
 * Sayfayı başlatır (sayfada #produced yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @param {{storage?: Storage|null}} [options] Ses düzeyinin saklandığı yer (testte sahte).
 * @returns {object|undefined} Denetleyici (testler için).
 */
export function initProduced(doc, options = {}) {
  const root = doc.getElementById('produced');
  if (!root) {
    return undefined;
  }
  const text = root.dataset;
  const storage = 'storage' in options ? options.storage : globalThis.localStorage;
  const units = { second: text.textSecond, minute: text.textMinute, hour: text.textHour };
  const el = (id) => doc.getElementById(id);
  const player = el('produced-player');
  const state = { profileId: null, count: 0, video: null, version: null, viewer: 'idle', agent: { connected: null, machineName: null }, token: 0 };

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

  function messageOf(error, fallback) {
    return (error && error.body && error.body.message) || fallback;
  }

  // --- Ses düzeyi (tema gibi bu tarayıcıda) ----------------------------------------------------------------------------------------
  function restoreVolume() {
    try {
      const saved = storage && JSON.parse(storage.getItem(VOLUME_KEY) || 'null');
      if (saved && typeof saved.volume === 'number') {
        player.volume = Math.min(1, Math.max(0, saved.volume));
        player.muted = Boolean(saved.muted);
      }
    } catch {
      // Depolama yoksa varsayılan ses.
    }
  }

  player.addEventListener('volumechange', () => {
    try {
      if (storage) {
        storage.setItem(VOLUME_KEY, JSON.stringify({ volume: player.volume, muted: player.muted }));
      }
    } catch {
      // Depolama yoksa yalnızca bu oturum.
    }
  });

  // --- Dosya eylemleri -------------------------------------------------------------------------------------------------------------
  const actions = [el('produced-open-folder'), el('produced-open-player')];

  function setActions(reason) {
    el('produced-actions').hidden = !state.version;
    for (const button of actions) {
      button.textContent = button.dataset.label;
      button.disabled = false;
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
  }

  async function openFile(button) {
    if (!state.version || button.getAttribute('aria-disabled') === 'true') {
      return;
    }
    const version = state.version;
    const status = el('produced-action-status');
    button.textContent = text.textSending;
    button.disabled = true;
    status.textContent = '';
    try {
      await post(`/uretilenler/surum/${encodeURIComponent(version.id)}/ac`, { action: button.dataset.action });
      status.textContent = button.dataset.action === 'folder' ? text.textOpenedFolder : text.textOpenedPlayer;
      button.textContent = button.dataset.label;
      button.disabled = false;
    } catch (error) {
      button.textContent = button.dataset.label;
      button.disabled = false;
      if (error && error.status === 409) {
        setViewer('offline');
      } else if (error && error.status === 404 && version === state.version) {
        setViewer('missing');
      }
      status.textContent = error && error.status === 504 ? text.textTimeout : messageOf(error, text.textActionFailed);
    }
  }

  for (const button of actions) {
    button.addEventListener('click', () => openFile(button));
  }

  // --- İzleme yeri ----------------------------------------------------------------------------------------------------------------
  function stopPlayer() {
    player.pause?.();
    player.hidden = true;
    player.removeAttribute('src');
    player.load?.();
  }

  function setViewer(kind) {
    state.viewer = kind;
    const status = el('produced-viewer-status');
    el('produced-stream-retry').hidden = !['error', 'other-device'].includes(kind);
    const device = state.agent.machineName || text.textDeviceUnknown;
    const messages = {
      idle: text.textViewerHint,
      loading: text.textLoading,
      ready: '',
      offline: text.textAgentOffline,
      'other-device': fill(text.textOtherDevice, device),
      missing: text.textFileMissing,
      error: text.textStreamError,
    };
    status.textContent = messages[kind] ?? '';
    if (!['ready', 'loading'].includes(kind)) {
      stopPlayer();
    }
    const reason = { offline: text.textReasonAgent, missing: text.textReasonFile }[kind] || null;
    setActions(reason);
  }

  async function loadStream() {
    const version = state.version;
    const token = ++state.token;
    if (!version) {
      setViewer('idle');
      return;
    }
    if (state.agent.connected === false) {
      setViewer('offline');
      return;
    }
    setViewer('loading');
    stopPlayer();
    try {
      const result = await post(`/uretim/video/${encodeURIComponent(version.id)}/izle`, {});
      if (token !== state.token) {
        return;
      }
      restoreVolume();
      player.src = result.url;
      player.setAttribute('aria-label', fill(text.textPlayer, state.video.title));
      player.hidden = false;
    } catch (error) {
      if (token !== state.token) {
        return;
      }
      if (error && error.status === 409) {
        setViewer('offline');
      } else if (error && error.status === 404 && error.body && error.body.code === 'production.file_missing') {
        setViewer('missing');
      } else {
        setViewer('error');
        el('produced-viewer-status').textContent = messageOf(error, text.textStreamError);
      }
    }
  }

  // Oynatılabilir olunca hazır; yüklenmeden hata → akışa bu cihazdan ulaşılamıyor; oynarken hata → akış hatası (Yeniden dene).
  player.addEventListener('loadedmetadata', () => {
    if (state.viewer === 'loading') {
      setViewer('ready');
    }
  });
  player.addEventListener('error', () => {
    if (!player.getAttribute('src')) {
      return;
    }
    setViewer(state.viewer === 'loading' ? 'other-device' : 'error');
  });
  el('produced-stream-retry').addEventListener('click', () => loadStream());

  doc.addEventListener('agent:status', (event) => {
    if (!root.isConnected) {
      return; // Sayfadan kaldırılmış eski denetleyici.
    }
    const before = state.agent.connected;
    state.agent = { connected: event.detail.connected, machineName: event.detail.machineName || state.agent.machineName };
    if (!state.version) {
      return;
    }
    if (state.agent.connected === false && before !== false) {
      setViewer('offline');
    } else if (state.agent.connected === true && before === false && state.viewer === 'offline') {
      loadStream(); // Ajan bağlanınca (≤3 sn yoklama) oynatma yeniden mümkün.
    }
  });

  // --- Sağ panel --------------------------------------------------------------------------------------------------------------------
  function money(value) {
    const m = moneyText(value);
    const span = node('span', 'produced-money', m ? m.text : text.textFree);
    if (m) {
      span.title = fill(text.textExactCost, m.exact);
      span.setAttribute('aria-label', fill(text.textExactCost, m.exact));
      span.tabIndex = 0; // İpucu klavye odağında da okunur.
    }
    return span;
  }

  function row(list, label, value) {
    const term = node('dt', '', label);
    const detail = node('dd', '');
    if (typeof value === 'string') {
      detail.textContent = value;
    } else {
      detail.append(value);
    }
    list.append(term, detail);
  }

  function renderPanel() {
    const video = state.video;
    const version = state.version;
    el('produced-panel-hint').hidden = true;
    el('produced-panel').hidden = false;
    el('produced-title').textContent = video.title;
    el('produced-scenes').replaceChildren(...version.scenes.map((scene, index) => {
      const item = node('li', 'produced-scenes__item');
      item.append(node('h4', 'produced-scenes__title', fill(text.textSceneTitle, index + 1, scene.title)), node('p', '', scene.narration));
      return item;
    }));
    const concept = el('produced-concept');
    concept.replaceChildren(node('h4', 'produced-concept__title', version.conceptTitle), ...version.scenes.map((scene, index) => {
      const block = node('div', 'produced-concept__scene');
      block.append(
        node('p', 'produced-concept__scene-title', fill(text.textSceneTitle, index + 1, scene.title)),
        node('p', '', `${text.textSceneVisual}: ${scene.visual}`),
        node('p', '', `${text.textSceneNarration}: ${scene.narration}`));
      return block;
    }));
    const details = el('produced-details');
    details.replaceChildren();
    row(details, text.textCost, money(version.costUsd));
    row(details, text.textTotalCost, money(video.totalCostUsd));
    row(details, text.textCodeModel, version.codeModel || text.textOff);
    row(details, text.textEngine, version.engine);
    row(details, text.textImageModel, version.imageModel || text.textOff);
    row(details, text.textAudioModel, version.audioModel || text.textOff);
    row(details, text.textSubtitles, version.subtitles ? text.textOn : text.textOff);
    row(details, text.textProgressBar, version.progressBar ? text.textOn : text.textOff);
    row(details, text.textDuration, formatDuration(version.durationSeconds, units));
    row(details, text.textFormat, version.format === 'portrait' ? text.textPortrait : text.textLandscape);
    row(details, text.textStarted, formatTime(version.startedAt));
    row(details, text.textEnded, formatTime(version.endedAt));
    if (version.aiVoiceNotice) {
      details.append(node('dd', 'produced-details__ai', text.textAiVoice));
    }
    el('produced-versions').replaceChildren(...video.versions.map((v) => {
      const item = node('li', 'produced-versions__item');
      const button = node('button', 'produced-versions__select');
      button.type = 'button';
      button.dataset.version = v.id;
      button.setAttribute('aria-pressed', v.id === version.id ? 'true' : 'false');
      const cost = moneyText(v.costUsd);
      const status = text[`textStatus${v.status.charAt(0).toUpperCase()}${v.status.slice(1)}`] || v.status;
      const costLabel = cost ? cost.text : text.textFree;
      button.append(
        node('span', 'produced-versions__number', fill(text.textVersion, v.number)),
        node('span', 'produced-versions__meta', `${formatDateTime(v.endedAt)} · ${v.changes} · ${costLabel} · ${status}`));
      button.setAttribute('aria-label', fill(text.textVersionLabel, v.number, formatDateTime(v.endedAt), v.changes, costLabel, status));
      if (cost) {
        button.title = fill(text.textExactCost, cost.exact);
      }
      item.append(button);
      return item;
    }));
  }

  el('produced-versions').addEventListener('click', (event) => {
    const button = event.target.closest('[data-version]');
    if (!button || !state.video) {
      return;
    }
    const version = state.video.versions.find((v) => v.id === button.dataset.version);
    if (version && version !== state.version) {
      state.version = version;
      el('produced-action-status').textContent = '';
      renderPanel();
      loadStream();
    }
  });

  // --- Liste ----------------------------------------------------------------------------------------------------------------------
  function clearSelection() {
    state.video = null;
    state.version = null;
    state.token += 1;
    el('produced-panel').hidden = true;
    el('produced-panel-hint').hidden = false;
    el('produced-action-status').textContent = '';
    setViewer('idle');
  }

  async function selectVideo(id, button) {
    for (const other of el('produced-list').querySelectorAll('[data-video]')) {
      other.setAttribute('aria-pressed', other === button ? 'true' : 'false');
    }
    const token = ++state.token;
    el('produced-panel').hidden = true;
    el('produced-panel-hint').hidden = false;
    el('produced-panel-hint').textContent = text.textLoading;
    el('produced-viewer-status').textContent = text.textLoading;
    try {
      const video = await get(`/uretilenler/video/${encodeURIComponent(id)}`);
      if (token !== state.token) {
        return;
      }
      state.video = video;
      state.version = video.versions[video.versions.length - 1]; // Varsayılan: en son tamamlanan sürüm.
      renderPanel();
      await loadStream();
    } catch (error) {
      if (token === state.token) {
        el('produced-panel-hint').textContent = messageOf(error, text.textLoadFailed);
        el('produced-viewer-status').textContent = '';
      }
    }
  }

  function item(video) {
    const li = node('li', 'produced-items__item');
    const button = node('button', 'produced-items__select');
    button.type = 'button';
    button.dataset.video = video.id;
    button.setAttribute('aria-pressed', 'false');
    button.append(
      node('span', 'produced-items__title', video.title),
      node('span', 'produced-items__meta', fill(text.textItemMeta, formatDuration(video.durationSeconds, units), formatDateTime(video.producedAt), video.versionCount)));
    li.append(button);
    return li;
  }

  async function loadList(more) {
    const profileId = state.profileId;
    if (!profileId) {
      return;
    }
    const status = el('produced-status');
    el('produced-retry').hidden = true;
    if (more) {
      el('produced-more').disabled = true;
      el('produced-more').textContent = text.textMoreLoading;
    } else {
      status.textContent = text.textLoading;
      el('produced-empty').hidden = true;
      el('produced-more').hidden = true;
    }
    try {
      const page = await get(`/uretilenler/${encodeURIComponent(profileId)}/liste?atla=${more ? state.count : 0}`);
      if (profileId !== state.profileId) {
        return;
      }
      const items = page.items || [];
      if (!more) {
        el('produced-list').replaceChildren();
        state.count = 0;
      }
      el('produced-list').append(...items.map(item));
      state.count += items.length;
      status.textContent = '';
      el('produced-empty').hidden = state.count > 0;
      el('produced-more').hidden = !page.hasMore;
    } catch (error) {
      if (profileId === state.profileId) {
        status.textContent = messageOf(error, text.textLoadFailed);
        el('produced-retry').hidden = false;
      }
    } finally {
      el('produced-more').disabled = false;
      el('produced-more').textContent = el('produced-more').dataset.label || el('produced-more').textContent;
    }
  }

  el('produced-more').dataset.label = el('produced-more').textContent;
  el('produced-more').addEventListener('click', () => loadList(true));
  el('produced-retry').addEventListener('click', () => loadList(false));
  el('produced-list').addEventListener('click', (event) => {
    const button = event.target.closest('[data-video]');
    if (button) {
      selectVideo(button.dataset.video, button);
    }
  });

  function selectProfile(profileId) {
    state.profileId = profileId;
    state.count = 0;
    el('produced-list').replaceChildren();
    clearSelection();
    loadList(false);
  }

  doc.addEventListener('profile:selected', (event) => {
    if (root.isConnected) {
      selectProfile(event.detail.id);
    }
  });
  const bar = el('profile-bar');
  if (bar && bar.dataset.selectedId) {
    selectProfile(bar.dataset.selectedId);
  }
  return { state, loadList, loadStream };
}
