/**
 * TK.uretilenler — Üretilen Videolar sayfası: sol liste (arama), orta oynatıcı (ajanın yerel sunucusundan; ses + tam ekran)
 * ve işlemler (Klasörde aç, Sistem oynatıcısında aç, Yeniden üret — önce düzenle, Yeni sürüm — sadece değişen aşama, Sil),
 * sağda sekmeler (sahneler + seslendirme, konsept metni, ücret/ayrıntı). ?video={id} ile doğrudan açılır.
 * MVC uçları: /Uretilenler/{Liste, Detay, KlasordeAc, SistemOynaticisi, YenidenUret, YeniSurum, Sil}. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var state = { videos: [], current: null, filter: '' };

    var VERSION_OPTIONS = [
        ['speech', 'Ses (TTS motoru / ses)', 'Ses → hizalama → birleştirme yeniden çalışır'],
        ['engine', 'Video motoru', 'Kod → render → birleştirme yeniden çalışır; ses korunur'],
        ['images', 'Görseller', 'Görseller → render → birleştirme'],
        ['subtitles', 'Altyazı / ilerleme çubuğu', 'Yalnızca birleştirme aşaması'],
        ['text', 'Metin (konsept)', 'Tüm aşamalar baştan']
    ];

    function renderList() {
        var $l = $('#ur-list');
        var f = state.filter.toLocaleLowerCase('tr-TR');
        var list = $.grep(state.videos, function (v) { return !f || String(v.title || '').toLocaleLowerCase('tr-TR').indexOf(f) >= 0; });
        $('#ur-count').text(state.videos.length ? state.videos.length + ' video' : 'Boş');
        if (!list.length) {
            $l.html(state.videos.length ? U.empty('list', 'Eşleşen video yok', 'Arama ifadesini değiştirin.') : U.empty('film', 'Henüz video üretilmedi', 'Konseptten Video sayfasından üretim başlatın.'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(list, function (_, v) {
            var sel = state.current && String(state.current.id) === String(v.id);
            html += '<li class="tk-list-item' + (sel ? ' is-selected' : '') + '" data-id="' + U.escape(v.id) + '">' +
                '<span class="tk-thumb' + (String(v.format || '').toLowerCase() === 'vertical' ? ' is-vertical' : '') + '">' + U.icon('play', 'tk-ic-sm') + '</span>' +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(v.title || 'Adsız video') + '</div>' +
                '<div class="tk-list-sub">' + U.escape(U.dateTime(v.createdAt)) + (v.version ? ' · s' + U.escape(v.version) : '') + '</div></div></li>';
        });
        $l.html(html + '</ul>');
    }

    function loadList() {
        return TK.api.get('/Uretilenler/Liste', null, { silent: true })
            .done(function (d) { state.videos = U.items(d); renderList(); })
            .fail(function () { $('#ur-list').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function renderPlayer(v) {
        var $p = $('#ur-player').empty();
        $('#ur-actions').prop('hidden', !v);
        if (!v) {
            $p.html('<div class="tk-player-empty">' + U.icon('film', 'tk-ic-lg') + '<div>Henüz video seçilmedi</div></div>');
            $('#ur-player-sub').text('Soldan bir video seçin');
            return;
        }
        $('#ur-player-sub').text(v.title || '');
        if (String(v.format || '').toLowerCase() === 'vertical') { $p.css('aspect-ratio', '9 / 16').css('max-height', '62vh').css('width', 'auto').css('margin', '0 auto'); } else { $p.removeAttr('style'); }
        if (v.streamUrl) {
            $('<video controls playsinline preload="metadata"></video>').attr('src', v.streamUrl).attr('poster', v.thumbUrl || null).appendTo($p);
        } else {
            $p.html('<div class="tk-player-empty">' + U.icon('monitor', 'tk-ic-lg') + '<div><strong>Masaüstü programına ulaşılamıyor.</strong><br/>Video bilgisayarınızda saklanır; bilgisayar açık ve program çalışırken burada oynatılır.</div></div>');
        }
    }

    function renderInfo(v) {
        if (!v) {
            $('#ur-scenes, #ur-concept, #ur-details').html(U.empty('info', 'Video seçilmedi', ''));
            return;
        }
        var d = v.document || v.concept || {};
        var scenes = d.scenes || v.scenes || [];
        $('#ur-scenes').html(scenes.length ? $.map(scenes, function (s, i) {
            return '<div style="padding:10px 0;border-bottom:1px solid var(--tk-divider)"><strong>' + (i + 1) + '. ' + U.escape(s.title || '') + '</strong>' +
                (s.durationSec || s.durationHintSec ? ' <span class="tk-badge">' + U.escape(s.durationSec || s.durationHintSec) + ' sn</span>' : '') +
                '<div class="tk-hint">Ekran: ' + U.escape(s.onScreen || '') + '</div>' +
                '<div>' + U.escape(s.narration || '') + '</div>' +
                (s.spokenText ? '<div class="tk-hint">Okunuş: ' + U.escape(s.spokenText) + '</div>' : '') + '</div>';
        }).join('') : U.empty('list', 'Sahne bilgisi yok', ''));

        $('#ur-concept').html('<h3>' + U.escape(d.title || v.title || '') + '</h3><p class="tk-muted" style="margin-top:6px">' + U.escape(d.summary || '') + '</p>' +
            (d.sources && d.sources.length ? '<h3 style="margin-top:12px">Kaynaklar</h3><ul>' + $.map(d.sources, function (s) {
                return '<li><a href="' + U.escape(s.url) + '" target="_blank" rel="noopener noreferrer">' + U.escape(s.title || s.url) + '</a></li>';
            }).join('') + '</ul>' : ''));

        var cost = v.cost || {};
        var rows = [
            ['Toplam ücret', U.money(v.costUsd !== undefined ? v.costUsd : cost.totalUsd)],
            ['LLM', cost.llmUsd !== undefined ? U.money(cost.llmUsd) : '—'],
            ['Ses', cost.speechUsd !== undefined ? U.money(cost.speechUsd) : '—'],
            ['Görsel', cost.imageUsd !== undefined ? U.money(cost.imageUsd) : '—'],
            ['Süre', v.durationSec ? Math.round(v.durationSec) + ' sn' : '—'],
            ['Biçim', String(v.format || '').toLowerCase() === 'vertical' ? 'Dikey 9:16' : v.format ? 'Yatay 16:9' : '—'],
            ['Video motoru', v.videoEngine || '—'],
            ['Ses modeli', v.speechModel || '—'],
            ['Görsel modeli', v.imageModel || '—'],
            ['Sürüm', v.version || '1'],
            ['Üretildi', U.dateTime(v.createdAt) || '—']
        ];
        $('#ur-details').html('<dl class="tk-kv">' + $.map(rows, function (r) { return '<dt>' + r[0] + '</dt><dd>' + U.escape(r[1]) + '</dd>'; }).join('') + '</dl>');
    }

    function select(id) {
        state.current = $.grep(state.videos, function (v) { return String(v.id) === String(id); })[0] || { id: id };
        renderList();
        renderPlayer(state.current);
        renderInfo(null);
        TK.api.get('/Uretilenler/Detay/' + encodeURIComponent(id)).done(function (v) {
            if (!v || $.isArray(v)) { return; }
            state.current = v;
            renderPlayer(v);
            renderInfo(v);
        });
    }

    function newVersion() {
        var v = state.current;
        var $b = $('<div><p class="tk-muted">Neyi değiştirdiniz? Yalnızca ilgili aşamalar yeniden çalışır, diğerleri yeniden kullanılır.</p><div class="tk-list"></div>' +
            '<div class="tk-field" style="margin-top:12px"><label for="ur-v-note">Not (isteğe bağlı)</label><input id="ur-v-note" class="tk-input" maxlength="200" placeholder="Ör. Supertonic yerine Chatterbox" /></div></div>');
        $.each(VERSION_OPTIONS, function (_, o) {
            $b.find('.tk-list').append('<label class="tk-list-item"><input type="checkbox" value="' + o[0] + '" style="accent-color:var(--tk-accent);width:17px;height:17px" />' +
                '<div class="tk-list-main"><div class="tk-list-title">' + o[1] + '</div><div class="tk-list-sub">' + o[2] + '</div></div></label>');
        });
        var $go = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('layers') + ' Yeni sürümü üret</button>');
        var h = TK.modal.open({ title: 'Yeni sürüm', body: $b, footer: $go, hint: 'Ayarlar, Konseptten Video sayfasındaki seçimlerden alınır.' });
        $go.on('click', function () {
            var changed = $b.find('input[type=checkbox]:checked').map(function () { return this.value; }).get();
            if (!changed.length) { TK.toast.warning('En az bir değişiklik seçin.'); return; }
            U.busy($go, TK.api.post('/Uretilenler/YeniSurum/' + encodeURIComponent(v.id), { changed: changed, note: $.trim($('#ur-v-note').val()) || null }))
                .done(function () { TK.toast.success('Yeni sürüm üretim sırasına alındı.'); h.close(true); });
        });
    }

    function regenerate() {
        var v = state.current;
        var d = $.extend(true, {}, v.document || v.concept || { title: v.title, scenes: [] });
        var $b = $('<div><div class="tk-field"><label>Başlık</label><input class="tk-input" name="title" /></div></div>');
        $b.find('[name=title]').val(d.title || '');
        $.each(d.scenes || [], function (i, s) {
            var $f = $('<div class="tk-field"><label></label><textarea class="tk-textarea" rows="3"></textarea></div>').attr('data-scene', i);
            $f.find('label').text('Sahne ' + (i + 1) + ' — ' + (s.title || ''));
            $f.find('textarea').val(s.narration || '');
            $b.append($f);
        });
        var $go = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('rotate') + ' Düzenle ve yeniden üret</button>');
        var h = TK.modal.open({ title: 'Yeniden üret', size: 'lg', body: $b, footer: $go });
        $go.on('click', function () {
            d.title = $.trim($b.find('[name=title]').val());
            $b.find('[data-scene]').each(function () { var i = +$(this).attr('data-scene'); d.scenes[i].narration = $(this).find('textarea').val(); });
            U.busy($go, TK.api.post('/Uretilenler/YenidenUret/' + encodeURIComponent(v.id), { document: d }))
                .done(function () { TK.toast.success('Video yeniden üretim sırasına alındı.'); h.close(true); });
        });
    }

    function act(name) {
        var v = state.current;
        if (!v) { return; }
        var id = encodeURIComponent(v.id);
        if (name === 'folder') { TK.api.post('/Uretilenler/KlasordeAc/' + id).done(function () { TK.toast.info('Klasör bilgisayarınızda açılıyor.'); }); }
        if (name === 'system') { TK.api.post('/Uretilenler/SistemOynaticisi/' + id).done(function () { TK.toast.info('Video sistem oynatıcısında açılıyor.'); }); }
        if (name === 'version') { newVersion(); }
        if (name === 'regenerate') { regenerate(); }
        if (name === 'delete') {
            TK.modal.confirm('Videoyu sil', '"' + (v.title || 'Video') + '" bilgisayarınızdan da silinecek. Emin misiniz?', 'Sil').done(function () {
                TK.api.del('/Uretilenler/Sil/' + id).done(function () {
                    TK.toast.info('Video silindi.');
                    state.current = null; renderPlayer(null); renderInfo(null); loadList();
                });
            });
        }
    }

    function init() {
        if (!$('#tk-uretilenler').length) { return; }
        U = TK.util;
        $('#ur-list').html(U.skeleton(5));
        renderInfo(null);

        $('#ur-list').on('click', '.tk-list-item', function () { select($(this).attr('data-id')); });
        $('#ur-search').on('input', function () { state.filter = $.trim($(this).val()); renderList(); });
        $('#ur-refresh').on('click', loadList);
        $('#ur-actions').on('click', '[data-act]', function () { act($(this).attr('data-act')); });
        $('#tk-uretilenler').on('click', '.tk-tab', function () {
            var tab = $(this).attr('data-tab');
            $('#tk-uretilenler .tk-tab').removeClass('is-active').filter(this).addClass('is-active');
            $('#tk-uretilenler .tk-tab-panel').removeClass('is-active').filter('[data-panel=' + tab + ']').addClass('is-active');
        });

        var initial = new window.URLSearchParams(window.location.search).get('video');
        loadList().done(function () { if (initial) { select(initial); } });
    }

    TK.uretilenler = { init: init };
    $(init);
})(window, window.jQuery);
