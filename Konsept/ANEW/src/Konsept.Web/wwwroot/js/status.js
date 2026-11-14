// Durum alanı ve Günlük şeridi (spec 0001 Req 4–5, AC-4, AC-6, AC-8): 3 sn'de bir Ajax ile yoklanır.
// Metinler resx'ten gelir (sayfadaki data-text-* öznitelikleri); burada sabit arayüz metni yoktur.
import { get } from './api.js';

export const POLL_INTERVAL_MS = 3000;
const LEVELS = ['info', 'warning', 'error'];

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

/**
 * Günlük şeridinde son olayı gösterir; seviye rengi data-level ile.
 * @param {Document} doc Belge.
 * @param {{message:string, level:number, occurredAt:string}|null} event Son olay.
 * @param {string} emptyText Kayıt yokken metin.
 */
export function renderJournal(doc, event, emptyText) {
  const journal = doc.getElementById('journal');
  const text = doc.getElementById('journal-text');
  if (!event) {
    text.textContent = emptyText;
    journal.dataset.level = 'info';
    return;
  }
  const time = new Date(event.occurredAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
  text.textContent = `${time} — ${event.message}`;
  journal.dataset.level = LEVELS[event.level] || 'info';
}

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

  if (!status || status.apiAvailable === false) {
    version.textContent = '';
    set(api, t.apiDown, 'error');
    set(db, '', 'unknown');
    set(agent, '', 'unknown');
    return;
  }

  version.textContent = `v${status.version}`;
  set(api, t.apiOk, 'ok');
  set(db, status.databaseAvailable ? t.dbOk : t.dbDown, status.databaseAvailable ? 'ok' : 'error');
  const connected = Boolean(status.agent && status.agent.isConnected);
  set(agent, connected ? t.agentConnected : t.agentDisconnected, connected ? 'ok' : 'off');
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
 * Yoklamayı başlatır (hemen bir kez, sonra her 3 sn'de).
 * @param {Document} doc Belge.
 * @param {{setInterval: Function}} [timers] Zamanlayıcı (testte sahte).
 * @returns {any} Zamanlayıcı kimliği.
 */
export function startPolling(doc, timers = globalThis) {
  refresh(doc);
  return timers.setInterval(() => refresh(doc), POLL_INTERVAL_MS);
}
