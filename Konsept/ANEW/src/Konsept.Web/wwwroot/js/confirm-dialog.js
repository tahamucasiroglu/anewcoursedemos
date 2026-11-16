// Kısa onay diyaloğu (spec 0016: konsept silme, kalıcı silme, önerileri sıfırla). Açılınca odak "Vazgeç"e geçer, Tab diyalog içinde
// döner, Esc ya da Vazgeç vazgeçer, onay düğmesi onaylar; kapanınca odak açan kontrole döner (AC-12). Diyalog düzeni: arka plan
// (.dialog-backdrop) içinde role="alertdialog", [data-confirm-cancel] ve [data-confirm-ok] düğmeleri.

/**
 * Onay diyaloğunu açar.
 * @param {HTMLElement} backdrop Diyaloğun arka planı.
 * @param {HTMLElement|null} opener Açan kontrol (kapanınca odak ona döner).
 * @returns {Promise<boolean>} Onaylandıysa true.
 */
export function confirmDialog(backdrop, opener) {
  const cancel = backdrop.querySelector('[data-confirm-cancel]');
  const ok = backdrop.querySelector('[data-confirm-ok]');
  return new Promise((resolve) => {
    function close(result) {
      backdrop.hidden = true;
      backdrop.removeEventListener('keydown', onKey);
      cancel.removeEventListener('click', onCancel);
      ok.removeEventListener('click', onOk);
      if (opener && opener.isConnected) {
        opener.focus();
      }
      resolve(result);
    }
    function onCancel() {
      close(false);
    }
    function onOk() {
      close(true);
    }
    function onKey(event) {
      if (event.key === 'Escape') {
        event.preventDefault();
        close(false);
      } else if (event.key === 'Tab') {
        event.preventDefault();
        (backdrop.ownerDocument.activeElement === cancel ? ok : cancel).focus();
      }
    }
    backdrop.addEventListener('keydown', onKey);
    cancel.addEventListener('click', onCancel);
    ok.addEventListener('click', onOk);
    backdrop.hidden = false;
    cancel.focus();
  });
}
