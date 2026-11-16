// Günlük şeridi ve geçmiş modalı (spec 0021). Şerit verisi durum yoklamasından gelir (status.js → renderJournal); geçmiş /gunluk'tan.
// Metinler resx'ten gelir (data-text-* öznitelikleri); kayıt metinleri her zaman textContent ile yazılır (Html.Raw/innerHTML yok, Req 12).
// Saatler Europe/Istanbul, 24 saat HH:mm; süre başlangıç–bitiş farkından hesaplanır (Req 4).
import { get } from './api.js';

export const LEVELS = ['info', 'warning', 'error', 'critical'];
const ZONE = 'Europe/Istanbul';
const ICONS = { warning: '⚠', error: '✖', critical: '‼' };
const FOCUSABLE = 'button:not([disabled]):not([hidden]), select:not([hidden])';

const timeFormat = new Intl.DateTimeFormat('tr-TR', { timeZone: ZONE, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' });

/**
 * Bir anı Europe/Istanbul'da HH:mm biçiminde verir.
 * @param {string} iso UTC zaman.
 * @returns {string} Saat.
 */
export function formatTime(iso) {
  return timeFormat.format(new Date(iso));
}

/**
 * Süreyi biçimler: 60 sn altı "57 sn"; 1 saate kadar "30 dk 12 sn"; 1 saat ve üstü "1 sa 5 dk".
 * @param {number} seconds Saniye.
 * @param {{second:string, minute:string, hour:string}} units Birimler (resx).
 * @returns {string} Süre.
 */
export function formatDuration(seconds, units) {
  const s = Math.max(0, Math.round(seconds));
  if (s < 60) {
    return `${s} ${units.second}`;
  }
  if (s < 3600) {
    return `${Math.floor(s / 60)} ${units.minute} ${s % 60} ${units.second}`;
  }
  return `${Math.floor(s / 3600)} ${units.hour} ${Math.floor((s % 3600) / 60)} ${units.minute}`;
}

/**
 * Kaydın gösterilecek metni: süreli işlemde "metin · süre · başlangıç–bitiş".
 * @param {{message:string, startedAt?:string|null, endedAt?:string|null}} event Kayıt.
 * @param {{second:string, minute:string, hour:string}} units Birimler.
 * @returns {string} Metin.
 */
export function entryText(event, units) {
  if (!event.startedAt || !event.endedAt) {
    return event.message;
  }
  const seconds = (new Date(event.endedAt).getTime() - new Date(event.startedAt).getTime()) / 1000;
  return `${event.message} · ${formatDuration(seconds, units)} · ${formatTime(event.startedAt)}–${formatTime(event.endedAt)}`;
}

function texts(el) {
  const d = (el && el.dataset) || {};
  return {
    loading: d.textLoading,
    empty: d.textEmpty,
    warning: d.textWarning || 'Uyarı',
    error: d.textError || 'Hata',
    critical: d.textCritical || 'Kritik',
    units: { second: d.textUnitSecond || 'sn', minute: d.textUnitMinute || 'dk', hour: d.textUnitHour || 'sa' },
  };
}

function levelOf(event) {
  return LEVELS[event.level] || 'info';
}

function keyOf(event) {
  return event.id || `${event.occurredAt}|${event.message}`;
}

function fillLevel(badge, level, t) {
  if (!badge) {
    return;
  }
  if (level === 'info') {
    badge.hidden = true;
    badge.textContent = '';
    return;
  }
  badge.hidden = false;
  badge.textContent = '';
  const icon = badge.ownerDocument.createElement('span');
  icon.className = 'journal__icon';
  icon.setAttribute('aria-hidden', 'true');
  icon.textContent = ICONS[level];
  badge.append(icon, ` ${t[level]}`);
}

function fillProfile(tag, name) {
  if (!tag) {
    return;
  }
  tag.hidden = !name;
  tag.textContent = name || '';
}

/**
 * Günlük şeridinde son kaydı gösterir (Req 1, 3, 4, 9): saat + metin, seviye (data-level + ikon + etiket), profil etiketi.
 * Yeni gelen Uyarı/Hata/Kritik kayıt polite alandan duyurulur; ilk yükleme ve Bilgi kayıtları duyurulmaz (Req Accessibility).
 * @param {Document} doc Belge.
 * @param {any} event Son kayıt ya da null.
 * @param {string} [emptyText] Kayıt yokken metin (verilmezse şeridin data-text-empty değeri).
 */
export function renderJournal(doc, event, emptyText) {
  const journal = doc.getElementById('journal');
  const text = doc.getElementById('journal-text');
  const t = texts(journal);
  const first = journal.dataset.state === 'loading' || journal.dataset.seen === undefined;
  journal.dataset.state = 'ready';
  if (!event) {
    text.textContent = emptyText || t.empty || '';
    journal.dataset.level = 'info';
    journal.dataset.seen = '';
    fillLevel(doc.getElementById('journal-level'), 'info', t);
    fillProfile(doc.getElementById('journal-profile'), null);
    return;
  }
  const level = levelOf(event);
  const shown = entryText(event, t.units);
  text.textContent = `${formatTime(event.occurredAt)} — ${shown}`;
  journal.dataset.level = level;
  fillLevel(doc.getElementById('journal-level'), level, t);
  fillProfile(doc.getElementById('journal-profile'), event.profileName);

  const key = keyOf(event);
  const announce = doc.getElementById('journal-announce');
  if (!first && key !== journal.dataset.seen && level !== 'info' && announce) {
    announce.textContent = `${t[level]}: ${shown}`;
  }
  journal.dataset.seen = key;
}

/**
 * Şerit güncellenemediğinde "Günlük güncellenemiyor" işaretini gösterir; son bilinen kayıt kalır. Bağlantı dönünce kalkar.
 * @param {Document} doc Belge.
 * @param {boolean} stale Güncellenemiyor mu.
 */
export function markJournalStale(doc, stale) {
  const marker = doc.getElementById('journal-stale');
  const journal = doc.getElementById('journal');
  if (marker) {
    marker.hidden = !stale;
  }
  if (journal) {
    journal.dataset.stale = stale ? 'true' : 'false';
  }
}

function addDays(iso, days) {
  const [y, m, d] = iso.split('-').map(Number);
  const date = new Date(Date.UTC(y, m - 1, d + days));
  return date.toISOString().slice(0, 10);
}

/**
 * Gün başlığı: "16 Kasım 2026 Pazartesi".
 * @param {string} iso Gün (yyyy-MM-dd).
 * @returns {string} Başlık.
 */
export function formatDay(iso) {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d, 12)).toLocaleDateString('tr-TR', {
    timeZone: 'UTC', day: 'numeric', month: 'long', year: 'numeric', weekday: 'long',
  });
}

