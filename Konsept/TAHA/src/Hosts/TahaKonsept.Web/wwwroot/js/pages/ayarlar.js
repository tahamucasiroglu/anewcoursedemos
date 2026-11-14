/**
 * TK.ayarlar — Ayarlar sayfası: okunuş sözlüğü (ekle, sil, dinle), önerileri sıfırla, tema. Bölüm menüsü kaydırmayla senkron.
 * Ayrı modüller: settings-agent.js (TK.settingsAgent, masaüstü programı), settings-providers.js (TK.settingsProviders,
 * API anahtarları + bütçe limiti + bu ayın harcaması). MVC uçları: /Ayarlar/{Sozluk, SozlukSil, SozlukDinle, OnerileriSifirla}.
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    /* ---------------- Okunuş sözlüğü ---------------- */
    function renderDict(items) {
        var $d = $('#st-dict');
        if (!items.length) { $d.html(U.empty('book', 'Sözlük boş', 'Yabancı özel isimleri ve kısaltmaları ekleyin; seslendirme bu okunuşu kullanır.')); return; }
        var html = '<table class="tk-table"><thead><tr><th>Yazılış</th><th>Okunuş</th><th>Kaynak</th><th></th></tr></thead><tbody>';
        $.each(items, function (_, e) {
            html += '<tr data-id="' + U.escape(e.id) + '" data-spoken="' + U.escape(e.spoken) + '"><td><strong>' + U.escape(e.written) + '</strong></td><td>' + U.escape(e.spoken) + '</td>' +
                '<td>' + (e.isSuggestion ? '<span class="tk-badge is-warning">Öneri</span>' : e.profileName ? '<span class="tk-badge">' + U.escape(e.profileName) + '</span>' : '<span class="tk-badge is-accent">Genel</span>') + '</td>' +
                '<td style="text-align:right;white-space:nowrap">' +
                (e.isSuggestion ? '<button type="button" class="tk-btn tk-btn-ghost tk-btn-sm" data-act="accept">Onayla</button>' : '') +
                '<button type="button" class="tk-mini-btn" data-act="listen" title="Dinle" aria-label="Dinle">' + U.icon('volume') + '</button>' +
                '<button type="button" class="tk-mini-btn is-danger" data-act="delete" title="Sil" aria-label="Sil">' + U.icon('trash') + '</button></td></tr>';
        });
        $d.html(html + '</tbody></table>');
    }

    function loadDict() {
        TK.api.get('/Ayarlar/Sozluk', null, { silent: true }).done(function (d) { renderDict(U.items(d)); })
            .fail(function () { $('#st-dict').html(U.empty('alert', 'Sözlük alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function listen(text) {
        if (!text) { TK.toast.warning('Dinlemek için okunuş yazın.'); return; }
        TK.api.post('/Ayarlar/SozlukDinle', { text: text }).done(function (res) {
            if (res && res.audioUrl) { try { new window.Audio(res.audioUrl).play(); } catch (e) { TK.toast.error('Ses çalınamadı.'); } }
            else { TK.toast.info('Önizleme masaüstü programında çalınıyor.'); }
        });
    }

    function syncNav() {
        var $links = $('.tk-settings-nav a');
        var top = $(window).scrollTop() + 120;
        var current = null;
        $('.tk-settings-sections > section').each(function () { if ($(this).offset().top <= top) { current = this.id; } });
        if (current) { $links.removeClass('is-active').filter('[href="#' + current + '"]').addClass('is-active'); }
    }

    function bind() {
        $('#st-dict-form').on('submit', function (e) {
            e.preventDefault();
            var written = $.trim($(this).find('[name=written]').val());
            var spoken = $.trim($(this).find('[name=spoken]').val());
            if (!written || !spoken) { TK.toast.warning('Yazılış ve okunuş gerekli.'); return; }
            var $form = $(this);
            U.busy($form.find('[type=submit]'), TK.api.post('/Ayarlar/Sozluk', { written: written, spoken: spoken })).done(function () {
                $form[0].reset();
                $form.find('[name=written]').trigger('focus');
                loadDict();
            });
        });
        $('#st-dict-listen').on('click', function () { listen($.trim($('#st-dict-form [name=spoken]').val())); });
        $('#st-dict').on('click', '[data-act]', function () {
            var $tr = $(this).closest('tr');
            var act = $(this).attr('data-act');
            if (act === 'listen') { listen($tr.attr('data-spoken')); }
            if (act === 'delete') { TK.api.del('/Ayarlar/SozlukSil/' + encodeURIComponent($tr.attr('data-id'))).done(loadDict); }
            if (act === 'accept') {
                TK.api.post('/Ayarlar/Sozluk', { id: $tr.attr('data-id'), written: $tr.find('td:first').text(), spoken: $tr.attr('data-spoken'), isSuggestion: false }).done(loadDict);
            }
        });

        $('#st-reset').on('click', function () {
            var $btn = $(this);
            TK.modal.confirm('Önerileri sıfırla', 'Konu önerisi hafızası temizlenecek. Devam edilsin mi?', 'Sıfırla').done(function () {
                U.busy($btn, TK.api.post('/Ayarlar/OnerileriSifirla')).done(function () { TK.toast.success('Öneri hafızası sıfırlandı.'); });
            });
        });

        $('input[name=theme][value="' + (TK.theme ? TK.theme.get() : 'system') + '"]').prop('checked', true);
        $('input[name=theme]').on('change', function () { TK.theme.set($(this).val()); });
        document.addEventListener('tk:theme', function (e) { if (e.detail) { $('input[name=theme][value="' + e.detail.preference + '"]').prop('checked', true); } });

        $(window).on('scroll', syncNav);
    }

    function init() {
        if (!$('#tk-ayarlar').length) { return; }
        U = TK.util;
        $('#st-dict').html(U.skeleton(2));
        bind();
        loadDict();
        if (window.location.hash) { syncNav(); }
    }

    TK.ayarlar = { init: init };
    $(init);
})(window, window.jQuery);
