import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { fill, ideaIssue, initConceptWork, jobNotes, parseCount, progressText } from '../concept-work.js';
import { installJquery } from './fixture.js';

const $ = installJquery();
const el = (id) => document.getElementById(id);

// Konseptler.cshtml'deki dört alanın en küçük karşılığı (metinler resx'teki değerler).
const TEXTS = {
  'count-invalid': '1 ile 100 arasında bir tam sayı girin.',
  'idea-required': 'Fikir metnini yazın.',
  'idea-too-long': 'Fikir metni en fazla 2.000 karakter olabilir.',
  'topics-running': 'Öneriler üretiliyor…',
  running: 'Üretiliyor: {0}/{1}',
  done: 'Tamamlandı: {0}/{1} konsept kaydedildi.',
  'topics-done': '{0} öneri listelendi.',
  stopped: 'Durduruldu: {0}/{1} konsept kaydedildi.',
  failures: '{0} konsept üretilemedi:',
  'failure-idea': '{0}. konsept',
  'format-rejected': '{0} öneri biçim dışı olduğu için gösterilmedi.',
  'topic-duplicates': '{0} öneri mevcut konularla aynı olduğu için gösterilmedi.',
  'idea-duplicates': '{0} konsept mevcut konularla aynı olduğu için kaydedilmedi.',
  'dedupe-unavailable': 'Anlamca denetim yapılamadı.',
  'no-new-topics': 'Yeni öneri bulunamadı: tüm adaylar mevcut konularla aynı ya da biçim dışıydı.',
  'request-failed': 'İşlem yapılamadı. Biraz sonra tekrar deneyin.',
  loading: 'Yükleniyor…',
  'load-failed': 'Konseptler alınamadı. Biraz sonra tekrar deneyin.',
  approve: 'Onayla: {0}',
  unapprove: 'Onayı geri al: {0}',
  select: 'Seç: {0}',
  approving: 'Onay kaydediliyor…',
  'approve-failed': 'Onay değiştirilemedi; durum aynı kaldı.',
  'warning-words': 'Kelime tekrarı',
  'warning-duration': 'Süre uyumsuz',
  page: 'Sayfa {0}/{1}',
  'scene-heading': 'Sahne {0}: {1}',
  'scene-visual': 'Ekran',
  'scene-narration': 'Seslendirme',
  'scene-duration': 'Süre: {0} sn',
  'detail-failed': 'Konsept alınamadı.',
  'status-generated': 'Üretildi',
  'status-approved': 'Onaylandı',
  'status-queued': 'Sırada',
  'status-producing': 'Üretiliyor',
  'status-video_ready': 'Video oldu',
};

