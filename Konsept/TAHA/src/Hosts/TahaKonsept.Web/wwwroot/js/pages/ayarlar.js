/**
 * TK.ayarlar — Ayarlar sayfası: API anahtarları (ekle/değiştir/sil; maskeli gösterim), okunuş sözlüğü (ekle, sil, dinle),
 * önerileri sıfırla, tema, bütçe limiti. Masaüstü programı bölümü ayrı modüldedir: settings-agent.js (TK.settingsAgent).
 * Bölüm menüsü kaydırmayla senkron. MVC uçları: /Ayarlar/{ApiAnahtarlari, ApiAnahtari, ApiAnahtariSil, Sozluk, SozlukSil,
 * SozlukDinle, OnerileriSifirla, Butce}. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    var PLATFORMS = [
        ['Anthropic', 'Anthropic (Claude)', 'Konsept, video kodu, konu önerisi'],
        ['OpenAI', 'OpenAI', 'LLM, gpt-image, gpt-4o-mini-tts'],
        ['OpenRouter', 'OpenRouter', 'Çok sağlayıcılı LLM'],
        ['GLM', 'Zhipu GLM', 'LLM, GLM-Image / CogView'],
        ['Kimi', 'Moonshot Kimi', 'LLM'],
        ['Google', 'Google Gemini', 'Nano Banana, Gemini TTS'],
        ['ElevenLabs', 'ElevenLabs', 'Online ses'],
        ['Azure', 'Azure Speech', 'Neural TTS'],
        ['Cartesia', 'Cartesia', 'Online ses'],
        ['Fal', 'fal.ai', 'Online görsel'],
        ['BFL', 'Black Forest Labs', 'FLUX görsel'],
        ['Replicate', 'Replicate', 'Online görsel']
    ];

    /* ---------------- API anahtarları ---------------- */
    function renderKeys(saved) {
        var map = {};
        $.each(saved || [], function (_, k) { map[String(k.platform || k.platformName).toLowerCase()] = k; });
        var html = '<table class="tk-table"><thead><tr><th>Sağlayıcı</th><th>Durum</th><th style="width:46%">Anahtar</th><th></th></tr></thead><tbody>';
        $.each(PLATFORMS, function (_, p) {
            var k = map[p[0].toLowerCase()];
            html += '<tr data-platform="' + p[0] + '"><td><strong>' + U.escape(p[1]) + '</strong><div class="tk-hint">' + U.escape(p[2]) + '</div></td>' +
                '<td>' + (k ? '<span class="tk-badge is-success">' + U.icon('check', 'tk-ic-sm') + ' ••••' + U.escape(k.last4 || k.mask || '') + '</span>' : '<span class="tk-badge">Yok</span>') + '</td>' +
                '<td><input type="password" class="tk-input" autocomplete="off" spellcheck="false" placeholder="' + (k ? 'Değiştirmek için yeni anahtar' : 'Anahtarı yapıştırın') + '" aria-label="' + U.escape(p[1]) + ' anahtarı" /></td>' +
                '<td style="text-align:right;white-space:nowrap"><button type="button" class="tk-btn tk-btn-secondary tk-btn-sm" data-act="save">Kaydet</button>' +
                (k ? ' <button type="button" class="tk-mini-btn is-danger" data-act="delete" title="Sil" aria-label="Sil">' + U.icon('trash') + '</button>' : '') + '</td></tr>';
        });
        $('#st-keys').html(html + '</tbody></table>');
    }

    function loadKeys() {
        TK.api.get('/Ayarlar/ApiAnahtarlari', null, { silent: true }).done(function (d) { renderKeys(U.items(d)); }).fail(function () { renderKeys([]); });
    }

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

    /* ---------------- Bütçe ---------------- */
    function loadBudget() {
        TK.api.get('/Ayarlar/Butce', null, { silent: true }).done(function (b) {
            if (!b) { return; }
            $('#st-budget').val(b.monthlyLimitUsd || '');
            $('#st-budget-form [name=stopOnExceed]').prop('checked', b.stopOnExceed !== false);
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
        $('#st-keys').on('click', '[data-act]', function () {
            var $tr = $(this).closest('tr');
            var platform = $tr.attr('data-platform');
            if ($(this).attr('data-act') === 'delete') {
                TK.modal.confirm('Anahtarı sil', platform + ' anahtarı silinecek; bu sağlayıcının modelleri kullanılamaz.', 'Sil').done(function () {
                    TK.api.del('/Ayarlar/ApiAnahtariSil/' + encodeURIComponent(platform)).done(function () { TK.toast.info('Anahtar silindi.'); loadKeys(); });
                });
                return;
            }
            var key = $.trim($tr.find('input').val());
            if (key.length < 8) { TK.toast.warning('Geçerli bir anahtar girin.'); $tr.find('input').trigger('focus'); return; }
            U.busy($(this), TK.api.put('/Ayarlar/ApiAnahtari', { platform: platform, apiKey: key })).done(function () {
                TK.toast.success(platform + ' anahtarı kaydedildi.');
                loadKeys();
                if (TK.layout) { TK.layout.refreshSpend(); }
            });
        });

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

        $('#st-budget-form').on('submit', function (e) {
            e.preventDefault();
            var limit = parseFloat($('#st-budget').val());
            U.busy($(this).find('[type=submit]'), TK.api.put('/Ayarlar/Butce', {
                monthlyLimitUsd: isNaN(limit) || limit < 0 ? 0 : limit,
                stopOnExceed: $(this).find('[name=stopOnExceed]').is(':checked')
            })).done(function () { TK.toast.success('Bütçe limiti kaydedildi.'); if (TK.layout) { TK.layout.refreshSpend(); } });
        });

        $(window).on('scroll', syncNav);
    }

    function init() {
        if (!$('#tk-ayarlar').length) { return; }
        U = TK.util;
        $('#st-keys').html('<div style="padding:16px">' + U.skeleton(4) + '</div>');
        $('#st-dict').html(U.skeleton(2));
        bind();
        loadKeys();
        loadDict();
        loadBudget();
        if (window.location.hash) { syncNav(); }
    }

    TK.ayarlar = { init: init };
    $(init);
})(window, window.jQuery);
