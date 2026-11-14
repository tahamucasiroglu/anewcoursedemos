/**
 * TK.layout — üst barın davranışları: aylık harcama rozeti (/Harcama/Aylik, 60 sn'de bir), profil seçimi
 * (/Profil/Liste, /Profil/Sec/{id}), açılır menüler, mobil menü, "Hata bildir" penceresi (/HataBildir/Gonder), çıkış.
 * Aktif profil değişince document üzerinde 'tk:profile' olayı tetiklenir. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var profiles = [];

    function loadSpend() {
        return TK.api.get('/Harcama/Aylik', null, { silent: true }).done(function (data) {
            var total = data ? (data.totalUsd !== undefined ? data.totalUsd : (data.total !== undefined ? data.total : data.amount)) : 0;
            var $badge = $('#tk-spend');
            $badge.find('strong').text(TK.util.money(total));
            var limit = data && data.limitUsd ? Number(data.limitUsd) : 0;
            $badge.toggleClass('is-over', limit > 0 && Number(total) >= limit);
            $badge.attr('title', limit > 0 ? 'Bu ayki harcama / limit: ' + TK.util.money(total) + ' / ' + TK.util.money(limit) : 'Bu ayki toplam harcama (LLM + ses + görsel)');
        });
    }

    function activeProfile() {
        var id = $('#tk-profile').val();
        for (var i = 0; i < profiles.length; i++) { if (String(profiles[i].id) === String(id)) { return profiles[i]; } }
        return null;
    }

    function renderProfiles(selectedId) {
        var $sel = $('#tk-profile').empty();
        if (!profiles.length) {
            $sel.append('<option value="">Profil yok — ➕ ile ekleyin</option>');
            return;
        }
        $.each(profiles, function (_, p) {
            var $o = $('<option></option>').val(p.id).text(p.name || p.channelName || 'Adsız profil');
            if ((selectedId && String(p.id) === String(selectedId)) || (!selectedId && p.isActive)) { $o.prop('selected', true); }
            $sel.append($o);
        });
    }

    function loadProfiles(selectedId) {
        return TK.api.get('/Profil/Liste', null, { silent: true }).done(function (data) {
            profiles = TK.util.items(data);
            renderProfiles(selectedId);
            $(document).trigger('tk:profile', [activeProfile()]);
        }).fail(function () {
            $('#tk-profile').empty().append('<option value="">Profiller yüklenemedi</option>');
        });
    }

    function openBugReport() {
        var $body = $(
            '<div>' +
            '<div class="tk-field"><label for="tk-bug-title">Kısa başlık</label><input id="tk-bug-title" class="tk-input" maxlength="160" placeholder="Ör. Konu önerisi boş liste döndü" /></div>' +
            '<div class="tk-field"><label for="tk-bug-desc">Ne oldu?</label><textarea id="tk-bug-desc" class="tk-textarea" rows="5" maxlength="4000" placeholder="Ne yapıyordunuz, ne bekliyordunuz, ne oldu?"></textarea></div>' +
            '<label class="tk-check"><input type="checkbox" id="tk-bug-logs" checked /> Son günlük kayıtlarını ekle</label>' +
            '</div>');
        var $send = $('<button type="button" class="tk-btn tk-btn-primary"></button>').html(TK.util.icon('send') + ' Gönder');
        var h = TK.modal.open({ title: 'Hata bildir', body: $body, footer: $send, hint: 'Bildirim yönetici paneline düşer.' });
        $send.on('click', function () {
            var title = $.trim($('#tk-bug-title').val());
            var desc = $.trim($('#tk-bug-desc').val());
            if (!title || !desc) { TK.toast.warning('Başlık ve açıklama gerekli.'); return; }
            TK.util.busy($send, TK.api.post('/HataBildir/Gonder', {
                title: title, description: desc, includeLogs: $('#tk-bug-logs').is(':checked'),
                page: window.location.pathname, userAgent: window.navigator.userAgent
            })).done(function () {
                TK.toast.success('Teşekkürler, bildiriminiz alındı.');
                h.close(true);
            });
        });
    }

    function bindDropdowns() {
        $(document).on('click', '[data-dropdown-toggle]', function (e) {
            e.preventDefault();
            var $dd = $(this).closest('.tk-dropdown');
            $('.tk-dropdown.is-open').not($dd).removeClass('is-open');
            $dd.toggleClass('is-open');
            $(this).attr('aria-expanded', $dd.hasClass('is-open'));
        });
        $(document).on('mousedown', function (e) {
            if (!$(e.target).closest('.tk-dropdown').length) { $('.tk-dropdown.is-open').removeClass('is-open'); }
        });
        $(document).on('keydown', function (e) { if (e.key === 'Escape') { $('.tk-dropdown.is-open').removeClass('is-open'); } });
    }

    function init() {
        if (!$('#tk-topbar').length) { return; }
        bindDropdowns();

        $('#tk-menu-toggle').on('click', function () {
            $('#tk-nav').toggleClass('is-open');
            $(this).attr('aria-expanded', $('#tk-nav').hasClass('is-open'));
        });

        $('#tk-profile').on('change', function () {
            var id = $(this).val();
            if (!id) { return; }
            TK.api.post('/Profil/Sec/' + encodeURIComponent(id)).done(function () {
                TK.toast.info('Aktif profil: ' + $('#tk-profile option:selected').text());
                $(document).trigger('tk:profile', [activeProfile()]);
            });
        });

        $('#tk-profile-edit').on('click', function () {
            var p = activeProfile();
            if (!p) { TK.toast.warning('Önce bir profil ekleyin.'); return; }
            TK.profile.edit(p.id);
        });
        $('#tk-profile-add').on('click', function () { TK.profile.create(); });
        $('#tk-bug').on('click', openBugReport);

        $('#tk-logout').on('click', function () {
            TK.api.post('/Account/Logout', {}, { allowAnonymous: true }).always(function () { window.location.href = '/Account/Login'; });
        });

        $(document).on('tk:profile-saved', function (_, saved) { loadProfiles(saved && saved.id); loadSpend(); });

        loadSpend();
        loadProfiles();
        window.setInterval(loadSpend, 60000);
    }

    TK.layout = { init: init, refreshSpend: loadSpend, refreshProfiles: loadProfiles, activeProfile: activeProfile };
    $(init);
})(window, window.jQuery);
