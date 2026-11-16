// Admin paneli (spec 0010): İstatistikler ve Aktiviteler sekmeleri. Veri /admin/istatistikler ve /admin/aktiviteler'den $.ajax
// ile (api.js, K-6) sayfa açılışında, sekme, aralık ve filtre değişiminde gelir; canlı yenileme yok (Req 9). Grafikler SVG çubuklar
// + aynı değerleri içeren tablo alternatifi (Req 4). Api verisi yalnızca textContent ile yazılır. Saatler Europe/Istanbul (Req 10).
import { get } from './api.js';

const SVG_NS = 'http://www.w3.org/2000/svg';
const ZONE = 'Europe/Istanbul';
const CHART_WIDTH = 300;
const CHART_HEIGHT = 100;

const timeFormat = new Intl.DateTimeFormat('tr-TR', {
  timeZone: ZONE, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit',
});

/**
 * Bir UTC anını Istanbul yerel saatiyle gösterir.
 * @param {string} iso ISO zaman.
 * @returns {string} "14.11.2026 12:00".
 */
export function formatTime(iso) {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? '' : timeFormat.format(date);
}

/**
 * <c>yyyy-MM-dd</c> gününü "14.11.2026" biçiminde gösterir.
 * @param {string} day Gün.
 * @returns {string} Metin.
 */
export function formatDay(day) {
  const [y, m, d] = String(day || '').split('-');
  return y && m && d ? `${d}.${m}.${y}` : String(day || '');
}

function fill(template, ...values) {
  return values.reduce((text, value, i) => text.replace(`{${i}}`, String(value)), template || '');
}

function failureText(failure, fallback, correlationTemplate) {
  if (failure && failure.status === 403) {
    return (failure.body && failure.body.message) || fallback;
  }
  const correlation = failure && failure.body && failure.body.correlationId;
  return correlation ? `${fallback} ${fill(correlationTemplate, correlation)}` : fallback;
}

function buildChart(doc, title, series, key, t) {
  const figure = doc.createElement('figure');
  figure.className = 'admin-chart';
  figure.dataset.series = key;
  const caption = doc.createElement('figcaption');
  caption.textContent = title;
  figure.append(caption);

  const values = series.map((d) => Number(d[key]) || 0);
  const total = values.reduce((a, b) => a + b, 0);
  const max = Math.max(1, ...values);
  const svg = doc.createElementNS(SVG_NS, 'svg');
  svg.setAttribute('class', 'admin-chart__svg');
  svg.setAttribute('viewBox', `0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`);
  svg.setAttribute('preserveAspectRatio', 'none');
  svg.setAttribute('role', 'img');
  svg.setAttribute('aria-label', fill(t.textChartSummary, title, series.length, total));
  const width = CHART_WIDTH / Math.max(1, series.length);
  series.forEach((day, i) => {
    const height = (values[i] / max) * CHART_HEIGHT;
    const bar = doc.createElementNS(SVG_NS, 'rect');
    bar.setAttribute('class', 'admin-chart__bar');
    bar.setAttribute('x', String(i * width));
    bar.setAttribute('y', String(CHART_HEIGHT - height));
    bar.setAttribute('width', String(Math.max(0.5, width - 1)));
    bar.setAttribute('height', String(height));
    const tip = doc.createElementNS(SVG_NS, 'title');
    tip.textContent = `${formatDay(day.date)}: ${values[i]}`;
    bar.append(tip);
    svg.append(bar);
  });
  const empty = doc.createElement('p');
  empty.className = 'admin-chart__empty';
  empty.textContent = t.textChartEmpty;
  svg.toggleAttribute('hidden', total === 0);
  empty.hidden = total !== 0;
  figure.append(svg, empty);

  const details = doc.createElement('details');
  const summary = doc.createElement('summary');
  summary.textContent = t.textChartTable;
  const table = doc.createElement('table');
  table.className = 'admin-table';
  const head = doc.createElement('tr');
  for (const text of [t.textColumnDate, t.textColumnValue]) {
    const th = doc.createElement('th');
    th.scope = 'col';
    th.textContent = text;
    head.append(th);
  }
  const thead = doc.createElement('thead');
  thead.append(head);
  const tbody = doc.createElement('tbody');
  series.forEach((day, i) => {
    const row = doc.createElement('tr');
    const date = doc.createElement('td');
    date.textContent = formatDay(day.date);
    const value = doc.createElement('td');
    value.textContent = String(values[i]);
    row.append(date, value);
    tbody.append(row);
  });
  table.append(thead, tbody);
  details.append(summary, table);
  figure.append(details);
  return figure;
}

/**
 * Admin panelini başlatır.
 * @param {Document} doc Belge.
 * @returns {{ loadStatistics: Function, loadActivities: Function, selectTab: Function }|null} Denetim (testler için).
 */
