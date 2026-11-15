import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { loadAdminOverview } from '../admin.js';
import { setNavigation } from '../api.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

const $ = installJquery();

// Views/Admin/Index.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function adminHtml() {
  return `
    <meta name="text-action-forbidden" content="Bu işlem için yetkiniz yok" />
    <section class="admin" id="admin" data-overview-url="/admin/ozet"
             data-text-forbidden="Bu işlem için yetkiniz yok" data-text-failed="Admin özeti şu an yüklenemedi">
      <p class="empty-state admin__empty" id="admin-empty">Admin alanı henüz boş.</p>
      <ul class="admin__sections" id="admin-sections" hidden></ul>
      <p class="admin__message" id="admin-message" role="alert" hidden></p>
    </section>`;
}

const empty = () => document.getElementById('admin-empty');
const message = () => document.getElementById('admin-message');
const sections = () => document.getElementById('admin-sections');

describe('admin iskeleti (spec 0011 AC-3)', () => {
  beforeEach(() => {
    document.body.innerHTML = adminHtml();
    setNavigation({ pathname: '/admin', search: '', assign: vi.fn() });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('özet /admin/ozet\'ten GET ile yüklenir; bölüm yoksa boş durum metni görünür kalır', async () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, { sections: [] }));

    await loadAdminOverview(document);

    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({ url: '/admin/ozet', method: 'GET' }));
    expect(empty().hidden).toBe(false);
    expect(sections().hidden).toBe(true);
    expect(message().hidden).toBe(true);
  });

  it('bölüm gelirse düz metin olarak listelenir ve boş durum gizlenir', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxResolving($, { sections: ['<b>İstatistikler</b>'] }));

    await loadAdminOverview(document);

    expect(sections().hidden).toBe(false);
    expect(sections().querySelector('li').textContent).toBe('<b>İstatistikler</b>');
    expect(sections().querySelector('b')).toBeNull();
    expect(empty().hidden).toBe(true);
  });

  it('yetki reddedilirse (403) "Bu işlem için yetkiniz yok" duyurulur', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 403, { code: 'auth.forbidden' }));

    await loadAdminOverview(document);

    expect(message().hidden).toBe(false);
    expect(message().getAttribute('role')).toBe('alert');
    expect(message().textContent).toBe('Bu işlem için yetkiniz yok');
  });

  it('başka hatada genel metin görünür; admin bölümü olmayan sayfada istek yapılmaz', async () => {
    vi.spyOn($, 'ajax').mockReturnValue(ajaxRejecting($, 502, { code: 'api.unavailable' }));
    await loadAdminOverview(document);
    expect(message().textContent).toBe('Admin özeti şu an yüklenemedi');

    document.body.innerHTML = '<main></main>';
    vi.restoreAllMocks();
    const ajax = vi.spyOn($, 'ajax');
    await loadAdminOverview(document);
    expect(ajax).not.toHaveBeenCalled();
  });
});
