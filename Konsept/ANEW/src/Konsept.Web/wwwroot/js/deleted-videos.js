// Silinenler — Videolar sekmesi (spec 0019): seçili profilin silinen videoları (0016'nın satır biçimi; başlık, silinme zamanı, sürüm sayısı;
// en yeni silinen üstte, 50'şerli) ve onaylı "Kalıcı sil". Kalıcı sil ajan aracılığıyla dosyaları siler: ajan yokken düğme pasiftir ve nedeni
// ("Ajan bağlı değil") fare ve klavye odağında ipucu olarak okunur; ajan durumu status.js'in 3 sn yoklamasından ('agent:status') gelir.
// Bazı dosyalar silinemezse kayıt yerinde kalır ve sade bir mesaj görünür. Geri yükleme yoktur (Ç-2). İstekler api.js ($.ajax, K-6);
// başlık güvenilmeyen veridir, yalnızca textContent. Metinler resx'ten data-text-* (ortak olanlar #deleted'dan).
import { get, post } from './api.js';
import { confirmDialog } from './confirm-dialog.js';
import { formatDeletedAt } from './deleted.js';

function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

/**
 * Videolar sekmesini başlatır (sayfada #deleted-panel-videos yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @returns {object|undefined} Denetleyici (testler için).
 */
export function initDeletedVideos(doc) {
  const panel = doc.getElementById('deleted-panel-videos');
  const shared = doc.getElementById('deleted');
  if (!panel || !shared) {
    return undefined;
  }
  const text = { ...shared.dataset, ...panel.dataset };
  const own = panel.dataset;
  const el = (id) => doc.getElementById(id);
  const state = { profileId: null, page: 1, agentConnected: null };

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

  // Ajan yokken Kalıcı sil pasif (aria-disabled; passive-controls.js eylemi engeller), nedeni ipucu ve erişilebilir açıklama.
  function applyAgent() {
    const offline = state.agentConnected === false;
    for (const button of el('deleted-videos-list').querySelectorAll('[data-purge-video]')) {
      if (offline) {
        button.setAttribute('aria-disabled', 'true');
        button.classList.add('passive');
        button.dataset.tooltip = own.textAgentOffline;
        button.setAttribute('aria-description', own.textAgentOffline);
      } else {
        button.removeAttribute('aria-disabled');
        button.classList.remove('passive');
        delete button.dataset.tooltip;
        button.removeAttribute('aria-description');
      }
    }
  }

  function render(result) {
    const items = result.items || [];
    el('deleted-videos-status').textContent = '';
    el('deleted-videos-empty').hidden = items.length > 0;
    el('deleted-videos-list').replaceChildren(...items.map((item) => {
      const row = node('li', 'concept-row');
      row.dataset.id = item.id;
      const purge = node('button', 'button button--danger concept-row__purge', text.textPurgeLabel);
      purge.type = 'button';
      purge.dataset.purgeVideo = item.id;
      purge.setAttribute('aria-label', fill(own.textPurge, item.title));
      row.append(
        node('span', 'concept-row__title', item.title),
        node('span', 'concept-row__meta', fill(own.textMeta, formatDeletedAt(item.deletedAt), item.versionCount)),
        purge);
      return row;
    }));
    const pages = Math.max(1, Math.ceil((result.total || 0) / (result.pageSize || 50)));
    el('deleted-videos-pager').hidden = pages <= 1;
    el('deleted-videos-page').textContent = fill(text.textPage, result.page || 1, pages);
    el('deleted-videos-prev').disabled = state.page <= 1;
    el('deleted-videos-next').disabled = state.page >= pages;
    applyAgent();
  }

  async function load() {
    const profileId = state.profileId;
    if (!profileId) {
      return;
    }
    el('deleted-videos-status').textContent = text.textLoading;
    el('deleted-videos-empty').hidden = true;
    el('deleted-videos-retry').hidden = true;
    try {
      const result = await get(`/silinenler/videolar/${encodeURIComponent(profileId)}?sayfa=${state.page}`);
      if (profileId === state.profileId) {
        render(result);
      }
    } catch {
      if (profileId === state.profileId) {
        el('deleted-videos-list').replaceChildren();
        el('deleted-videos-status').textContent = text.textLoadFailed;
        el('deleted-videos-retry').hidden = false;
      }
    }
  }

  // Onaydan sonra kalıcı siler; vazgeçilirse istek gitmez. Ajan yok / kısmi silme → kayıt yerinde, sade mesaj.
  async function purge(button) {
    if (button.getAttribute('aria-disabled') === 'true') {
      return;
    }
    const id = button.dataset.purgeVideo;
    if (!(await confirmDialog(el('deleted-video-purge-dialog'), button))) {
      return;
    }
    const status = el('deleted-videos-status');
    status.textContent = text.textPurging;
    button.disabled = true;
    try {
      await post(`/silinenler/videolar/${encodeURIComponent(id)}/kalici-sil`, {});
      await load();
      status.textContent = own.textPurged;
    } catch (error) {
      button.disabled = false;
      const code = error && error.body && error.body.code;
      if (code === 'production.purge_agent_offline') {
        state.agentConnected = false;
        applyAgent();
        status.textContent = own.textAgentOffline;
      } else if (code === 'production.purge_partial') {
        status.textContent = own.textPartial;
      } else {
        status.textContent = (error && error.body && error.body.message) || own.textPurgeFailed;
      }
    }
  }

  el('deleted-videos-list').addEventListener('click', (event) => {
    const button = event.target.closest('[data-purge-video]');
    if (button) {
      purge(button);
    }
  });
  el('deleted-videos-retry').addEventListener('click', () => load());
  el('deleted-videos-prev').addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    load();
  });
  el('deleted-videos-next').addEventListener('click', () => {
    state.page += 1;
    load();
  });

  doc.addEventListener('agent:status', (event) => {
    if (panel.isConnected) {
      state.agentConnected = event.detail.connected;
      applyAgent();
    }
  });

  function select(profileId) {
    state.profileId = profileId;
    state.page = 1;
    load();
  }

  doc.addEventListener('profile:selected', (event) => {
    if (panel.isConnected) {
      select(event.detail.id);
    }
  });
  const bar = el('profile-bar');
  if (bar && bar.dataset.selectedId) {
    select(bar.dataset.selectedId);
  }
  return { state, load };
}
