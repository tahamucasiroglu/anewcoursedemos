import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { get, loginRedirectUrl, post, setNavigation } from '../api.js';
import { ajaxRejecting, ajaxResolving, installJquery, layoutHtml } from './fixture.js';

const $ = installJquery();

function fakeLocation(pathname, search = '') {
  return { pathname, search, assign: vi.fn() };
}

describe('api.js — oturum düşünce girişe yönlendirme (AC-13, AC-14)', () => {
  beforeEach(() => {
    document.body.innerHTML = layoutHtml();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('401 alan Ajax isteği "oturum bitti" bilgisiyle ve dönüş adresiyle giriş sayfasına götürür, sessizce kalmaz', async () => {
    const location = fakeLocation('/uretilenler', '?sayfa=2');
    setNavigation(location);
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 401, { code: 'auth.session_expired', message: 'Oturumunuz sona erdi' }));

    await expect(get('/system/status')).rejects.toMatchObject({ status: 401 });

    expect(location.assign).toHaveBeenCalledTimes(1);
    expect(location.assign).toHaveBeenCalledWith('/giris?oturum=bitti&returnUrl=%2Furetilenler%3Fsayfa%3D2');
  });

  it('POST isteğinde de 401 girişe yönlendirir; CSRF belirteci gönderilir', async () => {
    const location = fakeLocation('/');
    setNavigation(location);
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 401, null));

    await expect(post('/system/agent-test')).rejects.toMatchObject({ status: 401 });

    expect(ajax.mock.calls[0][0].headers.RequestVerificationToken).toBe('test-token');
    expect(location.assign).toHaveBeenCalledWith('/giris?oturum=bitti&returnUrl=%2F');
  });

  it('başka hatalar ve başarılı cevaplar yönlendirme yapmaz', async () => {
    const location = fakeLocation('/');
    setNavigation(location);
    vi.spyOn($, 'ajax').mockReturnValueOnce(ajaxRejecting($, 503, { code: 'agent.not_connected' }));
    await expect(post('/system/agent-test')).rejects.toMatchObject({ status: 503 });
    vi.spyOn($, 'ajax').mockReturnValueOnce(ajaxResolving($, { ok: true }));
    await expect(get('/system/status')).resolves.toEqual({ ok: true });

    expect(location.assign).not.toHaveBeenCalled();
  });

  it('giriş adresi bulunulan sayfayı kodlanmış dönüş adresi yapar', () => {
    expect(loginRedirectUrl({ pathname: '/silinenler', search: '' })).toBe('/giris?oturum=bitti&returnUrl=%2Fsilinenler');
  });
});
