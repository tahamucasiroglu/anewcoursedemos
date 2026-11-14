/**
 * TK.settingsAgent — Ayarlar › Masaüstü programı bölümü: eşleme kodunu onaylama ("Kodu gir"; ?kod=XXXX-XXXX ile önceden dolar),
 * eşlenmiş bilgisayar listesi (çevrimiçi noktası, sürüm, GPU, son görülme; 10 sn polling), eşlemeyi kaldırma.
 * MVC uçları: GET /Ayarlar/Ajanlar → api/agents, POST /Ayarlar/CihazOnayla → api/agents/device/approve,
 * DELETE /Ayarlar/AjanKaldir/{id} → api/agents/{id}. Tek modül (IIFE); istekler yalnızca TK.api ile.
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var poller = null;
    var CODE_PATTERN = /^[A-Z0-9]{4}-[A-Z0-9]{4}$/;

    function formatCode(value) {
        var v = String(value || '').toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 8);
        return v.length > 4 ? v.slice(0, 4) + '-' + v.slice(4) : v;
    }

    function relative(value) {
        if (!value) { return 'hiç bağlanmadı'; }
        var d = new Date(value);
        if (isNaN(d.getTime())) { return ''; }
        var sec = Math.max(0, Math.round((Date.now() - d.getTime()) / 1000));
        if (sec < 45) { return 'az önce'; }
        if (sec < 3600) { return Math.round(sec / 60) + ' dk önce'; }
        if (sec < 86400) { return Math.round(sec / 3600) + ' sa önce'; }
        return U.dateTime(value);
    }

    function dot(online) {
        return '<span aria-hidden="true" style="display:inline-block;width:10px;height:10px;border-radius:50%;flex:0 0 10px;background:' +
            (online ? 'var(--tk-success)' : 'var(--tk-text-muted)') + ';' + (online ? 'box-shadow:0 0 0 3px var(--tk-success-soft);' : '') + '"></span>';
    }

    function render(items) {
        var $list = $('#st-agents');
        if (!items.length) {
            $list.html(U.empty('monitor', 'Eşlenmiş bilgisayar yok', 'Masaüstü programını kurup ekranda görünen kodu yukarıya girin.'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(items, function (_, a) {
            var online = !!a.isOnline;
            var vram = a.vramMb ? Math.round(a.vramMb / 1024) + ' GB VRAM' : '';
            var details = [a.agentVersion ? 's' + a.agentVersion : '', a.gpuName || '', vram, a.lanAddress || ''].filter(Boolean).join(' · ');
            html += '<li class="tk-list-item" style="cursor:default" data-id="' + U.escape(a.id) + '" data-name="' + U.escape(a.machineName) + '">' +
                dot(online) +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(a.machineName || 'Bilgisayar') + '</div>' +
                '<div class="tk-list-sub">' + U.escape(details) + '</div>' +
                '<div class="tk-hint">' + (online ? 'Şu an bağlı' : 'Son görülme: ' + U.escape(relative(a.lastSeenAtUtc))) + '</div></div>' +
                '<span class="tk-badge ' + (online ? 'is-success">Çevrimiçi' : '">Çevrimdışı') + '</span>' +
                '<button type="button" class="tk-mini-btn is-danger" data-act="unpair" title="Eşlemeyi kaldır" aria-label="Eşlemeyi kaldır">' + U.icon('x') + '</button></li>';
        });
        $list.html(html + '</ul>');
    }

    function approve($form) {
        var $input = $form.find('[name=userCode]');
        var code = formatCode($input.val());
        $input.val(code);
        if (!CODE_PATTERN.test(code)) {
            TK.toast.warning('Kod XXXX-XXXX biçiminde olmalı.');
            $input.trigger('focus');
            return;
        }
        U.busy($form.find('[type=submit]'), TK.api.post('/Ayarlar/CihazOnayla', { userCode: code })).done(function (res) {
            var name = res && res.machineName ? res.machineName : 'Bilgisayar';
            TK.toast.success(name + ' eşlendi. Masaüstü programı birkaç saniye içinde bağlanacak.');
            $form[0].reset();
            if (poller) { poller.now(); }
        });
    }

    function unpair($item) {
        var id = $item.attr('data-id');
        var name = $item.attr('data-name') || 'Bu bilgisayar';
        TK.modal.confirm('Eşlemeyi kaldır', name + ' artık bu hesaba erişemeyecek; tekrar kullanmak için yeniden eşlemeniz gerekir.', 'Kaldır').done(function () {
            TK.api.del('/Ayarlar/AjanKaldir/' + encodeURIComponent(id)).done(function () {
                TK.toast.info('Eşleme kaldırıldı.');
                if (poller) { poller.now(); }
            });
        });
    }

    function prefillFromQuery() {
        var match = /[?&]kod=([^&#]+)/.exec(window.location.search);
        if (!match) { return; }
        var code = formatCode(decodeURIComponent(match[1]));
        $('#st-agent-form [name=userCode]').val(code).trigger('focus');
        var section = document.getElementById('masaustu');
        if (section && section.scrollIntoView) { section.scrollIntoView(); }
    }

    function init() {
        if (!$('#st-agent-form').length) { return; }
        U = TK.util;
        $('#st-agents').html(U.skeleton(2));

        $('#st-agent-form [name=userCode]').on('input', function () { $(this).val(formatCode($(this).val())); });
        $('#st-agent-form').on('submit', function (e) { e.preventDefault(); approve($(this)); });
        $('#st-agents').on('click', '[data-act=unpair]', function () { unpair($(this).closest('[data-id]')); });

        poller = TK.poll('/Ayarlar/Ajanlar', 10000, function (d) { render(U.items(d)); }, {
            onError: function () { $('#st-agents').html(U.empty('alert', 'Bilgisayar listesi alınamadı', 'API şu an yanıt vermiyor.')); }
        });
        TK.settingsAgent.poller = poller;
        prefillFromQuery();
    }

    TK.settingsAgent = { init: init, poller: null, formatCode: formatCode };
    $(init);
})(window, window.jQuery);
