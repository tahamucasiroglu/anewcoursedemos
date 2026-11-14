/**
 * TK.settingsAgent — Ayarlar › Masaüstü programı bölümü. Eşleme mantığı ortak modülde (agent-pairing.js, TK.agentPairing);
 * burada yalnızca bölüme bağlama ve ?kod=XXXX-XXXX ile kodu önceden doldurma var. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    function prefillFromQuery() {
        var match = /[?&]kod=([^&#]+)/.exec(window.location.search);
        if (!match) { return; }
        var code = TK.agentPairing.formatCode(decodeURIComponent(match[1]));
        $('#st-agent-form [name=userCode]').val(code).trigger('focus');
        var section = document.getElementById('masaustu');
        if (section && section.scrollIntoView) { section.scrollIntoView(); }
    }

    function init() {
        if (!$('#st-agent-form').length) { return; }
        var handle = TK.agentPairing.mount({ form: '#st-agent-form', list: '#st-agents', pollMs: 10000 });
        TK.settingsAgent.poller = handle.poller;
        prefillFromQuery();
    }

    TK.settingsAgent = { init: init, poller: null, formatCode: function (v) { return TK.agentPairing.formatCode(v); } };
    $(init);
})(window, window.jQuery);
