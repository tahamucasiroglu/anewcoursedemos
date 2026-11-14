/**
 * TK.konsepttenVideo — Konseptten Video sayfası.
 *  - Model şeridi: video kodu modeli + efor, video motoru, görsel (kapalı/yerel/online + model), ses (kapalı/yerel/online + model +
 *    ses + hız). Tık → ekranın üstünde pencere; kapatınca üretim ayarına kaydedilir. Yerel modellerde VRAM ve dil etiketi.
 *  - Sağ üst anahtarlar: altyazı, üst ilerleme çubuğu (üretim ayarına gerçekten yazılır).
 *  - Sol: onaylanan konseptler (arama, hepsini seç, kutucuk → sıraya ekle) + altta düzenlenebilir ayrıntı (Kaydet → Concepts API).
 *  - Orta: üretim sırası (↑ ↓ ✕, Sırayla üret, Durdur, Kaldığı yerden devam, bitenleri temizle); çalışan iş için aşama aşama
 *    zaman çizelgesi (2 sn TK.poll). Ajan çevrimdışıysa / bütçe dolduysa üstte uyarı.
 *  - Sağ: üretilen videolar; tıklayınca kesikli çizginin sağında HTML5 oynatıcı (adres ajanın yerel sunucusu).
 * MVC uçları: /KonsepttenVideo/{Modeller, Sesler, Ajanlar, Ayarlar, Onaylananlar, Konsept, Sira, SirayaEkle, Tasi, SiradanCikar,
 * Baslat, Durdur, Devam, BitenleriTemizle, Uretilenler, Video}. Ortak parçalar: TK.production (production-shared.js). Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var P = null;

    var KIND_TITLES = { code: 'Video kodu modeli ve efor', engine: 'Video motoru', image: 'Görsel üretimi', speech: 'Seslendirme' };

    var state = {
        models: [], agentVramMb: null, settings: {}, voices: {},
        approved: [], filter: '', checked: {},
        detail: null, dirty: false,
        queueState: null, queue: [], lastQueueJson: '', doneCount: null, poller: null,
        produced: [], playing: null
    };

    /* ---------------- Ayarlar + model şeridi ---------------- */
    function ctx() {
        return { models: state.models, agentVramMb: state.agentVramMb, loadVoices: loadVoices };
    }

    function loadVoices(modelKey) {
        if (state.voices[modelKey]) { return $.Deferred().resolve(state.voices[modelKey]).promise(); }
        return TK.api.get('/KonsepttenVideo/Sesler', { modelKey: modelKey }, { silent: true }).done(function (v) { state.voices[modelKey] = v; });
    }

    function renderSettings() {
        var s = state.settings || {};
        $.each(['code', 'engine', 'image', 'speech'], function (_, k) { $('#kv-sum-' + k).text(P.summary(k, s, state.models)); });
        $('#kv-subtitles').prop('checked', !!s.burnSubtitles);
        $('#kv-progressbar').prop('checked', !!s.showProgressBar);
    }

    function saveSettings(patch) {
        var next = $.extend({}, state.settings, patch);
        return TK.api.put('/KonsepttenVideo/Ayarlar', next).done(function (res) {
            state.settings = res && !$.isArray(res) ? res : next;
            renderSettings();
        });
    }

    function openModel(kind) {
        var ed = P.editor(kind, state.settings, ctx());
        var before = JSON.stringify(ed.read());
        TK.modal.open({
            title: KIND_TITLES[kind], size: kind === 'engine' ? '' : 'lg', body: ed.$el, hint: 'Pencereyi kapatınca kaydedilir.',
            onClose: function () {
                var patch = ed.read();
                if (JSON.stringify(patch) === before) { return true; }
                return saveSettings(patch).done(function () { TK.toast.success(KIND_TITLES[kind] + ' kaydedildi.'); });
            }
        });
    }

    function toggleSetting($input, field, label) {
        var on = $input.is(':checked');
        var patch = {};
        patch[field] = on;
        saveSettings(patch)
            .done(function () { TK.toast.info(label + ' ' + (on ? 'açık' : 'kapalı') + ' (yeni üretimlerde).'); })
            .fail(function () { $input.prop('checked', !on); });
    }

    /* ---------------- Onaylanan konseptler ---------------- */
    function queuedConceptIds() {
        var ids = {};
        $.each(state.queue, function (_, q) {
            var st = P.itemStatus(q.status);
            if (st === 'waiting' || st === 'running' || st === 'paused') { ids[String(q.conceptId).toLowerCase()] = true; }
        });
        return ids;
    }

    function visibleApproved() {
        var f = state.filter.toLocaleLowerCase('tr-TR');
        return $.grep(state.approved, function (c) {
            return !f || (String(c.title || '') + ' ' + String(c.summary || '')).toLocaleLowerCase('tr-TR').indexOf(f) >= 0;
        });
    }

    function checkedIds() {
        var ids = [];
        $.each(state.checked, function (id, v) { if (v) { ids.push(id); } });
        return ids;
    }

    function renderApproved() {
        var $l = $('#kv-approved');
        var list = visibleApproved();
        var queued = queuedConceptIds();
        $('#kv-approved-sub').text(state.approved.length ? state.approved.length + ' konsept · tıkla: ayrıntı · kutucuk: sıraya ekle' : 'Tıkla: ayrıntı · Kutucuk: sıraya ekle');
        if (!state.approved.length) {
            $l.html(U.empty('check', 'Onaylı konsept yok', 'Konseptler sayfasında bir konsepti "Onayla" ile buraya gönderin.'));
        } else if (!list.length) {
            $l.html(U.empty('list', 'Eşleşen konsept yok', 'Arama ifadesini değiştirin.'));
        } else {
            var html = '<ul class="tk-list">';
            $.each(list, function (_, c) {
                var sel = state.detail && P.isSameId(state.detail.id, c.id);
                var meta = [];
                if (c.sceneCount) { meta.push('<span>' + U.escape(c.sceneCount) + ' sahne</span>'); }
                if (c.estimatedDurationSec) { meta.push('<span>~' + U.escape(P.clock(c.estimatedDurationSec)) + '</span>'); }
                meta.push('<span>' + U.escape(U.dateTime(c.approvedAt || c.updatedAt || c.createdAt)) + '</span>');
                html += '<li class="tk-list-item kv-concept' + (sel ? ' is-selected' : '') + (queued[String(c.id).toLowerCase()] ? ' is-queued' : '') + '" data-id="' + U.escape(c.id) + '">' +
                    '<input type="checkbox" data-check aria-label="Sıraya eklemek için seç" ' + (state.checked[c.id] ? 'checked' : '') + ' />' +
                    '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(c.title || 'Adsız') + '</div>' +
                    '<div class="tk-list-sub">' + meta.join(' · ') + '</div></div></li>';
            });
            $l.html(html + '</ul>');
        }
        var n = checkedIds().length;
        var visibleChecked = $.grep(list, function (c) { return !!state.checked[c.id]; }).length;
        $('#kv-check-all').prop('checked', list.length > 0 && visibleChecked === list.length).prop('indeterminate', visibleChecked > 0 && visibleChecked < list.length);
        $('#kv-add-queue').prop('disabled', n === 0).html(U.icon('queue', 'tk-ic-sm') + ' Sıraya ekle' + (n ? ' (' + n + ')' : ''));
    }

    function loadApproved() {
        return TK.api.get('/KonsepttenVideo/Onaylananlar', null, { silent: true })
            .done(function (data) {
                state.approved = U.items(data);
                var alive = {};
                $.each(state.approved, function (_, c) { alive[c.id] = true; });
                $.each(state.checked, function (id) { if (!alive[id]) { delete state.checked[id]; } });
                renderApproved();
            })
            .fail(function () { $('#kv-approved').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    function addToQueue() {
        var ids = checkedIds();
        if (!ids.length) { return; }
        U.busy($('#kv-add-queue'), TK.api.post('/KonsepttenVideo/SirayaEkle', { conceptIds: ids, settings: state.settings })).done(function () {
            state.checked = {};
            renderApproved();
            TK.toast.success(ids.length + ' konsept üretim sırasına eklendi. "Sırayla üret" ile başlatın.');
            if (state.poller) { state.poller.now(); }
        });
    }

    /* ---------------- Konsept ayrıntısı (düzenleyici) ---------------- */
    function setDirty(on) {
        state.dirty = on;
        $('#kv-detail-save').prop('hidden', !state.detail).toggleClass('tk-btn-primary', on).toggleClass('tk-btn-secondary', !on);
        var d = state.detail && state.detail.document;
        $('#kv-detail-sub').html(state.detail
            ? U.escape(((d && d.scenes) || []).length + ' sahne') + (on ? ' · <span class="kv-dirty">kaydedilmedi</span>' : ' · düzenlenebilir')
            : 'Düzenlenebilir');
    }

    function field(label, name, value, multi, rows) {
        return '<div class="tk-field"><label>' + U.escape(label) + '</label>' + (multi
            ? '<textarea class="tk-textarea" name="' + name + '" rows="' + (rows || 2) + '">' + U.escape(value || '') + '</textarea>'
            : '<input class="tk-input" name="' + name + '" value="' + U.escape(value || '') + '" />') + '</div>';
    }

    function renderDetail() {
        var c = state.detail;
        var $d = $('#kv-detail');
        if (!c) {
            $d.html(U.empty('edit-doc', 'Konsept seçilmedi', 'Soldaki listeden bir konsepte tıklayın; metinleri burada düzenleyip kaydedebilirsiniz.'));
            setDirty(false);
            return;
        }
        var d = c.document || {};
        var html = field('Başlık', 'title', d.title || c.title) + field('Kanca (ilk cümle)', 'hook', d.hook) + field('Özet', 'summary', d.summary, true, 2);
        $.each(d.scenes || [], function (i, s) {
            html += '<div class="kv-scene" data-scene="' + i + '"><div class="kv-scene-head"><span class="kv-scene-no">' + (i + 1) + '</span>' +
                '<input class="tk-input" name="sceneTitle" value="' + U.escape(s.title || '') + '" aria-label="Sahne başlığı" /></div>' +
                field('Ekranda', 'onScreen', s.onScreen, true, 2) + field('Seslendirme', 'narration', s.narration, true, 3) + '</div>';
        });
        $d.html(html);
        setDirty(false);
    }

    function selectDetail(id) {
        var go = function () {
            $('#kv-detail').html(U.skeleton(4));
            TK.api.get('/KonsepttenVideo/Konsept/' + encodeURIComponent(id)).done(function (c) {
                state.detail = c;
                renderDetail();
                renderApproved();
            }).fail(function () { state.detail = null; renderDetail(); });
        };
        if (state.dirty && state.detail && !P.isSameId(state.detail.id, id)) {
            TK.modal.confirm('Kaydedilmemiş değişiklik', 'Açık konseptteki değişiklikler kaydedilmedi. Yine de geçilsin mi?', 'Kaydetmeden geç').done(go);
        } else if (!state.detail || !P.isSameId(state.detail.id, id)) {
            go();
        }
    }

    function saveDetail() {
        var c = state.detail;
        if (!c) { return; }
        var $d = $('#kv-detail');
        var d = $.extend(true, {}, c.document || {});
        d.title = $.trim($d.find('[name=title]').val());
        d.hook = $.trim($d.find('[name=hook]').val());
        d.summary = $.trim($d.find('[name=summary]').val());
        if (!d.title) { TK.toast.warning('Başlık boş olamaz.'); return; }
        $d.find('[data-scene]').each(function () {
            var i = parseInt($(this).attr('data-scene'), 10);
            if (!d.scenes || !d.scenes[i]) { return; }
            d.scenes[i].title = $.trim($(this).find('[name=sceneTitle]').val());
            d.scenes[i].onScreen = $(this).find('[name=onScreen]').val();
            d.scenes[i].narration = $(this).find('[name=narration]').val();
        });
        U.busy($('#kv-detail-save'), TK.api.put('/KonsepttenVideo/Konsept/' + encodeURIComponent(c.id), { title: d.title, document: d }))
            .done(function (res) {
                state.detail = res && res.id ? res : $.extend(c, { title: d.title, document: d });
                setDirty(false);
                TK.toast.success('Konsept kaydedildi.');
                loadApproved();
            });
    }

    /* ---------------- Üretim sırası ---------------- */
    function stepIcon(status, waiting) {
        if (status === 'done') { return U.icon('check'); }
        if (status === 'failed') { return U.icon('x'); }
        if (status === 'skipped' || status === 'cancelled') { return U.icon('stop'); }
        if (status === 'running') { return waiting ? U.icon('monitor') : U.icon('play'); }
        return '';
    }

    function timelineHtml(item) {
        var st = P.itemStatus(item.status);
        var current = item.currentStage !== null && item.currentStage !== undefined ? P.stageName(item.currentStage) : '';
        var html = '<ol class="kv-timeline">';
        $.each(P.GROUPS, function (_, g) {
            var gs = P.groupState(item.stages, g);
            if (gs.status === 'pending' && $.inArray(current, g.stages) >= 0 && st === 'running') { gs.status = 'running'; }
            var waiting = gs.status === 'running' && item.waitingForAgent && gs.agent;
            var name = U.escape(g.name);
            if (g.perScene && gs.scenesTotal) { name += ' <small>(sahne ' + gs.scenesDone + '/' + gs.scenesTotal + ')</small>'; }
            if (gs.reused) { name += ' <small class="tk-muted">· yeniden kullanıldı</small>'; }
            if (gs.status === 'skipped') { name += ' <small class="tk-muted">· atlandı</small>'; }
            if (waiting) { name += ' <small>· ajan bekleniyor</small>'; }
            var time = gs.durationMs ? P.duration(gs.durationMs) : '';
            if (gs.status === 'running') { time = pct(gs.progress) + '%' + (time ? ' · ' + time : ''); }
            if (gs.costUsd > 0) { time += (time ? ' · ' : '') + U.money(gs.costUsd); }
            var cls = waiting ? 'is-waiting' : 'is-' + gs.status;
            html += '<li class="kv-step ' + cls + '" title="' + U.escape(gs.message || g.name) + '"><span class="kv-step-icon">' + stepIcon(gs.status, waiting) + '</span>' +
                '<span class="kv-step-name">' + name + (gs.status === 'running' ? '<span class="kv-step-bar"><span style="width:' + pct(gs.progress) + '%"></span></span>' : '') + '</span>' +
                '<span class="kv-step-time">' + U.escape(time) + '</span></li>';
        });
        var ready = st === 'done';
        html += '<li class="kv-step ' + (ready ? 'is-done' : 'is-pending') + '"><span class="kv-step-icon">' + (ready ? U.icon('check') : '') + '</span>' +
            '<span class="kv-step-name">Hazır</span><span class="kv-step-time">' + (ready && item.finishedAt ? U.escape(U.time(item.finishedAt)) : '') + '</span></li>';
        return html + '</ol>';
    }

    function pct(v) {
        var n = Number(v || 0);
        return Math.max(0, Math.min(100, Math.round(n)));
    }

    function dotsHtml(item) {
        return '<div class="kv-dots" aria-hidden="true">' + $.map(P.GROUPS, function (g) {
            return '<span class="kv-dot is-' + P.groupState(item.stages, g).status + '" title="' + U.escape(g.name) + '"></span>';
        }).join('') + '</div>';
    }

    function elapsed(item) {
        if (!item.startedAt) { return ''; }
        var end = item.finishedAt ? new Date(item.finishedAt) : new Date();
        return P.duration(end - new Date(item.startedAt));
    }

    function badgeFor(st, item) {
        if (st === 'running') { return item.waitingForAgent ? '<span class="tk-badge is-warning">Ajan bekleniyor</span>' : '<span class="tk-badge is-accent">Üretiliyor</span>'; }
        if (st === 'paused') { return '<span class="tk-badge is-warning">Durduruldu</span>'; }
        if (st === 'done') { return '<span class="tk-badge is-success">Hazır</span>'; }
        if (st === 'failed') { return '<span class="tk-badge is-danger">Hata</span>'; }
        if (st === 'cancelled') { return '<span class="tk-badge">İptal</span>'; }
        return '<span class="tk-badge">Sırada</span>';
    }

    function queueItemHtml(item, i, total) {
        var st = P.itemStatus(item.status);
        var active = st === 'running' || st === 'paused';
        var finished = st === 'done' || st === 'failed' || st === 'cancelled';
        var meta = [];
        if (item.version > 1) { meta.push('<span>Sürüm <strong>' + U.escape(item.version) + '</strong></span>'); }
        if (item.startedAt) { meta.push('<span>Başladı <strong>' + U.escape(U.time(item.startedAt)) + '</strong></span>'); }
        if (item.startedAt) { meta.push('<span>Süre <strong>' + U.escape(elapsed(item)) + '</strong></span>'); }
        if (Number(item.costUsd) > 0) { meta.push('<span>Ücret <strong>' + U.escape(U.money(item.costUsd)) + '</strong></span>'); }
        var actions = '';
        if (!finished) {
            actions += '<button type="button" class="tk-mini-btn" data-act="up" title="Yukarı" aria-label="Yukarı"' + (i === 0 ? ' disabled' : '') + '>' + U.icon('up') + '</button>' +
                '<button type="button" class="tk-mini-btn" data-act="down" title="Aşağı" aria-label="Aşağı"' + (i === total - 1 ? ' disabled' : '') + '>' + U.icon('down') + '</button>';
        }
        if (st === 'done' && item.videoId) {
            actions += '<button type="button" class="tk-mini-btn" data-act="play" title="İzle" aria-label="İzle">' + U.icon('play') + '</button>';
        }
        actions += '<button type="button" class="tk-mini-btn is-danger" data-act="remove" title="' + (active ? 'İptal et ve sıradan çıkar' : 'Sıradan çıkar') + '" aria-label="Sıradan çıkar">' + U.icon('x') + '</button>';

        return '<li class="kv-qitem is-' + st + '" data-id="' + U.escape(item.id) + '" data-video="' + U.escape(item.videoId || '') + '">' +
            '<div class="kv-qhead"><span class="kv-qtitle"><span class="kv-qorder">' + (i + 1) + '.</span> ' + U.escape(item.title || 'Adsız') + '</span>' +
            badgeFor(st, item) + '<span class="kv-qactions">' + actions + '</span></div>' +
            (meta.length ? '<div class="kv-qmeta">' + meta.join('') + '</div>' : '') +
            (active || st === 'done' ? '<div class="tk-progress"><span style="width:' + (st === 'done' ? 100 : pct(item.progress)) + '%"></span></div>' : '') +
            (active ? timelineHtml(item) : dotsHtml(item)) +
            (item.message && (active || st === 'failed') ? '<div class="kv-qmsg">' + U.escape(item.message) + '</div>' : '') +
            '</li>';
    }

    function renderQueue() {
        var $q = $('#kv-queue');
        var qs = state.queueState || {};
        var run = P.runState(qs.status);
        var waiting = $.grep(state.queue, function (q) { return P.itemStatus(q.status) === 'waiting'; }).length;
        var finished = $.grep(state.queue, function (q) { var s = P.itemStatus(q.status); return s === 'done' || s === 'failed' || s === 'cancelled'; }).length;
        var hasPausedItem = $.grep(state.queue, function (q) { return P.itemStatus(q.status) === 'paused'; }).length > 0;

        $('#kv-start').prop('hidden', run !== 'idle' || hasPausedItem).prop('disabled', waiting === 0);
        $('#kv-pause').prop('hidden', run !== 'running');
        $('#kv-resume').prop('hidden', !(run === 'paused' || (run === 'idle' && hasPausedItem)));
        $('#kv-clear').prop('hidden', finished === 0);

        var status = run === 'running' ? 'Üretiliyor' : run === 'paused' ? 'Durduruldu — kaldığı yerden devam edebilirsiniz' : waiting ? waiting + ' iş bekliyor' : state.queue.length ? 'Tamamlandı' : 'Boş';
        if (run === 'running' && waiting) { status += ' · ' + waiting + ' iş bekliyor'; }
        $('#kv-queue-status').text(qs.message ? status + ' · ' + qs.message : status);

        $('#kv-agent-banner').prop('hidden', qs.agentOnline !== false);
        var agentWait = $.grep(state.queue, function (q) { return q.waitingForAgent; }).length > 0;
        $('#kv-agent-banner-text').text(agentWait
            ? ' Üretim, masaüstü programını bekliyor. Bilgisayarda Taha Konsept programını açın; iş kaldığı yerden devam eder.'
            : ' Render, yerel ses ve yerel görsel aşamaları masaüstü programı bağlanınca çalışır. Sunucu aşamaları beklemeden ilerler.');
        $('#kv-budget-banner').prop('hidden', !qs.budgetBlocked);

        if (!state.queue.length) {
            $q.html(U.empty('queue', 'Sıra boş', 'Onaylanan konseptleri işaretleyip "Sıraya ekle" deyin, sonra "Sırayla üret".'));
            return;
        }
        var html = '<ul class="kv-queue-list">';
        $.each(state.queue, function (i, item) { html += queueItemHtml(item, i, state.queue.length); });
        $q.html(html + '</ul>');
    }

    function onQueue(data) {
        state.queueState = data && !$.isArray(data) ? data : { items: data };
        state.queue = U.items(data).slice().sort(function (a, b) { return Number(a.order || 0) - Number(b.order || 0); });
        var json = JSON.stringify(state.queueState);
        var anyActive = $.grep(state.queue, function (q) { return P.itemStatus(q.status) === 'running'; }).length > 0;
        if (json !== state.lastQueueJson || anyActive) {
            state.lastQueueJson = json;
            renderQueue();
            renderApproved();
        }
        var done = $.grep(state.queue, function (q) { return P.itemStatus(q.status) === 'done'; }).length;
        if (state.doneCount !== null && done > state.doneCount) { loadProduced(); TK.toast.success('Bir video hazırlandı.'); }
        state.doneCount = done;
    }

    function queueAction(url, okMessage) {
        return TK.api.post(url).done(function () { if (okMessage) { TK.toast.info(okMessage); } if (state.poller) { state.poller.now(); } });
    }

    function onQueueClick() {
        var $item = $(this).closest('[data-id]');
        var id = $item.attr('data-id');
        var act = $(this).attr('data-act');
        var item = $.grep(state.queue, function (q) { return P.isSameId(q.id, id); })[0] || {};
        var refresh = function () { if (state.poller) { state.poller.now(); } };
        if (act === 'play') { play($item.attr('data-video')); return; }
        if (act === 'remove') {
            var st = P.itemStatus(item.status);
            var del = function () { U.busy($(this), TK.api.del('/KonsepttenVideo/SiradanCikar/' + encodeURIComponent(id))).done(refresh); }.bind(this);
            if (st === 'running' || st === 'paused') {
                TK.modal.confirm('İşi iptal et', '"' + (item.title || 'Video') + '" üretimi iptal edilip sıradan çıkarılacak. Emin misiniz?', 'İptal et').done(del);
            } else { del(); }
            return;
        }
        U.busy($(this), TK.api.post('/KonsepttenVideo/Tasi/' + encodeURIComponent(id), { direction: act === 'up' ? -1 : 1 })).done(refresh);
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
            var sel = state.playing && P.isSameId(state.playing.id, v.id);
            var sub = [P.clock(v.durationSec), U.dateTime(v.createdAt), U.money(v.costUsd)];
            html += '<li class="tk-list-item' + (sel ? ' is-selected' : '') + '" data-id="' + U.escape(v.id) + '">' +
                '<span class="tk-thumb kv-video-thumb' + (P.isVertical(v.format) ? ' is-vertical' : '') + '">' + U.icon('play', 'tk-ic-sm') + '</span>' +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(v.title || 'Adsız video') + (v.version > 1 ? ' <span class="tk-badge">s' + U.escape(v.version) + '</span>' : '') + '</div>' +
                '<div class="tk-list-sub">' + U.escape(sub.join(' · ')) + '</div></div></li>';
        });
        $l.html(html + '</ul>');
    }

    function play(id) {
        if (!id) { return; }
        var v = $.grep(state.produced, function (x) { return P.isSameId(x.id, id); })[0] || { id: id, title: '' };
        state.playing = v;
        $('#kv-produced').addClass('has-player');
        $('#kv-player-title').text(v.title || 'Video');
        $('#kv-player-open').attr('href', '/Uretilenler?video=' + encodeURIComponent(id));
        $('#kv-player-meta').empty();
        var $box = $('#kv-player-box').empty().toggleClass('is-vertical', P.isVertical(v.format)).html('<div class="tk-player-empty">' + U.icon('film', 'tk-ic-lg') + '<div>Yükleniyor…</div></div>');
        renderProduced();
        TK.api.get('/KonsepttenVideo/Video/' + encodeURIComponent(id), null, { silent: true }).done(function (d) {
            if (!state.playing || !P.isSameId(state.playing.id, id) || !d) { return; }
            $('#kv-player-title').text(d.title || v.title || 'Video');
            $box.empty().toggleClass('is-vertical', P.isVertical(d.format));
            if (d.playbackUrl) {
                $('<video controls playsinline preload="metadata"></video>').attr('src', d.playbackUrl).attr('poster', d.thumbnailUrl || null).appendTo($box)
                    .on('error', function () { $box.html('<div class="tk-player-empty">' + U.icon('alert', 'tk-ic-lg') + '<div>Video açılamadı. Masaüstü programı çalışıyor mu?</div></div>'); });
            } else {
                $box.html('<div class="tk-player-empty">' + U.icon('monitor', 'tk-ic-lg') + '<div>' + U.escape(d.playbackMessage || 'Video masaüstü programındadır; bilgisayar açık ve program çalışıyorken oynatılır.') + '</div></div>');
            }
            $('#kv-player-meta').html([
                '<span class="tk-badge">' + U.escape(P.clock(d.durationSec)) + '</span>',
                '<span class="tk-badge">' + (P.isVertical(d.format) ? 'Dikey 9:16' : 'Yatay 16:9') + '</span>',
                d.version > 1 ? '<span class="tk-badge">Sürüm ' + U.escape(d.version) + '</span>' : '',
                '<span class="tk-badge is-accent">' + U.escape(U.money(d.costUsd)) + '</span>'
            ].join(' '));
        }).fail(function () {
            $box.html('<div class="tk-player-empty">' + U.icon('alert', 'tk-ic-lg') + '<div>Video bilgisi alınamadı.</div></div>');
        });
    }

    function closePlayer() {
        state.playing = null;
        $('#kv-player-box').find('video').each(function () { this.pause(); });
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

    /* ---------------- Başlatma ---------------- */
    function bind() {
        $('[data-model-kind]').on('click', function () { openModel($(this).attr('data-model-kind')); });
        $('#kv-subtitles').on('change', function () { toggleSetting($(this), 'burnSubtitles', 'Altyazı'); });
        $('#kv-progressbar').on('change', function () { toggleSetting($(this), 'showProgressBar', 'Üst ilerleme çubuğu'); });

        $('#kv-approved').on('click', '.tk-list-item', function (e) {
            var id = $(this).attr('data-id');
            if ($(e.target).is('[data-check]')) { state.checked[id] = $(e.target).is(':checked'); renderApproved(); return; }
            selectDetail(id);
        });
        $('#kv-approved-search').on('input', function () { state.filter = $.trim($(this).val()); renderApproved(); });
        $('#kv-check-all').on('change', function () {
            var on = $(this).is(':checked');
            $.each(visibleApproved(), function (_, c) { state.checked[c.id] = on; });
            renderApproved();
        });
        $('#kv-add-queue').on('click', addToQueue);
        $('#kv-detail').on('input', 'input, textarea', function () { if (!state.dirty) { setDirty(true); } });
        $('#kv-detail-save').on('click', saveDetail);

        $('#kv-queue').on('click', '[data-act]', onQueueClick);
        $('#kv-start').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/Baslat', 'Üretim başladı.')); });
        $('#kv-pause').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/Durdur', 'Üretim durduruluyor; çalışan aşama güvenle duraklatılır.')); });
        $('#kv-resume').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/Devam', 'Kaldığı yerden devam ediliyor.')); });
        $('#kv-clear').on('click', function () { U.busy($(this), queueAction('/KonsepttenVideo/BitenleriTemizle', 'Biten işler sıradan temizlendi.')); });

        $('#kv-produced-list').on('click', '.tk-list-item', function () { play($(this).attr('data-id')); });
        $('#kv-produced-refresh').on('click', function () { U.busy($(this), loadProduced()); });
        $('#kv-player-close').on('click', closePlayer);
        $(window).on('beforeunload', function () { if (state.dirty) { return 'Kaydedilmemiş değişiklik var.'; } return undefined; });
    }

    function init() {
        if (!$('#tk-video').length) { return; }
        U = TK.util;
        P = TK.production;
        bind();
        renderDetail();
        $('#kv-approved, #kv-queue, #kv-produced-list').html(U.skeleton(3));

        TK.api.get('/KonsepttenVideo/Modeller', null, { silent: true }).done(function (d) { state.models = U.items(d); renderSettings(); });
        TK.api.get('/KonsepttenVideo/Ayarlar', null, { silent: true }).done(function (d) { state.settings = d || {}; renderSettings(); });
        TK.api.get('/KonsepttenVideo/Ajanlar', null, { silent: true }).done(function (d) {
            var max = 0;
            $.each(U.items(d), function (_, a) { max = Math.max(max, Number(a.vramMb || (a.gpu && a.gpu.vramMb) || 0)); });
            state.agentVramMb = max || null;
        });
        loadApproved();
        loadProduced();

        state.poller = TK.poll('/KonsepttenVideo/Sira', 2000, onQueue, {
            onError: function () {
                if (!state.queue.length) { $('#kv-queue').html(U.empty('alert', 'Sıra alınamadı', 'API bağlantısı bekleniyor; otomatik yeniden denenecek.')); }
            }
        });
    }

    TK.konsepttenVideo = { init: init };
    $(init);
})(window, window.jQuery);
