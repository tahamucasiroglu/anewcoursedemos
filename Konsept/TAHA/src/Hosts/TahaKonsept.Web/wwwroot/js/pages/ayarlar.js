/**
 * TK.ayarlar — Ayarlar sayfası: önerileri sıfırla, tema. Bölüm menüsü kaydırmayla senkron.
 * Ayrı modüller: settings-pronunciation.js (TK.settingsPronunciation, okunuş sözlüğü), settings-agent.js (TK.settingsAgent,
 * masaüstü programı), settings-providers.js (TK.settingsProviders, API anahtarları + bütçe limiti + bu ayın harcaması).
 * MVC uçları: /Ayarlar/{OnerileriSifirla}.
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    function syncNav() {
        var $links = $('.tk-settings-nav a');
        var top = $(window).scrollTop() + 120;
        var current = null;
        $('.tk-settings-sections > section').each(function () { if ($(this).offset().top <= top) { current = this.id; } });
        if (current) { $links.removeClass('is-active').filter('[href="#' + current + '"]').addClass('is-active'); }
    }

    function bind() {
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
        bind();
        if (window.location.hash) { syncNav(); }
    }

    TK.ayarlar = { init: init };
    $(init);
})(window, window.jQuery);
