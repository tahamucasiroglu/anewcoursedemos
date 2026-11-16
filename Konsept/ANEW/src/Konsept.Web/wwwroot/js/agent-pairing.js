// "Ajanı eşleştir" sayfası (spec 0012 Req 3–8, AC-4, AC-10, AC-14): kod elle girilir, Enter formu gönderir; gönderilirken
// "gönderiliyor" görünür ve düğmeler pasiftir — çift tıklama ikinci isteği göndermez. Hata (kod yok/süresi dolmuş/kullanılmış,
// çok fazla deneme) role="alert" ile duyurulur ve odak kod alanına döner. Metinler resx'ten (data-text-*).
import { post } from './api.js';

function format(template, ...values) {
  return values.reduce((text, value, i) => text.replace(`{${i}}`, value), template || '');
}

/**
 * Sayfayı bağlar (sayfa yoksa bir şey yapmaz).
 * @param {Document} doc Belge.
 */
export function initPairing(doc) {
  const root = doc.getElementById('pairing');
  if (!root) {
    return;
  }
  const t = root.dataset;
  const form = doc.getElementById('pairing-form');
  const input = doc.getElementById('pairing-code');
  const buttons = ['pairing-lookup', 'pairing-approve', 'pairing-reject'].map((id) => doc.getElementById(id));
  const device = doc.getElementById('pairing-device');
  const deviceName = doc.getElementById('pairing-device-name');
  const status = doc.getElementById('pairing-status');
  const error = doc.getElementById('pairing-error');
  let busy = false;
  let code = null;

  const setBusy = (on) => {
    busy = on;
    buttons.forEach((b) => {
      b.disabled = on;
    });
    root.dataset.state = on ? 'sending' : root.dataset.state === 'sending' ? '' : root.dataset.state;
    if (on) {
      status.textContent = t.textSending;
    }
  };

  const showError = (message) => {
    status.textContent = '';
    error.textContent = message || t.textFailed;
    error.hidden = false;
    input.setAttribute('aria-invalid', 'true');
    input.focus();
  };

  const clearError = () => {
    error.hidden = true;
    error.textContent = '';
    input.removeAttribute('aria-invalid');
  };

  const run = async (url, onDone) => {
    if (busy) {
      return;
    }
    setBusy(true);
    clearError();
    let reply;
    try {
      reply = await post(url, { code });
    } catch (failure) {
      setBusy(false);
      device.hidden = true;
      showError(failure && failure.body ? failure.body.message : null);
      return;
    }
    // Düğmeler yeniden etkinleşince sonuç gösterilir (odak pasif düğmeye taşınamaz).
    setBusy(false);
    status.textContent = '';
    onDone(reply);
  };

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    if (busy) {
      return;
    }
    const value = input.value.trim();
    if (!value) {
      showError(t.textCodeRequired);
      return;
    }
    code = value;
    device.hidden = true;
    run(t.lookupUrl, (reply) => {
      code = reply.code;
      deviceName.textContent = format(t.textDeviceFormat, reply.machineName, reply.agentVersion);
      device.hidden = false;
      root.dataset.state = 'device';
      doc.getElementById('pairing-approve').focus();
    });
  });

  doc.getElementById('pairing-approve').addEventListener('click', () =>
    run(t.approveUrl, () => {
      device.hidden = true;
      input.value = '';
      root.dataset.state = 'approved';
      status.textContent = t.textApproved;
    }));

  doc.getElementById('pairing-reject').addEventListener('click', () =>
    run(t.rejectUrl, () => {
      device.hidden = true;
      input.value = '';
      root.dataset.state = 'rejected';
      status.textContent = t.textRejected;
    }));
}
