/**
 * TK.toast — sağ altta kısa bildirimler. TK.toast.show(mesaj, tür, süreMs); kısayollar: success/error/warning/info.
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var $host = null;

    function host() {
        if (!$host || !$host.length) {
            $host = $('<div class="tk-toasts" role="status" aria-live="polite"></div>').appendTo(document.body);
        }
        return $host;
    }

    function dismiss($t) {
        $t.addClass('is-leaving');
        window.setTimeout(function () { $t.remove(); }, 220);
    }

    function show(message, type, durationMs) {
        type = type || 'info';
        var $t = $('<div class="tk-toast"></div>').addClass('is-' + type);
        $('<div class="tk-toast-msg"></div>').text(message || '').appendTo($t);
        $('<button type="button" class="tk-toast-close" aria-label="Kapat">&times;</button>')
            .on('click', function () { dismiss($t); }).appendTo($t);
        host().append($t);
        if (host().children().length > 4) { dismiss(host().children().first()); }
        window.setTimeout(function () { dismiss($t); }, durationMs || (type === 'error' ? 6500 : 3800));
        return $t;
    }

    TK.toast = {
        show: show,
        success: function (m) { return show(m, 'success'); },
        error: function (m) { return show(m, 'error'); },
        warning: function (m) { return show(m, 'warning'); },
        info: function (m) { return show(m, 'info'); }
    };
})(window, window.jQuery);
