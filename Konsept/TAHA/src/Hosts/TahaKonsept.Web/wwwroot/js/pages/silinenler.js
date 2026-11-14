/**
 * TK.silinenler — Silinenler sayfası: silinen konseptler tablosu (arama: başlık, özet, etiket), Geri al (Üretilenler'e döner)
 * ve Kalıcı sil (onay penceresiyle; geri alınamaz, konu hafızasındaki izi de silinir).
 * MVC uçları: /Silinenler/{Liste, GeriAl, KaliciSil}. Yalnız TK.api ($.ajax). Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var state = { items: [], filter: '' };

    function matches(c, f) {
        if (!f) { return true; }
        var hay = [c.title, c.summary].concat(c.tags || []).join(' ').toLocaleLowerCase('tr-TR');
        return hay.indexOf(f) >= 0;
    }

    function render() {
        var $l = $('#sl-list');
        var f = state.filter.toLocaleLowerCase('tr-TR');
        var list = $.grep(state.items, function (c) { return matches(c, f); });
        $('#sl-count').text(state.items.length ? state.items.length + ' konsept' : '');
        if (!list.length) {
            $l.html(state.items.length ? U.empty('list', 'Eşleşen kayıt yok', 'Aramayı değiştirip tekrar deneyin.') : U.empty('trash', 'Silinen konsept yok', 'Konseptler sayfasında 🗑 ile sildikleriniz burada görünür.'));
            return;
        }
        var html = '<table class="tk-table"><thead><tr><th>Başlık</th><th>Silinme</th><th>Ayrıntı</th><th style="text-align:right">İşlem</th></tr></thead><tbody>';
        $.each(list, function (_, c) {
            html += '<tr data-id="' + U.escape(c.id) + '" data-title="' + U.escape(c.title || '') + '"><td><strong>' + U.escape(c.title || 'Adsız') + '</strong>' +
                (c.summary ? '<div class="tk-hint">' + U.escape(c.summary) + '</div>' : '') + '</td>' +
                '<td class="tk-muted" style="white-space:nowrap">' + U.escape(U.dateTime(c.deletedAt || c.updatedAt)) + '</td>' +
                '<td style="white-space:nowrap"><span class="tk-badge">' + (c.sceneCount || 0) + ' sahne</span> <span class="tk-hint">' + U.escape(U.money(c.costUsd)) + '</span></td>' +
                '<td style="text-align:right;white-space:nowrap">' +
                '<button type="button" class="tk-btn tk-btn-ghost tk-btn-sm" data-act="restore">' + U.icon('restore', 'tk-ic-sm') + ' Geri al</button> ' +
                '<button type="button" class="tk-btn tk-btn-danger tk-btn-sm" data-act="purge">' + U.icon('trash', 'tk-ic-sm') + ' Kalıcı sil</button>' +
                '</td></tr>';
        });
        $l.html(html + '</tbody></table>');
    }

    function load() {
        return TK.api.get('/Silinenler/Liste', null, { silent: true })
            .done(function (d) { state.items = U.items(d); render(); })
            .fail(function () { $('#sl-list').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function restore(id, $btn) {
        U.busy($btn, TK.api.post('/Silinenler/GeriAl/' + encodeURIComponent(id))).done(function () {
            TK.toast.success('Konsept Üretilenler\'e geri alındı.');
            load();
        });
    }

    function purge(id, title) {
        TK.modal.confirm('Kalıcı olarak sil', '"' + title + '" tamamen silinecek ve geri alınamayacak. Benzer konular yeniden önerilebilir.', 'Kalıcı sil').done(function () {
            TK.api.del('/Silinenler/KaliciSil/' + encodeURIComponent(id)).done(function () { TK.toast.info('Kalıcı olarak silindi.'); load(); });
        });
    }

    function init() {
        if (!$('#tk-silinenler').length) { return; }
        U = TK.util;
        $('#sl-list').html('<div style="padding:16px">' + U.skeleton(5) + '</div>');
        $('#sl-search').on('input', function () { state.filter = $.trim($(this).val()); render(); });
        $('#sl-refresh').on('click', load);
        $('#sl-list').on('click', '[data-act]', function () {
            var $row = $(this).closest('tr');
            var id = $row.attr('data-id');
            if ($(this).attr('data-act') === 'restore') { restore(id, $(this)); } else { purge(id, $row.attr('data-title') || 'Bu konsept'); }
        });
        load();
    }

    TK.silinenler = { init: init };
    $(init);
})(window, window.jQuery);
