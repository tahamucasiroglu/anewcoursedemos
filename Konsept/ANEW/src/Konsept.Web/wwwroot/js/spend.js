// Üst bardaki aylık harcama göstergesi (spec 0013 Req 15–18): yalnızca bilgi; tıklanmaz, odak almaz. Açılışta ve 5 sn'de bir
// /harcama'dan yenilenir (yeni kayıt en geç 10 sn'de görünür). Biçim ($2,37) ve erişilebilir ad sunucudan gelir; veri alınamazsa
// "—" ve "Harcama alınamadı" ipucu görünür, sayfanın geri kalanı etkilenmez. Metinler resx'ten (data-* öznitelikleri).
import { get } from './api.js';

export const SPEND_INTERVAL_MS = 5000;

/**
 * Göstergeyi bir kez yeniler.
 * @param {HTMLElement} node #monthly-spend.
 * @returns {Promise<void>}
 */
export async function refreshSpend(node) {
  try {
    const data = await get(node.dataset.url);
    node.textContent = data.text;
    node.setAttribute('aria-label', data.label);
    node.removeAttribute('title');
    node.dataset.state = 'ok';
  } catch {
    node.textContent = node.dataset.textFailed;
    node.setAttribute('aria-label', node.dataset.labelFailed);
    node.title = node.dataset.textFailedHint;
    node.dataset.state = 'error';
  }
}

/**
 * Göstergeyi başlatır (sayfada yoksa — giriş yapılmamışsa — hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @param {{setInterval: Function}} [timers] Zamanlayıcı (testte sahte).
 * @returns {any} Zamanlayıcı kimliği ya da null.
 */
export function initSpend(doc, timers = globalThis) {
  const node = doc.getElementById('monthly-spend');
  if (!node) {
    return null;
  }
  refreshSpend(node);
  return timers.setInterval(() => refreshSpend(node), SPEND_INTERVAL_MS);
}
