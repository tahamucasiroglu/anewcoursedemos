/**
 * TK.konseptler — Konseptler sayfası: model / ince ayar pencereleri, kelime denetimi anahtarı, konudan üret
 * (başlık önerisi → seçtiklerini üret), fikirden üret, üretilenler listesi ve düzenleme alanı (kaydet, onayla,
 * "şunları düzelterek tekrar üret" → sonuç penceresi: Seç / Sil / Bunu da düzenle).
 * MVC uçları: /Konseptler/{Modeller, Ayarlar, KonuOner, SecilenleriUret, FikirdenUret, Liste, Getir, Guncelle, Sil, Revize, Onayla}.
 * Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    var state = { models: [], settings: {}, topics: [], concepts: [], selectedId: null, current: null, listPoller: null };

    var EFFORTS = [['low', 'Düşük'], ['medium', 'Orta'], ['high', 'Yüksek'], ['xhigh', 'Çok yüksek'], ['max', 'En yüksek']];
    var TONES = ['Bilgilendirici', 'Merak uyandıran', 'Dramatik', 'Esprili', 'Sakin', 'Heyecanlı'];
    var STYLES = ['Hikâye anlatımı', 'Liste (Top N)', 'Soru-cevap', 'Belgesel', 'Karşılaştırma'];

    function doc(c) { return (c && (c.document || c.content)) || c || {}; }

    function modelName(id) {
        for (var i = 0; i < state.models.length; i++) {
            var m = state.models[i];
            if (String(m.id) === String(id) || m.modelId === id) { return m.displayName || m.modelId; }
        }
        return id || 'Seçilmedi';
    }

    function renderSummaries() {
        var s = state.settings || {};
        $('#kp-models-summary').text('Konsept: ' + modelName(s.conceptModelId) + ' · Konu: ' + modelName(s.topicModelId) + ' · Denetim: ' + modelName(s.wordCheckModelId));
        var parts = $.grep([s.tone, s.audience, s.narrativeStyle], function (x) { return !!x; });
        $('#kp-tuning-summary').text(parts.length ? parts.join(' · ') : 'Varsayılan (profilden)');
        $('#kp-wordcheck').prop('checked', !!s.wordCheckEnabled);
    }

    function saveSettings(patch) {
        var next = $.extend({}, state.settings, patch);
        return TK.api.put('/Konseptler/Ayarlar', next).done(function (res) {
            state.settings = res || next;
            renderSummaries();
        });
    }

    function loadSettings() {
        TK.api.get('/Konseptler/Modeller', null, { silent: true }).done(function (data) { state.models = U.items(data); renderSummaries(); });
        TK.api.get('/Konseptler/Ayarlar', null, { silent: true })
            .done(function (data) { state.settings = data || {}; renderSummaries(); })
            .fail(function () { $('#kp-models-summary').text('Modeller alınamadı (API bağlantısı yok)'); });
    }

    function modelOptions(selected, capability) {
        var list = $.grep(state.models, function (m) {
            return !capability || !m.capabilities || String(m.capabilities).toLowerCase().indexOf(capability) >= 0 || $.inArray(capability, m.capabilities) >= 0;
        });
        if (!list.length) { return '<option value="">Model bulunamadı — Ayarlar\'dan API anahtarı ekleyin</option>'; }
        return $.map(list, function (m) {
            var id = m.id || m.modelId;
            var label = (m.platformName || m.platform ? (m.platformName || m.platform) + ' · ' : '') + (m.displayName || m.modelId) + (m.languageTag ? ' (' + m.languageTag + ')' : '');
            return '<option value="' + U.escape(id) + '"' + (String(id) === String(selected) ? ' selected' : '') + '>' + U.escape(label) + '</option>';
        }).join('');
    }

    function effortOptions(selected) {
        return $.map(EFFORTS, function (e) { return '<option value="' + e[0] + '"' + (e[0] === (selected || 'medium') ? ' selected' : '') + '>' + e[1] + '</option>'; }).join('');
    }

    function openModels() {
        var s = state.settings || {};
        var $b = $(
            '<div>' +
            '<div class="tk-field"><label>Konsept üretimi</label><div class="tk-row"><select class="tk-select tk-grow" name="conceptModelId">' + modelOptions(s.conceptModelId, 'text') + '</select>' +
            '<select class="tk-select" style="width:auto" name="conceptEffort" title="Efor">' + effortOptions(s.conceptEffort) + '</select></div>' +
            '<span class="tk-hint">Tam konsepti (sahneler, ekran metni, seslendirme) yazan model.</span></div>' +
            '<div class="tk-field"><label>Konu önerisi</label><select class="tk-select" name="topicModelId">' + modelOptions(s.topicModelId, 'text') + '</select>' +
            '<span class="tk-hint">Sadece başlık önerir; ucuz ve hızlı bir model yeterli.</span></div>' +
            '<div class="tk-field"><label>Kelime tekrar denetimi</label><select class="tk-select" name="wordCheckModelId">' + modelOptions(s.wordCheckModelId, 'text') + '</select></div>' +
            '</div>');
        TK.modal.open({
            title: 'Modeller', body: $b, hint: 'Pencereyi kapatınca kaydedilir.',
            onClose: function ($body) {
                return saveSettings({
                    conceptModelId: $body.find('[name=conceptModelId]').val() || null,
                    conceptEffort: $body.find('[name=conceptEffort]').val(),
                    topicModelId: $body.find('[name=topicModelId]').val() || null,
                    wordCheckModelId: $body.find('[name=wordCheckModelId]').val() || null
                });
            }
        });
    }

    function datalist(id, values) {
        return '<datalist id="' + id + '">' + $.map(values, function (v) { return '<option value="' + U.escape(v) + '">'; }).join('') + '</datalist>';
    }

    function openTuning() {
        var s = state.settings || {};
        var $b = $(
            '<div>' +
            '<div class="tk-field"><label for="kt-tone">Ton</label><input id="kt-tone" class="tk-input" name="tone" list="kt-tones" maxlength="80" placeholder="Ör. Merak uyandıran" />' + datalist('kt-tones', TONES) + '</div>' +
            '<div class="tk-field"><label for="kt-aud">İzleyici</label><input id="kt-aud" class="tk-input" name="audience" maxlength="120" placeholder="Ör. 18–35 yaş, tarih meraklısı" /></div>' +
            '<div class="tk-field"><label for="kt-style">Anlatım biçimi</label><input id="kt-style" class="tk-input" name="narrativeStyle" list="kt-styles" maxlength="80" placeholder="Ör. Hikâye anlatımı" />' + datalist('kt-styles', STYLES) + '</div>' +
            '<span class="tk-hint">Boş bırakılan alanlar aktif profildeki değerleri kullanır.</span>' +
            '</div>');
        $b.find('[name=tone]').val(s.tone || '');
        $b.find('[name=audience]').val(s.audience || '');
        $b.find('[name=narrativeStyle]').val(s.narrativeStyle || '');
        TK.modal.open({
            title: 'İnce ayar', body: $b, hint: 'Pencereyi kapatınca kaydedilir.',
            onClose: function ($body) {
                return saveSettings({
                    tone: $.trim($body.find('[name=tone]').val()) || null,
                    audience: $.trim($body.find('[name=audience]').val()) || null,
                    narrativeStyle: $.trim($body.find('[name=narrativeStyle]').val()) || null
                });
            }
        });
    }

    /* ---------------- Konudan üret ---------------- */
    function renderTopics() {
        var $list = $('#kp-topic-list');
        if (!state.topics.length) {
            $list.html(U.empty('bulb', 'Henüz öneri yok', 'Adet seçip "Konu öner"e basın. Daha önce üretilen ya da silinen konular tekrar önerilmez.'));
        } else {
            var html = '<ul class="tk-list">';
            $.each(state.topics, function (i, t) {
                html += '<li class="tk-list-item' + (t.selected ? ' is-selected' : '') + '" data-index="' + i + '">' +
                    '<input type="checkbox" aria-label="Seç" ' + (t.selected ? 'checked' : '') + ' style="accent-color:var(--tk-accent);width:17px;height:17px" />' +
                    '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(t.title) + '</div>' +
                    (t.reason || t.description ? '<div class="tk-list-sub">' + U.escape(t.reason || t.description) + '</div>' : '') + '</div></li>';
            });
            $list.html(html + '</ul>');
        }
        var n = $.grep(state.topics, function (t) { return t.selected; }).length;
        $('#kp-topic-selected').text(n);
        $('#kp-topic-generate').prop('disabled', n === 0);
        $('#kp-topic-all').prop('checked', state.topics.length > 0 && n === state.topics.length);
    }

    function suggestTopics() {
        var count = U.clampInt($('#kp-topic-count').val(), 1, 100, 10);
        $('#kp-topic-count').val(count);
        $('#kp-topic-list').html(U.skeleton(Math.min(count, 5)));
        U.busy($('#kp-topic-suggest'), TK.api.post('/Konseptler/KonuOner', { count: count, hint: $.trim($('#kp-topic-hint').val()) || null }))
            .done(function (data) {
                state.topics = $.map(U.items(data), function (t) { return typeof t === 'string' ? { title: t } : t; });
                renderTopics();
                TK.journal && TK.journal.push('info', state.topics.length + ' konu önerildi.');
            })
            .fail(function () { renderTopics(); });
    }

    function generateSelected() {
        var chosen = $.grep(state.topics, function (t) { return t.selected; });
        if (!chosen.length) { return; }
        U.busy($('#kp-topic-generate'), TK.api.post('/Konseptler/SecilenleriUret', {
            topics: $.map(chosen, function (t) { return { id: t.id || null, title: t.title }; })
        })).done(function () {
            TK.toast.success(chosen.length + ' konsept üretime alındı.');
            state.topics = $.grep(state.topics, function (t) { return !t.selected; });
            renderTopics();
            watchList();
        });
    }

    /* ---------------- Fikirden üret ---------------- */
    function generateFromIdea() {
        var idea = $.trim($('#kp-idea-text').val());
        if (idea.length < 5) { TK.toast.warning('Biraz daha ayrıntılı bir fikir yazın.'); $('#kp-idea-text').trigger('focus'); return; }
        var count = U.clampInt($('#kp-idea-count').val(), 1, 100, 1);
        $('#kp-idea-count').val(count);
        U.busy($('#kp-idea-generate'), TK.api.post('/Konseptler/FikirdenUret', { idea: idea, count: count })).done(function () {
            TK.toast.success('Fikir üretime alındı; sonuçlar Üretilenler\'e düşecek.');
            watchList();
        });
    }

    /* ---------------- Üretilenler ---------------- */
    function isPending(c) { var s = String(c.status || '').toLowerCase(); return s === 'generating' || s === 'pending' || s === 'queued'; }

    function renderList() {
        var $list = $('#kp-generated-list');
        $('#kp-generated-count').text(state.concepts.length ? state.concepts.length + ' konsept' : 'Boş');
        if (!state.concepts.length) {
            $list.html(U.empty('layers', 'Henüz konsept yok', 'Konudan ya da fikirden ürettiğiniz konseptler burada listelenir.'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(state.concepts, function (_, c) {
            var d = doc(c);
            var pending = isPending(c);
            var scenes = d.scenes ? d.scenes.length : (c.sceneCount || 0);
            html += '<li class="tk-list-item' + (String(c.id) === String(state.selectedId) ? ' is-selected' : '') + '" data-id="' + U.escape(c.id) + '">' +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(c.title || d.title || 'Adsız konsept') + '</div>' +
                '<div class="tk-list-sub">' + (pending ? '<span class="tk-badge is-accent">Üretiliyor…</span> ' : '') +
                (scenes ? scenes + ' sahne · ' : '') + U.escape(U.dateTime(c.createdAt)) + '</div></div>' +
                '<div class="tk-list-actions">' +
                '<button type="button" class="tk-mini-btn" data-act="edit" title="Düzenle" aria-label="Düzenle">' + U.icon('pencil') + '</button>' +
                '<button type="button" class="tk-mini-btn is-danger" data-act="delete" title="Sil" aria-label="Sil">' + U.icon('trash') + '</button>' +
                '</div></li>';
        });
        $list.html(html + '</ul>');
    }

    function loadList() {
        return TK.api.get('/Konseptler/Liste', null, { silent: true })
            .done(function (data) { state.concepts = U.items(data); renderList(); })
            .fail(function () { $('#kp-generated-list').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor; birazdan tekrar deneyin.')); });
    }

    function watchList() {
        if (state.listPoller) { state.listPoller.now(); return; }
        var started = Date.now();
        state.listPoller = TK.poll('/Konseptler/Liste', 3000, function (data) {
            state.concepts = U.items(data);
            renderList();
            var anyPending = $.grep(state.concepts, isPending).length > 0;
            if ((!anyPending && Date.now() - started > 10000) || Date.now() - started > 15 * 60000) { state.listPoller.stop(); state.listPoller = null; }
        });
    }

    function deleteConcept(id) {
        TK.modal.confirm('Konsepti sil', 'Konsept Silinenler\'e taşınır ve benzer konular bir daha önerilmez.', 'Sil').done(function () {
            TK.api.del('/Konseptler/Sil/' + encodeURIComponent(id)).done(function () {
                if (String(state.selectedId) === String(id)) { selectConcept(null); }
                TK.toast.info('Konsept Silinenler\'e taşındı.');
                loadList();
            });
        });
    }

    /* ---------------- Düzenleme alanı ---------------- */
    function sceneHtml(s, i) {
        return '<div class="tk-card" style="box-shadow:none;margin-bottom:10px" data-scene="' + i + '">' +
            '<div class="tk-card-head" style="padding:10px 12px"><strong>Sahne ' + (i + 1) + '</strong>' +
            '<input class="tk-input" name="sceneTitle" style="max-width:65%" value="' + U.escape(s.title || '') + '" placeholder="Sahne başlığı" /></div>' +
            '<div class="tk-card-body" style="padding:10px 12px">' +
            '<div class="tk-field"><label>Ekranda gösterilecek</label><textarea class="tk-textarea" rows="2" name="onScreen">' + U.escape(s.onScreen || '') + '</textarea></div>' +
            '<div class="tk-field" style="margin-bottom:0"><label>Seslendirme (yazılış)</label><textarea class="tk-textarea" rows="3" name="narration">' + U.escape(s.narration || '') + '</textarea></div>' +
            '</div></div>';
    }

    function renderEditor() {
        var c = state.current;
        var $ed = $('#kp-editor');
        if (!c) {
            $ed.html(U.empty('edit-doc', 'Düzenlenecek konsept seçilmedi', 'Soldaki listeden ✏️ ile bir konsept seçin; metinleri burada düzenleyin ya da düzeltme talimatı verin.'));
            $('#kp-editor-foot').prop('hidden', true);
            $('#kp-editor-sub').text('Üretilenlerden birini seçin');
            return;
        }
        var d = doc(c);
        var scenes = d.scenes || [];
        var html =
            '<div class="tk-tabs" role="tablist"><button type="button" class="tk-tab is-active" data-tab="text">Metinler</button><button type="button" class="tk-tab" data-tab="revise">Düzelterek tekrar üret</button></div>' +
            '<div class="tk-tab-panel is-active" data-panel="text">' +
            '<div class="tk-field"><label>Başlık</label><input class="tk-input" name="title" value="' + U.escape(d.title || c.title || '') + '" /></div>' +
            '<div class="tk-field"><label>Özet</label><textarea class="tk-textarea" rows="3" name="summary">' + U.escape(d.summary || '') + '</textarea></div>' +
            (scenes.length ? $.map(scenes, sceneHtml).join('') : '<p class="tk-muted">Bu konseptte henüz sahne yok.</p>') +
            '</div>' +
            '<div class="tk-tab-panel" data-panel="revise">' +
            '<div class="tk-field"><label for="kp-revise-text">Neler düzeltilsin?</label><textarea id="kp-revise-text" class="tk-textarea" rows="5" maxlength="4000" placeholder="Ör. Girişi daha çarpıcı yap, 3. sahnedeki tarihi kontrol et, seslendirmeyi kısalt…"></textarea></div>' +
            '<button type="button" class="tk-btn tk-btn-secondary" id="kp-revise">' + U.icon('refresh') + ' Düzelterek tekrar üret</button>' +
            '</div>';
        $ed.html(html);
        $('#kp-editor-foot').prop('hidden', false);
        $('#kp-editor-sub').text(d.title || c.title || '');
    }

    function readEditor() {
        var c = state.current;
        var d = $.extend(true, {}, doc(c));
        var $ed = $('#kp-editor');
        d.title = $.trim($ed.find('[name=title]').val());
        d.summary = $.trim($ed.find('[name=summary]').val());
        $ed.find('[data-scene]').each(function () {
            var i = parseInt($(this).attr('data-scene'), 10);
            if (!d.scenes || !d.scenes[i]) { return; }
            d.scenes[i].title = $.trim($(this).find('[name=sceneTitle]').val());
            d.scenes[i].onScreen = $(this).find('[name=onScreen]').val();
            d.scenes[i].narration = $(this).find('[name=narration]').val();
        });
        return d;
    }

    function selectConcept(id) {
        state.selectedId = id;
        state.current = null;
        renderList();
        if (!id) { renderEditor(); return; }
        $('#kp-editor').html(U.skeleton(4));
        TK.api.get('/Konseptler/Getir/' + encodeURIComponent(id)).done(function (c) { state.current = c; renderEditor(); })
            .fail(function () { renderEditor(); });
    }

    function saveEditor() {
        if (!state.current) { return $.Deferred().reject().promise(); }
        var d = readEditor();
        return TK.api.put('/Konseptler/Guncelle/' + encodeURIComponent(state.current.id), { title: d.title, document: d }).done(function (res) {
            state.current = res || $.extend(state.current, { title: d.title, document: d });
            TK.toast.success('Konsept kaydedildi.');
            loadList();
        });
    }

    function approve() {
        if (!state.current) { return; }
        var id = state.current.id;
        saveEditor().done(function () {
            TK.api.post('/Konseptler/Onayla/' + encodeURIComponent(id)).done(function () {
                TK.toast.success('Onaylandı — Konseptten Video sayfasında üretim sırasına ekleyebilirsiniz.');
                selectConcept(null);
                loadList();
            });
        });
    }

    function revise() {
        var text = $.trim($('#kp-revise-text').val());
        if (!text) { TK.toast.warning('Ne düzeltilmesini istediğinizi yazın.'); return; }
        var id = state.current.id;
        U.busy($('#kp-revise'), TK.api.post('/Konseptler/Revize/' + encodeURIComponent(id), { instruction: text, document: readEditor() }))
            .done(function (result) { showRevision(id, result); });
    }

    function showRevision(id, result) {
        var d = doc(result);
        var scenes = d.scenes || [];
        var body = '<h3 style="margin-bottom:6px">' + U.escape(d.title || '') + '</h3><p class="tk-muted">' + U.escape(d.summary || '') + '</p>' +
            $.map(scenes, function (s, i) {
                return '<div style="padding:10px 0;border-top:1px solid var(--tk-divider)"><strong>Sahne ' + (i + 1) + ': ' + U.escape(s.title || '') + '</strong>' +
                    '<div class="tk-hint">Ekran: ' + U.escape(s.onScreen || '') + '</div><div>' + U.escape(s.narration || '') + '</div></div>';
            }).join('');
        var $choose = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('check') + ' Seç</button>');
        var $again = $('<button type="button" class="tk-btn tk-btn-secondary">' + U.icon('pencil') + ' Bunu da düzenle</button>');
        var $drop = $('<button type="button" class="tk-btn tk-btn-danger">' + U.icon('trash') + ' Sil</button>');
        var h = TK.modal.open({ title: 'Düzeltilmiş sürüm', size: 'lg', body: body, footer: $('<div class="tk-row" style="margin-left:auto"></div>').append($drop, $again, $choose) });
        $choose.on('click', function () {
            TK.api.put('/Konseptler/Guncelle/' + encodeURIComponent(id), { title: d.title, document: d }).done(function (res) {
                state.current = res || $.extend(state.current, { document: d, title: d.title });
                renderEditor(); loadList(); h.close(true);
                TK.toast.success('Yeni sürüm seçildi.');
            });
        });
        $again.on('click', function () {
            state.current = $.extend({}, state.current, { document: d, title: d.title });
            renderEditor(); h.close(true);
            $('#kp-editor .tk-tab[data-tab=revise]').trigger('click');
        });
        $drop.on('click', function () { h.close(true); TK.toast.info('Düzeltilmiş sürüm atıldı.'); });
    }

    function bind() {
        $('#kp-models').on('click', openModels);
        $('#kp-tuning').on('click', openTuning);
        $('#kp-wordcheck').on('change', function () {
            var on = $(this).is(':checked');
            saveSettings({ wordCheckEnabled: on }).done(function () { TK.toast.info('Kelime denetimi ' + (on ? 'açıldı' : 'kapatıldı') + '.'); })
                .fail(function () { $('#kp-wordcheck').prop('checked', !on); });
        });

        $('#kp-topic-suggest').on('click', suggestTopics);
        $('#kp-topic-hint').on('keydown', function (e) { if (e.key === 'Enter') { suggestTopics(); } });
        $('#kp-topic-list').on('click', '.tk-list-item', function (e) {
            var i = parseInt($(this).attr('data-index'), 10);
            if (!$(e.target).is('input')) { state.topics[i].selected = !state.topics[i].selected; } else { state.topics[i].selected = $(e.target).is(':checked'); }
            renderTopics();
        });
        $('#kp-topic-all').on('change', function () {
            var on = $(this).is(':checked');
            $.each(state.topics, function (_, t) { t.selected = on; });
            renderTopics();
        });
        $('#kp-topic-generate').on('click', generateSelected);
        $('#kp-idea-generate').on('click', generateFromIdea);

        $('#kp-generated-refresh').on('click', loadList);
        $('#kp-generated-list').on('click', '.tk-list-item', function (e) {
            var id = $(this).attr('data-id');
            var act = $(e.target).closest('[data-act]').attr('data-act');
            if (act === 'delete') { deleteConcept(id); return; }
            selectConcept(id);
        });

        $('#kp-editor').on('click', '.tk-tab', function () {
            var tab = $(this).attr('data-tab');
            $('#kp-editor .tk-tab').removeClass('is-active').filter(this).addClass('is-active');
            $('#kp-editor .tk-tab-panel').removeClass('is-active').filter('[data-panel=' + tab + ']').addClass('is-active');
        });
        $('#kp-editor').on('click', '#kp-revise', revise);
        $('#kp-editor-save').on('click', function () { U.busy($(this), saveEditor()); });
        $('#kp-editor-approve').on('click', approve);

        $(document).on('tk:profile', function () { state.topics = []; renderTopics(); loadList(); });
    }

    function init() {
        if (!$('#tk-konseptler').length) { return; }
        U = TK.util;
        bind();
        renderTopics();
        renderEditor();
        $('#kp-generated-list').html(U.skeleton(4));
        loadSettings();
        loadList();
    }

    TK.konseptler = { init: init };
    $(init);
})(window, window.jQuery);
