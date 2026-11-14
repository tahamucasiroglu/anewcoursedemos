// Tema: açık/koyu (spec 0001 Req 2, AC-2). <head> içinde klasik script olarak yüklenir ki sayfa ilk
// çizilmeden doğru tema uygulansın. Seçim tarayıcıda hatırlanır; hiç seçim yoksa açık mod.
(function (root) {
  'use strict';

  var KEY = 'anew-theme';

  function read(storage) {
    try {
      return storage.getItem(KEY);
    } catch (e) {
      return null;
    }
  }

  function write(storage, value) {
    try {
      storage.setItem(KEY, value);
    } catch (e) {
      // Özel pencere vb.: seçim hatırlanmaz ama tema yine değişir.
    }
  }

  function current(doc) {
    return doc.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
  }

  function apply(doc, theme) {
    doc.documentElement.setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
  }

  function init(doc, storage) {
    apply(doc, read(storage) === 'dark' ? 'dark' : 'light');
  }

  function toggle(doc, storage) {
    var next = current(doc) === 'dark' ? 'light' : 'dark';
    apply(doc, next);
    write(storage, next);
    return next;
  }

  function bind(doc, storage) {
    var button = doc.getElementById('theme-toggle');
    if (button) {
      button.addEventListener('click', function () {
        toggle(doc, storage);
      });
    }
  }

  root.KonseptTheme = { KEY: KEY, init: init, toggle: toggle, bind: bind, current: current };

  if (root.document) {
    var storage = null;
    try {
      storage = root.localStorage;
    } catch (e) {
      storage = null;
    }
    var safeStorage = storage || { getItem: function () { return null; }, setItem: function () {} };
    init(root.document, safeStorage);
    root.document.addEventListener('DOMContentLoaded', function () {
      bind(root.document, safeStorage);
    });
  }
})(typeof window !== 'undefined' ? window : globalThis);
