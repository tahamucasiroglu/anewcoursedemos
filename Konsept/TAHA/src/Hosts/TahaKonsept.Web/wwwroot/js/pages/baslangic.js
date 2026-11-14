/**
 * TK.baslangic — Başlangıç (karşılama) sayfası: ilk videoya giden adımların canlı kontrol listesi.
 * Durum: GET /Baslangic/Durum?yenile=true (15 sn polling; { steps: [{ key, state: Done|Waiting|Unknown, detail }], remaining, isComplete }).
 * Masaüstü programı eşleme formu ve bilgisayar listesi ortak modülle (TK.agentPairing; Ayarlar ile aynı kod); canlı "bağlı / bağlı değil"
 * göstergesi o listeden çizilir. Profil oluştur → TK.profile.create(). Durum değişince document'ta 'tk:onboarding' tetiklenir
 * (TK.layout üst menü rozetini günceller). Tamamlanan adımlar katlanır (başlığa tıklayınca açılır). Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var statusPoller = null;
    var pairing = null;
    var lastPaired = null;
    var userToggled = {};

    var STATE_LABEL = { Done: 'Tamam', Waiting: 'Bekliyor', Unknown: 'Bilinmiyor' };
    var STATE_CLASS = { Done: 'is-success', Waiting: 'is-warning', Unknown: '' };

    function setExpanded($step, expanded) {
        $step.toggleClass('is-collapsed', !expanded);
        $step.find('.bs-step-head').attr('aria-expanded', expanded ? 'true' : 'false');
    }

    function renderStatus(status) {
        var steps = (status && status.steps) || [];
        var done = 0;
        $.each(steps, function (_, s) {
            var $step = $('.bs-step[data-step="' + s.key + '"]');
            if (!$step.length) { return; }
            var isDone = s.state === 'Done';
            if (isDone) { done++; }
            $step.toggleClass('is-done', isDone).toggleClass('is-unknown', s.state === 'Unknown');
            $step.find('[data-state]').attr('class', 'tk-badge ' + (STATE_CLASS[s.state] || '')).text(STATE_LABEL[s.state] || s.state);
            $step.find('[data-detail]').text(s.detail || '');
            if (!userToggled[s.key]) { setExpanded($step, !isDone); }
        });

        var total = steps.length || 5;
        $('#bs-progress-bar').css('width', Math.round(done * 100 / total) + '%');
        $('.bs-progress').attr({ 'aria-valuenow': done, 'aria-valuemax': total });
        $('#bs-progress-text').text(status && status.isComplete ? 'Hepsi tamam' : done + ' / ' + total + ' adım tamam');
        $('#bs-complete').prop('hidden', !(status && status.isComplete));
        $('#tk-baslangic').toggleClass('is-complete', !!(status && status.isComplete));

        // İlk bekleyen adımı vurgula ("sıradaki").
        $('.bs-step').removeClass('is-next');
        $('.bs-step:not(.is-done):not(.is-unknown)').first().addClass('is-next');

        $(document).trigger('tk:onboarding', [status]);
    }

    function renderLive(items) {
        var s = TK.agentPairing.summary(items);
        var $live = $('#bs-agent-live');
        var text;
        if (s.online > 0) {
            text = 'Bağlı: ' + s.onlineNames.join(', ');
        } else if (s.paired > 0) {
            text = 'Bağlı değil — eşli bilgisayar şu an çevrimdışı (son görülme: ' + s.lastSeenText + '). Masaüstü programını açın.';
        } else {
            text = 'Bağlı değil — henüz eşlenmiş bilgisayar yok.';
        }
        $live.toggleClass('is-online', s.online > 0);
        $live.find('.tk-live-dot').toggleClass('is-on', s.online > 0);
        $live.find('.bs-agent-live-text').text(text);

        // Eşleme sayısı değişince kontrol listesini hemen tazele.
        if (lastPaired !== null && lastPaired !== s.paired && statusPoller) { statusPoller.now(); }
        lastPaired = s.paired;
    }

    function bind() {
        $('#bs-steps').on('click', '.bs-step-head', function () {
            var $step = $(this).closest('.bs-step');
            userToggled[$step.attr('data-step')] = true;
            setExpanded($step, $step.hasClass('is-collapsed'));
        });
        $('#bs-profile-add').on('click', function () { TK.profile.create(); });
        $(document).on('tk:profile-saved', function () { if (statusPoller) { statusPoller.now(); } });
    }

    function init() {
        if (!$('#tk-baslangic').length) { return; }
        bind();
        statusPoller = TK.poll('/Baslangic/Durum', 15000, renderStatus, {
            query: { yenile: true },
            onError: function () { $('#bs-progress-text').text('Durum alınamadı; tekrar denenecek.'); }
        });
        pairing = TK.agentPairing.mount({
            form: '#bs-agent-form',
            list: '#bs-agents',
            pollMs: 8000,
            onAgents: renderLive,
            onError: function () { $('#bs-agent-live .bs-agent-live-text').text('Bağlantı durumu alınamadı.'); }
        });
    }

    TK.baslangic = { init: init, refresh: function () { if (statusPoller) { statusPoller.now(); } if (pairing) { pairing.refresh(); } } };
    $(init);
})(window, window.jQuery);
