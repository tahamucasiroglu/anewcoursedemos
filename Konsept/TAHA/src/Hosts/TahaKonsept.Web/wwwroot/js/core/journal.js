/**
 * TK.journal — alttaki ince "Günlük" şeridi. /Gunluk/Son adresini yoklar (TK.poll, 3 sn), son kaydı şeritte gösterir,
 * tıklayınca son 100 kaydın listesi açılır. Renkler: hata/kritik kırmızı, uyarı sarı, bilgi nötr.
 * Beklenen cevap: [{ id, level, message, source, createdAt }] ya da { items: [...] }; tarihe göre sıralanır.
 * TK.journal.push(level, message) sayfadan yerel kayıt ekler. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var MAX = 100;
    var LABELS = { info: 'Bilgi', warning: 'Uyarı', error: 'Hata', critical: 'Kritik' };
    var entries = [];
    var seen = {};
    var lastId = null;

    function levelKey(level) {
        var l = String(level === undefined || level === null ? 'info' : level).toLowerCase();
        if (l === '4' || l === '5' || l.indexOf('crit') === 0 || l.indexOf('fatal') === 0) { return 'critical'; }
        if (l === '3' || l.indexOf('err') === 0) { return 'error'; }
        if (l === '2' || l.indexOf('warn') === 0) { return 'warning'; }
        return 'info';
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
            $ticker.html('<span class="tk-log-time">' + TK.util.time(latest.createdAt) + '</span> &nbsp;' +
                '<span class="tk-log-inline is-' + lk + '">' + TK.util.escape(latest.message) + '</span>');
        }
        $strip.find('.tk-journal-count').text(entries.length ? entries.length + ' kayıt' : '');

        if ($panel.hasClass('is-open')) {
            var html = '';
            for (var i = entries.length - 1; i >= 0; i--) {
                var e = entries[i];
                var k = levelKey(e.level);
                html += '<div class="tk-log is-' + k + '"><span class="tk-log-time">' + TK.util.time(e.createdAt) + '</span>' +
                    '<span class="tk-log-level">' + LABELS[k] + '</span>' +
                    '<span class="tk-log-msg">' + (e.source ? '<strong>' + TK.util.escape(e.source) + ':</strong> ' : '') +
                    TK.util.escape(e.message) + '</span></div>';
            }
            $panel.html(html || TK.util.empty('list', 'Günlük boş', 'İşlemler başladıkça burada akacak.'));
        }
    }

    function merge(data) {
        var list = TK.util.items(data);
        var added = false;
        $.each(list, function (_, e) {
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
        TK.journal.poller = TK.poll('/Gunluk/Son', 3000, function (data) {
            $strip.removeClass('is-offline').addClass('is-live');
            if (merge(data)) { render(); }
        }, {
            query: function () { return lastId !== null ? { afterId: lastId } : {}; },
            onError: function () {
                $strip.removeClass('is-live').addClass('is-offline');
                if (!entries.length) { $strip.find('.tk-journal-ticker').html('<span class="tk-muted">Günlük şu an alınamıyor; yeniden denenecek.</span>'); }
            }
        });
    }

    TK.journal = {
        init: init,
        push: function (level, message) {
            merge([{ id: 'local-' + Date.now() + '-' + Math.random(), level: level, message: message, createdAt: new Date().toISOString() }]);
            render();
        }
    };

    $(init);
})(window, window.jQuery);
