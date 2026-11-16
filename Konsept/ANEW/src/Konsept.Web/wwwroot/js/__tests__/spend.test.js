import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setNavigation } from '../api.js';
import { initSpend, refreshSpend, SPEND_INTERVAL_MS } from '../spend.js';
import { installJquery } from './fixture.js';

const $ = installJquery();

// _Layout.cshtml'deki göstergenin en küçük karşılığı (metinler resx'teki değerler).
function spendHtml() {
  return `
    <header class="topbar">
      <div class="topbar__brand">ANEW Konsept</div>
      <span class="topbar__spend" id="monthly-spend" role="status" aria-live="off" data-state="loading" data-url="/harcama"
            data-text-failed="Bu ay: —" data-text-failed-hint="Harcama alınamadı"
            data-label-failed="Bu ayki harcama: Harcama alınamadı" aria-label="Bu ayki harcama yükleniyor">Bu ay: …</span>
    </header>
    <main><p id="rest">Sayfa</p></main>`;
}

const node = () => document.getElementById('monthly-spend');
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

describe('Aylık harcama göstergesi (spec 0013)', () => {
  let responses;

  beforeEach(() => {
    document.body.innerHTML = spendHtml();
    setNavigation({ pathname: '/', search: '', assign: vi.fn() });
    responses = [];
    vi.spyOn($, 'ajax').mockImplementation(() => {
      const next = responses.shift();
      return next ? next() : $.Deferred().reject({ status: 0 }).promise();
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('AC-18: veri gelene kadar yükleniyor; gelince tutar ve erişilebilir ad; odak almaz, tıklanınca bir şey açılmaz', async () => {
    let release;
    responses.push(() => $.Deferred((d) => { release = () => d.resolve({ text: 'Bu ay: $2,37', label: 'Bu ayki harcama: 2,37 dolar' }); }).promise());

    const pending = refreshSpend(node());
    expect(node().dataset.state).toBe('loading');
    expect(node().textContent).toBe('Bu ay: …');
    release();
    await pending;

    expect(node().textContent).toBe('Bu ay: $2,37');
    expect(node().getAttribute('aria-label')).toBe('Bu ayki harcama: 2,37 dolar');
    expect(node().dataset.state).toBe('ok');
    expect(node().tabIndex).toBe(-1);
    node().click();
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect($.ajax).toHaveBeenCalledTimes(1);
  });

  it('AC-16: yeni kayıt sonrası sayfa yenilenmeden en geç 10 sn içinde yeni toplam görünür (5 sn aralık)', async () => {
    const timers = { setInterval: vi.fn() };
    responses.push(() => $.Deferred().resolve({ text: 'Bu ay: $0,00', label: 'Bu ayki harcama: 0,00 dolar' }).promise());
    responses.push(() => $.Deferred().resolve({ text: 'Bu ay: $0,01', label: 'Bu ayki harcama: 0,01 dolar' }).promise());

    initSpend(document, timers);
    await flush();
    expect(node().textContent).toBe('Bu ay: $0,00');
    expect(timers.setInterval).toHaveBeenCalledWith(expect.any(Function), SPEND_INTERVAL_MS);
    expect(SPEND_INTERVAL_MS).toBeLessThanOrEqual(10000);

    timers.setInterval.mock.calls[0][0]();
    await flush();
    expect(node().textContent).toBe('Bu ay: $0,01');
    expect(node().getAttribute('aria-label')).toBe('Bu ayki harcama: 0,01 dolar');
  });

  it('AC-16: veri alınamazsa "—" ve "Harcama alınamadı" ipucu; sayfanın geri kalanı yerinde', async () => {
    responses.push(() => $.Deferred().reject({ status: 502, responseJSON: { code: 'api.unavailable' } }).promise());

    await refreshSpend(node());

    expect(node().textContent).toBe('Bu ay: —');
    expect(node().title).toBe('Harcama alınamadı');
    expect(node().getAttribute('aria-label')).toBe('Bu ayki harcama: Harcama alınamadı');
    expect(node().dataset.state).toBe('error');
    expect(document.getElementById('rest').textContent).toBe('Sayfa');
  });

  it('AC-18: gösterge yoksa (giriş yapılmamış) istek yapılmaz', () => {
    document.body.innerHTML = '<main></main>';
    const timers = { setInterval: vi.fn() };

    expect(initSpend(document, timers)).toBeNull();
    expect($.ajax).not.toHaveBeenCalled();
    expect(timers.setInterval).not.toHaveBeenCalled();
  });
});
