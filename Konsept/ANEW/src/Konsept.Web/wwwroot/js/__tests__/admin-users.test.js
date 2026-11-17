import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { initAdminPanel } from '../admin-panel.js';
import { failureText } from '../admin-users.js';
import { setNavigation } from '../api.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

const $ = installJquery();

// Views/Admin/Index.cshtml'in Kullanıcılar bölümünün en küçük karşılığı (metinler resx'teki değerler).
function panelHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="admin-panel" data-statistics-url="/admin/istatistikler" data-activities-url="/admin/aktiviteler"
         data-text-loading="Yükleniyor…" data-text-statistics-failed="İstatistikler şu an yüklenemedi."
         data-text-correlation="Hata no: {0}" data-text-page="Sayfa {0}/{1}">
      <div role="tablist">
        <button type="button" role="tab" id="admin-tab-statistics" aria-selected="true" aria-controls="admin-statistics">İstatistikler</button>
        <button type="button" role="tab" id="admin-tab-users" aria-selected="false" aria-controls="admin-users" tabindex="-1">Kullanıcılar</button>
        <button type="button" role="tab" id="admin-tab-activities" aria-selected="false" aria-controls="admin-activities" tabindex="-1">Aktiviteler</button>
      </div>
      <section role="tabpanel" id="admin-statistics">
        <p id="admin-statistics-status"></p>
        <div id="admin-statistics-error" hidden><p id="admin-statistics-error-text"></p><button id="admin-statistics-retry">Tekrar dene</button></div>
        <dd id="admin-total-users"></dd><dd id="admin-total-clips"></dd><dd id="admin-open-reports"></dd><dd id="admin-banned-users">–</dd>
        <select id="admin-range"><option value="30" selected>30</option></select>
        <div id="admin-charts"></div>
      </section>
      <section role="tabpanel" id="admin-users" hidden data-users-url="/admin/kullanicilar"
               data-text-failed="Kullanıcılar şu an yüklenemedi." data-text-no-match="Eşleşen kullanıcı yok" data-text-empty="Henüz kullanıcı yok"
               data-text-active="Aktif" data-text-banned="Banlı" data-text-role-admin="Admin" data-text-role-user="Kullanıcı"
               data-text-ban="Banla" data-text-unban="Banı kaldır" data-text-cannot-self="Kendinizi banlayamazsınız"
               data-text-cannot-admin="Admin hesapları banlanamaz" data-text-history-ban="Banlandı: {0}, {1}"
               data-text-history-unban="Ban kaldırıldı: {0}, {1}" data-text-history-reason="Sebep: {0}"
               data-text-ban-title="Kullanıcıyı banla: {0}" data-text-unban-title="Banı kaldır: {0}" data-text-ban-failed="Ban uygulanamadı."
               data-text-unban-failed="Ban kaldırılamadı." data-text-processing="İşleniyor…" data-text-reason-count="{0}/500" data-text-never="—">
        <form id="admin-user-filter"><input id="admin-user-search" />
          <select id="admin-user-status"><option value="all" selected>Tümü</option><option value="active">Aktif</option><option value="banned">Banlı</option></select>
          <button type="submit">Filtrele</button></form>
        <p id="admin-users-status" role="status"></p>
        <div id="admin-users-error" role="alert" hidden><p id="admin-users-error-text"></p><button id="admin-users-retry">Tekrar dene</button></div>
        <table id="admin-user-table" hidden><tbody id="admin-user-rows"></tbody></table>
        <nav id="admin-user-pager" hidden><button id="admin-user-prev">Önceki</button><span id="admin-user-page"></span><button id="admin-user-next">Sonraki</button></nav>
      </section>
      <div id="admin-ban-dialog" hidden><div role="dialog">
        <h2 id="admin-ban-title"></h2>
        <textarea id="admin-ban-reason"></textarea><span id="admin-ban-reason-count"></span>
        <span id="admin-ban-reason-error" role="alert" hidden>En fazla 500 karakter</span>
        <p id="admin-ban-message" role="alert" hidden></p>
        <button id="admin-ban-cancel">Vazgeç</button><button id="admin-ban-ok">Banla</button>
      </div></div>
      <div id="admin-unban-dialog" hidden><div role="dialog">
        <h2 id="admin-unban-title"></h2>
        <p id="admin-unban-message" role="alert" hidden></p>
        <button id="admin-unban-cancel">Vazgeç</button><button id="admin-unban-ok">Banı kaldır</button>
      </div></div>
      <section role="tabpanel" id="admin-activities" hidden>
        <form id="admin-activity-filter"><input id="admin-filter-user" /><input type="date" id="admin-filter-from" /><input type="date" id="admin-filter-to" /></form>
        <p id="admin-filter-warning" hidden></p><p id="admin-activities-status"></p>
        <div id="admin-activities-error" hidden><p id="admin-activities-error-text"></p><button id="admin-activities-retry">Tekrar dene</button></div>
        <table id="admin-activity-table" hidden><tbody id="admin-activity-rows"></tbody></table>
        <nav id="admin-pager" hidden><button id="admin-prev">Önceki</button><span id="admin-page"></span><button id="admin-next">Sonraki</button></nav>
      </section>
    </div>`;
}

const el = (id) => document.getElementById(id);
const flush = () => new Promise((resolve) => { setTimeout(resolve, 0); });
const stats = { totalUsers: 3, totalClips: 0, openErrorReports: 0, bannedUsers: 1, days: 30, series: [] };

function user(overrides = {}) {
  return {
    id: 'u-1', name: 'Ayşe <b>Y</b>', email: 'ayse@ornek.test', registeredAt: '2026-11-14T09:00:00Z', lastLoginAt: null,
    status: 'active', role: 'user', clips: 2, isSelf: false, history: [], ...overrides,
  };
}

function page(items, extra = {}) {
  return { items, page: 1, pageSize: 25, total: items.length, filtered: false, ...extra };
}

const key = (target, k) => target.dispatchEvent(new KeyboardEvent('keydown', { key: k, bubbles: true }));

describe('admin Kullanıcılar (spec 0024)', () => {
  let ajax;
  let panel;

  async function openUsers(body) {
    ajax.mockReturnValue(ajaxResolving($, stats));
    panel = initAdminPanel(document);
    await flush();
    ajax.mockReturnValue(ajaxResolving($, body));
    await panel.selectTab('admin-tab-users', false);
  }

  const rowCells = (i) => [...el('admin-user-rows').rows[i].cells].map((c) => c.textContent);
  const button = (id) => el('admin-user-rows').querySelector(`button[data-user-id="${id}"]`);

  beforeEach(() => {
    document.body.innerHTML = panelHtml();
    setNavigation({ pathname: '/admin', search: '', assign: vi.fn() });
    ajax = vi.spyOn($, 'ajax');
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('sekme sırası ve liste: sütunlar düz metin, durum metinle, Istanbul saati, banlı kartı (AC-2, AC-12, AC-14)', async () => {
    await openUsers(page([
      user({ lastLoginAt: '2026-11-14T21:30:00Z' }),
      user({ id: 'u-2', name: 'Veli', email: 'veli@ornek.test', status: 'banned', clips: 0,
        history: [{ action: 'ban', adminName: 'Admin', at: '2026-11-14T10:00:00Z', reason: 'spam' }] }),
    ]));

    expect([...document.querySelectorAll('[role="tab"]')].map((t) => t.textContent)).toEqual(['İstatistikler', 'Kullanıcılar', 'Aktiviteler']);
    expect(el('admin-banned-users').textContent).toBe('1');
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/kullanicilar?sayfa=1&ara=&durum=all', method: 'GET' }));
    expect(el('admin-users').hidden).toBe(false);
    expect(rowCells(0).slice(0, 7)).toEqual(['Ayşe <b>Y</b>', 'ayse@ornek.test', '14.11.2026 12:00', '15.11.2026 00:30', 'Aktif', 'Kullanıcı', '2']);
    expect(el('admin-user-rows').querySelector('b')).toBeNull();
    expect(rowCells(1)[3]).toBe('—');
    expect(rowCells(1)[4]).toBe('Banlı');
    expect(button('u-1').textContent).toBe('Banla');
    expect(button('u-2').textContent).toBe('Banı kaldır');
    expect(rowCells(1)[7]).toContain('Banlandı: Admin, 14.11.2026 13:00 Sebep: spam');
    expect(el('admin-users-status').textContent).toBe('');
  });

  it('admin satırlarında Banla pasif + ipucu: kendi satırı ve başka admin (AC-11)', async () => {
    await openUsers(page([
      user({ id: 'me', role: 'admin', isSelf: true }),
      user({ id: 'other', role: 'admin' }),
    ]));

    expect(button('me').disabled).toBe(true);
    expect(button('me').title).toBe('Kendinizi banlayamazsınız');
    expect(el(button('me').getAttribute('aria-describedby')).textContent).toBe('Kendinizi banlayamazsınız');
    expect(button('other').disabled).toBe(true);
    expect(rowCells(1)[7]).toContain('Admin hesapları banlanamaz');
    expect(rowCells(0)[5]).toBe('Admin');
  });

  it('arama 2 karakterden kısaysa uygulanmaz, durum filtresi ve sayfalama istek parametreleri, eşleşme yoksa mesaj (AC-2)', async () => {
    await openUsers(page([user()], { total: 30 }));
    expect(el('admin-user-pager').hidden).toBe(false);
    expect(el('admin-user-page').textContent).toBe('Sayfa 1/2');

    ajax.mockReturnValue(ajaxResolving($, page([user()], { total: 30 })));
    el('admin-user-next').click();
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/kullanicilar?sayfa=2&ara=&durum=all' }));

    el('admin-user-search').value = 'a';
    el('admin-user-status').value = 'banned';
    ajax.mockReturnValue(ajaxResolving($, page([], { filtered: true })));
    el('admin-user-filter').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/kullanicilar?sayfa=1&ara=&durum=banned' }));
    expect(el('admin-users-status').textContent).toBe('Eşleşen kullanıcı yok');
    expect(el('admin-user-table').hidden).toBe(true);

    el('admin-user-search').value = ' AYŞE ';
    ajax.mockReturnValue(ajaxResolving($, page([user()], { filtered: true })));
    el('admin-user-filter').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: `/admin/kullanicilar?sayfa=1&ara=${encodeURIComponent('AYŞE')}&durum=banned` }));
  });

  it('yükleniyor, sunucu hatasında sade mesaj + korelasyon + Tekrar dene; ham ayrıntı yok (AC-3)', async () => {
    ajax.mockReturnValue(ajaxResolving($, stats));
    panel = initAdminPanel(document);
    await flush();
    ajax.mockReturnValue(ajaxRejecting($, 500, { code: 'api.error', message: 'Npgsql SELECT at Stack', correlationId: '0HN-7' }));
    const loading = panel.selectTab('admin-tab-users', false);
    expect(el('admin-users-status').textContent).toBe('Yükleniyor…');
    await loading;

    expect(el('admin-users-error').hidden).toBe(false);
    expect(el('admin-users-error-text').textContent).toBe('Kullanıcılar şu an yüklenemedi. Hata no: 0HN-7');
    expect(el('admin-users').textContent).not.toContain('SELECT');

    ajax.mockReturnValue(ajaxResolving($, page([user()])));
    el('admin-users-retry').click();
    await flush();
    expect(el('admin-users-error').hidden).toBe(true);
    expect(el('admin-user-rows').rows).toHaveLength(1);
  });

  it('Banla: pencere açılır, odak sebebe; Esc vazgeçer ve odak düğmeye döner; istek yok (AC-4, AC-14)', async () => {
    await openUsers(page([user()]));
    const calls = ajax.mock.calls.length;
    button('u-1').click();

    expect(el('admin-ban-dialog').hidden).toBe(false);
    expect(el('admin-ban-title').textContent).toBe('Kullanıcıyı banla: Ayşe <b>Y</b>');
    expect(document.activeElement).toBe(el('admin-ban-reason'));
    key(el('admin-ban-reason'), 'Tab');
    expect(document.activeElement).toBe(el('admin-ban-cancel'));
    el('admin-ban-ok').focus();
    key(el('admin-ban-ok'), 'Tab');
    expect(document.activeElement).toBe(el('admin-ban-reason'));

    key(el('admin-ban-reason'), 'Escape');
    expect(el('admin-ban-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(button('u-1'));
    el('admin-ban-cancel').click();
    expect(ajax.mock.calls.length).toBe(calls);
  });

  it('sebep 500 karakteri aşınca onay pasif ve uyarı; işleniyor sırasında onay pasif, çift tıklama tek istek; başarıda satır Banlı (AC-4)', async () => {
    await openUsers(page([user()]));
    button('u-1').click();
    const reason = el('admin-ban-reason');
    reason.value = 'x'.repeat(501);
    reason.dispatchEvent(new Event('input'));
    expect(el('admin-ban-ok').disabled).toBe(true);
    expect(el('admin-ban-reason-error').hidden).toBe(false);
    expect(el('admin-ban-reason-count').textContent).toBe('501/500');
    el('admin-ban-ok').click();
    expect(ajax.mock.calls.filter(([s]) => s.method === 'POST')).toHaveLength(0);

    reason.value = 'spam';
    reason.dispatchEvent(new Event('input'));
    expect(el('admin-ban-ok').disabled).toBe(false);
    const pending = $.Deferred();
    ajax.mockReturnValue(pending.promise());
    el('admin-ban-ok').click();
    el('admin-ban-ok').click();
    expect(el('admin-ban-ok').disabled).toBe(true);
    expect(el('admin-ban-ok').textContent).toBe('İşleniyor…');
    key(reason, 'Escape');
    expect(el('admin-ban-dialog').hidden).toBe(false);

    const posts = ajax.mock.calls.filter(([s]) => s.method === 'POST');
    expect(posts).toHaveLength(1);
    expect(posts[0][0]).toEqual(expect.objectContaining({ url: '/admin/kullanicilar/u-1/banla', data: JSON.stringify({ reason: 'spam' }) }));
    expect(posts[0][0].headers.RequestVerificationToken).toBe('test-token');

    ajax.mockReturnValue(ajaxResolving($, page([user({ status: 'banned',
      history: [{ action: 'ban', adminName: 'Admin', at: '2026-11-14T09:00:00Z', reason: 'spam' }] })])));
    pending.resolve(user({ status: 'banned' }));
    await flush();
    await flush();

    expect(el('admin-ban-dialog').hidden).toBe(true);
    expect(rowCells(0)[4]).toBe('Banlı');
    expect(rowCells(0)[7]).toContain('Banlandı: Admin, 14.11.2026 12:00 Sebep: spam');
    expect(document.activeElement).toBe(button('u-1'));
    expect(el('admin-ban-ok').textContent).toBe('Banla');
  });

  it('ban sunucu hatasında durum değişmez, pencerede hata + korelasyon; 409 mesajı Api\'den (AC-4, AC-11)', async () => {
    await openUsers(page([user()]));
    button('u-1').click();
    ajax.mockReturnValue(ajaxRejecting($, 500, { code: 'api.error', message: 'stack trace', correlationId: 'C-9' }));
    el('admin-ban-ok').click();
    await flush();

    expect(el('admin-ban-dialog').hidden).toBe(false);
    expect(el('admin-ban-message').hidden).toBe(false);
    expect(el('admin-ban-message').textContent).toBe('Ban uygulanamadı. Hata no: C-9');
    expect(el('admin-ban-ok').disabled).toBe(false);
    expect(rowCells(0)[4]).toBe('Aktif');
    expect(failureText({ status: 409, body: { message: 'Admin hesapları banlanamaz', correlationId: 'X' } }, 'f', 'Hata no: {0}'))
      .toBe('Admin hesapları banlanamaz Hata no: X');
  });

  it('Banı kaldır: vazgeçince değişmez; hata durumda Banlı kalır; başarıda Aktif ve kayıt (AC-10)', async () => {
    await openUsers(page([user({ status: 'banned' })]));
    button('u-1').click();
    expect(el('admin-unban-dialog').hidden).toBe(false);
    expect(document.activeElement).toBe(el('admin-unban-cancel'));
    el('admin-unban-cancel').click();
    expect(el('admin-unban-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(button('u-1'));

    button('u-1').click();
    ajax.mockReturnValue(ajaxRejecting($, 503, { code: 'api.unavailable', correlationId: 'C-1' }));
    el('admin-unban-ok').click();
    await flush();
    expect(el('admin-unban-message').textContent).toBe('Ban kaldırılamadı. Hata no: C-1');
    expect(rowCells(0)[4]).toBe('Banlı');

    ajax.mockReturnValue(ajaxResolving($, user()));
    el('admin-unban-ok').click();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/kullanicilar/u-1/ban-kaldir', method: 'POST' }));
    ajax.mockReturnValue(ajaxResolving($, page([user({ history: [
      { action: 'unban', adminName: 'Admin', at: '2026-11-14T11:00:00Z', reason: null },
      { action: 'ban', adminName: 'Admin', at: '2026-11-14T10:00:00Z', reason: null }] })])));
    await flush();
    await flush();

    expect(el('admin-unban-dialog').hidden).toBe(true);
    expect(rowCells(0)[4]).toBe('Aktif');
    expect(rowCells(0)[7]).toContain('Ban kaldırıldı: Admin, 14.11.2026 14:00');
  });
});
