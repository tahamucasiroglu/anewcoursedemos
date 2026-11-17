import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { runAgentTest } from '../agent-test.js';
import { ajaxRejecting, ajaxResolving, installJquery, layoutHtml } from './fixture.js';

const $ = installJquery();
const result = () => document.getElementById('agent-test-result');

describe('"Ajanı test et" (AC-7)', () => {
  beforeEach(() => {
    document.body.innerHTML = layoutHtml();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('başarılıysa ajanın makine adı ve versiyonu görünür', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, { machineName: 'TAHA-PC', agentVersion: '0.1.0' }));

    await runAgentTest(document);

    expect(result().textContent).toBe('TAHA-PC · v0.1.0');
    expect(result().dataset.state).toBe('ok');
  });

  it('ajan bağlı değilse resx metni görünür (sunucu metni değil)', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 503, { code: 'agent.not_connected', message: 'server text' }));

    await runAgentTest(document);

    expect(result().textContent).toBe('Ajan bağlı değil');
    expect(result().dataset.state).toBe('error');
  });

  it('zaman aşımında "Zaman aşımı" görünür', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 504, { code: 'agent.timeout', message: 'server text' }));

    await runAgentTest(document);

    expect(result().textContent).toBe('Zaman aşımı');
  });

  it('spec 0023 AC-6: sürümü uyumsuz ajanda "Ajan güncellemesi gerekli" görünür', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 503, { code: 'agent.update_required', message: 'server text' }));
    await runAgentTest(document);

    expect(document.getElementById('agent-test-result').textContent).toBe('Ajan güncellemesi gerekli');
  });

  it('API erişilemezse resx metni, bilinmeyen hatada genel metin görünür', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 502, { code: 'api.unavailable', message: 'server text' }));
    await runAgentTest(document);
    expect(result().textContent).toBe('API erişilemiyor');

    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 500, null));
    await runAgentTest(document);
    expect(result().textContent).toBe('Ajana ulaşılamadı');
  });

  it('POST isteği CSRF belirteciyle gider ve gönderilirken düğme pasiftir', async () => {
    let seen;
    const ajax = vi.spyOn($, 'ajax').mockImplementation((settings) => {
      seen = { disabled: document.getElementById('agent-test-button').disabled, text: result().textContent };
      return ajaxResolving($, { machineName: 'X', agentVersion: '0.1.0' });
    });

    await runAgentTest(document);

    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({
      url: '/system/agent-test',
      method: 'POST',
      headers: { RequestVerificationToken: 'test-token' },
    }));
    expect(seen).toEqual({ disabled: true, text: 'Gönderiliyor…' });
    expect(document.getElementById('agent-test-button').disabled).toBe(false);
  });
});
