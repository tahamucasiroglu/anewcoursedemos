/**
 * TK.konseptler — Konseptler sayfası.
 * - Model şeridi (tık → üç model + efor penceresi), ince ayar (tık → ton/izleyici/anlatım/not/gerçek bilgi penceresi),
 *   kelime denetimi anahtarı (gerçekten ayarı kaydeder).
 * - Konudan üret: adet + yön → Öner (yalnız başlık + kanca), seç / 👎 beğenme → "Seçtiklerini üret" (arka plan işi).
 * - Fikirden üret: metin + adet → arka plan işi. İş ilerlemesi üstte çubukla (TK.poll /Konseptler/Gorev, 2 sn), Durdur.
 * - Üretilenler: ✏️ düzenle, ✔ onayla, 🗑 sil. Düzenleme alanı: başlık/kanca/özet + sahneler (başlık, ekran, seslendirme,
 *   görsel istemi), sahne ekle/sil, kaydet, onayla; "Şunları düzelterek tekrar üret" → aday penceresi: Seç / Sil / Bunu da düzenle.
 * MVC uçları: /Konseptler/{Modeller, Ayarlar, KonuOner, KonuBegenme, SecilenleriUret, FikirdenUret, Gorev, GorevIptal,
 * Liste, Getir, Guncelle, Sil, Revize, AdayKabul, AdaySil, Onayla}. Yalnız TK.api ($.ajax). Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    /* Anlatım hızı (kelime/sn): sunucu üretimdeki ses modeline göre verir (EffectiveWordsPerSecond); gelene kadar yedek. */
    var WPS = 2.0;
    var EFFORT_LABELS = { Low: 'Düşük', Medium: 'Orta', High: 'Yüksek', XHigh: 'Çok yüksek', Max: 'En yüksek' };
    var TONES = ['Bilgilendirici', 'Merak uyandıran', 'Dramatik', 'Esprili', 'Sakin', 'Heyecanlı', 'Gizemli'];
    var AUDIENCES = ['Genel izleyici', '18–35 yaş, meraklı', 'Tarih meraklıları', 'Öğrenciler', 'Teknoloji meraklıları'];
    var STYLES = ['Hikâye anlatımı', 'Liste (Top N)', 'Soru-cevap', 'Belgesel', 'Karşılaştırma', 'Ters köşe finalli'];

    var state = {
        models: [], settings: {}, topics: [], concepts: [], selectedId: null, current: null, draft: null,
        dirty: false, job: null, jobPoller: null, lastCompleted: -1
    };

    /* ================================================================ Yardımcılar */
    function words(text) { var m = String(text || '').match(/[^\s]+/g); return m ? m.length : 0; }
    function secs(text) { return Math.max(0, Math.round(words(text) / WPS * 10) / 10); }
    function fmtSec(s) { s = Math.round(s || 0); return s >= 60 ? Math.floor(s / 60) + ' dk ' + (s % 60) + ' sn' : s + ' sn'; }
    /* Geçiş kayıtları: model kimliği kullanıcıdan (openrouter:<id>, local-openai:<model adı>). */
    var PASSTHROUGH = { 'openrouter': 'OpenRouter model kimliği yaz…', 'local-openai': 'Yerel model adı yaz… (Yerel LLM)' };
    var PT_PLACEHOLDER = { 'openrouter': 'ör. anthropic/claude-sonnet-5.5', 'local-openai': 'Yerel model adı (ör. qwen3:8b)' };
    function passthroughOf(key) {
        var hit = null;
        $.each(PASSTHROUGH, function (base) { if (key && key.indexOf(base + ':') === 0 && key.length > base.length + 1) { hit = base; } });
        return hit;
    }
    function model(key) {
        var base = passthroughOf(key);
        return $.grep(state.models, function (m) { return m.key === (base || key); })[0] || null;
    }
    function modelName(key) {
        var m = model(key);
        var base = passthroughOf(key);
        if (m && base) { return m.displayName + ': ' + key.slice(base.length + 1); }
        return m ? m.displayName : (key || 'Seçilmedi');
    }
    function enc(id) { return encodeURIComponent(id); }

    /* ================================================================ Ayarlar + modeller */
    function renderSummaries() {
        var s = state.settings || {};
        var effort = s.conceptEffort ? ' (' + (EFFORT_LABELS[s.conceptEffort] || s.conceptEffort) + ')' : '';
        $('#kp-models-summary').text('Konsept: ' + modelName(s.conceptModelKey) + effort + ' · Konu: ' + modelName(s.topicModelKey) +
            ' · Denetim: ' + modelName(s.wordCheckModelKey));
        var parts = $.grep([s.tone, s.audience, s.narrativeStyle], function (x) { return !!x; });
        if (s.notes) { parts.push('not'); }
        $('#kp-tuning-summary').text(parts.length ? parts.join(' · ') : 'Profilden');
        $('#kp-wordcheck').prop('checked', !!s.wordRepetitionCheck);
        var fact = !!s.effectiveFactGrounding;
        $('#kp-fact-badge').prop('hidden', false).toggleClass('is-success', fact)
            .text(fact ? 'Gerçek bilgi: açık' + (s.factGrounding === null || s.factGrounding === undefined ? ' (oto)' : '') : 'Gerçek bilgi: kapalı');
        $('#kp-noweb-chip').prop('hidden', !(fact && !canSearchWeb(s)));
        if (Number(s.effectiveWordsPerSecond) > 0) { WPS = Number(s.effectiveWordsPerSecond); }
    }

    /** Seçili konsept modeli web araması yapabiliyor mu: sunucu yanıtı (conceptModelCanSearchWeb), yoksa model listesi. */
    function canSearchWeb(s) {
        if (s.conceptModelCanSearchWeb === true || s.conceptModelCanSearchWeb === false) { return s.conceptModelCanSearchWeb; }
        var m = model(s.conceptModelKey);
        return !m || (!!m.supportsWebSearch && m.platform !== 'local-openai');
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
            .fail(function () { $('#kp-models-summary').text('Ayarlar alınamadı (API bağlantısı yok)'); });
    }

    function priceText(m) {
        if (!m || !m.pricing) { return ''; }
        var p = m.pricing;
        if (p.inputPerMTok === null || p.inputPerMTok === undefined) { return p.providerReportsCost ? 'Fiyat sağlayıcıdan' : ''; }
        if (Number(p.inputPerMTok) === 0 && Number(p.outputPerMTok) === 0) { return 'Ücretsiz (yerel)'; }
        return '1M token: $' + p.inputPerMTok + ' giriş / $' + p.outputPerMTok + ' çıkış' + (m.supportsWebSearch ? ' · web araması' : '');
    }

    function modelOptions(selected) {
        if (!state.models.length) { return '<option value="' + U.escape(selected || '') + '">' + U.escape(selected || 'Model listesi alınamadı') + '</option>'; }
        var html = '';
        var groups = {};
        $.each(state.models, function (_, m) { (groups[m.platform] = groups[m.platform] || []).push(m); });
        $.each(groups, function (platform, list) {
            html += '<optgroup label="' + U.escape(platform) + '">';
            $.each(list, function (_, m) {
                if (PASSTHROUGH[m.key]) {
                    html += '<option value="__pt:' + U.escape(m.key) + '"' + (passthroughOf(selected) === m.key ? ' selected' : '') + '>' + U.escape(PASSTHROUGH[m.key]) + '</option>';
                    return;
                }
                html += '<option value="' + U.escape(m.key) + '"' + (m.key === selected ? ' selected' : '') + '>' +
                    U.escape(m.displayName + (m.languageLabel ? ' (' + m.languageLabel + ')' : '')) + '</option>';
            });
            html += '</optgroup>';
        });
        if (selected && !model(selected)) { html = '<option value="' + U.escape(selected) + '" selected>' + U.escape(selected) + '</option>' + html; }
        return html;
    }

    /** Geçiş seçeneği için model adı kutusu (seçili değilse gizli). */
    function passthroughInput(name, selected) {
        var base = passthroughOf(selected);
        return '<input type="text" class="tk-input kp-pt-input" data-pt-for="' + name + '" maxlength="190" spellcheck="false" autocomplete="off"' +
            ' value="' + U.escape(base ? selected.slice(base.length + 1) : '') + '"' + (base ? '' : ' hidden') + ' aria-label="Model adı" />';
    }

    /** Seçimi okur: geçiş seçeneğinde önek + yazılan ad (boşsa önceki değer). */
    function readModel($body, name, fallback) {
        var v = $body.find('[name=' + name + ']').val();
        if (v && v.indexOf('__pt:') === 0) {
            var id = $.trim($body.find('[data-pt-for=' + name + ']').val());
            return id ? v.slice(5) + ':' + id : fallback;
        }
        return v || fallback;
    }

    function effortOptions(modelKey, selected) {
        var m = model(modelKey);
        var levels = m && m.effortLevels && m.effortLevels.length ? m.effortLevels : [];
        var html = '<option value="">Model varsayılanı</option>';
        $.each(levels, function (_, e) {
            html += '<option value="' + e + '"' + (e === selected ? ' selected' : '') + '>' + (EFFORT_LABELS[e] || e) + '</option>';
        });
        return html;
    }

    function openModels() {
        var s = state.settings || {};
        var $b = $(
            '<div>' +
            '<div class="tk-field"><label for="km-concept">Konsept üretimi</label><div class="kp-model-row">' +
            '<select class="tk-select" id="km-concept" name="conceptModelKey">' + modelOptions(s.conceptModelKey) + '</select>' +
            '<select class="tk-select" name="conceptEffort" title="Efor (düşünme derinliği)"></select></div>' + passthroughInput('conceptModelKey', s.conceptModelKey) +
            '<span class="tk-hint">Tam konsepti (sahneler, ekran metni, seslendirme, görsel istemi) yazan model. <span class="kp-model-price" data-price="conceptModelKey"></span></span></div>' +
            '<div class="tk-field"><label for="km-topic">Konu önerisi</label><select class="tk-select" id="km-topic" name="topicModelKey">' + modelOptions(s.topicModelKey) + '</select>' + passthroughInput('topicModelKey', s.topicModelKey) +
            '<span class="tk-hint">Yalnız başlık önerir; ucuz ve hızlı bir model yeterli. <span class="kp-model-price" data-price="topicModelKey"></span></span></div>' +
            '<div class="tk-field"><label for="km-word">Kelime tekrar denetimi</label><select class="tk-select" id="km-word" name="wordCheckModelKey">' + modelOptions(s.wordCheckModelKey) + '</select>' + passthroughInput('wordCheckModelKey', s.wordCheckModelKey) +
            '<span class="tk-hint">Önce ücretsiz yerel analiz; yalnız tekrar bulunursa ilgili sahneler bu modelle yeniden yazılır. <span class="kp-model-price" data-price="wordCheckModelKey"></span></span></div>' +
            '</div>');
        function refresh() {
            var key = $b.find('[name=conceptModelKey]').val();
            var current = $b.find('[name=conceptEffort]').val() || s.conceptEffort;
            $b.find('[name=conceptEffort]').html(effortOptions(key, current));
            $b.find('[data-pt-for]').each(function () {
                var v = $b.find('[name=' + $(this).attr('data-pt-for') + ']').val() || '';
                var on = v.indexOf('__pt:') === 0;
                $(this).prop('hidden', !on).attr('placeholder', on ? PT_PLACEHOLDER[v.slice(5)] || 'Model adı' : '');
            });
            $b.find('[data-price]').each(function () {
                var v = $b.find('[name=' + $(this).attr('data-price') + ']').val() || '';
                $(this).text(priceText(model(v.indexOf('__pt:') === 0 ? v.slice(5) : v)));
            });
        }
        $b.on('change', 'select', refresh);
        refresh();
        TK.modal.open({
            title: 'Modeller', body: $b, hint: 'Pencereyi kapatınca kaydedilir.',
            onClose: function ($body) {
                return saveSettings({
                    conceptModelKey: readModel($body, 'conceptModelKey', s.conceptModelKey),
                    conceptEffort: $body.find('[name=conceptEffort]').val() || null,
                    topicModelKey: readModel($body, 'topicModelKey', s.topicModelKey),
                    wordCheckModelKey: readModel($body, 'wordCheckModelKey', s.wordCheckModelKey)
                });
            }
        });
    }

    function datalist(id, values) {
        return '<datalist id="' + id + '">' + $.map(values, function (v) { return '<option value="' + U.escape(v) + '">'; }).join('') + '</datalist>';
    }

    function openTuning() {
        var s = state.settings || {};
        var factVal = s.factGrounding === true ? 'on' : s.factGrounding === false ? 'off' : 'auto';
        var $b = $(
            '<div>' +
            '<div class="tk-form-grid">' +
            '<div class="tk-field"><label for="kt-tone">Ton</label><input id="kt-tone" class="tk-input" name="tone" list="kt-tones" maxlength="200" placeholder="Ör. Merak uyandıran" />' + datalist('kt-tones', TONES) + '</div>' +
            '<div class="tk-field"><label for="kt-aud">İzleyici</label><input id="kt-aud" class="tk-input" name="audience" list="kt-auds" maxlength="200" placeholder="Ör. 18–35 yaş, tarih meraklısı" />' + datalist('kt-auds', AUDIENCES) + '</div>' +
            '<div class="tk-field"><label for="kt-style">Anlatım biçimi</label><input id="kt-style" class="tk-input" name="narrativeStyle" list="kt-styles" maxlength="200" placeholder="Ör. Hikâye anlatımı" />' + datalist('kt-styles', STYLES) + '</div>' +
            '<div class="tk-field"><label for="kt-fact">Gerçek bilgi (web araması)</label><select id="kt-fact" class="tk-select" name="fact">' +
            '<option value="auto">Otomatik (tarih/spor/bilim kanallarında açık)</option><option value="on">Açık — kaynak zorunlu</option><option value="off">Kapalı</option></select></div>' +
            '</div>' +
            '<div class="tk-field"><label for="kt-notes">Ek notlar</label><textarea id="kt-notes" class="tk-textarea" name="notes" rows="3" maxlength="1500" placeholder="Ör. Argo kullanma; her videonun sonunda izleyiciye soru sor; rakamları yuvarlama."></textarea></div>' +
            '<span class="tk-hint">Boş bırakılan alanlar aktif profildeki değerleri kullanır. Gerçek bilgi açıkken uydurma yapılmaz, kaynaklar listelenir (biraz daha pahalı).</span>' +
            '</div>');
        $b.find('[name=tone]').val(s.tone || '');
        $b.find('[name=audience]').val(s.audience || '');
        $b.find('[name=narrativeStyle]').val(s.narrativeStyle || '');
        $b.find('[name=notes]').val(s.notes || '');
        $b.find('[name=fact]').val(factVal);
        TK.modal.open({
            title: 'İnce ayar', body: $b, hint: 'Pencereyi kapatınca kaydedilir.',
            onClose: function ($body) {
                var f = $body.find('[name=fact]').val();
                return saveSettings({
                    tone: $.trim($body.find('[name=tone]').val()) || null,
                    audience: $.trim($body.find('[name=audience]').val()) || null,
                    narrativeStyle: $.trim($body.find('[name=narrativeStyle]').val()) || null,
                    notes: $.trim($body.find('[name=notes]').val()) || null,
                    factGrounding: f === 'on' ? true : f === 'off' ? false : null
                });
            }
        });
    }

    /* ================================================================ Konudan üret */
    function renderTopics() {
        var $list = $('#kp-topic-list');
        if (!state.topics.length) {
            $list.html(U.empty('bulb', 'Henüz öneri yok', 'Adet seçip "Öner"e basın. Daha önce üretilen, silinen ya da beğenmediğiniz konular tekrar önerilmez.'));
        } else {
            var html = '<ul class="tk-list">';
            $.each(state.topics, function (i, t) {
                html += '<li class="tk-list-item kp-topic-item' + (t.selected ? ' is-selected' : '') + '" data-index="' + i + '">' +
                    '<input type="checkbox" aria-label="Seç" ' + (t.selected ? 'checked' : '') + ' />' +
                    '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(t.title) + '</div>' +
                    (t.hook ? '<div class="tk-list-sub">' + U.escape(t.hook) + '</div>' : '') + '</div>' +
                    '<div class="tk-list-actions"><button type="button" class="tk-mini-btn is-danger" data-act="dislike" title="Beğenmedim — bir daha önerme" aria-label="Beğenmedim">👎</button></div></li>';
            });
            $list.html(html + '</ul>');
        }
        var n = $.grep(state.topics, function (t) { return t.selected; }).length;
        $('#kp-topic-selected').text(n);
        $('#kp-topic-generate').prop('disabled', n === 0 || isJobActive());
        $('#kp-topic-all').prop('checked', state.topics.length > 0 && n === state.topics.length);
    }

    function suggestTopics() {
        var count = U.clampInt($('#kp-topic-count').val(), 1, 100, 10);
        $('#kp-topic-count').val(count);
        $('#kp-topic-list').html(U.skeleton(Math.min(count, 5)));
        $('#kp-topic-info').text('');
        U.busy($('#kp-topic-suggest'), TK.api.post('/Konseptler/KonuOner', { count: count, hint: $.trim($('#kp-topic-hint').val()) || null }, { timeout: 180000 }))
            .done(function (data) {
                state.topics = $.map(U.items(data), function (t) { return { title: t.title, hook: t.hook, selected: false }; });
                renderTopics();
                var info = state.topics.length + ' öneri · ' + U.money(data && data.costUsd);
                if (data && data.filteredOut) { info += ' · ' + data.filteredOut + ' tekrar elendi'; }
                $('#kp-topic-info').text(info);
            })
            .fail(function () { renderTopics(); });
    }

    function dislikeTopic(i) {
        var t = state.topics[i];
        if (!t) { return; }
        state.topics.splice(i, 1);
        renderTopics();
        TK.api.post('/Konseptler/KonuBegenme', { titles: [t.title] }, { silent: true })
            .done(function () { TK.toast.info('"' + t.title + '" bir daha önerilmeyecek.'); });
    }

    function generateSelected() {
        var chosen = $.grep(state.topics, function (t) { return t.selected; });
        if (!chosen.length) { return; }
        U.busy($('#kp-topic-generate'), TK.api.post('/Konseptler/SecilenleriUret', {
            titles: $.map(chosen, function (t) { return t.title; })
        })).done(function (job) {
            TK.toast.success(chosen.length + ' konsept üretime alındı.');
            state.topics = $.grep(state.topics, function (t) { return !t.selected; });
            renderTopics();
            startJobWatch(job);
        });
    }

    /* ================================================================ Fikirden üret */
    function generateFromIdea() {
        var idea = $.trim($('#kp-idea-text').val());
        if (idea.length < 5) { TK.toast.warning('Biraz daha ayrıntılı bir fikir yazın.'); $('#kp-idea-text').trigger('focus'); return; }
        var count = U.clampInt($('#kp-idea-count').val(), 1, 100, 1);
        $('#kp-idea-count').val(count);
        U.busy($('#kp-idea-generate'), TK.api.post('/Konseptler/FikirdenUret', { idea: idea, count: count })).done(function (job) {
            TK.toast.success(count > 1 ? count + ' farklı açıdan konsept üretime alındı.' : 'Fikir üretime alındı; sonuç Üretilenler\'e düşecek.');
            startJobWatch(job);
        });
    }

    /* ================================================================ Arka plan işi */
    function isJobActive() { var s = state.job && state.job.status; return s === 'Running' || s === 'Queued'; }

    function renderJob() {
        var j = state.job;
        var $box = $('#kp-job');
        if (!j || j.status === 'Idle') { $box.prop('hidden', true); return; }
        var active = isJobActive();
        var total = j.total || 0;
        var done = (j.completed || 0) + (j.failed || 0);
        var pct = total ? Math.round(done / total * 100) : (active ? 5 : 100);
        $box.prop('hidden', false).toggleClass('is-done', j.status === 'Completed').toggleClass('is-failed', j.status === 'Failed' || j.status === 'Cancelled');
        var title = j.status === 'Queued' ? 'Sırada bekliyor…'
            : j.status === 'Running' ? 'Konseptler üretiliyor… ' + (j.completed || 0) + '/' + (total || '?')
            : j.status === 'Completed' ? (j.completed || 0) + ' konsept üretildi'
            : j.status === 'Cancelled' ? 'Durduruldu'
            : 'Üretim başarısız';
        $('#kp-job-title').text(title);
        $('#kp-job-current').text(active ? (j.currentTitle ? '— ' + j.currentTitle : '') : (j.message || ''));
        var meta = U.money(j.costUsd);
        if (j.failed) { meta += ' · ' + j.failed + ' hata'; }
        if (j.queuedJobs) { meta += ' · sırada ' + j.queuedJobs + ' iş'; }
        $('#kp-job-meta').text(meta);
        $('#kp-job-bar').css('width', pct + '%');
        $('#kp-job-cancel').prop('hidden', !active);
        $('#kp-job-close').prop('hidden', active);
        $('#kp-topic-generate').prop('disabled', active || !$.grep(state.topics, function (t) { return t.selected; }).length);
    }

    function onJob(job) {
        var wasActive = isJobActive();
        state.job = job || null;
        renderJob();
        var completed = job ? (job.completed || 0) : 0;
        if (completed !== state.lastCompleted) { state.lastCompleted = completed; loadList(); }
        if (wasActive && !isJobActive() && job) {
            stopJobWatch();
            loadList();
            if (job.status === 'Completed') {
                (job.failed ? TK.toast.warning : TK.toast.success)((job.completed || 0) + ' konsept üretildi (' + U.money(job.costUsd) + ')' + (job.failed ? ', ' + job.failed + ' hata' : '') + '.');
            } else if (job.status === 'Failed') {
                TK.toast.error(job.message || 'Konsept üretilemedi.');
            } else if (job.status === 'Cancelled') {
                TK.toast.info('Üretim durduruldu.');
            }
        }
    }

    function startJobWatch(job) {
        if (job) { state.job = job; state.lastCompleted = job.completed || 0; renderJob(); }
        if (state.jobPoller) { state.jobPoller.now(); return; }
        state.jobPoller = TK.poll('/Konseptler/Gorev', 2000, onJob);
    }

    function stopJobWatch() { if (state.jobPoller) { state.jobPoller.stop(); state.jobPoller = null; } }

    function checkJobOnce() {
        TK.api.get('/Konseptler/Gorev', null, { silent: true }).done(function (job) {
            state.job = job || null;
            if (isJobActive()) { startJobWatch(job); } else { renderJob(); }
        });
    }

    function cancelJob() {
        TK.modal.confirm('Üretimi durdur', 'Çalışan ve sırada bekleyen konsept üretimleri durdurulacak. Şimdiye kadar üretilenler kalır.', 'Durdur').done(function () {
            TK.api.post('/Konseptler/GorevIptal').done(function (job) { onJob(job); if (state.jobPoller) { state.jobPoller.now(); } });
        });
    }

    /* ================================================================ Üretilenler */
    function renderList() {
        var $list = $('#kp-generated-list');
        $('#kp-generated-count').text(state.concepts.length ? state.concepts.length + ' konsept' : 'Boş');
        if (!state.concepts.length) {
            $list.html(U.empty('layers', 'Henüz konsept yok', 'Konudan ya da fikirden ürettiğiniz konseptler burada listelenir.'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(state.concepts, function (_, c) {
            html += '<li class="tk-list-item' + (String(c.id) === String(state.selectedId) ? ' is-selected' : '') + '" data-id="' + U.escape(c.id) + '">' +
                '<div class="tk-list-main"><div class="tk-list-title" title="' + U.escape(c.title) + '">' + U.escape(c.title || 'Adsız konsept') + '</div>' +
                '<div class="tk-list-sub">' + U.escape(c.summary || '') + '</div>' +
                '<div class="kp-concept-meta"><span class="tk-badge">' + (c.sceneCount || 0) + ' sahne</span>' +
                '<span class="tk-badge">~' + fmtSec(c.estimatedDurationSec) + '</span>' +
                (c.origin === 'Idea' ? '<span class="tk-badge is-info">Fikir</span>' : '') +
                '<span class="tk-hint">' + U.escape(U.money(c.costUsd)) + ' · ' + U.escape(U.dateTime(c.createdAt)) + '</span></div></div>' +
                '<div class="tk-list-actions">' +
                '<button type="button" class="tk-mini-btn" data-act="edit" title="Düzenle" aria-label="Düzenle">' + U.icon('pencil') + '</button>' +
                '<button type="button" class="tk-mini-btn is-success" data-act="approve" title="Onayla (Konseptten Video\'ya geçer)" aria-label="Onayla">' + U.icon('check') + '</button>' +
                '<button type="button" class="tk-mini-btn is-danger" data-act="delete" title="Sil (Silinenler\'e taşınır)" aria-label="Sil">' + U.icon('trash') + '</button>' +
                '</div></li>';
        });
        $list.html(html + '</ul>');
    }

    function loadList() {
        return TK.api.get('/Konseptler/Liste', null, { silent: true })
            .done(function (data) { state.concepts = U.items(data); renderList(); })
            .fail(function () { $('#kp-generated-list').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor; birazdan tekrar deneyin.')); });
    }

    function deleteConcept(id) {
        TK.modal.confirm('Konsepti sil', 'Konsept Silinenler\'e taşınır ve benzer konular bir daha önerilmez.', 'Sil').done(function () {
            TK.api.del('/Konseptler/Sil/' + enc(id)).done(function () {
                if (String(state.selectedId) === String(id)) { selectConcept(null, true); }
                TK.toast.info('Konsept Silinenler\'e taşındı.');
                loadList();
            });
        });
    }

    function approveConcept(id) {
        TK.api.post('/Konseptler/Onayla/' + enc(id)).done(function () {
            TK.toast.success('Onaylandı — Konseptten Video sayfasında üretim sırasına ekleyebilirsiniz.');
            if (String(state.selectedId) === String(id)) { selectConcept(null, true); }
            loadList();
        });
    }

    /* ================================================================ Düzenleme alanı */
    function setDirty(on) { state.dirty = !!on; $('#kp-editor-dirty').prop('hidden', !state.dirty); }

    function sceneHtml(s, i) {
        return '<div class="kp-scene" data-scene="' + i + '">' +
            '<div class="kp-scene-head"><span class="kp-scene-no">' + (i + 1) + '</span>' +
            '<input class="tk-input" name="sceneTitle" value="' + U.escape(s.title || '') + '" placeholder="Sahne başlığı" aria-label="Sahne başlığı" />' +
            '<span class="kp-scene-dur" data-dur>' + fmtSec(secs(s.narration)) + '</span>' +
            '<button type="button" class="tk-mini-btn is-danger" data-act="remove-scene" title="Sahneyi sil" aria-label="Sahneyi sil">' + U.icon('x') + '</button></div>' +
            '<div class="kp-scene-body">' +
            '<div class="tk-field kp-wide"><label>Seslendirme (yazılış)</label><textarea class="tk-textarea" rows="3" name="narration">' + U.escape(s.narration || '') + '</textarea></div>' +
            '<div class="tk-field"><label>Ekranda gösterilecek</label><textarea class="tk-textarea" rows="2" name="onScreen">' + U.escape(s.onScreen || '') + '</textarea></div>' +
            '<div class="tk-field"><label>Görsel istemi (İngilizce)</label><textarea class="tk-textarea" rows="2" name="visualPrompt">' + U.escape(s.visualPrompt || '') + '</textarea></div>' +
            '</div></div>';
    }

    function extrasHtml(d) {
        var html = '<div class="kp-extra">';
        if (d.tags && d.tags.length) {
            html += '<div><h3>Etiketler</h3><div class="kp-chips">' + $.map(d.tags, function (t) { return '<span class="tk-badge">#' + U.escape(t) + '</span>'; }).join('') + '</div></div>';
        }
        if (d.pronunciations && d.pronunciations.length) {
            html += '<div><h3>Okunuş önerileri</h3><div class="kp-chips">' + $.map(d.pronunciations, function (p) {
                return '<span class="tk-badge is-info">' + U.escape(p.written) + ' → ' + U.escape(p.spoken) + '</span>';
            }).join('') + '</div></div>';
        }
        if (d.reviewNotes && d.reviewNotes.length) {
            html += '<div><h3>Kontrol edilecekler</h3><ul class="kp-review">' + $.map(d.reviewNotes, function (n) {
                return '<li>' + U.escape(n) + '</li>';
            }).join('') + '</ul></div>';
        }
        if (d.sources && d.sources.length) {
            html += '<div><h3>Kaynaklar</h3><ol class="kp-sources">' + $.map(d.sources, function (s) {
                return '<li><a href="' + U.escape(s.url) + '" target="_blank" rel="noopener noreferrer">' + U.escape(s.title || s.url) + '</a></li>';
            }).join('') + '</ol></div>';
        }
        return html + '</div>';
    }

    function renderEditor() {
        var c = state.current;
        var $ed = $('#kp-editor');
        setDirty(false);
        if (!c) {
            $ed.html(U.empty('edit-doc', 'Düzenlenecek konsept seçilmedi', 'Soldaki listeden ✏️ ile bir konsept seçin; metinleri burada düzenleyin ya da düzeltme talimatı verin.'));
            $('#kp-editor-foot').prop('hidden', true);
            $('#kp-editor-sub').text('Üretilenlerden birini seçin');
            return;
        }
        var d = state.draft;
        var html =
            '<div class="tk-tabs" role="tablist"><button type="button" class="tk-tab is-active" data-tab="text">Metinler</button><button type="button" class="tk-tab" data-tab="revise">Düzelterek tekrar üret</button></div>' +
            '<div class="tk-tab-panel is-active" data-panel="text">' +
            '<div class="kp-doc-head">' +
            '<div class="tk-field"><label for="kp-f-title">Başlık</label><input id="kp-f-title" class="tk-input" name="title" maxlength="300" value="' + U.escape(d.title || '') + '" /></div>' +
            '<div class="tk-field"><label for="kp-f-hook">Kanca (ilk 2 saniye)</label><input id="kp-f-hook" class="tk-input" name="hook" value="' + U.escape(d.hook || '') + '" /></div>' +
            '<div class="tk-field"><label for="kp-f-summary">Özet</label><textarea id="kp-f-summary" class="tk-textarea" rows="2" name="summary">' + U.escape(d.summary || '') + '</textarea></div>' +
            '</div>' +
            '<div id="kp-scenes">' + $.map(d.scenes || [], sceneHtml).join('') + '</div>' +
            '<button type="button" class="tk-btn tk-btn-ghost kp-add-scene" data-act="add-scene">' + U.icon('plus') + ' Sahne ekle</button>' +
            extrasHtml(d) +
            '</div>' +
            '<div class="tk-tab-panel" data-panel="revise"><div class="kp-revise-box">' +
            '<div class="tk-field"><label for="kp-revise-text">Şunları düzelterek tekrar üret</label><textarea id="kp-revise-text" class="tk-textarea" rows="5" maxlength="4000" placeholder="Ör. Girişi daha çarpıcı yap, 3. sahnedeki tarihi kontrol et, seslendirmeyi 10 saniye kısalt…"></textarea></div>' +
            '<span class="tk-hint">Sonuç ayrı bir pencerede açılır; seçmezseniz mevcut konsept değişmez. Kaydedilmemiş düzenlemeleriniz de talimata dahil edilir.</span>' +
            '<div class="tk-row"><button type="button" class="tk-btn tk-btn-secondary" id="kp-revise">' + U.icon('refresh') + ' Düzelterek tekrar üret</button></div>' +
            '</div></div>';
        $ed.html(html);
        $('#kp-editor-foot').prop('hidden', false);
        $('#kp-editor-sub').text(d.title || c.title || '');
        updateStats();
    }

    function readEditor() {
        var d = $.extend(true, {}, state.draft);
        var $ed = $('#kp-editor');
        if (!$ed.find('[name=title]').length) { return d; }
        d.title = $.trim($ed.find('[name=title]').val());
        d.hook = $.trim($ed.find('[name=hook]').val());
        d.summary = $.trim($ed.find('[name=summary]').val());
        var scenes = [];
        $ed.find('[data-scene]').each(function (i) {
            var orig = (state.draft.scenes || [])[parseInt($(this).attr('data-scene'), 10)] || {};
            var narration = $(this).find('[name=narration]').val();
            scenes.push($.extend({}, orig, {
                index: i,
                title: $.trim($(this).find('[name=sceneTitle]').val()),
                onScreen: $(this).find('[name=onScreen]').val(),
                narration: narration,
                visualPrompt: $(this).find('[name=visualPrompt]').val(),
                durationHintSec: orig.narration === narration && orig.durationHintSec ? orig.durationHintSec : secs(narration)
            }));
        });
        d.scenes = scenes;
        return d;
    }

    function updateStats() {
        var total = 0;
        var w = 0;
        $('#kp-editor [data-scene]').each(function () {
            var n = $(this).find('[name=narration]').val();
            total += secs(n);
            w += words(n);
            $(this).find('[data-dur]').text(fmtSec(secs(n)));
        });
        var target = (state.draft && state.draft.targetDurationSec) || 0;
        $('#kp-editor-stats').text(w + ' kelime · ~' + fmtSec(total) + (target ? ' / hedef ' + fmtSec(target) : '') +
            (state.current ? ' · ' + U.money(state.current.costUsd) : ''));
    }

    function confirmDiscard() {
        if (!state.dirty) { return $.Deferred().resolve().promise(); }
        return TK.modal.confirm('Kaydedilmemiş değişiklikler', 'Düzenleme alanındaki değişiklikler kaydedilmedi. Yine de devam edilsin mi?', 'Değişiklikleri at');
    }

    function selectConcept(id, force) {
        var go = function () {
            state.selectedId = id;
            state.current = null;
            state.draft = null;
            renderList();
            if (!id) { renderEditor(); return; }
            $('#kp-editor').html(U.skeleton(4));
            TK.api.get('/Konseptler/Getir/' + enc(id)).done(function (c) { setCurrent(c); }).fail(function () { renderEditor(); });
        };
        if (force) { go(); } else { confirmDiscard().done(go); }
    }

    function setCurrent(c) {
        state.current = c;
        state.draft = $.extend(true, {}, (c && c.document) || {});
        renderEditor();
    }

    function saveEditor() {
        if (!state.current) { return $.Deferred().reject().promise(); }
        var d = readEditor();
        if (!d.scenes || !d.scenes.length) { TK.toast.warning('Konseptte en az bir sahne olmalı.'); return $.Deferred().reject().promise(); }
        return TK.api.put('/Konseptler/Guncelle/' + enc(state.current.id), { title: d.title, document: d }).done(function (res) {
            setCurrent(res || $.extend({}, state.current, { title: d.title, document: d }));
            TK.toast.success('Konsept kaydedildi.');
            loadList();
        });
    }

    function approveCurrent() {
        if (!state.current) { return; }
        var id = state.current.id;
        var proceed = function () { setDirty(false); approveConcept(id); };
        if (state.dirty) { saveEditor().done(proceed); } else { proceed(); }
    }

    /* ================================================================ Düzeltme döngüsü (aday penceresi) */
    function candidateBody(cand, base) {
        var d = cand.document || {};
        var baseScenes = (base && base.scenes) || [];
        var changed = 0;
        var scenes = $.map(d.scenes || [], function (s, i) {
            var b = baseScenes[i];
            var isChanged = !b || b.narration !== s.narration || b.onScreen !== s.onScreen || b.title !== s.title;
            if (isChanged) { changed++; }
            return '<div class="kp-cand-scene' + (isChanged ? ' is-changed' : '') + '"><strong>' + (i + 1) + '. ' + U.escape(s.title || '') +
                (isChanged ? ' <span class="tk-badge is-accent">değişti</span>' : '') + '</strong>' +
                '<div class="kp-onscreen">Ekran: ' + U.escape(s.onScreen || '—') + ' · ~' + fmtSec(s.durationHintSec || secs(s.narration)) + '</div>' +
                '<div>' + U.escape(s.narration || '') + '</div></div>';
        }).join('');
        return '<div class="kp-candidate"><h3>' + U.escape(d.title || '') + '</h3>' +
            (d.hook ? '<p class="kp-hook">“' + U.escape(d.hook) + '”</p>' : '') +
            '<p class="tk-muted">' + U.escape(d.summary || '') + '</p>' +
            '<p class="tk-hint">' + (d.scenes || []).length + ' sahne · ' + changed + ' sahne değişti · bu düzeltme ' + U.money(cand.costUsd) + '</p>' +
            scenes + '</div>' +
            '<div class="kp-cand-again" hidden><label class="tk-label" for="kp-cand-instr">Bunu da şöyle düzelt</label>' +
            '<textarea id="kp-cand-instr" class="tk-textarea" rows="3" maxlength="4000" placeholder="Ör. Son sahneyi daha şaşırtıcı yap."></textarea>' +
            '<div class="tk-row" style="justify-content:flex-end"><button type="button" class="tk-btn tk-btn-secondary" data-act="again-run">' + U.icon('refresh') + ' Tekrar üret</button></div></div>';
    }

    function openCandidate(conceptId, cand, base) {
        var decided = false;
        var $choose = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('check') + ' Seç</button>');
        var $again = $('<button type="button" class="tk-btn tk-btn-secondary">' + U.icon('pencil') + ' Bunu da düzenle</button>');
        var $drop = $('<button type="button" class="tk-btn tk-btn-danger">' + U.icon('trash') + ' Sil</button>');
        var h = TK.modal.open({
            title: 'Düzeltilmiş sürüm', size: 'lg', body: candidateBody(cand, base),
            footer: $('<div class="kp-modal-foot"></div>').append($drop, $again, $choose),
            onClose: function () {
                if (!decided) { TK.api.del('/Konseptler/AdaySil/' + enc(conceptId) + '?candidateId=' + enc(cand.candidateId), { silent: true }); }
            }
        });
        function lock(on) { $choose.add($again).add($drop).prop('disabled', on); }
        $choose.on('click', function () {
            lock(true);
            TK.api.post('/Konseptler/AdayKabul/' + enc(conceptId) + '?candidateId=' + enc(cand.candidateId)).done(function (res) {
                decided = true;
                h.close(true);
                if (String(state.selectedId) === String(conceptId)) { setCurrent(res); }
                loadList();
                TK.toast.success('Düzeltilmiş sürüm seçildi.');
            }).fail(function () { lock(false); });
        });
        $drop.on('click', function () {
            decided = true;
            TK.api.del('/Konseptler/AdaySil/' + enc(conceptId) + '?candidateId=' + enc(cand.candidateId), { silent: true });
            h.close(true);
            TK.toast.info('Düzeltilmiş sürüm atıldı; konsept değişmedi.');
        });
        $again.on('click', function () {
            h.$body.find('.kp-cand-again').prop('hidden', false).find('textarea').trigger('focus');
        });
        h.$body.on('click', '[data-act=again-run]', function () {
            var text = $.trim(h.$body.find('#kp-cand-instr').val());
            if (!text) { TK.toast.warning('Ne düzeltilmesini istediğinizi yazın.'); return; }
            var $btn = $(this);
            lock(true);
            U.busy($btn, TK.api.post('/Konseptler/Revize/' + enc(conceptId), { instruction: text, document: cand.document }, { timeout: 300000 }))
                .done(function (next) {
                    decided = true;
                    TK.api.del('/Konseptler/AdaySil/' + enc(conceptId) + '?candidateId=' + enc(cand.candidateId), { silent: true });
                    h.close(true);
                    openCandidate(conceptId, next, cand.document);
                })
                .fail(function () { lock(false); });
        });
    }

    function revise() {
        var text = $.trim($('#kp-revise-text').val());
        if (!text) { TK.toast.warning('Ne düzeltilmesini istediğinizi yazın.'); $('#kp-revise-text').trigger('focus'); return; }
        var id = state.current.id;
        var base = readEditor();
        U.busy($('#kp-revise'), TK.api.post('/Konseptler/Revize/' + enc(id), { instruction: text, document: base }, { timeout: 300000 }))
            .done(function (cand) { openCandidate(id, cand, base); });
    }

    /* ================================================================ Olaylar */
    function bind() {
        $('#kp-models').on('click', openModels);
        $('#kp-tuning').on('click', openTuning);
        $('#kp-wordcheck').on('change', function () {
            var on = $(this).is(':checked');
            saveSettings({ wordRepetitionCheck: on }).done(function () { TK.toast.info('Kelime denetimi ' + (on ? 'açıldı' : 'kapatıldı') + '.'); })
                .fail(function () { $('#kp-wordcheck').prop('checked', !on); });
        });

        $('#kp-topic-suggest').on('click', suggestTopics);
        $('#kp-topic-hint').on('keydown', function (e) { if (e.key === 'Enter') { suggestTopics(); } });
        $('#kp-topic-list').on('click', '.tk-list-item', function (e) {
            var i = parseInt($(this).attr('data-index'), 10);
            if ($(e.target).closest('[data-act=dislike]').length) { dislikeTopic(i); return; }
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
        $('#kp-job-cancel').on('click', cancelJob);
        $('#kp-job-close').on('click', function () { $('#kp-job').prop('hidden', true); });

        $('#kp-generated-refresh').on('click', loadList);
        $('#kp-generated-list').on('click', '.tk-list-item', function (e) {
            var id = $(this).attr('data-id');
            var act = $(e.target).closest('[data-act]').attr('data-act');
            if (act === 'delete') { deleteConcept(id); return; }
            if (act === 'approve') { approveConcept(id); return; }
            if (String(id) !== String(state.selectedId)) { selectConcept(id); }
        });

        var $ed = $('#kp-editor');
        $ed.on('click', '.tk-tab', function () {
            var tab = $(this).attr('data-tab');
            $ed.find('.tk-tab').removeClass('is-active').filter(this).addClass('is-active');
            $ed.find('.tk-tab-panel').removeClass('is-active').filter('[data-panel=' + tab + ']').addClass('is-active');
        });
        $ed.on('input', 'input, textarea', function () {
            if (this.id === 'kp-revise-text') { return; }
            setDirty(true);
            if (this.name === 'narration') { updateStats(); }
        });
        $ed.on('click', '[data-act=add-scene]', function () {
            state.draft = readEditor();
            state.draft.scenes.push({ index: state.draft.scenes.length, title: '', onScreen: '', narration: '', visualPrompt: '' });
            renderEditor();
            setDirty(true);
            $ed.find('[data-scene]').last().find('[name=narration]').trigger('focus');
        });
        $ed.on('click', '[data-act=remove-scene]', function () {
            var i = parseInt($(this).closest('[data-scene]').attr('data-scene'), 10);
            state.draft = readEditor();
            state.draft.scenes.splice(i, 1);
            renderEditor();
            setDirty(true);
        });
        $ed.on('click', '#kp-revise', revise);
        $('#kp-editor-save').on('click', function () { U.busy($(this), saveEditor()); });
        $('#kp-editor-approve').on('click', approveCurrent);

        $(window).on('beforeunload', function () { if (state.dirty) { return 'Kaydedilmemiş değişiklikler var.'; } return undefined; });
        $(document).on('tk:profile', function () { state.topics = []; renderTopics(); loadSettings(); loadList(); });
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
        checkJobOnce();
    }

    TK.konseptler = { init: init };
    $(init);
})(window, window.jQuery);
