import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initDeleted } from '../deleted.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Silinenler.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function pageHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="deleted" data-text-loading="Silinenler yükleniyor…" data-text-load-failed="Silinenler alınamadı. Biraz sonra tekrar deneyin."
         data-text-deleted-at="Silinme: {0}" data-text-purge="Kalıcı sil: {0}" data-text-purge-label="Kalıcı sil"
         data-text-purging="Kalıcı siliniyor…" data-text-purged="Konsept kalıcı olarak silindi."
         data-text-purge-failed="Konsept kalıcı silinemedi; yerinde kaldı." data-text-page="Sayfa {0}/{1}"
         data-text-scene-heading="Sahne {0}: {1}" data-text-scene-visual="Ekran" data-text-scene-narration="Seslendirme"
         data-text-scene-duration="Süre: {0} sn" data-text-detail-failed="Konsept alınamadı.">
      <div role="tablist">
        <button type="button" role="tab" id="tab-c" aria-selected="true" aria-controls="panel-c">Konseptler</button>
        <button type="button" role="tab" id="tab-v" aria-selected="false" aria-controls="panel-v" tabindex="-1">Videolar</button>
      </div>
      <section role="tabpanel" id="panel-c">
        <p class="empty-state" id="deleted-empty">Henüz silinmiş konsept yok</p>
        <p id="deleted-status" role="status"></p><ul id="deleted-list"></ul>
        <span id="deleted-pager" hidden><button type="button" id="deleted-prev">Önceki</button><span id="deleted-page"></span><button type="button" id="deleted-next">Sonraki</button></span>
        <p id="deleted-detail-hint">Seçin.</p><article id="deleted-detail" tabindex="-1" hidden></article>
      </section>
      <section role="tabpanel" id="panel-v" hidden><p class="empty-state">Henüz silinmiş video yok</p></section>
    </div>
    <div id="deleted-purge-dialog" hidden>
      <div role="alertdialog"><p>Konsept kalıcı olarak silinecek, geri alınamaz.</p>
        <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Kalıcı sil</button></div>
    </div>`;
}

const item = (id, title) => ({ id, title, deletedAt: '2026-11-16T10:00:00Z' });
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
const titles = () => [...el('deleted-list').querySelectorAll('[data-open]')].map((n) => n.textContent);

async function start(profile = 'p1') {
  initDeleted(document);
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: profile } }));
  await flush();
}

describe('Silinenler sayfası (spec 0016)', () => {
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

  it('AC-3/AC-12: sekmeler tıklama ve ok tuşlarıyla değişir; Videolar boş durumu görünür', async () => {
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = ok(listOf([]));
    await start();
    key(el('tab-c'), 'ArrowRight');
    expect(el('tab-v').getAttribute('aria-selected')).toBe('true');
    expect(document.activeElement).toBe(el('tab-v'));
    expect(el('panel-v').hidden).toBe(false);
    expect(el('panel-c').hidden).toBe(true);
    expect(el('tab-c').tabIndex).toBe(-1);
    key(el('tab-v'), 'ArrowRight');
    expect(el('tab-c').getAttribute('aria-selected')).toBe('true');
    el('tab-v').click();
    expect(el('panel-v').textContent).toContain('Henüz silinmiş video yok');
  });

  it('AC-3: yükleniyor, boş ve alınamadı durumları', async () => {
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = pending();
    await start();
    expect(el('deleted-status').textContent).toBe('Silinenler yükleniyor…');
    routes['GET /silinenler/konseptler/p2?sayfa=1'] = ok(listOf([]));
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p2' } }));
    await flush();
    expect(el('deleted-empty').hidden).toBe(false);
    expect(el('deleted-status').textContent).toBe('');
    routes['GET /silinenler/konseptler/p3?sayfa=1'] = fail(500, {});
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p3' } }));
    await flush();
    expect(el('deleted-status').textContent).toBe('Silinenler alınamadı. Biraz sonra tekrar deneyin.');
  });

  it('AC-4: liste başlık + silinme zamanı; sayfalama; profil değişince yeni profilin listesi', async () => {
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')], { total: 51 }));
    await start();
    expect(titles()).toEqual(['Bir']);
    expect(el('deleted-list').textContent).toContain('Silinme: ');
    expect(el('deleted-pager').hidden).toBe(false);
    routes['GET /silinenler/konseptler/p1?sayfa=2'] = ok(listOf([item('z', 'Elli bir')], { page: 2, total: 51 }));
    el('deleted-next').click();
    await flush();
    expect(titles()).toEqual(['Elli bir']);
    expect(el('deleted-page').textContent).toBe('Sayfa 2/2');
    routes['GET /silinenler/konseptler/p2?sayfa=1'] = ok(listOf([item('x', 'Öteki profil')]));
    document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p2' } }));
    await flush();
    expect(titles()).toEqual(['Öteki profil']);
  });

  it('AC-5: tıklanınca sahneler salt okunur düz metin; düzenleme/onay/geri yükleme kontrolü yok', async () => {
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = ok(listOf([item('a', '<b>Bir</b>')]));
    routes['GET /silinenler/konseptler/detay/a'] = ok({ id: 'a', title: '<b>Bir</b>', scenes: [{ title: 'Açılış', visual: '<script>x()</script>', narration: 'Metin', durationSeconds: 20 }] });
    await start();
    el('deleted-list').querySelector('[data-open]').click();
    await flush();
    const detail = el('deleted-detail');
    expect(detail.hidden).toBe(false);
    expect(document.activeElement).toBe(detail);
    expect(detail.querySelector('script, b, textarea, input')).toBeNull();
    expect(detail.querySelectorAll('button')).toHaveLength(0);
    expect(detail.textContent).toContain('Ekran: <script>x()</script>');
    expect(document.body.textContent).not.toContain('Geri yükle');
  });

  it('AC-6/AC-12: kalıcı sil onay ister (erişilebilir ad, odak, Esc); vazgeçince istek yok; onaylanınca kayıt gider', async () => {
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')]));
    await start();
    const purge = el('deleted-list').querySelector('[data-purge="a"]');
    expect(purge.getAttribute('aria-label')).toBe('Kalıcı sil: Bir');
    expect(purge.textContent).toBe('Kalıcı sil');
    purge.focus();
    purge.click();
    expect(el('deleted-purge-dialog').hidden).toBe(false);
    expect(document.activeElement.textContent).toBe('Vazgeç');
    key(document.activeElement, 'Escape');
    await flush();
    expect(el('deleted-purge-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(purge);
    expect(posts()).toHaveLength(0);

    routes['POST /silinenler/konseptler/a/kalici-sil'] = ok({ id: 'a' });
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = ok(listOf([]));
    purge.click();
    el('deleted-purge-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(posts().map((c) => c.url)).toEqual(['/silinenler/konseptler/a/kalici-sil']);
    expect(titles()).toEqual([]);
    expect(el('deleted-status').textContent).toBe('Konsept kalıcı olarak silindi.');
  });

  it('AC-10: kalıcı silme sunucu hatasıyla biterse sade mesaj, kayıt listede kalır', async () => {
    routes['GET /silinenler/konseptler/p1?sayfa=1'] = ok(listOf([item('a', 'Bir')]));
    routes['POST /silinenler/konseptler/a/kalici-sil'] = fail(500, {});
    await start();
    el('deleted-list').querySelector('[data-purge="a"]').click();
    el('deleted-purge-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(el('deleted-status').textContent).toBe('Konsept kalıcı silinemedi; yerinde kaldı.');
    expect(titles()).toEqual(['Bir']);
  });
});