export function initAdminPanel(doc) {
  const root = doc.getElementById('admin-panel');
  if (!root) {
    return null;
  }
  const t = root.dataset;
  const el = (id) => doc.getElementById(id);
  const tabs = [...root.querySelectorAll('[role="tab"]')];
  const state = { page: 1, filter: { user: '', from: '', to: '' }, statsRequest: 0, activityRequest: 0, activitiesLoaded: false };

  function showError(prefix, text) {
    el(`admin-${prefix}-error-text`).textContent = text;
    el(`admin-${prefix}-error`).hidden = !text;
  }

  async function loadStatistics() {
    const request = ++state.statsRequest;
    const status = el('admin-statistics-status');
    status.textContent = t.textLoading;
    showError('statistics', '');
    try {
      const data = await get(`${t.statisticsUrl}?gun=${encodeURIComponent(el('admin-range').value)}`);
      if (request !== state.statsRequest) {
        return;
      }
      el('admin-total-users').textContent = String(data.totalUsers ?? 0);
      el('admin-total-clips').textContent = String(data.totalClips ?? 0);
      el('admin-open-reports').textContent = String(data.openErrorReports ?? 0);
      const series = data.series || [];
      el('admin-charts').replaceChildren(
        buildChart(doc, t.textChartRegistrations, series, 'registrations', t),
        buildChart(doc, t.textChartClips, series, 'clips', t),
        buildChart(doc, t.textChartActive, series, 'activeUsers', t),
      );
      status.textContent = '';
    } catch (failure) {
      if (request === state.statsRequest) {
        status.textContent = '';
        showError('statistics', failureText(failure, t.textStatisticsFailed, t.textCorrelation));
      }
    }
  }

  async function loadActivities() {
    const request = ++state.activityRequest;
    const status = el('admin-activities-status');
    const table = el('admin-activity-table');
    status.textContent = t.textLoading;
    showError('activities', '');
    const { user, from, to } = state.filter;
    const query = `sayfa=${state.page}&kullanici=${encodeURIComponent(user)}&baslangic=${encodeURIComponent(from)}&bitis=${encodeURIComponent(to)}`;
    try {
      const data = await get(`${t.activitiesUrl}?${query}`);
      if (request !== state.activityRequest) {
        return;
      }
      state.activitiesLoaded = true;
      const items = data.items || [];
      el('admin-activity-rows').replaceChildren(...items.map((item) => {
        const row = doc.createElement('tr');
        const time = doc.createElement('td');
        time.textContent = formatTime(item.occurredAt);
        const who = doc.createElement('td');
        who.textContent = `${item.userName} (${item.userEmail})`;
        const what = doc.createElement('td');
        what.textContent = item.activity;
        row.append(time, who, what);
        return row;
      }));
      table.hidden = items.length === 0;
      status.textContent = items.length > 0 ? '' : (data.filtered ? t.textActivitiesNoMatch : t.textActivitiesEmpty);
      const pages = Math.max(1, Math.ceil((data.total || 0) / (data.pageSize || 25)));
      state.page = Math.min(state.page, pages);
      el('admin-pager').hidden = pages <= 1;
      el('admin-page').textContent = fill(t.textPage, state.page, pages);
      el('admin-prev').disabled = state.page <= 1;
      el('admin-next').disabled = state.page >= pages;
    } catch (failure) {
      if (request === state.activityRequest) {
        status.textContent = '';
        table.hidden = true;
        showError('activities', failureText(failure, t.textActivitiesFailed, t.textCorrelation));
      }
    }
  }

  function selectTab(tab, focus) {
    for (const other of tabs) {
      const selected = other === tab;
      other.setAttribute('aria-selected', selected ? 'true' : 'false');
      other.tabIndex = selected ? 0 : -1;
      el(other.getAttribute('aria-controls')).hidden = !selected;
    }
    if (focus) {
      tab.focus();
    }
    // Sekme değişiminde veri yeniden gelir (Req 9).
    return tab.id === 'admin-tab-activities' ? loadActivities() : loadStatistics();
  }

  for (const tab of tabs) {
    tab.addEventListener('click', () => selectTab(tab, true));
    tab.addEventListener('keydown', (event) => {
      const index = tabs.indexOf(tab);
      const next = { ArrowRight: index + 1, ArrowLeft: index - 1, Home: 0, End: tabs.length - 1 }[event.key];
      if (next !== undefined) {
        event.preventDefault();
        selectTab(tabs[(next + tabs.length) % tabs.length], true);
      }
    });
  }

  el('admin-range').addEventListener('change', () => loadStatistics());
  el('admin-statistics-retry').addEventListener('click', () => loadStatistics());
  el('admin-activities-retry').addEventListener('click', () => loadActivities());
  el('admin-prev').addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    return loadActivities();
  });
  el('admin-next').addEventListener('click', () => {
    state.page += 1;
    return loadActivities();
  });
  el('admin-activity-filter').addEventListener('submit', (event) => {
    event.preventDefault();
    const from = el('admin-filter-from').value;
    const to = el('admin-filter-to').value;
    const warning = el('admin-filter-warning');
    if (from && to && from > to) {
      // Başlangıç bitişten sonra: filtre uygulanmaz, uyarı görünür (Req 7).
      warning.textContent = t.textDateInvalid;
      warning.hidden = false;
      return undefined;
    }
    warning.hidden = true;
    warning.textContent = '';
    state.filter = { user: el('admin-filter-user').value.trim(), from, to };
    state.page = 1;
    return loadActivities();
  });

  loadStatistics();
  return { loadStatistics, loadActivities, selectTab: (id, focus) => selectTab(el(id), focus) };
}
