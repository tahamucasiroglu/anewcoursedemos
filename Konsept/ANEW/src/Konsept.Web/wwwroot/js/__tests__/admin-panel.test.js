import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { formatTime, initAdminPanel } from '../admin-panel.js';
import { setNavigation } from '../api.js';
import { ajaxRejecting, ajaxResolving, installJquery } from './fixture.js';

const $ = installJquery();

// Views/Admin/Index.cshtml'in en küçük karşılığı (metinler resx'teki değerler).
function panelHtml() {
  return `
    <div id="admin-panel" data-statistics-url="/admin/istatistikler" data-activities-url="/admin/aktiviteler"
         data-text-loading="Yükleniyor…" data-text-statistics-failed="İstatistikler şu an yüklenemedi."
         data-text-activities-failed="Aktiviteler şu an yüklenemedi." data-text-correlation="Hata no: {0}"
         data-text-chart-registrations="Günlük yeni kayıt" data-text-chart-clips="Günlük üretilen klip"
         data-text-chart-active="Günlük aktif kullanıcı" data-text-chart-empty="Bu aralıkta veri yok"
         data-text-chart-table="Tablo olarak göster" data-text-chart-summary="{0}: {1} gün, toplam {2}"
         data-text-column-date="Tarih" data-text-column-value="Değer"
         data-text-date-invalid="Başlangıç tarihi bitişten sonra olamaz" data-text-activities-empty="Henüz aktivite yok"
         data-text-activities-no-match="Filtreye uyan aktivite yok" data-text-page="Sayfa {0}/{1}">
      <div role="tablist">
        <button type="button" role="tab" id="admin-tab-statistics" aria-selected="true" aria-controls="admin-statistics">İstatistikler</button>
        <button type="button" role="tab" id="admin-tab-activities" aria-selected="false" aria-controls="admin-activities" tabindex="-1">Aktiviteler</button>
      </div>
      <section role="tabpanel" id="admin-statistics">
        <p id="admin-statistics-status" role="status"></p>
        <div id="admin-statistics-error" role="alert" hidden><p id="admin-statistics-error-text"></p><button id="admin-statistics-retry">Tekrar dene</button></div>
        <dd id="admin-total-users">–</dd><dd id="admin-total-clips">–</dd><dd id="admin-open-reports">–</dd>
        <select id="admin-range"><option value="7">7</option><option value="30" selected>30</option><option value="90">90</option></select>
        <div id="admin-charts"></div>
      </section>
      <section role="tabpanel" id="admin-activities" hidden>
        <form id="admin-activity-filter"><input id="admin-filter-user" /><input type="date" id="admin-filter-from" /><input type="date" id="admin-filter-to" /><button type="submit">Filtrele</button></form>
        <p id="admin-filter-warning" role="alert" hidden></p>
        <p id="admin-activities-status" role="status"></p>
        <div id="admin-activities-error" role="alert" hidden><p id="admin-activities-error-text"></p><button id="admin-activities-retry">Tekrar dene</button></div>
        <table id="admin-activity-table" hidden><tbody id="admin-activity-rows"></tbody></table>
        <nav id="admin-pager" hidden><button id="admin-prev">Önceki</button><span id="admin-page"></span><button id="admin-next">Sonraki</button></nav>
      </section>
    </div>`;
}

const el = (id) => document.getElementById(id);
const flush = () => new Promise((resolve) => { setTimeout(resolve, 0); });

function day(date, registrations, clips, activeUsers) {
  return { date, registrations, clips, activeUsers };
}

const stats = {
  totalUsers: 3, totalClips: 2, openErrorReports: 1, days: 7,
  series: [day('2026-11-12', 0, 0, 0), day('2026-11-13', 2, 0, 1), day('2026-11-14', 1, 0, 3)],
};

function activities(count, total, extra = {}) {
  return {
    items: Array.from({ length: count }, (_, i) => ({
      occurredAt: '2026-11-14T09:00:00Z', userName: `Kişi <b>${i}</b>`, userEmail: `k${i}@ornek.test`, activity: 'Giriş',
    })),
    page: 1, pageSize: 25, total, filtered: false, dateFilterIgnored: false, ...extra,
  };
}

