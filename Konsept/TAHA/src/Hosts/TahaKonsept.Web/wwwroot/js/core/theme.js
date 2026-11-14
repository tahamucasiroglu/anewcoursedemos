/**
 * TK.theme — açık / koyu tema. <head> içinde, CSS'ten hemen sonra yüklenir; ilk boyamadan önce temayı uygular (titreme olmaz).
 * Tercih sırası: localStorage ('tk-theme': light|dark|system) → işletim sistemi (prefers-color-scheme).
 * localStorage erişimi try/catch içinde. Düğme: [data-theme-toggle]. Sunucuya kayıt: TK.api yüklüyse PUT /Ayarlar/Tema.
 * Olay: document üzerinde 'tk:theme'. Tek modül (IIFE); jQuery'ye ihtiyaç duymaz.
 */
(function (window, document) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var KEY = 'tk-theme';
    var media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    function read() {
        try { return window.localStorage.getItem(KEY) || 'system'; } catch (e) { return 'system'; }
    }

    function write(value) {
        try { window.localStorage.setItem(KEY, value); } catch (e) { /* gizli pencere vb. */ }
    }

    function resolve(pref) {
        if (pref === 'light' || pref === 'dark') { return pref; }
        return media && media.matches ? 'dark' : 'light';
    }

    function apply(pref) {
        var effective = resolve(pref);
        document.documentElement.setAttribute('data-theme', effective);
        return effective;
    }

    function set(pref, persistServer) {
        write(pref);
        var effective = apply(pref);
        if (persistServer !== false && TK.api) {
            TK.api.put('/Ayarlar/Tema', { theme: pref }, { silent: true });
        }
        try { document.dispatchEvent(new CustomEvent('tk:theme', { detail: { preference: pref, theme: effective } })); } catch (e) { /* eski tarayıcı */ }
        return effective;
    }

    function toggle() {
        var current = document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
        return set(current === 'dark' ? 'light' : 'dark');
    }

    apply(read());

    if (media) {
        var onChange = function () { if (read() === 'system') { apply('system'); } };
        if (media.addEventListener) { media.addEventListener('change', onChange); } else if (media.addListener) { media.addListener(onChange); }
    }

    document.addEventListener('click', function (e) {
        var btn = e.target && e.target.closest ? e.target.closest('[data-theme-toggle]') : null;
        if (btn) { e.preventDefault(); toggle(); }
    });

    TK.theme = {
        get: read,
        set: set,
        toggle: toggle,
        current: function () { return document.documentElement.getAttribute('data-theme'); }
    };
})(window, document);
