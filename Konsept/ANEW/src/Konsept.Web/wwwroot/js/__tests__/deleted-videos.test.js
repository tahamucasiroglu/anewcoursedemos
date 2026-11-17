import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initDeletedVideos } from '../deleted-videos.js';
import { bindPassiveControls } from '../passive-controls.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Silinenler.cshtml'in Videolar sekmesinin en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="deleted" data-text-loading="Silinenler yükleniyor…" data-text-load-failed="Silinenler alınamadı. Biraz sonra tekrar deneyin."
         data-text-purge-label="Kalıcı sil" data-text-purging="Kalıcı siliniyor…" data-text-page="Sayfa {0}/{1}">
      <section role="tabpanel" id="deleted-panel-videos"
               data-text-meta="Silinme: {0} · {1} sürüm" data-text-purge="Videoyu kalıcı sil: {0}" data-text-purged="Video kalıcı olarak silindi."
               data-text-purge-failed="Video kalıcı silinemedi; yerinde kaldı." data-text-agent-offline="Ajan bağlı değil"
               data-text-partial="Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin">
        <p class="empty-state" id="deleted-videos-empty">Henüz silinmiş video yok</p>
        <p id="deleted-videos-status" role="status"></p>
        <button type="button" id="deleted-videos-retry" hidden>Yeniden dene</button>
        <ul id="deleted-videos-list"></ul>
        <span id="deleted-videos-pager" hidden><button type="button" id="deleted-videos-prev">Önceki</button><span id="deleted-videos-page"></span><button type="button" id="deleted-videos-next">Sonraki</button></span>
      </section>
    </div>
    <div id="deleted-video-purge-dialog" hidden>
      <div role="alertdialog"><p>Video kaydı ve tüm sürüm dosyaları bilgisayarınızdan kalıcı olarak silinecek, geri alınamaz.</p>
        <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Kalıcı sil</button></div>
    </div>`;
}

const item = (id, title, deletedAt = '2026-11-16T10:00:00Z') => ({ id, title, deletedAt, versionCount: 2 });
const listOf = (items, extra = {}) => ({ items, page: 1, pageSize: 50, total: items.length, ...extra });

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const pending = () => () => $.Deferred().promise();
const flush = async () => {
  for (let i = 0; i < 6; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = () => calls.filter((c) => c.method === 'POST');
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));
const titles = () => [...el('deleted-videos-list').querySelectorAll('.concept-row__title')].map((n) => n.textContent);
const purgeOf = (id) => el('deleted-videos-list').querySelector(`[data-purge-video="${id}"]`);
const agent = (connected) => document.dispatchEvent(new CustomEvent('agent:status', { detail: { connected, machineName: 'Ofis-PC' } }));

async function start(profile = 'p1') {
  initDeletedVideos(document);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: profile } }));
  await flush();
}

describe('Silinenler — Videolar (spec 0019)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/silinenler', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-3: başlık, silinme tarihi ve sürüm sayısı sunucunun sırasıyla (en yeni silinen üstte); başlık düz metin; boşken 0016 metni', async () => {
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([item('b', '<b>Yeni silinen</b>', '2026-11-16T12:00:00Z'), item('a', 'Eski silinen')]));
    await start();
    expect(titles()).toEqual(['<b>Yeni silinen</b>', 'Eski silinen']);
    expect(el('deleted-videos-list').querySelector('b')).toBeNull();
    expect(el('deleted-videos-list').textContent).toContain('Silinme: ');
    expect(el('deleted-videos-list').textContent).toContain('· 2 sürüm');
    expect(el('deleted-videos-empty').hidden).toBe(true);
    expect(document.body.textContent).not.toContain('Geri yükle');

    routes['GET /silinenler/videolar/p2?sayfa=1'] = ok(listOf([]));
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p2' } }));
    await flush();
    expect(titles()).toEqual([]);
    expect(el('deleted-videos-empty').hidden).toBe(false);
    expect(el('deleted-videos-empty').textContent).toBe('Henüz silinmiş video yok');
  });

  it('AC-10: yüklenirken gösterge; alınamazsa sade Türkçe hata ve "Yeniden dene" yeniden ister', async () => {
    routes['GET /silinenler/videolar/p1?sayfa=1'] = pending();
    await start();
    expect(el('deleted-videos-status').textContent).toBe('Silinenler yükleniyor…');

    routes['GET /silinenler/videolar/p2?sayfa=1'] = fail(500, { message: 'Npgsql stack trace' });
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p2' } }));
    await flush();
    expect(el('deleted-videos-status').textContent).toBe('Silinenler alınamadı. Biraz sonra tekrar deneyin.');
    expect(el('deleted-videos-retry').hidden).toBe(false);

    routes['GET /silinenler/videolar/p2?sayfa=1'] = ok(listOf([item('a', 'Bir')]));
    el('deleted-videos-retry').click();
    await flush();
    expect(titles()).toEqual(['Bir']);
    expect(el('deleted-videos-retry').hidden).toBe(true);
  });

  it('AC-4/AC-10: kalıcı sil onay ister (erişilebilir ad, odak diyalogda, Tab içeride, Esc vazgeçer → istek yok); onaylanınca kayıt gider', async () => {
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')]));
    await start();
    const purge = purgeOf('a');
    expect(purge.tagName).toBe('BUTTON');
    expect(purge.getAttribute('aria-label')).toBe('Videoyu kalıcı sil: Bir');
    purge.focus();
    purge.click();
    expect(el('deleted-video-purge-dialog').hidden).toBe(false);
    expect(document.activeElement.textContent).toBe('Vazgeç');
    key(document.activeElement, 'Tab');
    expect(document.activeElement.textContent).toBe('Kalıcı sil');
    key(document.activeElement, 'Escape');
    await flush();
    expect(el('deleted-video-purge-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(purge);
    expect(posts()).toHaveLength(0);

    routes['POST /silinenler/videolar/a/kalici-sil'] = ok({ id: 'a' });
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([]));
    purge.click();
    el('deleted-video-purge-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(posts().map((c) => c.url)).toEqual(['/silinenler/videolar/a/kalici-sil']);
    expect(titles()).toEqual([]);
    expect(el('deleted-videos-status').textContent).toBe('Video kalıcı olarak silindi.');
  });

  it('AC-5/AC-10: ajan çevrimdışıyken Kalıcı sil pasif, nedeni ipucu (fare/odak) ve açıklama; tıklama/Enter istek göndermez; ajan gelince etkin', async () => {
    bindPassiveControls(document);
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')]));
    await start();
    agent(false);
    const purge = purgeOf('a');
    expect(purge.getAttribute('aria-disabled')).toBe('true');
    expect(purge.classList.contains('passive')).toBe(true);
    expect(purge.dataset.tooltip).toBe('Ajan bağlı değil');
    expect(purge.getAttribute('aria-description')).toBe('Ajan bağlı değil');
    expect(purge.disabled).toBe(false); // Odaklanabilir kalır: ipucu klavye odağında da okunur.
    purge.click();
    key(purge, 'Enter');
    await flush();
    expect(el('deleted-video-purge-dialog').hidden).toBe(true);
    expect(posts()).toHaveLength(0);
    expect(titles()).toEqual(['Bir']);

    agent(true);
    expect(purge.hasAttribute('aria-disabled')).toBe(false);
    expect(purge.dataset.tooltip).toBeUndefined();
  });

  it('AC-5/AC-6: sunucu "ajan bağlı değil" ya da "bazı dosyalar silinemedi" derse kayıt listede kalır, sade mesaj; tekrar denenince gider', async () => {
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')]));
    routes['POST /silinenler/videolar/a/kalici-sil'] = fail(409, { code: 'production.purge_partial', message: 'Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin' });
    await start();
    purgeOf('a').click();
    el('deleted-video-purge-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(el('deleted-videos-status').textContent).toBe('Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin');
    expect(titles()).toEqual(['Bir']);
    expect(purgeOf('a').disabled).toBe(false);

    routes['POST /silinenler/videolar/a/kalici-sil'] = fail(409, { code: 'production.purge_agent_offline', message: 'Ajan bağlı değil' });
    purgeOf('a').click();
    el('deleted-video-purge-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(el('deleted-videos-status').textContent).toBe('Ajan bağlı değil');
    expect(purgeOf('a').getAttribute('aria-disabled')).toBe('true');
    expect(titles()).toEqual(['Bir']);

    agent(true);
    routes['POST /silinenler/videolar/a/kalici-sil'] = ok({ id: 'a' });
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([]));
    purgeOf('a').click();
    el('deleted-video-purge-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(titles()).toEqual([]);
  });

  it('AC-3: 50\'den fazla silinen video sayfalanır', async () => {
    routes['GET /silinenler/videolar/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')], { total: 51 }));
    await start();
    expect(el('deleted-videos-pager').hidden).toBe(false);
    expect(el('deleted-videos-prev').disabled).toBe(true);
    routes['GET /silinenler/videolar/p1?sayfa=2'] = ok(listOf([item('z', 'Elli bir')], { page: 2, total: 51 }));
    el('deleted-videos-next').click();
    await flush();
    expect(titles()).toEqual(['Elli bir']);
    expect(el('deleted-videos-page').textContent).toBe('Sayfa 2/2');
  });
});