describe('admin paneli (spec 0010)', () => {
  let ajax;

  beforeEach(() => {
    document.body.innerHTML = panelHtml();
    setNavigation({ pathname: '/admin', search: '', assign: vi.fn() });
    ajax = vi.spyOn($, 'ajax');
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('açılışta İstatistikler 30 günle yüklenir: kartlar, üç grafik + tablo alternatifi, veri yoksa boş durum (AC-1, AC-3, AC-4)', async () => {
    ajax.mockReturnValue(ajaxResolving($, stats));

    initAdminPanel(document);
    expect(el('admin-statistics-status').textContent).toBe('Yükleniyor…');
    await flush();

    expect(ajax).toHaveBeenCalledWith(expect.objectContaining({ url: '/admin/istatistikler?gun=30', method: 'GET' }));
    expect(el('admin-activities').hidden).toBe(true);
    expect(el('admin-total-users').textContent).toBe('3');
    expect(el('admin-total-clips').textContent).toBe('2');
    expect(el('admin-open-reports').textContent).toBe('1');
    const charts = [...el('admin-charts').querySelectorAll('figure')];
    expect(charts.map((c) => c.querySelector('figcaption').textContent)).toEqual(['Günlük yeni kayıt', 'Günlük üretilen klip', 'Günlük aktif kullanıcı']);
    const registrations = charts[0];
    expect(registrations.querySelectorAll('rect')).toHaveLength(3);
    expect(registrations.querySelector('svg').getAttribute('aria-label')).toBe('Günlük yeni kayıt: 3 gün, toplam 3');
    const cells = [...registrations.querySelectorAll('tbody tr')].map((r) => [...r.cells].map((c) => c.textContent));
    expect(cells).toEqual([['12.11.2026', '0'], ['13.11.2026', '2'], ['14.11.2026', '1']]);
    const clips = charts[1];
    expect(clips.querySelector('.admin-chart__empty').hidden).toBe(false);
    expect(clips.querySelector('.admin-chart__empty').textContent).toBe('Bu aralıkta veri yok');
    expect(clips.querySelector('svg').hasAttribute('hidden')).toBe(true);
    expect(registrations.querySelector('.admin-chart__empty').hidden).toBe(true);
    expect(el('admin-statistics-status').textContent).toBe('');
  });

  it('aralık değişince seçilen gün sayısıyla yeniden yüklenir (AC-4)', async () => {
    ajax.mockReturnValue(ajaxResolving($, stats));
    initAdminPanel(document);
    await flush();

    el('admin-range').value = '90';
    el('admin-range').dispatchEvent(new Event('change'));
    await flush();

    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/istatistikler?gun=90' }));
  });

  it('sunucu hatasında sade mesaj + korelasyon numarası + Tekrar dene; ham metin yazılmaz (AC-5)', async () => {
    ajax.mockReturnValue(ajaxRejecting($, 500, { code: 'api.error', message: 'Npgsql SELECT stack', correlationId: '0HN-42' }));
    const panel = initAdminPanel(document);
    await flush();

    expect(el('admin-statistics-error').hidden).toBe(false);
    expect(el('admin-statistics-error-text').textContent).toBe('İstatistikler şu an yüklenemedi. Hata no: 0HN-42');
    expect(el('admin-statistics-error').textContent).not.toContain('SELECT');

    ajax.mockReturnValue(ajaxResolving($, stats));
    el('admin-statistics-retry').click();
    await flush();
    expect(el('admin-statistics-error').hidden).toBe(true);

    ajax.mockReturnValue(ajaxRejecting($, 502, { code: 'api.unavailable', correlationId: 'C-7' }));
    await panel.selectTab('admin-tab-activities', false);
    expect(el('admin-activities-error-text').textContent).toBe('Aktiviteler şu an yüklenemedi. Hata no: C-7');
    expect(el('admin-activity-table').hidden).toBe(true);
  });

  it('Aktiviteler sekmesi: satırlar düz metin, Istanbul saati, 25\'erli sayfalama ileri/geri (AC-1, AC-6, AC-8)', async () => {
    ajax.mockReturnValue(ajaxResolving($, stats));
    initAdminPanel(document);
    await flush();
    ajax.mockReturnValue(ajaxResolving($, activities(25, 30)));

    el('admin-tab-activities').click();
    await flush();

    expect(el('admin-tab-activities').getAttribute('aria-selected')).toBe('true');
    expect(el('admin-tab-activities').tabIndex).toBe(0);
    expect(el('admin-tab-statistics').tabIndex).toBe(-1);
    expect(el('admin-activities').hidden).toBe(false);
    expect(el('admin-statistics').hidden).toBe(true);
    expect(document.activeElement).toBe(el('admin-tab-activities'));
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/aktiviteler?sayfa=1&kullanici=&baslangic=&bitis=' }));
    const rows = el('admin-activity-rows').rows;
    expect(rows).toHaveLength(25);
    expect([...rows[0].cells].map((c) => c.textContent)).toEqual(['14.11.2026 12:00', 'Kişi <b>0</b> (k0@ornek.test)', 'Giriş']);
    expect(rows[0].querySelector('b')).toBeNull();
    expect(el('admin-pager').hidden).toBe(false);
    expect(el('admin-page').textContent).toBe('Sayfa 1/2');
    expect(el('admin-prev').disabled).toBe(true);

    ajax.mockReturnValue(ajaxResolving($, activities(5, 30, { page: 2 })));
    el('admin-next').click();
    await flush();
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/aktiviteler?sayfa=2&kullanici=&baslangic=&bitis=' }));
    expect(el('admin-activity-rows').rows).toHaveLength(5);
    expect(el('admin-page').textContent).toBe('Sayfa 2/2');
    expect(el('admin-next').disabled).toBe(true);

    ajax.mockReturnValue(ajaxResolving($, stats));
    el('admin-tab-activities').dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    await flush();
    expect(el('admin-tab-statistics').getAttribute('aria-selected')).toBe('true'); // son sekmeden sağa → ilk sekme.
    expect(document.activeElement).toBe(el('admin-tab-statistics'));
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({ url: '/admin/istatistikler?gun=30' })); // sekme değişiminde yeniden.
  });

  it('filtreler gönderilir; başlangıç bitişten sonraysa istek yok ve uyarı; boş durumlar metinle (AC-6)', async () => {
    ajax.mockReturnValue(ajaxResolving($, stats));
    const panel = initAdminPanel(document);
    await flush();
    ajax.mockReturnValue(ajaxResolving($, activities(0, 0)));
    await panel.selectTab('admin-tab-activities', false);
    expect(el('admin-activities-status').textContent).toBe('Henüz aktivite yok');
    expect(el('admin-activity-table').hidden).toBe(true);

    const calls = ajax.mock.calls.length;
    el('admin-filter-from').value = '2026-11-10';
    el('admin-filter-to').value = '2026-11-01';
    el('admin-activity-filter').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(ajax.mock.calls.length).toBe(calls);
    expect(el('admin-filter-warning').hidden).toBe(false);
    expect(el('admin-filter-warning').textContent).toBe('Başlangıç tarihi bitişten sonra olamaz');

    ajax.mockReturnValue(ajaxResolving($, activities(0, 0, { filtered: true })));
    el('admin-filter-user').value = ' Ayşe ';
    el('admin-filter-to').value = '2026-11-12';
    el('admin-activity-filter').dispatchEvent(new Event('submit', { cancelable: true }));
    await flush();
    expect(el('admin-filter-warning').hidden).toBe(true);
    expect(ajax).toHaveBeenLastCalledWith(expect.objectContaining({
      url: `/admin/aktiviteler?sayfa=1&kullanici=${encodeURIComponent('Ayşe')}&baslangic=2026-11-10&bitis=2026-11-12`,
    }));
    expect(el('admin-activities-status').textContent).toBe('Filtreye uyan aktivite yok');
  });

  it('saat Europe/Istanbul ile gösterilir; panel olmayan sayfada istek yapılmaz (AC-8)', () => {
    expect(formatTime('2026-11-14T21:30:00Z')).toBe('15.11.2026 00:30');
    document.body.innerHTML = '<main></main>';
    expect(initAdminPanel(document)).toBeNull();
    expect(ajax).not.toHaveBeenCalled();
  });
});
