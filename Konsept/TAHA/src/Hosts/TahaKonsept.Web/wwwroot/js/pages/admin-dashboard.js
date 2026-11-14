/**
 * TK.adminDashboard — Yönetim paneli: istatistik kutuları, Chart.js grafikleri (günlük klip — dikey çubuk; sağlayıcıya göre
 * harcama — yatay çubuk; tek seri, tema rengine uyar), hata bildirimleri tablosu ve son aktiviteler.
 * Tema değişince grafikler yeni renklerle yeniden çizilir. MVC uçları: /Admin/Dashboard/{Istatistik, HataBildirimleri}.
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var charts = {};
    var lastStats = null;

    function css(name) { return window.getComputedStyle(document.documentElement).getPropertyValue(name).trim(); }

    function baseOptions(horizontal, money) {
        var grid = css('--tk-divider');
        var ink = css('--tk-text-muted');
        var valueAxis = {
            beginAtZero: true,
            grid: { color: grid, drawTicks: false },
            border: { display: false },
            ticks: { color: ink, padding: 6, precision: money ? undefined : 0, callback: money ? function (v) { return '$' + v; } : undefined }
        };
        var catAxis = { grid: { display: false }, border: { color: grid }, ticks: { color: ink, maxRotation: 0, autoSkip: true, autoSkipPadding: 12 } };
        return {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: horizontal ? 'y' : 'x',
            animation: { duration: 300 },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: css('--tk-surface'), titleColor: css('--tk-text'), bodyColor: css('--tk-text-soft'),
                    borderColor: css('--tk-border'), borderWidth: 1, padding: 10, displayColors: false,
                    callbacks: money ? { label: function (c) { return U.money(c.parsed[horizontal ? 'x' : 'y']); } } : {}
                }
            },
            scales: horizontal ? { x: valueAxis, y: catAxis } : { x: catAxis, y: valueAxis }
        };
    }

    function draw(id, labels, values, horizontal, money) {
        if (!window.Chart) { return; }
        if (charts[id]) { charts[id].destroy(); }
        var canvas = document.getElementById(id);
        if (!canvas) { return; }
        charts[id] = new window.Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: css('--tk-accent'),
                    hoverBackgroundColor: css('--tk-accent-hover'),
                    borderRadius: 4,
                    borderSkipped: 'start',
                    maxBarThickness: horizontal ? 18 : 22,
                    categoryPercentage: 0.8,
                    barPercentage: 0.9
                }]
            },
            options: baseOptions(horizontal, money)
        });
    }

    function render(s) {
        lastStats = s || {};
        $('#ad-clips').text(lastStats.totalClips !== undefined ? lastStats.totalClips : '0');
        $('#ad-concepts').text(lastStats.totalConcepts !== undefined ? lastStats.totalConcepts : '0');
        $('#ad-spend').text(U.money(lastStats.monthlySpendUsd));
        $('#ad-errors').text(lastStats.errorCount !== undefined ? lastStats.errorCount : '0');

        var daily = lastStats.daily || [];
        draw('ad-chart-daily',
            $.map(daily, function (d) { var dt = new Date(d.date); return isNaN(dt) ? d.date : dt.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' }); }),
            $.map(daily, function (d) { return d.clips || 0; }), false, false);

        var prov = (lastStats.byProvider || []).slice().sort(function (a, b) { return (b.spendUsd || 0) - (a.spendUsd || 0); });
        draw('ad-chart-provider', $.map(prov, function (p) { return p.name; }), $.map(prov, function (p) { return Number(p.spendUsd || 0); }), true, true);

        var acts = lastStats.recentActivities || [];
        $('#ad-activities').html(acts.length ? '<ul class="tk-list">' + $.map(acts, function (a) {
            return '<li class="tk-list-item" style="cursor:default"><div class="tk-list-main"><div class="tk-list-title">' + U.escape(a.message || a.title) + '</div>' +
                '<div class="tk-list-sub">' + U.escape(U.dateTime(a.createdAt)) + '</div></div></li>';
        }).join('') + '</ul>' : U.empty('list', 'Aktivite yok', ''));
    }

    function renderReports(items) {
        if (!items.length) { $('#ad-reports').html(U.empty('check', 'Açık hata bildirimi yok', '')); return; }
        $('#ad-reports').html('<table class="tk-table"><thead><tr><th>Başlık</th><th>Sayfa</th><th>Tarih</th></tr></thead><tbody>' +
            $.map(items, function (r) {
                return '<tr><td><strong>' + U.escape(r.title) + '</strong><div class="tk-hint">' + U.escape(r.description || '') + '</div></td>' +
                    '<td><code>' + U.escape(r.page || '') + '</code></td><td class="tk-muted">' + U.escape(U.dateTime(r.createdAt)) + '</td></tr>';
            }).join('') + '</tbody></table>');
    }

    function load() {
        TK.api.get('/Admin/Dashboard/Istatistik', null, { silent: true }).done(render).fail(function () { render({}); });
        TK.api.get('/Admin/Dashboard/HataBildirimleri', null, { silent: true }).done(function (d) { renderReports(U.items(d)); })
            .fail(function () { $('#ad-reports').html(U.empty('alert', 'Bildirimler alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function init() {
        if (!$('#tk-admin').length) { return; }
        U = TK.util;
        $('#ad-reports, #ad-activities').html(U.skeleton(3));
        $('#ad-refresh').on('click', load);
        document.addEventListener('tk:theme', function () { if (lastStats) { render(lastStats); } });
        load();
    }

    TK.adminDashboard = { init: init };
    $(init);
})(window, window.jQuery);
