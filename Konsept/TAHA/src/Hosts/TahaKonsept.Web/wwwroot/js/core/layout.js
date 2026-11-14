/**
 * TK.layout — üst barın davranışları: aylık harcama rozeti (/Harcama/Aylik, 60 sn'de bir), profil seçimi
 * (/Profil/Liste, /Profil/Sec/{id}), açılır menüler, mobil menü, "Hata bildir" düğmesi (pencere: TK.errorReport), çıkış.
 * Aktif profil değişince document üzerinde 'tk:profile' olayı tetiklenir. "Başlangıç" menü rozeti: kalan adım sayısı
 * (/Baslangic/Durum, sunucuda 30 sn önbellekli; hepsi tamamsa rozet gizli, sayfa menüde kalır ama dürtmez; Başlangıç sayfası
 * 'tk:onboarding' olayıyla canlı günceller). Tek modül (IIFE).
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
            var over = data && data.exceeded !== undefined ? !!data.exceeded : (limit > 0 && Number(total) >= limit);
            $badge.toggleClass('is-over', over);
            var title = limit > 0 ? 'Bu ayki harcama / limit: ' + TK.util.money(total) + ' / ' + TK.util.money(limit) : 'Bu ayki toplam harcama (LLM + ses + görsel)';
            if (over) { title += data && data.stopOnExceed === false ? ' — limit aşıldı (yalnızca uyarı)' : ' — limit aşıldı, yeni üretim durduruldu'; }
            else if (data && data.warning) { title += ' — uyarı eşiğine ulaşıldı'; }
            $badge.attr('title', title);
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
            /* Kısa metin dar üst barda da sığar; ayrıntı ipucunda (seçici metni kesilmeden üç noktayla biter). */
            $sel.append('<option value="">Profil yok</option>').attr('title', 'Profil yok — ➕ ile ekleyin');
            return;
        }
        $.each(profiles, function (_, p) {
            var $o = $('<option></option>').val(p.id).text(p.name || p.channelName || 'Adsız profil');
            if ((selectedId && String(p.id) === String(selectedId)) || (!selectedId && p.isActive)) { $o.prop('selected', true); }
            $sel.append($o);
        });
        $sel.attr('title', 'Aktif profil: ' + ($sel.find('option:selected').text() || ''));
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

    /** "Hata bildir" penceresi TK.errorReport'ta (js/core/error-report.js, Diagnostics). */
    function openBugReport() {
        TK.errorReport.open();
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
                $('#tk-profile').attr('title', 'Aktif profil: ' + $('#tk-profile option:selected').text());
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

        $(document).on('tk:onboarding', function (_, status) { renderOnboarding(status); });
        $(document).on('tk:profile-saved', function () { loadOnboarding(true); });

        loadSpend();
        loadProfiles();
        loadOnboarding();
        window.setInterval(loadSpend, 60000);
    }

    function renderOnboarding(status) {
        var remaining = status && !status.isComplete ? Number(status.remaining || 0) : 0;
        var $badge = $('#tk-onboarding-badge');
        $badge.text(remaining > 0 ? String(remaining) : '').prop('hidden', remaining <= 0);
        $('#tk-nav-start').attr('title', remaining > 0 ? 'İlk videoya ' + remaining + ' adım kaldı' : 'Başlangıç adımları');
    }

    function loadOnboarding(refresh) {
        if (!$('#tk-onboarding-badge').length || $('#tk-baslangic').length) { return; }
        TK.api.get('/Baslangic/Durum', refresh ? { yenile: true } : null, { silent: true }).done(renderOnboarding);
    }

    TK.layout = { init: init, refreshSpend: loadSpend, refreshProfiles: loadProfiles, activeProfile: activeProfile };
    $(init);
})(window, window.jQuery);
