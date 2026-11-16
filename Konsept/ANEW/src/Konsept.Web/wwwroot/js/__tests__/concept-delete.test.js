import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initConceptWork } from '../concept-work.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Konseptler.cshtml'deki alanların en küçük karşılığı + silme onayı (spec 0016; metinler resx'teki değerler).
const TEXTS = {
  loading: 'Yükleniyor…',
  'load-failed': 'Konseptler alınamadı.',
  select: 'Seç: {0}',
  approve: 'Onayla: {0}',
  unapprove: 'Onayı geri al: {0}',
  page: 'Sayfa {0}/{1}',
  edit: 'Düzenle: {0}',
  'edit-locked-queue': 'Önce üretim sırasından çıkarın',
  'edit-locked-video': 'Bu konseptin videosu var',
  delete: 'Sil: {0}',
  'delete-locked-video': 'Videosu olan konsept burada silinemez',
  deleting: 'Siliniyor…',
  deleted: "Konsept Silinenler'e taşındı.",
  'delete-failed': 'Konsept silinemedi; yerinde kaldı.',
  'status-generated': 'Üretildi',
  'status-approved': 'Onaylandı',
  'status-queued': 'Sırada',
  'status-producing': 'Üretiliyor',
  'status-video_ready': 'Video oldu',
  'scene-heading': 'Sahne {0}: {1}',
  'scene-visual': 'Ekran',
  'scene-narration': 'Seslendirme',
  'scene-duration': 'Süre: {0} sn',
};

function pageHtml() {
  const attrs = Object.entries(TEXTS).map(([k, v]) => `data-text-${k}="${v}"`).join(' ');
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <div id="concept-work" ${attrs}>
      <form id="topic-form"><input id="topic-count" value="10" /><button type="submit" id="topic-generate">x</button><p id="topic-count-error"></p></form>
      <p id="topic-progress"></p><button type="button" id="topic-stop" hidden>x</button><ul id="topic-notes"></ul><p id="topic-empty"></p>
      <ul id="topic-list"></ul><p id="topic-produce-error"></p><button type="button" id="topic-produce">x</button>
      <form id="idea-form"><textarea id="idea-text"></textarea><p id="idea-text-error"></p><input id="idea-count" value="5" />
        <button type="submit" id="idea-generate">x</button><p id="idea-count-error"></p></form>
      <p id="idea-progress"></p><button type="button" id="idea-stop" hidden>x</button><ul id="idea-notes"></ul>
      <fieldset id="concept-filter"><input type="checkbox" value="generated" checked /><input type="checkbox" value="approved" checked /></fieldset>
      <p id="concepts-empty">Henüz konsept yok.</p><p id="concepts-status" role="status"></p><ul id="concept-list"></ul>
      <button type="button" id="concepts-approve-selected" disabled>x</button>
      <span id="concepts-pager" hidden><button type="button" id="concepts-prev">x</button><span id="concepts-page"></span><button type="button" id="concepts-next">x</button></span>
      <p id="concept-detail-hint">Seçin.</p><article id="concept-detail" tabindex="-1" hidden></article>
    </div>
    <div class="dialog-backdrop" id="concept-delete-dialog" hidden>
      <div role="alertdialog" aria-modal="true"><p>Konsept Silinenler'e taşınacak; geri yükleme yok.</p>
        <button type="button" data-confirm-cancel>Vazgeç</button><button type="button" data-confirm-ok>Sil</button></div>
    </div>`;
}

const READY = { topics: true, fromTopics: true, idea: true, missingRoles: [] };
const work = { profileId: 'p1', readiness: READY, suggestions: [], topicJob: null, ideaJob: null };
const listOf = (items) => ({ items, page: 1, pageSize: 50, total: items.length, statuses: ['generated', 'approved'] });
const concept = (id, title, status = 'generated') => ({ id, title, status, wordRepetitionWarning: false, durationMismatchWarning: false, createdAt: '2026-11-16T10:00:00Z' });
const LIST_URL = '/konseptler/liste/p1?durumlar=generated%2Capproved&sayfa=1';

let routes;
let calls;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 6; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = () => calls.filter((c) => c.method === 'POST');
const key = (target, name) => target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));

async function start(list) {
  routes['GET /konsept-uretim/p1'] = ok(work);
  routes[`GET ${LIST_URL}`] = ok(list);
  initConceptWork(document, { setTimer: () => 0, clearTimer: () => {} });
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
}

const trashOf = (id) => el('concept-list').querySelector(`[data-delete="${id}"]`);

describe('Üretilenler çöp ikonu ve silme onayı (spec 0016)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-2/AC-12: Üretildi/Onaylandı çöpü etkin ve adı başlığı içerir; Sırada/Üretiliyor/Video oldu pasif + açıklama, istek gitmez', async () => {
    await start(listOf([concept('a', 'Bir'), concept('b', 'İki', 'approved'), concept('q', 'Üç', 'queued'), concept('p', 'Dört', 'producing'), concept('v', 'Beş', 'video_ready')]));
    expect(trashOf('a').getAttribute('aria-label')).toBe('Sil: Bir');
    expect(trashOf('a').hasAttribute('aria-disabled')).toBe(false);
    expect(trashOf('b').hasAttribute('aria-disabled')).toBe(false);
    for (const [id, reason] of [['q', 'Önce üretim sırasından çıkarın'], ['p', 'Önce üretim sırasından çıkarın'], ['v', 'Videosu olan konsept burada silinemez']]) {
      const trash = trashOf(id);
      expect(trash.getAttribute('aria-disabled')).toBe('true');
      expect(el(trash.getAttribute('aria-describedby')).textContent).toBe(reason);
      trash.click();
    }
    await flush();
    expect(el('concept-delete-dialog').hidden).toBe(true);
    expect(posts()).toHaveLength(0);
  });

  it('AC-1/AC-12: çöp onay diyaloğunu açar, odak Vazgeç\'e geçer; Esc ve Vazgeç hiçbir şey değiştirmez ve odak çöpe döner', async () => {
    await start(listOf([concept('a', 'Bir')]));
    trashOf('a').focus();
    trashOf('a').click();
    expect(el('concept-delete-dialog').hidden).toBe(false);
    expect(document.activeElement.textContent).toBe('Vazgeç');
    key(document.activeElement, 'Tab');
    expect(document.activeElement.textContent).toBe('Sil');
    key(document.activeElement, 'Escape');
    await flush();
    expect(el('concept-delete-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(trashOf('a'));
    trashOf('a').click();
    el('concept-delete-dialog').querySelector('[data-confirm-cancel]').click();
    await flush();
    expect(posts()).toHaveLength(0);
    expect(el('concept-list').textContent).toContain('Bir');
  });

  it('AC-1: onaylanınca istek gider, liste yenilenir, açık detay kapanır ve "taşındı" görünür', async () => {
    await start(listOf([concept('a', 'Bir'), concept('b', 'İki')]));
    routes['GET /konseptler/detay/a'] = ok({ ...concept('a', 'Bir'), scenes: [] });
    el('concept-list').querySelector('[data-open="a"]').click();
    await flush();
    expect(el('concept-detail').hidden).toBe(false);
    const deleted = vi.fn();
    document.addEventListener('concept:deleted', deleted);
    routes['POST /konseptler/a/sil'] = ok({ id: 'a', title: 'Bir', deletedAt: '2026-11-16T12:00:00Z' });
    routes[`GET ${LIST_URL}`] = ok(listOf([concept('b', 'İki')]));
    trashOf('a').click();
    el('concept-delete-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(posts().map((c) => c.url)).toEqual(['/konseptler/a/sil']);
    expect(el('concept-list').textContent).not.toContain('Bir');
    expect(el('concept-detail').hidden).toBe(true);
    expect(el('concepts-status').textContent).toBe("Konsept Silinenler'e taşındı.");
    expect(deleted).toHaveBeenCalled();
  });

  it('AC-10: sunucu hatasında sade mesaj, konsept listede kalır', async () => {
    await start(listOf([concept('a', 'Bir')]));
    routes['POST /konseptler/a/sil'] = fail(500, {});
    trashOf('a').click();
    el('concept-delete-dialog').querySelector('[data-confirm-ok]').click();
    await flush();
    expect(el('concepts-status').textContent).toBe('Konsept silinemedi; yerinde kaldı.');
    expect(el('concept-list').textContent).toContain('Bir');
  });
});
