/**
 * TK.util — küçük ortak yardımcılar (HTML kaçışı, para/tarih biçimi, ikon, boş durum, düğme yükleniyor durumu).
 * Tek modül (IIFE). Diğer çekirdek modüller buna dayanır; jQuery'den hemen sonra yüklenir.
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    var usd = null;
    try { usd = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2 }); } catch (e) { usd = null; }

    function escape(value) {
        if (value === null || value === undefined) { return ''; }
        return String(value)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function money(value) {
        var n = Number(value || 0);
        if (!isFinite(n)) { n = 0; }
        return usd ? usd.format(n) : '$' + n.toFixed(2);
    }

    function time(value) {
        if (!value) { return ''; }
        var d = new Date(value);
        if (isNaN(d.getTime())) { return ''; }
        return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    }

    function dateTime(value) {
        if (!value) { return ''; }
        var d = new Date(value);
        if (isNaN(d.getTime())) { return ''; }
        return d.toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
    }

    function icon(name, cls) {
        return '<svg class="tk-ic ' + (cls || '') + '" aria-hidden="true"><use href="/img/icons.svg#i-' + name + '"></use></svg>';
    }

    function empty(iconName, title, text) {
        return '<div class="tk-empty"><span class="tk-empty-icon">' + icon(iconName, 'tk-ic-lg') + '</span>' +
            '<strong>' + escape(title) + '</strong>' + (text ? '<p>' + escape(text) + '</p>' : '') + '</div>';
    }

    function skeleton(count) {
        var html = '';
        for (var i = 0; i < (count || 3); i++) { html += '<div class="tk-skeleton"></div>'; }
        return html;
    }

    /** Düğmeyi istek süresince kilitler; promise bitince geri açar. */
    function busy($btn, promise) {
        $btn = $($btn);
        $btn.prop('disabled', true).addClass('is-loading');
        var done = function () { $btn.prop('disabled', false).removeClass('is-loading'); };
        if (promise && typeof promise.always === 'function') { promise.always(done); } else { done(); }
        return promise;
    }

    function clampInt(value, min, max, fallback) {
        var n = parseInt(value, 10);
        if (isNaN(n)) { return fallback; }
        return Math.max(min, Math.min(max, n));
    }

    /** Dizi ya da {items:[...]} biçimli sayfalı cevabı diziye çevirir. */
    function items(data) {
        if (!data) { return []; }
        if ($.isArray(data)) { return data; }
        if ($.isArray(data.items)) { return data.items; }
        if ($.isArray(data.value)) { return data.value; }
        return [];
    }

    TK.util = {
        escape: escape, money: money, time: time, dateTime: dateTime, icon: icon,
        empty: empty, skeleton: skeleton, busy: busy, clampInt: clampInt, items: items
    };
})(window, window.jQuery);
