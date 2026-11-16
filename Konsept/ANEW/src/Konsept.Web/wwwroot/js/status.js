// Durum alanı ve Günlük şeridi (spec 0001 Req 4–5, AC-4, AC-6, AC-8): 3 sn'de bir Ajax ile yoklanır.
// Metinler resx'ten gelir (sayfadaki data-text-* öznitelikleri); burada sabit arayüz metni yoktur.
import { get } from './api.js';
import { markJournalStale, renderJournal } from './journal.js';

export const POLL_INTERVAL_MS = 3000;
export const BACKGROUND_POLL_INTERVAL_MS = 15000;

function texts(box) {
  const d = box.dataset;
  return {
    apiOk: d.textApiOk,
    apiDown: d.textApiDown,
    dbOk: d.textDbOk,
    dbDown: d.textDbDown,
    agentConnected: d.textAgentConnected,
    agentDisconnected: d.textAgentDisconnected,
    journalEmpty: d.textJournalEmpty,
  };
}

function set(el, text, state) {
  el.textContent = text;
  el.dataset.state = state;
}

// Günlük şeridinin gösterimi journal.js'tedir (spec 0021); burada yalnızca durum cevabından beslenir.
export { renderJournal };

/**
 * Sistem durumunu ekrana yazar.
 * @param {Document} doc Belge.
 * @param {{apiAvailable:boolean, version?:string, databaseAvailable?:boolean, agent?:{isConnected:boolean}, lastEvent?:any}} status Durum.
 */
export function render(doc, status) {
  const t = texts(doc.getElementById('system-status'));
  const version = doc.getElementById('status-version');
  const api = doc.getElementById('status-api');
  const db = doc.getElementById('status-db');
  const agent = doc.getElementById('status-agent');
  // "Ajanı eşleştir" bağlantısı (spec 0012 Req 3): yalnızca ajan bağlı değilken görünür.
  const pair = doc.getElementById('status-agent-pair');
  // "Ajanı indir" (spec 0009 Req 1): durum alanında yalnızca ajan bağlı değilken; Ayarlar'da her zaman.
  const download = doc.getElementById('status-agent-download');

  if (!status || status.apiAvailable === false) {
    markJournalStale(doc, true); // Son bilinen kayıt kalır (spec 0021 AC-4).
    version.textContent = '';
    set(api, t.apiDown, 'error');
    set(db, '', 'unknown');
    set(agent, '', 'unknown');
    if (pair) {
      pair.hidden = true;
    }
    if (download) {
      download.hidden = true;
    }
    return;
  }

  version.textContent = `v${status.version}`;
  set(api, t.apiOk, 'ok');
  set(db, status.databaseAvailable ? t.dbOk : t.dbDown, status.databaseAvailable ? 'ok' : 'error');
  const connected = Boolean(status.agent && status.agent.isConnected);
  set(agent, connected ? t.agentConnected : t.agentDisconnected, connected ? 'ok' : 'off');
  if (pair) {
    pair.hidden = connected;
  }
  if (download) {
    download.hidden = connected;
  }
  if (!status.databaseAvailable) {
    markJournalStale(doc, true); // Veritabanı yokken kayıt okunamaz: son bilinen kayıt kalır.
    return;
  }
  markJournalStale(doc, false);
  renderJournal(doc, status.lastEvent || null, t.journalEmpty);
}

/**
 * Durumu bir kez yoklar; Web'e de ulaşılamazsa "API erişilemiyor" gösterir.
 * @param {Document} doc Belge.
 * @returns {Promise<void>}
 */
export async function refresh(doc) {
  try {
    render(doc, await get('/system/status'));
  } catch {
    render(doc, { apiAvailable: false });
  }
}

/**
 * Yoklamayı başlatır (spec 0021 Req 2): hemen bir kez; sekme önplandayken 3 sn'de bir, arka plandayken son istekten 15 sn geçince;
 * sekme yeniden görünür olunca beklemeden.
 * @param {Document} doc Belge.
 * @param {{setInterval: Function}} [timers] Zamanlayıcı (testte sahte).
 * @returns {any} Zamanlayıcı kimliği.
 */
export function startPolling(doc, timers = globalThis) {
  let last = 0;
  const tick = (force) => {
    if (!force && doc.hidden && Date.now() - last < BACKGROUND_POLL_INTERVAL_MS) {
      return;
    }
    last = Date.now();
    refresh(doc);
  };
  tick(true);
  doc.addEventListener('visibilitychange', () => {
    if (!doc.hidden) {
      tick(true);
    }
  });
  return timers.setInterval(() => tick(false), POLL_INTERVAL_MS);
}
