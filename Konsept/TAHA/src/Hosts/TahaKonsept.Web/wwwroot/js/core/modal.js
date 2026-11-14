/**
 * TK.modal — ekranın üstünde açılan ortak pencere.
 * TK.modal.open({ title, body (html|jQuery), size: 'sm'|'lg', footer (html|jQuery), hint, onOpen($body, h), onClose($body, h) })
 * Dışına tıklayınca, ✕ düğmesiyle ya da Esc ile kapanır; kapanmadan önce onClose çağrılır (ayarları kaydetmek için).
 * onClose false dönerse pencere açık kalır; promise dönerse o başarıyla bitince kapanır.
 * Dönen nesne: { $el, $body, close(skipSave) }. TK.modal.confirm(başlık, mesaj, düğme) → Deferred.
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var stack = [];

    function open(options) {
        var o = $.extend({ title: '', body: '', size: '', footer: null, hint: '', onOpen: null, onClose: null }, options);
        var $backdrop = $('<div class="tk-modal-backdrop" role="presentation"></div>');
        var $modal = $('<div class="tk-modal" role="dialog" aria-modal="true" tabindex="-1"></div>');
        if (o.size) { $modal.addClass('is-' + o.size); }

        var $head = $('<div class="tk-modal-head"></div>');
        $('<h2></h2>').text(o.title).appendTo($head);
        var $x = $('<button type="button" class="tk-mini-btn" aria-label="Kapat"></button>').html(TK.util.icon('x')).appendTo($head);
        var $body = $('<div class="tk-modal-body"></div>').append(o.body);
        $modal.append($head, $body);

        if (o.footer || o.hint) {
            var $foot = $('<div class="tk-modal-foot"></div>');
            if (o.hint) { $('<span class="tk-hint"></span>').text(o.hint).appendTo($foot); }
            if (o.footer) { $foot.append(o.footer); }
            $modal.append($foot);
        }

        $backdrop.append($modal).appendTo(document.body);
        $('body').addClass('tk-modal-open');
        var lastFocus = document.activeElement;
        var closing = false;
        var saving = false;

        var handle = {
            $el: $modal,
            $body: $body,
            close: function (skipSave) {
                if (closing || saving) { return; }
                var finish = function () {
                    closing = true;
                    $backdrop.removeClass('is-open');
                    window.setTimeout(function () {
                        $backdrop.remove();
                        stack = $.grep(stack, function (h) { return h !== handle; });
                        if (!stack.length) { $('body').removeClass('tk-modal-open'); }
                        if (lastFocus && lastFocus.focus) { lastFocus.focus(); }
                    }, 180);
                };
                if (skipSave || typeof o.onClose !== 'function') { finish(); return; }
                var result = o.onClose($body, handle);
                if (result === false) { return; }
                if (result && typeof result.then === 'function') {
                    saving = true;
                    result.then(function () { saving = false; finish(); }, function () { saving = false; });
                    return;
                }
                finish();
            }
        };

        $x.on('click', function () { handle.close(); });
        $backdrop.on('mousedown', function (e) { if (e.target === $backdrop[0]) { handle.close(); } });
        stack.push(handle);

        window.requestAnimationFrame(function () {
            $backdrop.addClass('is-open');
            var $first = $body.find('input, select, textarea').filter(':visible').first();
            ($first.length ? $first : $modal).trigger('focus');
        });
        if (typeof o.onOpen === 'function') { o.onOpen($body, handle); }
        return handle;
    }

    /** Basit onay penceresi; jQuery Deferred döner (onay → resolve, vazgeç/kapat → reject). */
    function confirm(title, message, okText) {
        var d = $.Deferred();
        var $ok = $('<button type="button" class="tk-btn tk-btn-primary"></button>').text(okText || 'Onayla');
        var $cancel = $('<button type="button" class="tk-btn tk-btn-ghost">Vazgeç</button>');
        var h = open({
            title: title, size: 'sm', body: $('<p></p>').text(message),
            footer: $('<div class="tk-row" style="margin-left:auto"></div>').append($cancel, $ok),
            onClose: function () { if (d.state() === 'pending') { d.reject(); } }
        });
        $ok.on('click', function () { d.resolve(); h.close(true); });
        $cancel.on('click', function () { d.reject(); h.close(true); });
        return d.promise();
    }

    $(document).on('keydown', function (e) {
        if (e.key === 'Escape' && stack.length) { stack[stack.length - 1].close(); }
    });

    TK.modal = {
        open: open,
        confirm: confirm,
        close: function () { if (stack.length) { stack[stack.length - 1].close(); } }
    };
})(window, window.jQuery);
