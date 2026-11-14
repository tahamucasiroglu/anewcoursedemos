/**
 * TK.journal — alttaki ince "Günlük" şeridi. /Gunluk/Son adresini yoklar (TK.poll, 3 sn; afterId ile yalnızca yeniler),
 * son kaydı şeritte renkli gösterir, tıklayınca son 50 kaydın listesi açılır (en yeni üstte; kaynak, süre, ayrıntı).
 * Renkler: hata/kritik kırmızı, uyarı sarı, bilgi nötr. Cevap: [{ id, level, source, message, details, durationMs, createdAt }]
 * (ya da { items: [...] }); tarihe göre sıralanır, kimliğe göre tekilleştirilir.
 * Genel API: TK.journal.push(level, message) — yalnızca bu sayfada yerel satır; TK.journal.log(level, message, details) —
 * sunucuya yazar (/Gunluk/Kaydet) ve şeridi hemen yeniler; TK.journal.recent(n) — son n kayıt (hata bildirimine eklemek için).
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var MAX = 50;
    var LABELS = { info: 'Bilgi', warning: 'Uyarı', error: 'Hata', critical: 'Kritik' };
    var SERVER_LEVELS = { info: 'Information', warning: 'Warning', error: 'Error', critical: 'Critical' };
    var entries = [];
    var seen = {};
    var lastId = null;
    var poller = null;

    function levelKey(level) {
        var l = String(level === undefined || level === null ? 'info' : level).toLowerCase();
        if (l === '4' || l === '5' || l.indexOf('crit') === 0 || l.indexOf('fatal') === 0) { return 'critical'; }
        if (l === '3' || l.indexOf('err') === 0) { return 'error'; }
        if (l === '2' || l.indexOf('warn') === 0) { return 'warning'; }
        return 'info';
    }

    function isToday(value) {
        var d = new Date(value);
        var now = new Date();
        return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate();
    }

    function stamp(value) {
        return isToday(value) ? TK.util.time(value) : TK.util.dateTime(value);
    }

    function duration(ms) {
        var n = Number(ms);
        if (!isFinite(n) || n <= 0) { return ''; }
        if (n < 1000) { return n + ' ms'; }
        var s = Math.round(n / 1000);
        if (s < 60) { return s + ' sn'; }
        var m = Math.floor(s / 60);
        if (m < 60) { return m + ' dk' + (s % 60 ? ' ' + (s % 60) + ' sn' : ''); }
        return Math.floor(m / 60) + ' sa' + (m % 60 ? ' ' + (m % 60) + ' dk' : '');
    }

    function prettyDetails(details) {
        if (details === undefined || details === null || details === '') { return ''; }
        var text = typeof details === 'string' ? details : JSON.stringify(details);
        try { text = JSON.stringify(JSON.parse(text), null, 2); } catch (e) { /* düz metin olarak kalsın */ }
        return text;
    }

    function rowHtml(e) {
        var k = levelKey(e.level);
        var meta = '';
        if (e.durationMs) { meta += '<span class="tk-badge">' + TK.util.escape(duration(e.durationMs)) + '</span>'; }
        var details = prettyDetails(e.details);
        return '<div class="tk-log is-' + k + '">' +
            '<span class="tk-log-time" title="' + TK.util.escape(TK.util.dateTime(e.createdAt)) + '">' + TK.util.escape(stamp(e.createdAt)) + '</span>' +
            '<span class="tk-log-level">' + LABELS[k] + '</span>' +
            '<span class="tk-log-msg">' + (e.source ? '<strong>' + TK.util.escape(e.source) + ':</strong> ' : '') +
            TK.util.escape(e.message) + (meta ? ' ' + meta : '') +
            (details ? '<details class="tk-log-details"><summary>Ayrıntı</summary><pre>' + TK.util.escape(details) + '</pre></details>' : '') +
            '</span></div>';
    }

    function render() {
        var $strip = $('#tk-journal');
        var $panel = $('#tk-journal-panel');
        var latest = entries[entries.length - 1];
        var $ticker = $strip.find('.tk-journal-ticker');
        if (!latest) {
            $ticker.html('<span class="tk-muted">Henüz işlem yok.</span>');
        } else {
            var lk = levelKey(latest.level);
            $ticker.html('<span class="tk-log-time">' + TK.util.escape(stamp(latest.createdAt)) + '</span> &nbsp;' +
                (latest.source ? '<span class="tk-muted">' + TK.util.escape(latest.source) + ' ·</span> ' : '') +
                '<span class="tk-log-inline is-' + lk + '">' + TK.util.escape(latest.message) + '</span>');
        }
        var problems = $.grep(entries, function (e) { var k = levelKey(e.level); return k === 'error' || k === 'critical'; }).length;
        $strip.find('.tk-journal-count').html(entries.length
            ? (problems ? '<span class="tk-badge is-danger">' + problems + ' hata</span> ' : '') + entries.length + ' kayıt'
            : '');

        if ($panel.hasClass('is-open')) {
            var html = '';
            for (var i = entries.length - 1; i >= 0; i--) { html += rowHtml(entries[i]); }
            $panel.html(html
                ? '<div class="tk-journal-head"><strong>Son ' + entries.length + ' işlem</strong><span class="tk-hint">En yeni üstte · kapatmak için şeride tıklayın</span></div>' + html
                : TK.util.empty('list', 'Günlük boş', 'İşlemler başladıkça burada akacak.'));
        }
    }

    function merge(data) {
        var list = TK.util.items(data);
        var added = false;
        $.each(list, function (_, e) {
            if (!e) { return; }
            var key = e.id !== undefined && e.id !== null ? String(e.id) : (e.createdAt + '|' + e.message);
            if (seen[key]) { return; }
            seen[key] = true;
            entries.push(e);
            added = true;
            if (e.id !== undefined && e.id !== null && String(e.id).indexOf('local-') !== 0) { lastId = e.id; }
        });
        if (added) {
            entries.sort(function (a, b) { return new Date(a.createdAt) - new Date(b.createdAt); });
            if (entries.length > MAX) { entries = entries.slice(entries.length - MAX); }
        }
        return added;
    }

    function init() {
        var $strip = $('#tk-journal');
        var $panel = $('#tk-journal-panel');
        if (!$strip.length) { return; }

        $strip.on('click keydown', function (e) {
            if (e.type === 'keydown' && e.key !== 'Enter' && e.key !== ' ') { return; }
            e.preventDefault();
            $panel.toggleClass('is-open');
            $strip.attr('aria-expanded', $panel.hasClass('is-open'));
            render();
        });
        $(document).on('mousedown', function (e) {
            if ($panel.hasClass('is-open') && !$(e.target).closest('#tk-journal, #tk-journal-panel').length) {
                $panel.removeClass('is-open');
                $strip.attr('aria-expanded', false);
            }
        });

        render();
        poller = TK.poll('/Gunluk/Son', 3000, function (data) {
            $strip.removeClass('is-offline').addClass('is-live');
            if (merge(data)) { render(); }
        }, {
            query: function () { return lastId !== null ? { afterId: lastId } : {}; },
            onError: function () {
                $strip.removeClass('is-live').addClass('is-offline');
                if (!entries.length) { $strip.find('.tk-journal-ticker').html('<span class="tk-muted">Günlük şu an alınamıyor; yeniden denenecek.</span>'); }
            }
        });
        TK.journal.poller = poller;
    }

    TK.journal = {
        init: init,
        /** Yalnızca bu sayfada görünen yerel satır ekler (sunucuya gitmez). */
        push: function (level, message) {
            merge([{ id: 'local-' + Date.now() + '-' + Math.random(), level: level, message: message, source: 'Web', createdAt: new Date().toISOString() }]);
            render();
        },
        /** Olayı sunucu günlüğüne yazar (kalıcı); başarılıysa şerit hemen yenilenir. jqXHR promise döner. */
        log: function (level, message, details) {
            var body = { level: SERVER_LEVELS[levelKey(level)], source: 'Web', message: String(message || '') };
            if (details !== undefined && details !== null) { body.details = details; }
            return TK.api.post('/Gunluk/Kaydet', body, { silent: true }).done(function () { if (poller) { poller.now(); } });
        },
        /** Son n kaydı (eskiden yeniye) düz metin satırları olarak döner. */
        recent: function (n) {
            var list = entries.slice(Math.max(0, entries.length - (n || 20)));
            return $.map(list, function (e) {
                return TK.util.dateTime(e.createdAt) + ' [' + LABELS[levelKey(e.level)] + '] ' + (e.source ? e.source + ': ' : '') + e.message;
            });
        }
    };

    $(init);
})(window, window.jQuery);
