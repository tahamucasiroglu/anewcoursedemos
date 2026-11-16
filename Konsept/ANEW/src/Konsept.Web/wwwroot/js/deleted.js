// Silinenler sayfası (spec 0016): Konseptler / Videolar sekmeleri (ok tuşları, Home/End; sekme rolü), seçili profilin silinmiş
// konseptleri (50'şerli, en yeni silinen üstte), tıklayınca salt okunur sahneler ve onaylı "Kalıcı sil". Geri yükleme yoktur (Ç-2).
// Tüm istekler api.js ($.ajax, K-6); model çıktısı güvenilmeyen veridir, yalnızca textContent ile yazılır. Metinler resx'ten data-text-*.
import { get, post } from './api.js';
import { confirmDialog } from './confirm-dialog.js';

function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

/**
 * Silinme zamanını yerel tarih-saat olarak yazar.
 * @param {string} iso ISO zaman.
 * @returns {string} Metin.
 */
export function formatDeletedAt(iso) {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? String(iso || '') : date.toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' });
}

/**
 * Sayfayı başlatır (sayfada #deleted yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @returns {object|undefined} Denetleyici.
 */
export function initDeleted(doc) {
  const root = doc.getElementById('deleted');
  if (!root) {
    return undefined;
  }
  const text = root.dataset;
  const el = (id) => doc.getElementById(id);
  const state = { profileId: null, page: 1 };
  const tabs = [...root.querySelectorAll('[role="tab"]')];

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

  // --- Sekmeler (WAI-ARIA tabs: seçili sekme Tab ile odaklanır, ok tuşları arasında gezer) ---------------------------------------
  function selectTab(tab, focus) {
    for (const other of tabs) {
      const selected = other === tab;
      other.setAttribute('aria-selected', selected ? 'true' : 'false');
      other.tabIndex = selected ? 0 : -1;
      el(other.getAttribute('aria-controls')).hidden = !selected;
    }
    if (focus) {
      tab.focus();
    }
  }

  for (const tab of tabs) {
    tab.addEventListener('click', () => selectTab(tab, true));
    tab.addEventListener('keydown', (event) => {
      const index = tabs.indexOf(tab);
      const next = { ArrowRight: index + 1, ArrowLeft: index - 1, Home: 0, End: tabs.length - 1 }[event.key];
      if (next !== undefined) {
        event.preventDefault();
        selectTab(tabs[(next + tabs.length) % tabs.length], true);
      }
    });
  }

  // --- Liste --------------------------------------------------------------------------------------------------------------------
  function hideDetail() {
    el('deleted-detail').hidden = true;
    delete el('deleted-detail').dataset.id;
    el('deleted-detail-hint').hidden = false;
  }

  function render(result) {
    const items = result.items || [];
    el('deleted-status').textContent = '';
    el('deleted-empty').hidden = items.length > 0;
    el('deleted-list').replaceChildren(...items.map((item) => {
      const row = node('li', 'concept-row');
      row.dataset.id = item.id;
      const open = node('button', 'concept-row__title', item.title);
      open.type = 'button';
      open.dataset.open = item.id;
      const purge = node('button', 'button button--danger concept-row__purge', text.textPurgeLabel);
      purge.type = 'button';
      purge.dataset.purge = item.id;
      purge.setAttribute('aria-label', fill(text.textPurge, item.title));
      row.append(open, node('span', 'concept-row__meta', fill(text.textDeletedAt, formatDeletedAt(item.deletedAt))), purge);
      return row;
    }));
    const pages = Math.max(1, Math.ceil((result.total || 0) / (result.pageSize || 50)));
    el('deleted-pager').hidden = pages <= 1;
    el('deleted-page').textContent = fill(text.textPage, result.page || 1, pages);
    el('deleted-prev').disabled = state.page <= 1;
    el('deleted-next').disabled = state.page >= pages;
  }

  async function load() {
    const profileId = state.profileId;
    if (!profileId) {
      return;
    }
    el('deleted-status').textContent = text.textLoading;
    el('deleted-empty').hidden = true;
    try {
      const result = await get(`/silinenler/konseptler/${encodeURIComponent(profileId)}?sayfa=${state.page}`);
      if (profileId === state.profileId) {
        render(result);
      }
    } catch {
      if (profileId === state.profileId) {
        el('deleted-list').replaceChildren();
        el('deleted-status').textContent = text.textLoadFailed;
      }
    }
  }

  // --- Salt okunur detay (G-15): düz metin; düzenleme, onay ve yeniden üretim kontrolü yok -------------------------------------------
  async function openDetail(id) {
    try {
      const concept = await get(`/silinenler/konseptler/detay/${encodeURIComponent(id)}`);
      const scenes = node('ol', 'concept-detail__scenes');
      concept.scenes.forEach((scene, index) => {
        const item = node('li', 'concept-detail__scene');
        item.append(
          node('h4', 'concept-detail__scene-title', fill(text.textSceneHeading, index + 1, scene.title)),
          node('p', '', `${text.textSceneVisual}: ${scene.visual}`),
          node('p', '', `${text.textSceneNarration}: ${scene.narration}`),
          node('p', 'concept-detail__duration', fill(text.textSceneDuration, scene.durationSeconds)));
        scenes.append(item);
      });
      const article = el('deleted-detail');
      article.replaceChildren(node('h3', 'concept-detail__title', concept.title), scenes);
      article.dataset.id = concept.id;
      article.hidden = false;
      el('deleted-detail-hint').hidden = true;
      article.focus();
    } catch {
      hideDetail();
      el('deleted-detail-hint').textContent = text.textDetailFailed;
    }
  }

  // Onaydan sonra kalıcı siler; vazgeçilirse istek gitmez. Hata → sade mesaj, kayıt yerinde (AC-10).
  async function purge(button) {
    const id = button.dataset.purge;
    if (!(await confirmDialog(el('deleted-purge-dialog'), button))) {
      return;
    }
    el('deleted-status').textContent = text.textPurging;
    try {
      await post(`/silinenler/konseptler/${encodeURIComponent(id)}/kalici-sil`, {});
      if (el('deleted-detail').dataset.id === id) {
        hideDetail();
      }
      await load();
      el('deleted-status').textContent = text.textPurged;
    } catch (error) {
      el('deleted-status').textContent = (error && error.body && error.body.message) || text.textPurgeFailed;
    }
  }

  el('deleted-list').addEventListener('click', (event) => {
    const purgeButton = event.target.closest('[data-purge]');
    if (purgeButton) {
      purge(purgeButton);
      return;
    }
    const open = event.target.closest('[data-open]');
    if (open) {
      openDetail(open.dataset.open);
    }
  });
  el('deleted-prev').addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    load();
  });
  el('deleted-next').addEventListener('click', () => {
    state.page += 1;
    load();
  });

  function select(profileId) {
    state.profileId = profileId;
    state.page = 1;
    hideDetail();
    load();
  }

  doc.addEventListener('profile:selected', (event) => select(event.detail.id));
  const bar = el('profile-bar');
  if (bar && bar.dataset.selectedId) {
    select(bar.dataset.selectedId);
  }
  return { state, load, selectTab };
}
