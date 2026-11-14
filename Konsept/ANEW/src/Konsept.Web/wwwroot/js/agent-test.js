// "Ajanı test et" (spec 0001 Req 8, AC-7): API → ajan komut yolunun kanıtı. Hata mesajları
// ("Ajan bağlı değil", "Zaman aşımı") API'den gelir.
import { post } from './api.js';

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
    result.textContent = (failure && failure.body && failure.body.message) || button.dataset.textFailed;
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
