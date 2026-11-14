/**
 * TK.adminDashboard — Yönetim paneli (Areas/Admin/Dashboard). Dört sekme (adres çubuğunda #genel, #kullanicilar, #hatalar, #gunluk):
 *  - Genel bakış: istatistik kutuları + Chart.js (günlük video — çizgi, günlük harcama — çubuk, seviyeye göre günlük kaydı — halka).
 *    Tek serili grafiklerde lejant yok (başlık adlandırır); halkada lejant + sayılar var. Tema değişince ('tk:theme') yeniden çizilir.
 *  - Kullanıcılar: tablo + Yasakla / Yasağı kaldır (onaylı).
 *  - Hata bildirimleri: durum filtresi, sayfalama, satır içi durum seçimi (Yeni / Görüldü / Çözüldü), ayrıntı açılır.
 *  - Etkinlik günlüğü: gün / seviye / kaynak / metin filtresi, sayfalama.
 * MVC uçları: /Admin/Dashboard/{Istatistik, Kullanicilar, Yasakla/{id}, YasagiKaldir/{id}, HataBildirimleri, HataDurumu/{id}, Etkinlikler}.
 * Tüm istekler TK.api ile. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var charts = {};
    var lastStats = null;
    var loaded = {};
    var reportsPage = 1;
    var logsPage = 1;
    var LEVELS = [
        { key: 'Information', css: 'info', label: 'Bilgi', color: '--tk-info' },
        { key: 'Warning', css: 'warning', label: 'Uyarı', color: '--tk-warning' },
        { key: 'Error', css: 'error', label: 'Hata', color: '--tk-danger' },
        { key: 'Critical', css: 'critical', label: 'Kritik', color: '--tk-accent-press' }
    ];
    var STATUS = { New: 'Yeni', Seen: 'Görüldü', Resolved: 'Çözüldü' };
    var STATUS_BADGE = { New: 'is-danger', Seen: 'is-warning', Resolved: 'is-success' };
    var SOURCES = { Web: 'Web', Agent: 'Ajan', Installer: 'Kurulum', Api: 'API' };

    function css(name) { return window.getComputedStyle(document.documentElement).getPropertyValue(name).trim(); }

    function levelInfo(level) {
        var l = String(level || '').toLowerCase();
        for (var i = 0; i < LEVELS.length; i++) { if (LEVELS[i].key.toLowerCase() === l) { return LEVELS[i]; } }
        return LEVELS[0];
    }

    function shortDate(d) {
        var dt = new Date(d + 'T00:00:00');
        return isNaN(dt) ? d : dt.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
    }

    function num(v) { var n = Number(v || 0); return isFinite(n) ? n : 0; }

    function fmtInt(v) { return num(v).toLocaleString('tr-TR'); }

    /* ------------------------------------------------------------ Grafikler */

    function tooltip(money) {
        return {
            backgroundColor: css('--tk-surface'), titleColor: css('--tk-text'), bodyColor: css('--tk-text-soft'),
            borderColor: css('--tk-border'), borderWidth: 1, padding: 10, displayColors: false,
            callbacks: {
                label: function (c) {
                    var v = c.parsed && typeof c.parsed === 'object' ? c.parsed.y : c.parsed;
                    return money ? U.money(v) : fmtInt(v) + (c.chart.config.type === 'doughnut' ? ' kayıt' : ' video');
                }
            }
        };
    }

    function axes(money) {
        var grid = css('--tk-divider');
        var ink = css('--tk-text-muted');
        return {
            x: { grid: { display: false }, border: { color: grid }, ticks: { color: ink, maxRotation: 0, autoSkip: true, autoSkipPadding: 14 } },
            y: {
                beginAtZero: true, grid: { color: grid, drawTicks: false }, border: { display: false },
                ticks: { color: ink, padding: 6, precision: money ? undefined : 0, callback: money ? function (v) { return '$' + v; } : undefined }
            }
        };
    }

    function draw(id, config) {
        if (!window.Chart) { return; }
        if (charts[id]) { charts[id].destroy(); }
        var canvas = document.getElementById(id);
        if (canvas) { charts[id] = new window.Chart(canvas, config); }
    }

    function drawCharts(s) {
        var videos = s.videosPerDay || [];
        var spend = s.spendPerDayUsd || [];
        var accent = css('--tk-accent');

        draw('ad-chart-videos', {
            type: 'line',
            data: {
                labels: $.map(videos, function (d) { return shortDate(d.date); }),
                datasets: [{
                    data: $.map(videos, function (d) { return num(d.value); }),
                    borderColor: accent, backgroundColor: css('--tk-accent-softer'), fill: true, tension: 0.3,
                    borderWidth: 2, pointRadius: 0, pointHoverRadius: 5, pointHitRadius: 12,
                    pointBackgroundColor: accent, pointBorderColor: css('--tk-surface'), pointBorderWidth: 2
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false, animation: { duration: 300 },
                interaction: { mode: 'index', intersect: false },
                plugins: { legend: { display: false }, tooltip: tooltip(false) },
                scales: axes(false)
            }
        });

        draw('ad-chart-spend', {
            type: 'bar',
            data: {
                labels: $.map(spend, function (d) { return shortDate(d.date); }),
                datasets: [{
                    data: $.map(spend, function (d) { return num(d.value); }),
                    backgroundColor: css('--tk-brown'), hoverBackgroundColor: accent,
                    borderRadius: 4, borderSkipped: 'start', maxBarThickness: 22, categoryPercentage: 0.8, barPercentage: 0.9
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false, animation: { duration: 300 },
                plugins: { legend: { display: false }, tooltip: tooltip(true) },
                scales: axes(true)
            }
        });

        var byLevel = {};
        $.each(s.activityByLevel || [], function (_, l) { byLevel[levelInfo(l.level).key] = num(l.count); });
        var counts = $.map(LEVELS, function (l) { return byLevel[l.key] || 0; });
        var total = counts.reduce(function (a, b) { return a + b; }, 0);
        $('#ad-activity-30').text(fmtInt(total));
        draw('ad-chart-levels', {
            type: 'doughnut',
            data: {
                labels: $.map(LEVELS, function (l) { return l.label; }),
                datasets: [{
                    data: total ? counts : [1],
                    backgroundColor: total ? $.map(LEVELS, function (l) { return css(l.color); }) : [css('--tk-surface-3')],
                    borderColor: css('--tk-surface'), borderWidth: 2, hoverOffset: 4
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false, cutout: '68%', animation: { duration: 300 },
                plugins: { legend: { display: false }, tooltip: total ? tooltip(false) : { enabled: false } }
            }
        });
        $('#ad-levels-legend').html($.map(LEVELS, function (l, i) {
            return '<span><i style="background:' + css(l.color) + '"></i>' + l.label + ' <strong>' + fmtInt(counts[i]) + '</strong></span>';
        }).join(''));

        $('#ad-videos-30').text(fmtInt(videos.reduce(function (a, d) { return a + num(d.value); }, 0)));
        $('#ad-spend-30').text(U.money(spend.reduce(function (a, d) { return a + num(d.value); }, 0)));
    }

    function renderStats(s) {
        lastStats = s || {};
        $('#ad-users').text(fmtInt(lastStats.userCount));
        $('#ad-concepts').text(fmtInt(lastStats.totalConcepts));
        $('#ad-videos').text(fmtInt(lastStats.totalVideos));
        $('#ad-spend').text(U.money(lastStats.monthSpendUsd));
        var open = num(lastStats.openErrorReports);
        $('#ad-errors').text(fmtInt(open));
        $('#ad-errors-tile').toggleClass('is-alert', open > 0);
        $('#ad-open-badge').text(open).prop('hidden', open === 0);
        $('#ad-generated').text(lastStats.generatedAt ? 'Güncellendi: ' + U.time(lastStats.generatedAt) : '');
        var failed = lastStats.failedContributors || [];
        $('#ad-failed').prop('hidden', !failed.length).find('span')
            .text(failed.length ? 'Bazı modüller istatistik veremedi, değerleri eksik olabilir: ' + failed.join(', ') : '');
        drawCharts(lastStats);
    }

    function loadStats() {
        return TK.api.get('/Admin/Dashboard/Istatistik', null, { silent: true }).done(renderStats).fail(function (xhr) {
            renderStats({});
            $('#ad-failed').prop('hidden', false).find('span').text('İstatistikler alınamadı: ' + TK.api.errorMessage(xhr));
        });
    }

    /* ------------------------------------------------------------ Kullanıcılar */

    function renderUsers(users) {
        if (!users.length) {
            $('#ad-users-list').html(U.empty('user', 'Kullanıcı bulunamadı', 'Kimlik modülü bağlı değilse liste boş görünür.'));
            return;
        }
        $('#ad-users-list').html('<table class="tk-table tk-ad-table"><thead><tr><th>Kullanıcı</th><th>Rol</th><th>Durum</th><th>Kayıt</th><th>Son giriş</th><th></th></tr></thead><tbody>' +
            $.map(users, function (u) {
                var name = u.userName || '—';
                return '<tr class="' + (u.isBanned ? 'tk-ad-row-muted' : '') + '">' +
                    '<td><span class="tk-ad-user"><span class="tk-avatar">' + U.escape(name.charAt(0).toLocaleUpperCase('tr-TR')) + '</span>' + U.escape(name) + '</span></td>' +
                    '<td>' + (u.isAdmin ? '<span class="tk-badge is-accent">Yönetici</span>' : '<span class="tk-badge">Kullanıcı</span>') + '</td>' +
                    '<td>' + (u.isBanned ? '<span class="tk-badge is-danger">Yasaklı</span>' : '<span class="tk-badge is-success">Etkin</span>') + '</td>' +
                    '<td class="tk-ad-time">' + U.escape(U.dateTime(u.createdAt) || '—') + '</td>' +
                    '<td class="tk-ad-time">' + U.escape(U.dateTime(u.lastLoginAt) || '—') + '</td>' +
                    '<td class="tk-ad-actions">' + (u.isBanned
                        ? '<button type="button" class="tk-btn tk-btn-secondary tk-btn-sm" data-unban="' + U.escape(u.id) + '">' + U.icon('check', 'tk-ic-sm') + ' Yasağı kaldır</button>'
                        : '<button type="button" class="tk-btn tk-btn-danger tk-btn-sm" data-ban="' + U.escape(u.id) + '" data-name="' + U.escape(name) + '">' + U.icon('lock', 'tk-ic-sm') + ' Yasakla</button>') +
                    '</td></tr>';
            }).join('') + '</tbody></table>');
    }

    function loadUsers() {
        $('#ad-users-list').html('<div class="tk-card-body">' + U.skeleton(3) + '</div>');
        return TK.api.get('/Admin/Dashboard/Kullanicilar', null, { silent: true })
            .done(function (d) { renderUsers(U.items(d)); })
            .fail(function (xhr) { $('#ad-users-list').html(U.empty('alert', 'Kullanıcılar alınamadı', TK.api.errorMessage(xhr))); });
    }

    /* ------------------------------------------------------------ Hata bildirimleri */

    function pager($el, page, onGo) {
        var p = page || {};
        var pages = p.totalPages || 0;
        if (pages <= 1) { $el.prop('hidden', true).empty(); return; }
        var $prev = $('<button type="button" class="tk-btn tk-btn-ghost tk-btn-sm">‹ Önceki</button>').prop('disabled', p.page <= 1);
        var $next = $('<button type="button" class="tk-btn tk-btn-ghost tk-btn-sm">Sonraki ›</button>').prop('disabled', p.page >= pages);
        $prev.on('click', function () { onGo(p.page - 1); });
        $next.on('click', function () { onGo(p.page + 1); });
        $el.prop('hidden', false).empty().append(
            $('<span></span>').text('Sayfa ' + p.page + ' / ' + pages + ' · ' + fmtInt(p.totalCount) + ' kayıt'),
            $('<div class="tk-row"></div>').append($prev, $next));
    }

    function statusSelect(r) {
        var html = '<select class="tk-select" data-report="' + U.escape(r.id) + '" aria-label="Durum">';
        $.each(STATUS, function (key, label) {
            html += '<option value="' + key + '"' + (r.status === key ? ' selected' : '') + '>' + label + '</option>';
        });
        return html + '</select>';
    }

    function renderReports(page) {
        var items = U.items(page);
        if (!items.length) {
            $('#ad-reports').html(U.empty('check', 'Bildirim yok', 'Bu durumda hata bildirimi bulunmuyor.'));
            pager($('#ad-reports-pager'), null);
            return;
        }
        $('#ad-reports').html('<table class="tk-table tk-ad-table"><thead><tr><th>Tarih</th><th>Durum</th><th>Bildirim</th><th>Kaynak</th><th>Ortam</th><th></th></tr></thead><tbody>' +
            $.map(items, function (r) {
                var env = [r.appVersion ? 'v' + r.appVersion : '', r.machine || '', r.userAgent ? r.userAgent.slice(0, 60) : ''].filter(Boolean).join(' · ');
                return '<tr>' +
                    '<td class="tk-ad-time">' + U.escape(U.dateTime(r.createdAt)) + '</td>' +
                    '<td><span class="tk-badge ' + (STATUS_BADGE[r.status] || '') + '">' + U.escape(STATUS[r.status] || r.status) + '</span></td>' +
                    '<td class="tk-ad-msg"><strong>' + U.escape(r.message) + '</strong>' +
                    (r.pageUrl ? '<span class="tk-ad-sub"><code>' + U.escape(r.pageUrl) + '</code></span>' : '') +
                    (r.details ? '<details class="tk-log-details"><summary>Ayrıntı</summary><pre>' + U.escape(r.details) + '</pre></details>' : '') + '</td>' +
                    '<td>' + U.escape(SOURCES[r.source] || r.source) + '</td>' +
                    '<td><span class="tk-ad-sub" style="margin:0">' + U.escape(env || '—') + '</span></td>' +
                    '<td class="tk-ad-actions">' + statusSelect(r) + '</td></tr>';
            }).join('') + '</tbody></table>');
        pager($('#ad-reports-pager'), page, function (p) { reportsPage = p; loadReports(); });
    }

    function loadReports() {
        $('#ad-reports').html('<div class="tk-card-body">' + U.skeleton(3) + '</div>');
        return TK.api.get('/Admin/Dashboard/HataBildirimleri', { status: $('#ad-report-status').val() || undefined, page: reportsPage }, { silent: true })
            .done(renderReports)
            .fail(function (xhr) { $('#ad-reports').html(U.empty('alert', 'Bildirimler alınamadı', TK.api.errorMessage(xhr))); });
    }

    /* ------------------------------------------------------------ Etkinlik günlüğü */

    function renderLogs(page) {
        var items = U.items(page);
        if (!items.length) {
            $('#ad-logs').html(U.empty('list', 'Kayıt yok', 'Bu filtrelerle eşleşen günlük kaydı bulunamadı.'));
            pager($('#ad-logs-pager'), null);
            return;
        }
        $('#ad-logs').html('<table class="tk-table tk-ad-table"><thead><tr><th>Zaman</th><th>Seviye</th><th>Kaynak</th><th>Mesaj</th><th>Süre</th></tr></thead><tbody>' +
            $.map(items, function (l) {
                var info = levelInfo(l.level);
                var details = l.details ? (function () { try { return JSON.stringify(JSON.parse(l.details), null, 2); } catch (e) { return l.details; } })() : '';
                return '<tr>' +
                    '<td class="tk-ad-time">' + U.escape(new Date(l.createdAt).toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit', second: '2-digit' })) + '</td>' +
                    '<td><span class="tk-level is-' + info.css + '">' + info.label + '</span></td>' +
                    '<td>' + U.escape(l.source) + '</td>' +
                    '<td class="tk-ad-msg">' + U.escape(l.message) +
                    (details ? '<details class="tk-log-details"><summary>Ayrıntı</summary><pre>' + U.escape(details) + '</pre></details>' : '') + '</td>' +
                    '<td class="tk-ad-time">' + (l.durationMs ? U.escape((l.durationMs / 1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' sn') : '—') + '</td></tr>';
            }).join('') + '</tbody></table>');
        pager($('#ad-logs-pager'), page, function (p) { logsPage = p; loadLogs(); });
    }

    function loadLogs() {
        $('#ad-logs').html('<div class="tk-card-body">' + U.skeleton(4) + '</div>');
        return TK.api.get('/Admin/Dashboard/Etkinlikler', {
            day: $('#ad-log-day').val() || undefined,
            level: $('#ad-log-level').val() || undefined,
            source: $('#ad-log-source').val() || undefined,
            search: $.trim($('#ad-log-search').val()) || undefined,
            page: logsPage
        }, { silent: true })
            .done(renderLogs)
            .fail(function (xhr) { $('#ad-logs').html(U.empty('alert', 'Günlük alınamadı', TK.api.errorMessage(xhr))); });
    }

    /* ------------------------------------------------------------ Sekmeler */

    var LOADERS = { genel: loadStats, kullanicilar: loadUsers, hatalar: loadReports, gunluk: loadLogs };

    function showTab(name, force) {
        if (!LOADERS[name]) { name = 'genel'; }
        $('#tk-admin .tk-tab').each(function () {
            var on = $(this).data('tab') === name;
            $(this).toggleClass('is-active', on).attr('aria-selected', on);
        });
        $('#tk-admin .tk-tab-panel').each(function () { $(this).toggleClass('is-active', $(this).data('panel') === name); });
        if (window.history && window.history.replaceState) { window.history.replaceState(null, '', '#' + name); }
        if (force || !loaded[name]) { loaded[name] = true; LOADERS[name](); }
        if (name === 'genel' && lastStats) { drawCharts(lastStats); }
    }

    function currentTab() { return $('#tk-admin .tk-tab.is-active').data('tab') || 'genel'; }

    function bind() {
        $('#tk-admin').on('click', '.tk-tab', function () { showTab($(this).data('tab')); });
        $('#ad-refresh').on('click', function () {
            var tab = currentTab();
            loaded = {};
            loaded[tab] = true;
            LOADERS[tab]();
            if (tab !== 'genel') { loadStats(); loaded.genel = true; }
        });

        $('#ad-users-list').on('click', '[data-ban]', function () {
            var $b = $(this);
            TK.modal.confirm('Kullanıcıyı yasakla', '"' + $b.data('name') + '" yasaklansın mı? Tüm oturumları (masaüstü ajanı dahil) kapatılır.', 'Yasakla')
                .done(function () {
                    U.busy($b, TK.api.post('/Admin/Dashboard/Yasakla/' + encodeURIComponent($b.data('ban'))))
                        .done(function () { TK.toast.success('Kullanıcı yasaklandı.'); loadUsers(); });
                });
        });
        $('#ad-users-list').on('click', '[data-unban]', function () {
            var $b = $(this);
            U.busy($b, TK.api.post('/Admin/Dashboard/YasagiKaldir/' + encodeURIComponent($b.data('unban'))))
                .done(function () { TK.toast.success('Yasak kaldırıldı.'); loadUsers(); });
        });

        $('#ad-report-status').on('change', function () { reportsPage = 1; loadReports(); });
        $('#ad-reports').on('change', 'select[data-report]', function () {
            var $s = $(this);
            var previous = $s.find('option[selected]').val();
            $s.prop('disabled', true);
            TK.api.put('/Admin/Dashboard/HataDurumu/' + encodeURIComponent($s.data('report')), { status: $s.val() })
                .done(function () { TK.toast.success('Durum güncellendi: ' + STATUS[$s.val()]); loadReports(); loadStats(); })
                .fail(function () { $s.val(previous); })
                .always(function () { $s.prop('disabled', false); });
        });

        var searchTimer = null;
        $('#ad-log-day, #ad-log-level, #ad-log-source').on('change', function () { logsPage = 1; loadLogs(); });
        $('#ad-log-search').on('input', function () {
            window.clearTimeout(searchTimer);
            searchTimer = window.setTimeout(function () { logsPage = 1; loadLogs(); }, 350);
        });
        $('#ad-log-filters').on('submit', function (e) { e.preventDefault(); logsPage = 1; loadLogs(); });
        $('#ad-log-clear').on('click', function () {
            $('#ad-log-day, #ad-log-search').val('');
            $('#ad-log-level, #ad-log-source').val('');
            logsPage = 1;
            loadLogs();
        });

        document.addEventListener('tk:theme', function () { if (lastStats) { drawCharts(lastStats); } });
    }

    function init() {
        if (!$('#tk-admin').length) { return; }
        U = TK.util;
        bind();
        var initial = (window.location.hash || '').replace('#', '');
        showTab(LOADERS[initial] ? initial : 'genel');
        if (initial && initial !== 'genel') { loaded.genel = true; loadStats(); }
    }

    TK.adminDashboard = { init: init, refresh: function () { showTab(currentTab(), true); } };
    $(init);
})(window, window.jQuery);
