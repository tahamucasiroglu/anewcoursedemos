// Pasif kontroller (spec 0001 AC-3): aria-disabled="true" olan öğeler tıklanınca ya da Enter/Space ile
// hiçbir eylem yapmaz. "Yakında" ipucu CSS ile (fareyle üzerine gelince ve klavye odağında) görünür.

const PASSIVE = '[aria-disabled="true"]';

function isPassive(target) {
  return Boolean(target && typeof target.closest === 'function' && target.closest(PASSIVE));
}

/**
 * Pasif kontrollerin eylemlerini engeller (yakalama aşamasında, başka dinleyicilerden önce).
 * @param {Document} doc Belge.
 */
export function bindPassiveControls(doc) {
  doc.addEventListener('click', (event) => {
    if (isPassive(event.target)) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }, true);
  doc.addEventListener('keydown', (event) => {
    if ((event.key === 'Enter' || event.key === ' ') && isPassive(event.target)) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }, true);
}
