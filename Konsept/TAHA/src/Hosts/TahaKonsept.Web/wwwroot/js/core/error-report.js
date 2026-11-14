/**
 * TK.errorReport — "Hata bildir" penceresi. TK.errorReport.open(preset?) açar: ne oldu (zorunlu), ayrıntı (isteğe bağlı),
 * "son günlük kayıtlarını ekle" seçeneği. Sayfa adresi, tarayıcı ve uygulama sürümü (<meta name="app-version">) otomatik eklenir
 * ve pencerede gösterilir. Gönderim: POST /HataBildir/Gonder → API POST api/diagnostics/error-reports
 * ({ source: "Web", message, details, pageUrl, userAgent, appVersion }). Üst bardaki #tk-bug düğmesi bunu çağırır (layout.js).
 * preset: { message, details } — ör. yakalanmamış bir hatadan önceden doldurmak için. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    function appVersion() {
        return $('meta[name="app-version"]').attr('content') || '';
    }

    function browserName(ua) {
        var m = /(Edg|OPR|Firefox|Chrome|Safari)\/([\d.]+)/.exec(ua || '');
        if (!m) { return ua || 'Bilinmiyor'; }
        var names = { Edg: 'Edge', OPR: 'Opera', Firefox: 'Firefox', Chrome: 'Chrome', Safari: 'Safari' };
        return names[m[1]] + ' ' + m[2].split('.')[0];
    }

    function open(preset) {
        preset = preset || {};
        var page = window.location.pathname + window.location.search;
        var ua = window.navigator.userAgent;
        var version = appVersion();
        var U = TK.util;

        var $body = $(
            '<div class="tk-report">' +
            '<div class="tk-field"><label for="tk-bug-msg">Ne oldu? <span class="tk-muted">(zorunlu)</span></label>' +
            '<textarea id="tk-bug-msg" class="tk-textarea" rows="3" maxlength="2000" placeholder="Ör. Konu önerisi boş liste döndü"></textarea></div>' +
            '<div class="tk-field"><label for="tk-bug-details">Ayrıntı <span class="tk-muted">(isteğe bağlı)</span></label>' +
            '<textarea id="tk-bug-details" class="tk-textarea" rows="5" maxlength="20000" placeholder="Ne yapıyordunuz, ne bekliyordunuz? Hata metni varsa buraya yapıştırın."></textarea></div>' +
            '<label class="tk-check"><input type="checkbox" id="tk-bug-logs" checked /> Son 20 günlük kaydını ekle</label>' +
            '<dl class="tk-report-meta">' +
            '<dt>Sayfa</dt><dd><code>' + U.escape(page) + '</code></dd>' +
            '<dt>Tarayıcı</dt><dd>' + U.escape(browserName(ua)) + '</dd>' +
            '<dt>Sürüm</dt><dd>' + U.escape(version || '—') + '</dd>' +
            '</dl>' +
            '</div>');
        $body.find('#tk-bug-msg').val(preset.message || '');
        $body.find('#tk-bug-details').val(preset.details || '');

        var $send = $('<button type="button" class="tk-btn tk-btn-primary"></button>').html(U.icon('send') + ' Gönder');
        var h = TK.modal.open({
            title: 'Hata bildir',
            body: $body,
            footer: $send,
            hint: 'Bildirim yönetim paneline düşer; sayfa, tarayıcı ve sürüm otomatik eklenir.'
        });

        $send.on('click', function () {
            var message = $.trim($('#tk-bug-msg').val());
            if (!message) {
                TK.toast.warning('Ne olduğunu kısaca yazın.');
                $('#tk-bug-msg').trigger('focus');
                return;
            }
            var details = $.trim($('#tk-bug-details').val());
            if ($('#tk-bug-logs').is(':checked') && TK.journal && TK.journal.recent) {
                var lines = TK.journal.recent(20);
                if (lines.length) { details += (details ? '\n\n' : '') + '--- Son günlük kayıtları ---\n' + lines.join('\n'); }
            }
            U.busy($send, TK.api.post('/HataBildir/Gonder', {
                source: 'Web',
                message: message,
                details: details || null,
                pageUrl: page,
                userAgent: ua,
                appVersion: version || null
            })).done(function () {
                TK.toast.success('Teşekkürler, bildiriminiz alındı.');
                h.close(true);
            });
        });
    }

    TK.errorReport = { open: open };
})(window, window.jQuery);
