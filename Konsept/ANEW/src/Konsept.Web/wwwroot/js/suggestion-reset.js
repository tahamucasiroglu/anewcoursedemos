// Ayarlar → "Öneriler" (spec 0016 Req 9): "Önerileri sıfırla" seçili profilin önerilip seçilmemiş başlık hafızasını, ne silineceğini
// anlatan onaydan sonra siler. Seçili profil yokken düğme pasiftir. İstek api.js ($.ajax, K-6) ile /oneriler/sifirla'ya gider.
import { post } from './api.js';
import { confirmDialog } from './confirm-dialog.js';

/**
 * Bölümü başlatır (sayfada #suggestion-reset yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @returns {object|undefined} Denetleyici.
 */
export function initSuggestionReset(doc) {
  const root = doc.getElementById('suggestion-reset');
  if (!root) {
    return undefined;
  }
  const text = root.dataset;
  const button = doc.getElementById('suggestion-reset-button');
  const status = doc.getElementById('suggestion-reset-status');
  const state = { profileId: null, busy: false };

  function render() {
    const usable = Boolean(state.profileId) && !state.busy;
    button.setAttribute('aria-disabled', usable ? 'false' : 'true');
    button.title = state.profileId ? '' : text.textNoProfile;
  }

  async function reset() {
    if (!state.profileId || state.busy) {
      if (!state.profileId) {
        status.textContent = text.textNoProfile;
      }
      return;
    }
    if (!(await confirmDialog(doc.getElementById('suggestion-reset-dialog'), button))) {
      return;
    }
    state.busy = true;
    render();
    status.textContent = text.textResetting;
    try {
      await post('/oneriler/sifirla', { profileId: state.profileId });
      status.textContent = text.textDone;
    } catch (error) {
      status.textContent = (error && error.body && error.body.message) || text.textFailed;
    } finally {
      state.busy = false;
      render();
    }
  }

  button.addEventListener('click', reset);
  doc.addEventListener('profile:selected', (event) => {
    state.profileId = event.detail.id || null;
    render();
  });
  const bar = doc.getElementById('profile-bar');
  if (bar && bar.dataset.selectedId) {
    state.profileId = bar.dataset.selectedId;
  }
  render();
  return { state, reset };
}
