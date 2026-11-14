/**
 * TK.konsepttenVideo — Konseptten Video sayfası: model seçimi pencereleri (video kodu + efor, motor, görsel, ses),
 * altyazı / ilerleme çubuğu anahtarları (üretim ayarına gerçekten kaydedilir), onaylanan konsept listesi + düzenlenebilir
 * ayrıntı, üretim sırası (↑ ↓ ✕, sırayla üret, durdur, devam; aşamalar 2 sn polling ile), üretilenler + oynatıcı.
 * MVC uçları: /KonsepttenVideo/{Modeller, Ayarlar, Onaylananlar, Sira, SirayaEkle, Tasi, SiradanCikar, Baslat, Durdur, Devam, Uretilenler}
 * ve /Konseptler/{Getir, Guncelle}. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    var STAGES = [
        ['Script', 'Konsept inceleniyor'], ['FactSheet', 'Bilgi doğrulama'], ['ScenePlan', 'Sahne planı'], ['SceneCode', 'Kod'],
        ['Images', 'Görseller'], ['Normalize', 'Okunuş'], ['Speech', 'Ses'], ['Align', 'Hizalama'],
        ['SceneRender', 'Render'], ['Compose', 'Altyazı/çubuk'], ['Mux', 'Birleştirme']
    ];
    var EFFORTS = [['low', 'Düşük'], ['medium', 'Orta'], ['high', 'Yüksek'], ['xhigh', 'Çok yüksek'], ['max', 'En yüksek']];
    var KINDS = {
        code: { title: 'Video kodu modeli', field: 'videoModelId', capability: 'text' },
        engine: { title: 'Video motoru', field: 'videoEngine', capability: 'videoengine' },
        image: { title: 'Görsel üretimi', field: 'imageModelId', capability: 'image', allowOff: 'Kapalı (sadece koddan çiz)' },
        speech: { title: 'Seslendirme', field: 'speechModelId', capability: 'speech', allowOff: 'Kapalı (sessiz video)' }
    };
    var FALLBACK_ENGINES = [
        { id: 'HyperFrames', displayName: 'HyperFrames (varsayılan)' },
        { id: 'Remotion', displayName: 'Remotion (≤3 kişi ücretsiz lisans)' },
        { id: 'Revideo', displayName: 'Revideo (deneysel)' }
    ];

    var state = { models: [], settings: {}, approved: [], checked: {}, detail: null, queue: [], queueState: null, produced: [], playing: null };

    function hasCap(m, cap) {
        var c = m.capabilities || m.capability || '';
        if ($.isArray(c)) { c = c.join(','); }
        return String(c).toLowerCase().indexOf(cap) >= 0;
    }

    function modelsFor(kind) {
        var list = $.grep(state.models, function (m) { return hasCap(m, KINDS[kind].capability); });
        if (kind === 'engine' && !list.length) { list = FALLBACK_ENGINES; }
        return list;
    }

    function nameOf(kind, id) {
        if (!id) { return KINDS[kind].allowOff ? 'Kapalı' : 'Seçilmedi'; }
        var list = modelsFor(kind);
        for (var i = 0; i < list.length; i++) { if (String(list[i].id || list[i].modelId) === String(id)) { return list[i].displayName || list[i].modelId; } }
        return id;
    }

    function renderSettings() {
        var s = state.settings || {};
        var effort = $.grep(EFFORTS, function (e) { return e[0] === s.videoEffort; })[0];
        $('#kv-sum-code').text(nameOf('code', s.videoModelId) + (effort ? ' · ' + effort[1] : ''));
        $('#kv-sum-engine').text(nameOf('engine', s.videoEngine));
        $('#kv-sum-image').text(nameOf('image', s.imageModelId));
        $('#kv-sum-speech').text(nameOf('speech', s.speechModelId));
        $('#kv-subtitles').prop('checked', !!s.subtitles);
        $('#kv-progressbar').prop('checked', !!s.progressBar);
    }

    function saveSettings(patch) {
        var next = $.extend({}, state.settings, patch);
        return TK.api.put('/KonsepttenVideo/Ayarlar', next).done(function (res) { state.settings = res || next; renderSettings(); });
    }

    function openModel(kind) {
        var k = KINDS[kind];
        var s = state.settings || {};
        var current = s[k.field];
        var list = modelsFor(kind);
        var html = '<div class="tk-list" role="radiogroup">';
        if (k.allowOff) {
            html += '<label class="tk-list-item' + (!current ? ' is-selected' : '') + '"><input type="radio" name="m" value="" ' + (!current ? 'checked' : '') + ' />' +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(k.allowOff) + '</div></div></label>';
        }
        $.each(list, function (_, m) {
            var id = m.id || m.modelId;
            var tags = [];
            if (m.location || m.runsOn) { tags.push('<span class="tk-badge ' + (String(m.location || m.runsOn).toLowerCase() === 'agent' ? 'is-info">Yerel' : 'is-accent">Online') + '</span>'); }
            if (m.languageTag) { tags.push('<span class="tk-badge is-warning">' + U.escape(m.languageTag) + '</span>'); }
            if (m.vramInsufficient) { tags.push('<span class="tk-badge is-danger">VRAM yetersiz</span>'); }
            if (m.priceHint) { tags.push('<span class="tk-badge">' + U.escape(m.priceHint) + '</span>'); }
            html += '<label class="tk-list-item' + (String(id) === String(current) ? ' is-selected' : '') + '"><input type="radio" name="m" value="' + U.escape(id) + '" ' + (String(id) === String(current) ? 'checked' : '') + ' />' +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(m.displayName || m.modelId) + '</div>' +
                '<div class="tk-list-sub">' + U.escape(m.platformName || m.platform || '') + '</div></div>' + tags.join(' ') + '</label>';
        });
        if (!list.length && !k.allowOff) { html += U.empty('cpu', 'Model bulunamadı', 'Ayarlar → API anahtarları bölümünden bir sağlayıcı ekleyin.'); }
        html += '</div>';
        if (kind === 'code') {
            html += '<div class="tk-field" style="margin-top:14px"><label>Efor</label><select class="tk-select" name="effort">' +
                $.map(EFFORTS, function (e) { return '<option value="' + e[0] + '"' + (e[0] === (s.videoEffort || 'high') ? ' selected' : '') + '>' + e[1] + '</option>'; }).join('') + '</select></div>';
        }
        var $b = $('<div></div>').html(html);
        $b.on('change', 'input[type=radio]', function () { $b.find('.tk-list-item').removeClass('is-selected'); $(this).closest('.tk-list-item').addClass('is-selected'); });
        TK.modal.open({
            title: k.title, body: $b, hint: 'Pencereyi kapatınca kaydedilir.',
            onClose: function ($body) {
                var patch = {};
                patch[k.field] = $body.find('input[name=m]:checked').val() || null;
                if (kind === 'code') { patch.videoEffort = $body.find('[name=effort]').val(); }
                return saveSettings(patch);
            }
        });
    }

    /* ---------------- Onaylananlar + ayrıntı ---------------- */
    function renderApproved() {
        var $l = $('#kv-approved');
        if (!state.approved.length) {
            $l.html(U.empty('check', 'Onaylı konsept yok', 'Konseptler sayfasında bir konsepti "Onayla" ile buraya gönderin.'));
        } else {
            var html = '<ul class="tk-list">';
            $.each(state.approved, function (_, c) {
                var sel = state.detail && String(state.detail.id) === String(c.id);
                html += '<li class="tk-list-item' + (sel ? ' is-selected' : '') + '" data-id="' + U.escape(c.id) + '">' +
                    '<input type="checkbox" data-check aria-label="Sıraya eklemek için seç" ' + (state.checked[c.id] ? 'checked' : '') + ' style="accent-color:var(--tk-accent);width:17px;height:17px" />' +
                    '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(c.title || 'Adsız') + '</div>' +
                    '<div class="tk-list-sub">' + U.escape(U.dateTime(c.approvedAt || c.createdAt)) + '</div></div></li>';
            });
            $l.html(html + '</ul>');
        }
        var n = 0; $.each(state.checked, function (_, v) { if (v) { n++; } });
        $('#kv-add-queue').prop('disabled', n === 0).html(U.icon('queue', 'tk-ic-sm') + ' Sıraya ekle' + (n ? ' (' + n + ')' : ''));
    }

    function loadApproved() {
        return TK.api.get('/KonsepttenVideo/Onaylananlar', null, { silent: true })
            .done(function (data) { state.approved = U.items(data); renderApproved(); })
            .fail(function () { $('#kv-approved').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function renderDetail() {
        var c = state.detail;
        var $d = $('#kv-detail');
        if (!c) {
            $d.html(U.empty('edit-doc', 'Konsept seçilmedi', 'Soldaki listeden bir konsepte tıklayın.'));
            $('#kv-detail-save').prop('hidden', true);
            $('#kv-detail-sub').text('Düzenlenebilir');
            return;
        }
        var d = c.document || c;
        var html = '<div class="tk-field"><label>Başlık</label><input class="tk-input" name="title" value="' + U.escape(d.title || c.title || '') + '" /></div>';
        $.each(d.scenes || [], function (i, s) {
            html += '<div class="tk-field" data-scene="' + i + '"><label>Sahne ' + (i + 1) + ' — ' + U.escape(s.title || '') + '</label>' +
                '<textarea class="tk-textarea" rows="3" name="narration">' + U.escape(s.narration || '') + '</textarea>' +
                '<span class="tk-hint">Ekran: ' + U.escape(s.onScreen || '') + '</span></div>';
        });
        $d.html(html);
        $('#kv-detail-save').prop('hidden', false);
        $('#kv-detail-sub').text((d.scenes || []).length + ' sahne');
    }

    function selectDetail(id) {
        $('#kv-detail').html(U.skeleton(4));
        TK.api.get('/Konseptler/Getir/' + encodeURIComponent(id)).done(function (c) { state.detail = c; renderDetail(); renderApproved(); });
    }

    function saveDetail() {
        var c = state.detail;
        if (!c) { return; }
        var d = $.extend(true, {}, c.document || c);
        d.title = $.trim($('#kv-detail [name=title]').val());
        $('#kv-detail [data-scene]').each(function () {
            var i = parseInt($(this).attr('data-scene'), 10);
            if (d.scenes && d.scenes[i]) { d.scenes[i].narration = $(this).find('[name=narration]').val(); }
        });
        U.busy($('#kv-detail-save'), TK.api.put('/Konseptler/Guncelle/' + encodeURIComponent(c.id), { title: d.title, document: d }))
            .done(function (res) { state.detail = res || $.extend(c, { document: d, title: d.title }); TK.toast.success('Konsept kaydedildi.'); loadApproved(); });
    }

    function addToQueue() {
        var ids = [];
        $.each(state.checked, function (id, v) { if (v) { ids.push(id); } });
        if (!ids.length) { return; }
        U.busy($('#kv-add-queue'), TK.api.post('/KonsepttenVideo/SirayaEkle', { conceptIds: ids, settings: state.settings })).done(function () {
            state.checked = {};
            renderApproved();
            TK.toast.success(ids.length + ' konsept üretim sırasına eklendi.');
            if (state.queuePoller) { state.queuePoller.now(); }
        });
    }

    /* ---------------- Üretim sırası ---------------- */
    function stageStatus(item, stage) {
        var list = item.stages || [];
        for (var i = 0; i < list.length; i++) { if (list[i].stage === stage) { return String(list[i].status || '').toLowerCase(); } }
        if (item.currentStage === stage) { return 'running'; }
        return '';
    }

    function renderQueue() {
        var $q = $('#kv-queue');
        var qs = state.queueState || {};
        var running = String(qs.status || '').toLowerCase() === 'running';
        var paused = String(qs.status || '').toLowerCase() === 'paused';
        $('#kv-start').prop('hidden', running || paused);
        $('#kv-pause').prop('hidden', !running);
        $('#kv-resume').prop('hidden', !paused);
        $('#kv-queue-status').text(running ? 'Üretiliyor…' : paused ? 'Durduruldu' : state.queue.length ? state.queue.length + ' iş bekliyor' : 'Boş');

        if (!state.queue.length) {
            $q.html(U.empty('queue', 'Sıra boş', 'Onaylanan konseptleri işaretleyip "Sıraya ekle" deyin, sonra "Sırayla üret".'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(state.queue, function (i, item) {
            var pct = Math.max(0, Math.min(100, Number(item.progress || 0)));
            var st = String(item.status || '').toLowerCase();
            var badge = st === 'running' ? '<span class="tk-badge is-accent">Üretiliyor</span>' : st === 'failed' ? '<span class="tk-badge is-danger">Hata</span>' :
                st === 'completed' ? '<span class="tk-badge is-success">Bitti</span>' : st === 'paused' ? '<span class="tk-badge is-warning">Durdu</span>' : '<span class="tk-badge">Sırada</span>';
            html += '<li class="tk-list-item" style="flex-direction:column;align-items:stretch;cursor:default" data-id="' + U.escape(item.id) + '">' +
                '<div class="tk-row"><strong class="tk-grow" style="min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">' + (i + 1) + '. ' + U.escape(item.title || 'Adsız') + '</strong>' + badge +
                '<div class="tk-list-actions" style="opacity:1">' +
                '<button type="button" class="tk-mini-btn" data-act="up" title="Yukarı" aria-label="Yukarı"' + (i === 0 ? ' disabled' : '') + '>' + U.icon('up') + '</button>' +
                '<button type="button" class="tk-mini-btn" data-act="down" title="Aşağı" aria-label="Aşağı"' + (i === state.queue.length - 1 ? ' disabled' : '') + '>' + U.icon('down') + '</button>' +
                '<button type="button" class="tk-mini-btn is-danger" data-act="remove" title="Sıradan çıkar" aria-label="Sıradan çıkar">' + U.icon('x') + '</button>' +
                '</div></div>' +
                '<div class="tk-progress" style="margin-top:8px"><span style="width:' + pct + '%"></span></div>' +
                '<div class="tk-stages">' + $.map(STAGES, function (s) {
                    var ss = stageStatus(item, s[0]);
                    var cls = ss === 'completed' || ss === 'done' || ss === 'skipped' ? 'is-done' : ss === 'running' ? 'is-running' : ss === 'failed' ? 'is-failed' : '';
                    return '<span class="tk-stage ' + cls + '" title="' + s[0] + '">' + s[1] + '</span>';
                }).join('') + '</div>' +
                (item.message ? '<div class="tk-hint" style="margin-top:4px">' + U.escape(item.message) + '</div>' : '') +
                '</li>';
        });
        $q.html(html + '</ul>');
    }

    function onQueue(data) {
        state.queueState = data && !$.isArray(data) ? data : { items: data };
        state.queue = U.items(data);
        renderQueue();
    }

    function queueAction(url, okMessage) {
        return TK.api.post(url).done(function () { if (okMessage) { TK.toast.info(okMessage); } if (state.queuePoller) { state.queuePoller.now(); } });
    }

    /* ---------------- Üretilenler + oynatıcı ---------------- */
    function renderProduced() {
        var $l = $('#kv-produced-list');
        if (!state.produced.length) {
            $l.html(U.empty('film', 'Henüz video yok', 'Sıradaki işler bittikçe burada görünür.'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(state.produced, function (_, v) {
            var sel = state.playing && String(state.playing.id) === String(v.id);
            html += '<li class="tk-list-item' + (sel ? ' is-selected' : '') + '" data-id="' + U.escape(v.id) + '">' +
                '<span class="tk-thumb' + (String(v.format || '').toLowerCase() === 'vertical' ? ' is-vertical' : '') + '">' + U.icon('play', 'tk-ic-sm') + '</span>' +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(v.title || 'Adsız video') + '</div>' +
                '<div class="tk-list-sub">' + U.escape(U.dateTime(v.createdAt)) + (v.costUsd !== undefined ? ' · ' + U.money(v.costUsd) : '') + '</div></div></li>';
        });
        $l.html(html + '</ul>');
    }

    function play(id) {
        var v = $.grep(state.produced, function (x) { return String(x.id) === String(id); })[0];
        if (!v) { return; }
        state.playing = v;
        $('#kv-produced').addClass('has-player');
        $('#kv-player-title').text(v.title || '');
        $('#kv-player-open').attr('href', '/Uretilenler?video=' + encodeURIComponent(v.id));
        var $box = $('#kv-player-box').empty();
        if (v.streamUrl) {
            $('<video controls playsinline preload="metadata"></video>').attr('src', v.streamUrl).attr('poster', v.thumbUrl || null).appendTo($box);
        } else {
            $box.html('<div class="tk-player-empty">' + U.icon('monitor', 'tk-ic-lg') + '<div>Video masaüstü programındadır.<br/>Bilgisayar açık ve program çalışıyorken oynatılır.</div></div>');
        }
        renderProduced();
    }

    function closePlayer() {
        state.playing = null;
        $('#kv-player-box').empty();
        $('#kv-produced').removeClass('has-player');
        renderProduced();
    }

    function loadProduced() {
        return TK.api.get('/KonsepttenVideo/Uretilenler', null, { silent: true }).done(function (data) {
            state.produced = U.items(data);
            renderProduced();
        }).fail(function () { $('#kv-produced-list').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function bind() {
        $('[data-model-kind]').on('click', function () { openModel($(this).attr('data-model-kind')); });
        $('#kv-subtitles').on('change', function () {
            var on = $(this).is(':checked');
            saveSettings({ subtitles: on }).done(function () { TK.toast.info('Altyazı ' + (on ? 'açık' : 'kapalı') + '.'); }).fail(function () { $('#kv-subtitles').prop('checked', !on); });
        });
        $('#kv-progressbar').on('change', function () {
            var on = $(this).is(':checked');
            saveSettings({ progressBar: on }).done(function () { TK.toast.info('İlerleme çubuğu ' + (on ? 'açık' : 'kapalı') + '.'); }).fail(function () { $('#kv-progressbar').prop('checked', !on); });
        });

        $('#kv-approved').on('click', '.tk-list-item', function (e) {
            var id = $(this).attr('data-id');
            if ($(e.target).is('[data-check]')) { state.checked[id] = $(e.target).is(':checked'); renderApproved(); return; }
            selectDetail(id);
        });
        $('#kv-add-queue').on('click', addToQueue);
        $('#kv-detail-save').on('click', saveDetail);

        $('#kv-queue').on('click', '[data-act]', function () {
            var id = $(this).closest('[data-id]').attr('data-id');
            var act = $(this).attr('data-act');
            if (act === 'remove') {
                TK.api.del('/KonsepttenVideo/SiradanCikar/' + encodeURIComponent(id)).done(function () { state.queuePoller.now(); });
            } else {
                TK.api.post('/KonsepttenVideo/Tasi/' + encodeURIComponent(id), { direction: act === 'up' ? -1 : 1 }).done(function () { state.queuePoller.now(); });
            }
        });
        $('#kv-start').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/Baslat', 'Üretim başladı.')); });
        $('#kv-pause').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/Durdur', 'Mevcut aşama bitince durdurulacak.')); });
        $('#kv-resume').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/Devam', 'Kaldığı yerden devam ediliyor.')); });

        $('#kv-produced-list').on('click', '.tk-list-item', function () { play($(this).attr('data-id')); });
        $('#kv-player-close').on('click', closePlayer);
    }

    function init() {
        if (!$('#tk-video').length) { return; }
        U = TK.util;
        bind();
        renderDetail();
        $('#kv-approved, #kv-queue, #kv-produced-list').html(U.skeleton(3));

        TK.api.get('/KonsepttenVideo/Modeller', null, { silent: true }).done(function (d) { state.models = U.items(d); renderSettings(); });
        TK.api.get('/KonsepttenVideo/Ayarlar', null, { silent: true }).done(function (d) { state.settings = d || {}; renderSettings(); });
        loadApproved();
        loadProduced();

        var lastCompleted = null;
        state.queuePoller = TK.poll('/KonsepttenVideo/Sira', 2000, function (data) {
            onQueue(data);
            var done = $.grep(state.queue, function (q) { return String(q.status || '').toLowerCase() === 'completed'; }).length;
            if (lastCompleted !== null && done !== lastCompleted) { loadProduced(); }
            lastCompleted = done;
        }, { onError: function () { if (!state.queue.length) { $('#kv-queue').html(U.empty('alert', 'Sıra alınamadı', 'API bağlantısı bekleniyor; otomatik yeniden denenecek.')); } } });
    }

    TK.konsepttenVideo = { init: init };
    $(init);
})(window, window.jQuery);
