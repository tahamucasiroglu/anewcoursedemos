// "Ajanı test et" (spec 0001 Req 8, AC-7): API → ajan komut yolunun kanıtı. Ekrandaki her metin resx'ten
// gelir (düğmenin data-text-* öznitelikleri); API'nin hata kodu resx metnine eşlenir (review I-1).
import { post } from './api.js';

const ERROR_TEXT_KEYS = {
  'agent.not_connected': 'textAgentNotConnected',
  'agent.timeout': 'textAgentTimeout',
  'api.unavailable': 'textApiUnavailable',
};

function errorText(button, failure) {
  const code = failure && failure.body ? failure.body.code : null;
  const key = code ? ERROR_TEXT_KEYS[code] : null;
  return (key && button.dataset[key]) || button.dataset.textFailed;
}

/**
 * Testi çalıştırır: gönderiliyor → başarılı (makine · versiyon) ya da hata mesajı.
 * @param {Document} doc Belge.
 * @returns {Promise<void>}
 */
export async function runAgentTest(doc) {
  const button = doc.getElementById('agent-test-button');
  const result = doc.getElementById('agent-test-result');
  button.disabled = true;
  result.dataset.state = 'pending';
  result.textContent = button.dataset.textSending;
  try {
    const reply = await post('/system/agent-test');
    result.dataset.state = 'ok';
    result.textContent = `${reply.machineName} · v${reply.agentVersion}`;
  } catch (failure) {
    result.dataset.state = 'error';
    result.textContent = errorText(button, failure);
  } finally {
    button.disabled = false;
  }
}

/**
 * Düğmeye tıklama olayını bağlar.
 * @param {Document} doc Belge.
 */
export function bindAgentTest(doc) {
  const button = doc.getElementById('agent-test-button');
  if (button) {
    button.addEventListener('click', () => runAgentTest(doc));
  }
}