function entryItem(doc, event, t) {
  const level = levelOf(event);
  const li = doc.createElement('li');
  li.className = 'journal-entry';
  li.dataset.level = level;
  li.dataset.key = keyOf(event);
  const time = doc.createElement('span');
  time.className = 'journal-entry__time';
  time.textContent = formatTime(event.occurredAt);
  const badge = doc.createElement('span');
  badge.className = 'journal__level';
  fillLevel(badge, level, t);
  const text = doc.createElement('span');
  text.className = 'journal-entry__text';
  text.textContent = entryText(event, t.units);
  const tag = doc.createElement('span');
  tag.className = 'journal__profile';
  fillProfile(tag, event.profileName);
  li.append(time, badge, text, tag);
  return li;
}

/**
 * Şeridi ve geçmiş modalını bağlar (Req 5–6): tık/Enter açar, odak modala alınır; Kapat, Esc ve dışarı tıklama kapatır, odak şeride
 * döner; önceki/sonraki gün, filtre, "Daha fazla" (50'şer), hata + tekrar dene.
 * @param {Document} doc Belge.
 * @returns {{open: Function, close: Function}|null} Denetim (testler için); şerit yoksa null.
 */
export function initJournal(doc) {
  const strip = doc.getElementById('journal');
  const modal = doc.getElementById('journal-modal');
  if (!strip || !modal) {
    return null;
  }
  const el = (id) => doc.getElementById(id);
  const t = { ...texts(strip), ...modal.dataset };
  const list = el('journal-list');
  const status = el('journal-status');
  const retry = el('journal-retry');
  const more = el('journal-more');
  const prev = el('journal-prev');
  const next = el('journal-next');
  const filter = el('journal-filter');
  const state = { date: '', items: 0, keys: new Set(), loading: false, request: 0 };

  function setStatus(text, kind) {
    status.textContent = text || '';
    status.dataset.kind = kind || '';
  }

  async function load(append) {
    const request = ++state.request;
    state.loading = true;
    if (append) {
      more.disabled = true;
      more.textContent = t.textLoading;
    } else {
      list.textContent = '';
      state.items = 0;
      state.keys = new Set();
      more.hidden = true;
      retry.hidden = true;
      setStatus(t.textLoading, 'loading');
    }
    const query = `tarih=${encodeURIComponent(state.date)}&filtre=${encodeURIComponent(filter.value)}&atla=${append ? state.items : 0}`;
    try {
      const day = await get(`/gunluk?${query}`);
      if (request !== state.request) {
        return;
      }
      state.date = day.date;
      el('journal-day').textContent = formatDay(day.date);
      prev.disabled = Boolean(day.isFirstDay);
      next.disabled = Boolean(day.isToday);
      for (const item of day.items || []) {
        const key = keyOf(item);
        if (!state.keys.has(key)) {
          state.keys.add(key);
          list.append(entryItem(doc, item, texts(strip)));
        }
      }
      state.items += (day.items || []).length;
      setStatus(state.keys.size === 0 ? t.textDayEmpty : '', state.keys.size === 0 ? 'empty' : '');
      more.hidden = !day.hasMore;
      retry.hidden = true;
    } catch {
      if (request !== state.request) {
        return;
      }
      setStatus(t.textLoadFailed, 'error');
      retry.hidden = false;
      retry.dataset.append = append ? 'true' : 'false';
    } finally {
      if (request === state.request) {
        state.loading = false;
        more.disabled = false;
        more.textContent = t.textMore;
      }
    }
  }

  function focusables() {
    return [...modal.querySelectorAll(FOCUSABLE)].filter((node) => !node.closest('[hidden]'));
  }

  function open() {
    modal.hidden = false;
    state.date = '';
    filter.value = 'all';
    el('journal-modal-close').focus();
    return load(false);
  }

  function close() {
    modal.hidden = true;
    state.request++;
    strip.focus();
  }

  strip.addEventListener('click', () => open());
  el('journal-modal-close').addEventListener('click', close);
  modal.addEventListener('click', (event) => {
    if (event.target === modal) {
      close();
    }
  });
  modal.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      close();
      return;
    }
    if (event.key === 'Tab') {
      const nodes = focusables();
      if (nodes.length === 0) {
        return;
      }
      const firstNode = nodes[0];
      const lastNode = nodes[nodes.length - 1];
      if (event.shiftKey && doc.activeElement === firstNode) {
        event.preventDefault();
        lastNode.focus();
      } else if (!event.shiftKey && doc.activeElement === lastNode) {
        event.preventDefault();
        firstNode.focus();
      }
    }
  });
  prev.addEventListener('click', () => {
    if (!prev.disabled && state.date) {
      state.date = addDays(state.date, -1);
      load(false);
    }
  });
  next.addEventListener('click', () => {
    if (!next.disabled && state.date) {
      state.date = addDays(state.date, 1);
      load(false);
    }
  });
  filter.addEventListener('change', () => load(false));
  more.addEventListener('click', () => {
    if (!state.loading) {
      load(true);
    }
  });
  retry.addEventListener('click', () => load(retry.dataset.append === 'true'));
  return { open, close };
}
