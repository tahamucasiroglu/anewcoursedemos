/**
 * TK.settingsPronunciation — Ayarlar → Okunuş sözlüğü bölümü.
 *  - Tablo: yazılış, okunuş, durum (Öneri / Onaylı), kaynak; süzgeç (Tümü / Öneri / Onaylı) + arama.
 *  - Ekle (üst form), düzenle (pencere), sil (onay penceresi), öneriyi onayla, önerilerin hepsini onayla.
 *  - "Dinle": okunuşu güncel ses modeliyle seslendirir (ajan ya da online; dönen ses çalınır).
 *  - Deneme kutusu: cümle → normalleştirilmiş okunuş + değişiklik listesi (+ ses). Değişiklik "Sözlüğe ekle" ile forma aktarılır.
 * MVC uçları: /Ayarlar/{Sozluk (GET|POST), SozlukSil, SozlukDinle}. Fark hesabı: TK.production.textChanges / diffHtml. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var state = { items: [], filter: 'all', search: '' };

    function isSuggested(e) {
        var st = String(e.status === undefined ? '' : e.status).toLowerCase();
        return e.isSuggestion === true || st === 'suggested' || st === '0';
    }

    /* ---------------- Tablo ---------------- */
    function visible() {
        var q = state.search.toLocaleLowerCase('tr-TR');
        return $.grep(state.items, function (e) {
            if (state.filter === 'suggested' && !isSuggested(e)) { return false; }
            if (state.filter === 'approved' && isSuggested(e)) { return false; }
            return !q || (String(e.written) + ' ' + String(e.spoken)).toLocaleLowerCase('tr-TR').indexOf(q) >= 0;
        });
    }

    function render() {
        var suggested = $.grep(state.items, isSuggested).length;
        $('#sp-count-all').text(state.items.length);
        $('#sp-count-suggested').text(suggested);
        $('#sp-count-approved').text(state.items.length - suggested);
        $('#sp-approve-all').prop('hidden', suggested === 0).html(U.icon('check', 'tk-ic-sm') + ' Önerilerin hepsini onayla (' + suggested + ')');

        var $d = $('#st-dict');
        var list = visible();
        if (!state.items.length) {
            $d.html(U.empty('book', 'Sözlük boş', 'Yabancı özel isimleri ve kısaltmaları ekleyin; seslendirme bu okunuşu kullanır. Konsept üretimi de öneri ekler.'));
            return;
        }
        if (!list.length) { $d.html(U.empty('list', 'Eşleşen kayıt yok', 'Süzgeci ya da aramayı değiştirin.')); return; }
        var html = '<div class="sp-table-wrap"><table class="tk-table sp-table"><thead><tr><th>Yazılış</th><th>Okunuş</th><th>Durum</th><th><span class="tk-sr-only">İşlemler</span></th></tr></thead><tbody>';
        $.each(list, function (_, e) {
            var sug = isSuggested(e);
            html += '<tr data-id="' + U.escape(e.id) + '" class="' + (sug ? 'is-suggested' : '') + '">' +
                '<td><strong>' + U.escape(e.written) + '</strong></td>' +
                '<td>' + U.escape(e.spoken) + (e.source ? '<span class="sp-source">' + U.escape(e.source) + '</span>' : '') + '</td>' +
                '<td>' + (sug ? '<span class="tk-badge is-warning">Öneri</span>' : '<span class="tk-badge is-success">Onaylı</span>') + '</td>' +
                '<td>' + (sug ? '<button type="button" class="tk-btn tk-btn-primary tk-btn-sm" data-act="approve">' + U.icon('check', 'tk-ic-sm') + ' Onayla</button> ' : '') +
                '<button type="button" class="tk-mini-btn" data-act="listen" title="Dinle" aria-label="Dinle">' + U.icon('volume') + '</button>' +
                '<button type="button" class="tk-mini-btn" data-act="edit" title="Düzenle" aria-label="Düzenle">' + U.icon('pencil') + '</button>' +
                '<button type="button" class="tk-mini-btn is-danger" data-act="delete" title="Sil" aria-label="Sil">' + U.icon('trash') + '</button></td></tr>';
        });
        $d.html(html + '</tbody></table></div>');
    }

    function load() {
        return TK.api.get('/Ayarlar/Sozluk', null, { silent: true })
            .done(function (d) {
                state.items = U.items(d).sort(function (a, b) {
                    return (isSuggested(b) - isSuggested(a)) || String(a.written).localeCompare(String(b.written), 'tr');
                });
                render();
            })
            .fail(function () { $('#st-dict').html(U.empty('alert', 'Sözlük alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function find(id) { return $.grep(state.items, function (e) { return String(e.id) === String(id); })[0]; }

    function save(entry) {
        return TK.api.post('/Ayarlar/Sozluk', { id: entry.id || null, written: entry.written, spoken: entry.spoken, isSuggestion: !!entry.isSuggestion });
    }

    /* ---------------- Ses ---------------- */
    function audioSrc(res) {
        if (!res) { return null; }
        if (res.audioUrl) { return res.audioUrl; }
        if (res.audioBase64) { return 'data:' + (res.mimeType || 'audio/wav') + ';base64,' + res.audioBase64; }
        return null;
    }

    function listen(text, $btn) {
        text = $.trim(text || '');
        if (!text) { TK.toast.warning('Dinlemek için okunuş yazın.'); return; }
        U.busy($btn, TK.api.post('/Ayarlar/SozlukDinle', { text: text, normalize: false })).done(function (res) {
            var src = audioSrc(res);
            if (src) {
                try {
                    var p = new window.Audio(src).play();
                    if (p && p.catch) { p.catch(function () { TK.toast.error('Ses çalınamadı (tarayıcı izin vermedi).'); }); }
                } catch (e) { TK.toast.error('Ses çalınamadı.'); }
            } else {
                TK.toast.warning((res && res.message) || 'Önizleme sesi üretilemedi.');
            }
        });
    }

    /* ---------------- Düzenleme penceresi ---------------- */
    function edit(entry) {
        var $b = $('<div>' +
            '<div class="tk-field"><label for="sp-e-written">Yazılış (ekranda/altyazıda)</label><input class="tk-input" id="sp-e-written" maxlength="120" /></div>' +
            '<div class="tk-field"><label for="sp-e-spoken">Okunuş (seslendirmede)</label><div class="tk-row" style="flex-wrap:nowrap">' +
            '<input class="tk-input tk-grow" id="sp-e-spoken" maxlength="200" />' +
            '<button type="button" class="tk-btn tk-btn-ghost" data-listen>' + U.icon('volume') + ' Dinle</button></div></div>' +
            '<label class="tk-switch"><input type="checkbox" id="sp-e-approved" /><span class="tk-switch-track"></span> Onaylı (seslendirmede kullanılır)</label>' +
            '</div>');
        $b.find('#sp-e-written').val(entry.written || '');
        $b.find('#sp-e-spoken').val(entry.spoken || '');
        $b.find('#sp-e-approved').prop('checked', !isSuggested(entry));
        $b.on('click', '[data-listen]', function () { listen($b.find('#sp-e-spoken').val(), $(this)); });
        var $save = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('check') + ' Kaydet</button>');
        var h = TK.modal.open({ title: 'Okunuşu düzenle', body: $b, footer: $save, hint: 'Yalnızca onaylı kayıtlar seslendirmede kullanılır.' });
        $save.on('click', function () {
            var next = {
                id: entry.id,
                written: $.trim($b.find('#sp-e-written').val()),
                spoken: $.trim($b.find('#sp-e-spoken').val()),
                isSuggestion: !$b.find('#sp-e-approved').is(':checked')
            };
            if (!next.written || !next.spoken) { TK.toast.warning('Yazılış ve okunuş gerekli.'); return; }
            U.busy($save, save(next)).done(function () { h.close(true); TK.toast.success('Kayıt güncellendi.'); load(); });
        });
    }

    function approveAll($btn) {
        var list = $.grep(state.items, isSuggested);
        if (!list.length) { return; }
        TK.modal.confirm('Önerileri onayla', list.length + ' öneri onaylanacak ve seslendirmede kullanılacak. Devam edilsin mi?', 'Hepsini onayla').done(function () {
            var chain = $.Deferred().resolve().promise();
            var failed = 0;
            $.each(list, function (_, e) {
                chain = chain.then(function () {
                    var d = $.Deferred();
                    save({ id: e.id, written: e.written, spoken: e.spoken, isSuggestion: false }).fail(function () { failed++; }).always(function () { d.resolve(); });
                    return d.promise();
                });
            });
            U.busy($btn, chain).then(function () {
                if (failed) { TK.toast.warning((list.length - failed) + ' öneri onaylandı, ' + failed + ' tanesi onaylanamadı.'); } else { TK.toast.success(list.length + ' öneri onaylandı.'); }
                load();
            });
        });
    }

    /* ---------------- Deneme kutusu ---------------- */
    function runTest($btn) {
        var text = $.trim($('#sp-test-text').val());
        var $r = $('#sp-test-result');
        if (!text) { TK.toast.warning('Denemek için bir cümle yazın.'); return; }
        $r.html('<div class="sp-result">' + U.skeleton(2) + '</div>');
        U.busy($btn, TK.api.post('/Ayarlar/SozlukDinle', { text: text, normalize: true })).done(function (res) {
            res = res || {};
            var display = res.displayText || text;
            var spoken = res.spokenText || display;
            var changes = $.isArray(res.changes) ? $.map(res.changes, function (c) { return { original: c.original, replacement: c.replacement, rule: c.rule }; })
                : TK.production.textChanges(display, spoken);
            var html = '<div class="sp-result">' +
                '<div class="sp-result-box"><span class="tk-label">Seslendirmede okunacak</span><p>' + (spoken !== display ? TK.production.diffHtml(display, spoken) : U.escape(spoken)) + '</p></div>' +
                '<div class="sp-result-box"><span class="tk-label">Değişiklikler (' + changes.length + ')</span>' + (changes.length
                    ? '<ul class="sp-changes">' + $.map(changes, function (c, i) {
                        return '<li><code>' + U.escape(c.original || '∅') + '</code><span class="sp-arrow">→</span><code>' + U.escape(c.replacement || '∅') + '</code>' +
                            (c.rule ? '<span class="tk-badge">' + U.escape(c.rule) + '</span>' : '') +
                            '<button type="button" class="tk-btn tk-btn-ghost sp-add-btn" data-add="' + i + '" title="Bu okunuşu sözlük formuna aktar">Sözlüğe ekle</button></li>';
                    }).join('') + '</ul>'
                    : '<p class="tk-muted">Değişiklik yok; metin yazıldığı gibi okunur.</p>') + '</div>';
            var src = audioSrc(res);
            html += src ? '<div class="sp-result-box"><span class="tk-label">Ses önizlemesi</span><audio controls autoplay></audio></div>'
                : (res.message ? '<div class="tk-alert is-warning">' + U.icon('volume') + '<div>' + U.escape(res.message) + '</div></div>' : '');
            $r.html(html + '</div>');
            if (src) { $r.find('audio').attr('src', src); }
            $r.data('changes', changes);
        }).fail(function () { $r.empty(); });
    }

    function bind() {
        var $form = $('#st-dict-form');
        $form.on('submit', function (e) {
            e.preventDefault();
            var written = $.trim($form.find('[name=written]').val());
            var spoken = $.trim($form.find('[name=spoken]').val());
            if (!written || !spoken) { TK.toast.warning('Yazılış ve okunuş gerekli.'); return; }
            var existing = $.grep(state.items, function (x) { return String(x.written).toLocaleLowerCase('tr-TR') === written.toLocaleLowerCase('tr-TR'); })[0];
            U.busy($form.find('[type=submit]'), save({ id: existing ? existing.id : null, written: written, spoken: spoken, isSuggestion: false })).done(function () {
                TK.toast.success(existing ? '"' + written + '" güncellendi.' : '"' + written + '" sözlüğe eklendi.');
                $form[0].reset();
                $form.find('[name=written]').trigger('focus');
                load();
            });
        });
        $('#st-dict-listen').on('click', function () { listen($form.find('[name=spoken]').val(), $(this)); });

        $('#st-dict').on('click', '[data-act]', function () {
            var $btn = $(this);
            var e = find($btn.closest('tr').attr('data-id'));
            if (!e) { return; }
            var act = $btn.attr('data-act');
            if (act === 'listen') { listen(e.spoken, $btn); }
            if (act === 'edit') { edit(e); }
            if (act === 'approve') {
                U.busy($btn, save({ id: e.id, written: e.written, spoken: e.spoken, isSuggestion: false })).done(function () { TK.toast.success('"' + e.written + '" onaylandı.'); load(); });
            }
            if (act === 'delete') {
                TK.modal.confirm('Kaydı sil', '"' + e.written + ' → ' + e.spoken + '" sözlükten silinecek.', 'Sil').done(function () {
                    TK.api.del('/Ayarlar/SozlukSil/' + encodeURIComponent(e.id)).done(function () { TK.toast.info('Kayıt silindi.'); load(); });
                });
            }
        });

        $('#st-pron .sp-filters').on('click', 'button', function () {
            state.filter = $(this).attr('data-filter');
            $(this).addClass('is-active').siblings().removeClass('is-active');
            render();
        });
        $('#sp-search').on('input', function () { state.search = $.trim($(this).val()); render(); });
        $('#sp-approve-all').on('click', function () { approveAll($(this)); });

        $('#sp-test-run').on('click', function () { runTest($(this)); });
        $('#sp-test-text').on('keydown', function (e) { if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); runTest($('#sp-test-run')); } });
        $('#sp-test-result').on('click', '[data-add]', function () {
            var c = ($('#sp-test-result').data('changes') || [])[+$(this).attr('data-add')];
            if (!c) { return; }
            $form.find('[name=written]').val(c.original || '');
            $form.find('[name=spoken]').val(c.replacement || '').trigger('focus');
            $form[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    function init() {
        if (!$('#st-pron').length) { return; }
        U = TK.util;
        $('#st-dict').html(U.skeleton(2));
        bind();
        load();
    }

    TK.settingsPronunciation = { init: init, reload: load };
    $(init);
})(window, window.jQuery);
