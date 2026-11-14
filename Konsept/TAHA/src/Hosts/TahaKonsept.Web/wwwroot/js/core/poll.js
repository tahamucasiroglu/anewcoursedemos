/**
 * TK.poll — canlı durum için periyodik GET (varsayılan 2 sn). Üst üste istek atmaz, sekme gizliyken yavaşlar,
 * hata alınca geri çekilir (en fazla 30 sn).
 * Kullanım: var p = TK.poll(url, ms, onData, { query: fn|obj, onError: fn, autoStart: true }); p.stop(), p.start(), p.now().
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    function poll(url, ms, onData, opts) {
        opts = opts || {};
        var interval = ms || 2000;
        var timer = null;
        var running = false;
        var inFlight = null;
        var failures = 0;

        function nextDelay() {
            var base = document.hidden ? Math.max(interval * 5, 10000) : interval;
            if (failures > 0) { base = Math.min(base * Math.pow(2, failures), 30000); }
            return base;
        }

        function schedule() {
            if (!running) { return; }
            window.clearTimeout(timer);
            timer = window.setTimeout(tick, nextDelay());
        }

        function tick() {
            if (!running || inFlight) { return; }
            var query = typeof opts.query === 'function' ? opts.query() : opts.query;
            inFlight = TK.api.get(url, query, { silent: true });
            inFlight
                .done(function (data) { failures = 0; if (onData) { onData(data); } })
                .fail(function (xhr) { failures = Math.min(failures + 1, 5); if (opts.onError) { opts.onError(xhr); } })
                .always(function () { inFlight = null; schedule(); });
        }

        var handle = {
            start: function () { if (!running) { running = true; tick(); } return handle; },
            stop: function () { running = false; window.clearTimeout(timer); if (inFlight) { inFlight.abort(); } return handle; },
            now: function () { window.clearTimeout(timer); tick(); return handle; },
            isRunning: function () { return running; }
        };

        document.addEventListener('visibilitychange', function () { if (running && !document.hidden) { handle.now(); } });
        return opts.autoStart === false ? handle : handle.start();
    }

    TK.poll = poll;
})(window, window.jQuery);