function pageHtml() {
  const attrs = Object.entries(TEXTS).map(([k, v]) => `data-text-${k}="${v}"`).join(' ');
  const notReady = (key) => `<p data-not-ready="${key}" hidden>Hazır değil <button type="button" data-open-models>Model bilgisi</button></p>`;
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="profile-bar"></div>
    <button type="button" id="concept-models-open">model</button>
    <div id="concept-work" ${attrs}>
      <section id="concept-topic">
        ${notReady('topics')}
        <form id="topic-form" novalidate><input id="topic-count" value="10" /><button type="submit" id="topic-generate" disabled>Konu üret</button>
          <p id="topic-count-error" role="alert"></p></form>
        <p id="topic-progress" role="status"></p><button type="button" id="topic-stop" hidden>Durdur</button>
        <ul id="topic-notes"></ul><p id="topic-empty">Henüz öneri yok.</p><ul id="topic-list"></ul>
        <p id="topic-produce-error" role="alert"></p><button type="button" id="topic-produce" disabled>Seçtiklerini üret</button>
        ${notReady('fromTopics')}
      </section>
      <section id="concept-idea">
        ${notReady('idea')}
        <form id="idea-form" novalidate><textarea id="idea-text"></textarea><p id="idea-text-error" role="alert"></p>
          <input id="idea-count" value="5" /><button type="submit" id="idea-generate" disabled>Üret</button><p id="idea-count-error" role="alert"></p></form>
        <p id="idea-progress" role="status"></p><button type="button" id="idea-stop" hidden>Durdur</button><ul id="idea-notes"></ul>
      </section>
      <section id="concept-produced">
        <fieldset id="concept-filter">
          <label><input type="checkbox" value="generated" checked /> Üretildi</label><label><input type="checkbox" value="approved" checked /> Onaylandı</label>
          <label><input type="checkbox" value="queued" /> Sırada</label>
        </fieldset>
        <p class="empty-state" id="concepts-empty">Henüz konsept yok.</p><p id="concepts-status" role="status"></p><ul id="concept-list"></ul>
        <button type="button" id="concepts-approve-selected" disabled>Seçilenleri onayla</button>
        <span id="concepts-pager" hidden><button type="button" id="concepts-prev">Önceki</button><span id="concepts-page"></span><button type="button" id="concepts-next">Sonraki</button></span>
      </section>
      <section><p id="concept-detail-hint">Okumak için Üretilenler'den bir konsept seçin.</p><article id="concept-detail" tabindex="-1" hidden></article></section>
    </div>`;
}

const READY = { topics: true, fromTopics: true, idea: true, missingRoles: [] };
const work = (extra = {}) => ({ profileId: 'p1', readiness: READY, suggestions: [], topicJob: null, ideaJob: null, ...extra });
const job = (extra = {}) => ({ id: 'j', lane: 'topic', kind: 'from_topics', status: 'running', total: 10, processed: 2, succeeded: 2, formatRejected: 0, duplicates: 0, dedupeUnavailable: false, failures: [], message: null, ...extra });
const listOf = (items, extra = {}) => ({ items, page: 1, pageSize: 50, total: items.length, statuses: ['generated', 'approved'], ...extra });
const concept = (id, title, status = 'generated', extra = {}) => ({ id, title, status, wordRepetitionWarning: false, durationMismatchWarning: false, createdAt: '2026-11-16T10:00:00Z', ...extra });

let routes;
let calls;
let timers;
const ok = (body) => () => $.Deferred().resolve(body).promise();
const fail = (status, body) => () => $.Deferred().reject({ status, responseJSON: body }).promise();
const flush = async () => {
  for (let i = 0; i < 5; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
};
const posts = () => calls.filter((c) => c.method === 'POST');
const body = (call) => JSON.parse(call.data);
const LIST_URL = '/konseptler/liste/p1?durumlar=generated%2Capproved&sayfa=1';

async function start(initialWork = work(), list = listOf([])) {
  routes['GET /konsept-uretim/p1'] = ok(initialWork);
  routes[`GET ${LIST_URL}`] = ok(list);
  const controller = initConceptWork(document, { setTimer: (fn, ms) => { timers.push({ fn, ms }); return timers.length; }, clearTimer: () => {} });
  document.dispatchEvent(new CustomEvent('profile:selected', { detail: { id: 'p1' } }));
  await flush();
  return controller;
}

function submit(form) {
  form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
}

describe('Konseptler üretim alanları ve Üretilenler (spec 0014)', () => {
  beforeEach(() => {
    document.body.innerHTML = pageHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    routes = {};
    calls = [];
    timers = [];
    $.ajax = (settings) => {
      calls.push(settings);
      const handler = routes[`${settings.method} ${settings.url}`];
      return handler ? handler(settings) : $.Deferred().reject({ status: 404 }).promise();
    };
  });

  it('AC-2/AC-9: adet yalnızca 1–100 tam sayı; fikir boş/boşluk/2.000+ reddedilir, 2.000 kabul', () => {
    expect([1, '100', ' 7 '].map(parseCount)).toEqual([1, 100, 7]);
    expect([0, 101, '', null, '1.5', 'abc', '-3'].map(parseCount)).toEqual([null, null, null, null, null, null, null]);
    expect(ideaIssue('')).toBe('required');
    expect(ideaIssue('   ')).toBe('required');
    expect(ideaIssue('a'.repeat(2001))).toBe('tooLong');
    expect(ideaIssue('a'.repeat(2000))).toBeNull();
    expect(fill('{0}/{1} $&', 3, '$1')).toBe('3/$1 $&');
  });

  it('AC-3/AC-6/AC-8/AC-11/AC-12/AC-21: ilerleme ve bilgi satırları', () => {
    const text = el('concept-work').dataset;
    expect(progressText(job({ kind: 'topics' }), text)).toBe('Öneriler üretiliyor…');
    expect(progressText(job({ processed: 12, total: 100 }), text)).toBe('Üretiliyor: 12/100');
    expect(progressText(job({ status: 'stopped', succeeded: 3 }), text)).toBe('Durduruldu: 3/10 konsept kaydedildi.');
    expect(progressText(job({ status: 'failed', message: 'API key geçersiz.' }), text)).toBe('API key geçersiz.');
    expect(jobNotes(job({ kind: 'topics', status: 'completed', succeeded: 0, formatRejected: 2, duplicates: 1, dedupeUnavailable: true }), text)).toEqual([
      '2 öneri biçim dışı olduğu için gösterilmedi.',
      '1 öneri mevcut konularla aynı olduğu için gösterilmedi.',
      'Anlamca denetim yapılamadı.',
      'Yeni öneri bulunamadı: tüm adaylar mevcut konularla aynı ya da biçim dışıydı.',
    ]);
    expect(jobNotes(job({ kind: 'from_idea', status: 'completed', duplicates: 2, failures: [{ label: '3', message: 'Sağlayıcı yanıt vermedi.' }] }), text)).toEqual([
      '2 konsept mevcut konularla aynı olduğu için kaydedilmedi.',
      '1 konsept üretilemedi:',
      '3. konsept: Sağlayıcı yanıt vermedi.',
    ]);
  });

  it('AC-1: hazır olmayan üretimin düğmesi pasif ve Ayarlar/model yönlendirmesi görünür; model bağlantısı modalı açar', async () => {
    const open = vi.fn();
    el('concept-models-open').addEventListener('click', open);
    await start(work({ readiness: { topics: false, fromTopics: true, idea: false, missingRoles: ['dedupe', 'topic'] } }));
    expect(el('topic-generate').disabled).toBe(true);
    expect(el('idea-generate').disabled).toBe(true);
    expect(document.querySelector('[data-not-ready="topics"]').hidden).toBe(false);
    expect(document.querySelector('[data-not-ready="idea"]').hidden).toBe(false);
    expect(document.querySelector('[data-not-ready="fromTopics"]').hidden).toBe(true);
    document.querySelector('[data-not-ready="topics"] [data-open-models]').click();
    expect(open).toHaveBeenCalled();
  });

  it('AC-2: geçersiz adette hata alanın yanında görünür ve istek gitmez', async () => {
    await start();
    el('topic-count').value = '101';
    submit(el('topic-form'));
    await flush();
    expect(el('topic-count-error').textContent).toBe('1 ile 100 arasında bir tam sayı girin.');
    expect(posts()).toHaveLength(0);
  });

  it('AC-7/AC-8/AC-22: Konu üret → ilerleme ve pasif düğmeler, yoklama; liste gelir, seçim yokken Seçtiklerini üret pasif, seçilenler gider', async () => {
    await start();
    routes['POST /konsept-uretim/konu'] = ok(work({ topicJob: job({ kind: 'topics', total: 5, processed: 0, succeeded: 0 }) }));
    el('topic-count').value = '5';
    submit(el('topic-form'));
    await flush();
    expect(body(posts()[0])).toEqual({ profileId: 'p1', count: 5 });
    expect(el('topic-progress').textContent).toBe('Öneriler üretiliyor…');
    expect(el('topic-generate').disabled).toBe(true);
    expect(el('topic-stop').hidden).toBe(false);
    expect(timers.at(-1).ms).toBe(2000);

    const suggestions = [{ id: 's1', title: 'Bizans <b>son</b> günü', summary: 'Özet bir.' }, { id: 's2', title: 'Mısır', summary: 'Özet iki.' }];
    routes['GET /konsept-uretim/p1'] = ok(work({ suggestions, topicJob: job({ kind: 'topics', status: 'completed', total: 5, processed: 5, succeeded: 2, formatRejected: 1 }) }));
    timers.at(-1).fn();
    await flush();
    expect(el('topic-empty').hidden).toBe(true);
    expect([...el('topic-list').querySelectorAll('.suggestion-list__title')].map((n) => n.textContent)).toEqual(['Bizans <b>son</b> günü', 'Mısır']);
    expect(el('topic-list').querySelector('b')).toBeNull();
    expect(el('topic-notes').textContent).toContain('1 öneri biçim dışı olduğu için gösterilmedi.');
    expect(el('topic-produce').disabled).toBe(true);

    const box = el('topic-list').querySelector('input[value="s2"]');
    box.checked = true;
    box.dispatchEvent(new Event('change', { bubbles: true }));
    expect(el('topic-produce').disabled).toBe(false);
    routes['POST /konsept-uretim/secilenler'] = ok(work({ topicJob: job({ total: 1, processed: 0, succeeded: 0 }) }));
    el('topic-produce').click();
    await flush();
    expect(body(posts()[1])).toEqual({ profileId: 'p1', suggestionIds: ['s2'] });
    expect(el('topic-progress').textContent).toBe('Üretiliyor: 0/1');
  });

  it('AC-8/AC-11: sağlayıcı/sunucu hatası sade Türkçe mesajla gösterilir', async () => {
    await start();
    routes['POST /konsept-uretim/fikir'] = fail(409, { code: 'concepts.not_ready', message: 'Bu üretim için gerekli modeller seçilmemiş.' });
    el('idea-text').value = 'Osmanlı';
    submit(el('idea-form'));
    await flush();
    expect(el('idea-progress').textContent).toBe('Bu üretim için gerekli modeller seçilmemiş.');
  });

  it('AC-9/AC-10/AC-21: fikir doğrulaması; geçerli fikirle Üret; süren iş Durdur ile durdurulur', async () => {
    await start();
    el('idea-text').value = '   ';
    submit(el('idea-form'));
    el('idea-text').value = 'a'.repeat(2001);
    submit(el('idea-form'));
    await flush();
    expect(el('idea-text-error').textContent).toBe('Fikir metni en fazla 2.000 karakter olabilir.');
    expect(posts()).toHaveLength(0);

    routes['POST /konsept-uretim/fikir'] = ok(work({ ideaJob: job({ lane: 'idea', kind: 'from_idea', processed: 3 }) }));
    el('idea-text').value = 'a'.repeat(2000);
    el('idea-count').value = '10';
    submit(el('idea-form'));
    await flush();
    expect(body(posts()[0])).toEqual({ profileId: 'p1', idea: 'a'.repeat(2000), count: 10 });
    expect(el('idea-progress').textContent).toBe('Üretiliyor: 3/10');
    routes['POST /konsept-uretim/durdur'] = ok(work({ ideaJob: job({ lane: 'idea', kind: 'from_idea', status: 'stopped', processed: 3, succeeded: 3 }) }));
    el('idea-stop').click();
    await flush();
    expect(body(posts()[1])).toEqual({ profileId: 'p1', lane: 'idea' });
    expect(el('idea-progress').textContent).toBe('Durduruldu: 3/10 konsept kaydedildi.');
    expect(el('idea-stop').hidden).toBe(true);
  });

  it('AC-24/AC-26/AC-27/AC-29: liste rozetler ve uyarılarla; ✓ ve seçim kutusu erişilebilir adlı; toplu onay seçimle etkin; onay sonrası liste yenilenir', async () => {
    const items = [concept('c1', 'Birinci', 'generated', { wordRepetitionWarning: true }), concept('c2', 'İkinci', 'approved', { durationMismatchWarning: true })];
    await start(work(), listOf(items));
    expect(el('concepts-empty').hidden).toBe(true);
    const rows = [...el('concept-list').querySelectorAll('.concept-row')];
    expect(rows[0].textContent).toContain('Üretildi');
    expect(rows[0].textContent).toContain('Kelime tekrarı');
    expect(rows[1].textContent).toContain('Onaylandı');
    expect(rows[1].textContent).toContain('Süre uyumsuz');
    expect(rows[0].querySelector('[data-approve]').getAttribute('aria-label')).toBe('Onayla: Birinci');
    expect(rows[1].querySelector('[data-approve]').getAttribute('aria-label')).toBe('Onayı geri al: İkinci');
    expect(rows[0].querySelector('.concept-row__select').getAttribute('aria-label')).toBe('Seç: Birinci');
    expect(el('concepts-approve-selected').disabled).toBe(true);

    routes['POST /konseptler/onayi-geri-al'] = ok([]);
    rows[1].querySelector('[data-approve]').click();
    await flush();
    expect(posts()[0]).toMatchObject({ url: '/konseptler/onayi-geri-al' });
    expect(body(posts()[0])).toEqual({ ids: ['c2'] });

    const box = el('concept-list').querySelector('.concept-row__select');
    box.checked = true;
    box.dispatchEvent(new Event('change', { bubbles: true }));
    expect(el('concepts-approve-selected').disabled).toBe(false);
    routes['POST /konseptler/onayla'] = ok([]);
    el('concepts-approve-selected').click();
    await flush();
    expect(body(posts()[1])).toEqual({ ids: ['c1'] });
    expect(calls.filter((c) => c.url === LIST_URL).length).toBeGreaterThanOrEqual(3);
  });

  it('AC-27: onay hatasında sade mesaj, durum değişmez', async () => {
    await start(work(), listOf([concept('c1', 'Birinci')]));
    routes['POST /konseptler/onayla'] = fail(409, { message: 'Yalnızca "Üretildi" durumundaki konseptler onaylanabilir.' });
    el('concept-list').querySelector('[data-approve]').click();
    await flush();
    expect(el('concepts-status').textContent).toBe('Yalnızca "Üretildi" durumundaki konseptler onaylanabilir.');
  });

  it('AC-24: boş liste "Henüz konsept yok", yüklenirken yükleniyor, filtre değişince seçili durumlarla 1. sayfa; sayfalama', async () => {
    await start(work(), listOf([]));
    expect(el('concepts-empty').hidden).toBe(false);
    routes['GET /konseptler/liste/p1?durumlar=generated%2Capproved%2Cqueued&sayfa=1'] = () => $.Deferred().promise();
    const queued = el('concept-filter').querySelector('input[value="queued"]');
    queued.checked = true;
    queued.dispatchEvent(new Event('change', { bubbles: true }));
    await flush();
    expect(el('concepts-status').textContent).toBe('Yükleniyor…');

    document.body.innerHTML = pageHtml();
    calls = [];
    await start(work(), listOf([concept('c1', 'Bir')], { total: 51 }));
    expect(el('concepts-pager').hidden).toBe(false);
    expect(el('concepts-page').textContent).toBe('Sayfa 1/2');
    routes['GET /konseptler/liste/p1?durumlar=generated%2Capproved&sayfa=2'] = ok(listOf([concept('c0', 'Sıfır')], { page: 2, total: 51 }));
    el('concepts-next').click();
    await flush();
    expect(el('concept-list').textContent).toContain('Sıfır');
    expect(el('concepts-page').textContent).toBe('Sayfa 2/2');
  });

  it('AC-25: detay tüm sahneleri düz metin olarak gösterir; HTML/script çalışmaz; seçilmemişken yönlendirme metni', async () => {
    await start(work(), listOf([concept('c1', '<img src=x onerror=alert(1)>')]));
    expect(el('concept-detail-hint').hidden).toBe(false);
    routes['GET /konseptler/detay/c1'] = ok({
      ...concept('c1', '<img src=x onerror=alert(1)>'),
      scenes: [{ title: 'Açılış', visual: '<script>alert(1)</script>', narration: 'Bir zamanlar…', durationSeconds: 30 }],
    });
    el('concept-list').querySelector('[data-open]').click();
    await flush();
    const detail = el('concept-detail');
    expect(detail.hidden).toBe(false);
    expect(el('concept-detail-hint').hidden).toBe(true);
    expect(detail.querySelector('script')).toBeNull();
    expect(detail.querySelector('img')).toBeNull();
    expect(detail.textContent).toContain('Sahne 1: Açılış');
    expect(detail.textContent).toContain('Ekran: <script>alert(1)</script>');
    expect(detail.textContent).toContain('Seslendirme: Bir zamanlar…');
    expect(detail.textContent).toContain('Süre: 30 sn');
  });

  it('AC-1: model seçimleri değişince hazır olma yeniden okunur', async () => {
    await start(work({ readiness: { topics: false, fromTopics: false, idea: false, missingRoles: ['concept', 'dedupe', 'topic'] } }));
    routes['GET /konsept-uretim/p1'] = ok(work());
    document.dispatchEvent(new CustomEvent('concept-settings:changed', { detail: { profileId: 'p1' } }));
    await flush();
    expect(el('topic-generate').disabled).toBe(false);
    expect(el('idea-generate').disabled).toBe(false);
  });
});
