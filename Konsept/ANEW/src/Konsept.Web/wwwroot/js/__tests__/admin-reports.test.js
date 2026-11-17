import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { initAdminPanel } from '../admin-panel.js';
import { setNavigation } from '../api.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

const $ = installJquery();

// Views/Admin/Index.cshtml'in Hata bildirimleri bölümünün en küçük karşılığı (metinler resx'teki değerler).
function panelHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <div id="admin-panel" data-statistics-url="/admin/istatistikler" data-activities-url="/admin/aktiviteler"
         data-text-loading="Yükleniyor…" data-text-statistics-failed="İstatistikler şu an yüklenemedi."
         data-text-correlation="Hata no: {0}" data-text-page="Sayfa {0}/{1}">
      <div role="tablist">
        <button type="button" role="tab" id="admin-tab-statistics" aria-selected="true" aria-controls="admin-statistics">İstatistikler</button>
        <button type="button" role="tab" id="admin-tab-activities" aria-selected="false" aria-controls="admin-activities" tabindex="-1">Aktiviteler</button>
        <button type="button" role="tab" id="admin-tab-reports" aria-selected="false" aria-controls="admin-reports" tabindex="-1">Hata bildirimleri</button>
      </div>
      <section role="tabpanel" id="admin-statistics">
        <p id="admin-statistics-status"></p>
        <div id="admin-statistics-error" hidden><p id="admin-statistics-error-text"></p><button id="admin-statistics-retry">Tekrar dene</button></div>
        <dd id="admin-total-users"></dd><dd id="admin-total-clips"></dd><dd id="admin-open-reports"></dd>
        <select id="admin-range"><option value="30" selected>30</option></select>
        <div id="admin-charts"></div>
      </section>
      <section role="tabpanel" id="admin-activities" hidden>
        <form id="admin-activity-filter"><input id="admin-filter-user" /><input type="date" id="admin-filter-from" /><input type="date" id="admin-filter-to" /></form>
        <p id="admin-filter-warning" hidden></p><p id="admin-activities-status"></p>
        <div id="admin-activities-error" hidden><p id="admin-activities-error-text"></p><button id="admin-activities-retry">Tekrar dene</button></div>
        <table id="admin-activity-table" hidden><tbody id="admin-activity-rows"></tbody></table>
        <nav id="admin-pager" hidden><button id="admin-prev">Önceki</button><span id="admin-page"></span><button id="admin-next">Sonraki</button></nav>
      </section>
      <section role="tabpanel" id="admin-reports" hidden data-reports-url="/admin/hata-bildirimleri"
               data-text-failed="Hata bildirimleri şu an yüklenemedi." data-text-detail-failed="Bildirim şu an yüklenemedi."
               data-text-empty="Açık hata bildirimi yok" data-text-no-match="Filtreye uyan bildirim yok"
               data-text-source-web="Web" data-text-source-agent="Ajan" data-text-source-api="API"
               data-text-status-new="Yeni" data-text-status-reviewing="İnceleniyor" data-text-status-resolved="Çözüldü"
               data-text-status-ignored="Yok sayıldı" data-text-open="Detay" data-text-open-label="Bildirim {0} detayı"
               data-text-detail-title="Hata bildirimi No: {0}" data-text-saved="Kaydedildi." data-text-save-failed="Durum kaydedilemedi."
               data-text-processing="İşleniyor…" data-text-note-count="{0}/500" data-text-none="—">
        <form id="admin-report-filter">
          <select id="admin-report-source-filter"><option value="all" selected>Tümü</option><option value="web">Web</option><option value="agent">Ajan</option><option value="api">API</option></select>
          <select id="admin-report-status-filter"><option value="open" selected>Açık</option><option value="new">Yeni</option><option value="reviewing">İnceleniyor</option>
            <option value="resolved">Çözüldü</option><option value="ignored">Yok sayıldı</option><option value="all">Tümü</option></select>
          <button type="submit">Filtrele</button></form>
        <p id="admin-reports-status" role="status"></p>
        <div id="admin-reports-error" role="alert" hidden><p id="admin-reports-error-text"></p><button id="admin-reports-retry">Tekrar dene</button></div>
        <table id="admin-report-table" hidden><tbody id="admin-report-rows"></tbody></table>
        <nav id="admin-report-pager" hidden><button id="admin-report-prev">Önceki</button><span id="admin-report-page"></span><button id="admin-report-next">Sonraki</button></nav>
      </section>
      <div id="admin-report-dialog" hidden><div role="dialog" tabindex="-1">
        <h2 id="admin-report-title"></h2>
        <p id="admin-report-loading" role="status"></p>
        <div id="admin-report-error" role="alert" hidden><p id="admin-report-error-text"></p><button id="admin-report-retry">Tekrar dene</button></div>
        <dl id="admin-report-body" hidden>
          <dd id="admin-report-time"></dd><dd id="admin-report-source"></dd><dd id="admin-report-reporter"></dd><dd id="admin-report-current"></dd>
          <dd id="admin-report-version"></dd><dd id="admin-report-agent-version"></dd><dd id="admin-report-os"></dd><dd id="admin-report-component"></dd>
          <dd id="admin-report-page-path"></dd><dd id="admin-report-correlation"></dd><pre id="admin-report-description"></pre>
          <pre id="admin-report-attachments"></pre><pre id="admin-report-note-current"></pre>
        </dl>
        <div id="admin-report-form" hidden>
          <select id="admin-report-status"><option value="new">Yeni</option><option value="reviewing">İnceleniyor</option><option value="resolved">Çözüldü</option><option value="ignored">Yok sayıldı</option></select>
          <textarea id="admin-report-note"></textarea><span id="admin-report-note-count"></span>
          <span id="admin-report-note-error" role="alert" hidden>En fazla 500 karakter</span>
        </div>
        <p id="admin-report-message" role="alert" hidden></p>
        <button id="admin-report-close">Kapat</button><button id="admin-report-save">Kaydet</button>
      </div></div>
    </div>`;
}

const el = (id) => document.getElementById(id);
const flush = () => new Promise((resolve) => { setTimeout(resolve, 0); });
const stats = (open) => ({ totalUsers: 1, totalClips: 0, openErrorReports: open, days: 30, series: [] });
const key = (target, k) => target.dispatchEvent(new KeyboardEvent('keydown', { key: k, bubbles: true }));

function item(overrides = {}) {
  return {
    number: 7, createdAt: '2026-11-14T21:30:00Z', source: 'web', reporterName: 'Ayşe', reporterEmail: 'ayse@ornek.test',
    summary: 'Video <script>alert(1)</script> üretilmedi', status: 'new', ...overrides,
  };
}

function page(items, extra = {}) {
  return { items, page: 1, pageSize: 25, total: items.length, filtered: false, ...extra };
}

function detail(overrides = {}) {
  return {
    report: {
      number: 7, source: 'agent', status: 'reviewing', createdAt: '2026-11-14T09:00:00Z', productVersion: '0.5.0', agentVersion: '0.5.1',
      operatingSystem: 'Windows 11', component: 'ffmpeg', step: 'indirme', page: null, correlationId: 'corr-7',
      description: 'Render <script>alert(1)</script> sk-…a3f', attachments: 'satır 1\nsatır 2',
    },
    reporterName: '—', reporterEmail: '—', adminNote: 'bakılıyor', statusChangedAt: null, ...overrides,
  };
}

describe('admin Hata bildirimleri (spec 0025)', () => {
  let ajax;
  let panel;

  async function openReports(body) {
    ajax.mockReturnValue(ajaxResolving($, stats(2)));
    panel = initAdminPanel(document);
    await flush();
    ajax.mockReturnValue(ajaxResolving($, body));
    await panel.selectTab('admin-tab-reports', false);
  }

  async function openDetail(body = detail()) {
    ajax.mockReturnValue(ajaxResolving($, body));
    await panel.reports.open(7, el('admin-report-rows').querySelector('button'));
  }

  const rowCells = (i) => [...el('admin-report-rows').rows[i].cells].map((c) => c.textContent);

  beforeEach(() => {
    document.body.innerHTML = panelHtml();
    setNavigation({ pathname: '/admin', search: '', assign: vi.fn() });
    ajax = vi.spyOn($, 'ajax');
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('liste: varsayılan Açık, sütunlar düz metin, kaynak/durum metinle, Istanbul saati (AC-2, AC-8)', async () => {
    await openReports(page([item(), item({ number: 6, source: 'api', reporterName: '—', reporterEmail: '—', status: 'reviewing' })]));

    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/hata-bildirimleri?sayfa=1&kaynak=all&durum=open', method: 'GET' }));
    expect(el('admin-reports').hidden).toBe(false);
    expect(rowCells(0).slice(0, 5)).toEqual(['15.11.2026 00:30', 'Web', 'Ayşe (ayse@ornek.test)', 'Video <script>alert(1)</script> üretilmedi', 'Yeni']);
    expect(el('admin-report-rows').querySelector('script')).toBeNull();
    expect(rowCells(1).slice(1, 5)).toEqual(['API', '—', 'Video <script>alert(1)</script> üretilmedi', 'İnceleniyor']);
    expect(el('admin-report-rows').querySelector('button').getAttribute('aria-label')).toBe('Bildirim 7 detayı');
    expect(el('admin-reports-status').textContent).toBe('');
  });

  it('boş: "Açık hata bildirimi yok"; filtre sonucu boş: "Filtreye uyan bildirim yok"; filtre ve sayfa parametreleri (AC-2)', async () => {
    await openReports(page([]));
    expect(el('admin-reports-status').textContent).toBe('Açık hata bildirimi yok');
    expect(el('admin-report-table').hidden).toBe(true);

    el('admin-report-source-filter').value = 'agent';
    el('admin-report-status-filter').value = 'resolved';
    ajax.mockReturnValue(ajaxResolving($, page([], { filtered: true })));
    el('admin-report-filter').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/hata-bildirimleri?sayfa=1&kaynak=agent&durum=resolved' }));
    expect(el('admin-reports-status').textContent).toBe('Filtreye uyan bildirim yok');

    ajax.mockReturnValue(ajaxResolving($, page([item()], { total: 30, filtered: true })));
    el('admin-report-filter').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(el('admin-report-pager').hidden).toBe(false);
    expect(el('admin-report-page').textContent).toBe('Sayfa 1/2');
    ajax.mockReturnValue(ajaxResolving($, page([item()], { total: 30, filtered: true })));
    el('admin-report-next').click();
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/hata-bildirimleri?sayfa=2&kaynak=agent&durum=resolved' }));
  });

  it('liste ve detay: yükleniyor, sunucu hatasında sade mesaj + korelasyon + Tekrar dene; ham ayrıntı yok (AC-3)', async () => {
    ajax.mockReturnValue(ajaxResolving($, stats(0)));
    panel = initAdminPanel(document);
    await flush();
    ajax.mockReturnValue(ajaxRejecting($, 500, { code: 'api.error', message: 'Npgsql SELECT at Stack', correlationId: '0HN-9' }));
    const loading = panel.selectTab('admin-tab-reports', false);
    expect(el('admin-reports-status').textContent).toBe('Yükleniyor…');
    await loading;
    expect(el('admin-reports-error').hidden).toBe(false);
    expect(el('admin-reports-error-text').textContent).toBe('Hata bildirimleri şu an yüklenemedi. Hata no: 0HN-9');
    expect(el('admin-reports').textContent).not.toContain('SELECT');

    ajax.mockReturnValue(ajaxResolving($, page([item()])));
    el('admin-reports-retry').click();
    await flush();
    expect(el('admin-reports-error').hidden).toBe(true);

    ajax.mockReturnValue(ajaxRejecting($, 500, { message: 'SELECT secret', correlationId: '0HN-10' }));
    const detailLoading = panel.reports.open(7, null);
    expect(el('admin-report-loading').textContent).toBe('Yükleniyor…');
    await detailLoading;
    expect(el('admin-report-error-text').textContent).toBe('Bildirim şu an yüklenemedi. Hata no: 0HN-10');
    expect(el('admin-report-body').hidden).toBe(true);
    expect(el('admin-report-save').disabled).toBe(true);

    ajax.mockReturnValue(ajaxRejecting($, 404, { message: 'Hata bildirimi bulunamadı.', correlationId: '0HN-11' }));
    el('admin-report-retry').click();
    await flush();
    expect(el('admin-report-error-text').textContent).toBe('Hata bildirimi bulunamadı. Hata no: 0HN-11');
  });

  it('detay: alanlar düz metin (<script> çalışmaz), mevcut not ve durum, Esc kapatır ve odak düğmeye döner (AC-4, AC-8)', async () => {
    await openReports(page([item()]));
    const opener = el('admin-report-rows').querySelector('button');
    ajax.mockReturnValue(ajaxResolving($, detail()));
    opener.click();
    await flush();

    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/hata-bildirimleri/7', method: 'GET' }));
    expect(el('admin-report-dialog').hidden).toBe(false);
    expect(el('admin-report-title').textContent).toBe('Hata bildirimi No: 7');
    expect(el('admin-report-description').textContent).toBe('Render <script>alert(1)</script> sk-…a3f');
    expect(el('admin-report-dialog').querySelector('script')).toBeNull();
    expect(el('admin-report-attachments').textContent).toBe('satır 1\nsatır 2');
    expect(el('admin-report-correlation').textContent).toBe('corr-7');
    expect(el('admin-report-agent-version').textContent).toBe('0.5.1');
    expect(el('admin-report-version').textContent).toBe('0.5.0');
    expect(el('admin-report-component').textContent).toBe('ffmpeg / indirme');
    expect(el('admin-report-page-path').textContent).toBe('—');
    expect(el('admin-report-time').textContent).toBe('14.11.2026 12:00');
    expect(el('admin-report-current').textContent).toBe('İnceleniyor');
    expect(el('admin-report-status').value).toBe('reviewing');
    expect(el('admin-report-note').value).toBe('bakılıyor');
    expect(el('admin-report-note-count').textContent).toBe('9/500');

    el('admin-report-save').focus();
    key(el('admin-report-save'), 'Tab');
    expect(document.activeElement).toBe(el('admin-report-status'));
    key(el('admin-report-note'), 'Escape');
    expect(el('admin-report-dialog').hidden).toBe(true);
    expect(document.activeElement).toBe(opener);
  });

  it('durum kaydı: istek sürerken Kaydet pasif + İşleniyor, başarıda liste ve açık sayısı yenilenir (AC-6)', async () => {
    await openReports(page([item()]));
    await openDetail();
    el('admin-report-status').value = 'resolved';
    el('admin-report-note').value = 'düzeltildi';
    let resolveSave;
    ajax.mockReturnValueOnce($.Deferred((d) => { resolveSave = d; }).promise());
    const saving = panel.reports.save();

    expect(el('admin-report-save').disabled).toBe(true);
    expect(el('admin-report-save').textContent).toBe('İşleniyor…');
    expect(el('admin-report-close').disabled).toBe(true);
    expect(el('admin-report-dialog').contains(document.activeElement)).toBe(true); // odak pencerede kalır (review r1 F-1)
    key(document.activeElement, 'Escape');
    expect(el('admin-report-dialog').hidden).toBe(false);
    await panel.reports.save(); // çift tıklama: ikinci istek yok
    expect(ajax.mock.calls.filter((c) => c[0].method === 'POST')).toHaveLength(1);
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({
      url: '/admin/hata-bildirimleri/7/durum', method: 'POST', data: JSON.stringify({ status: 'resolved', note: 'düzeltildi' }),
    }));

    ajax.mockImplementation((options) => ajaxResolving($, options.url.startsWith('/admin/istatistikler') ? stats(1) : page([])));
    resolveSave.resolve(detail({ adminNote: 'düzeltildi', report: { ...detail().report, status: 'resolved' } }));
    await saving;
    await flush();

    expect(el('admin-report-save').textContent).toBe('Kaydet');
    expect(el('admin-report-current').textContent).toBe('Çözüldü');
    expect(el('admin-report-note-current').textContent).toBe('düzeltildi');
    expect(el('admin-report-message').textContent).toBe('Kaydedildi.');
    expect(document.activeElement).toBe(el('admin-report-save'));
    expect(el('admin-open-reports').textContent).toBe('1');
    expect(el('admin-reports-status').textContent).toBe('Açık hata bildirimi yok');
    el('admin-report-close').click();
    expect(document.activeElement).toBe(el('admin-report-source-filter'));
  });

  it('durum kaydı hatası: durum değişmez, hata mesajı pencerede; not 500 karakteri aşınca Kaydet pasif + uyarı (AC-6)', async () => {
    await openReports(page([item()]));
    await openDetail();

    el('admin-report-note').value = 'n'.repeat(501);
    el('admin-report-note').dispatchEvent(new Event('input'));
    expect(el('admin-report-save').disabled).toBe(true);
    expect(el('admin-report-note-error').hidden).toBe(false);
    expect(el('admin-report-note-count').textContent).toBe('501/500');
    const calls = ajax.mock.calls.length;
    await panel.reports.save();
    expect(ajax.mock.calls.length).toBe(calls);

    el('admin-report-note').value = 'n'.repeat(500);
    el('admin-report-note').dispatchEvent(new Event('input'));
    expect(el('admin-report-save').disabled).toBe(false);
    expect(el('admin-report-note-error').hidden).toBe(true);

    el('admin-report-status').value = 'ignored';
    ajax.mockReturnValue(ajaxRejecting($, 500, { message: 'UPDATE gizli', correlationId: '0HN-12' }));
    await panel.reports.save();
    expect(el('admin-report-message').textContent).toBe('Durum kaydedilemedi. Hata no: 0HN-12');
    expect(el('admin-report-current').textContent).toBe('İnceleniyor');
    expect(el('admin-report-save').disabled).toBe(false);
    expect(el('admin-report-save').textContent).toBe('Kaydet');
    expect(el('admin-report-dialog').textContent).not.toContain('UPDATE');
    expect(document.activeElement).toBe(el('admin-report-save'));
    key(document.activeElement, 'Escape');
    expect(el('admin-report-dialog').hidden).toBe(true);
  });
});
